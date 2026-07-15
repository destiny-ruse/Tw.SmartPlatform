using AwesomeAssertions;
using Tw.Cli.Commands;
using Tw.Cli.Governance;
using Xunit;

namespace Tw.Cli.Tests;

/// <summary>
/// 验证仓库诊断服务和 CLI 退出码契约
/// </summary>
public sealed class DiagnoseCommandTests
{
    /// <summary>
    /// 验证诊断服务报告物理项目数量、解决方案一致性和真实仓库事实
    /// </summary>
    [Fact]
    public void Diagnose_ReportsRepositoryFactsAndRunsLockedRestore()
    {
        using var repository = TestRepository.Create();
        var restoreRunner = new RecordingLockedRestoreRunner(0);
        var service = new RepositoryDiagnosisService(new ProjectDependencyScanner(), restoreRunner);

        var report = service.Diagnose(repository.RootPath);

        report.SourceProjectCount.Should().Be(1);
        report.TestProjectCount.Should().Be(1);
        report.SolutionParity.Should().BeTrue();
        report.UnresolvedProjectReferences.Should().BeEmpty();
        report.RetiredReferences.Should().BeEmpty();
        report.MissingLockFiles.Should().BeEmpty();
        report.RetiredLockDependencies.Should().BeEmpty();
        report.LockedRestoreExitCode.Should().Be(0);
        report.ExitCode.Should().Be(0);
        restoreRunner.SolutionPath.Should().Be(repository.SolutionPath);
    }

    /// <summary>
    /// 验证诊断服务报告解决方案漂移、坏引用、淘汰引用和锁文件问题
    /// </summary>
    [Fact]
    public void Diagnose_ReportsTopologyReferenceAndLockViolations()
    {
        using var repository = TestRepository.Create();
        File.WriteAllText(
            repository.RuntimeProjectPath,
            "<Project><ItemGroup>"
            + "<PackageReference Include=\"Tw.Http.Client\" />"
            + "<ProjectReference Include=\"..\\Missing\\Missing.csproj\" />"
            + "</ItemGroup></Project>");
        File.Delete(Path.Combine(Path.GetDirectoryName(repository.RuntimeProjectPath)!, "packages.lock.json"));
        File.WriteAllText(
            Path.Combine(Path.GetDirectoryName(repository.TestProjectPath)!, "packages.lock.json"),
            "{\"version\":2,\"dependencies\":{\"net10.0\":{\"Tw.Http.Client\":{\"type\":\"Direct\",\"resolved\":\"1.0.0\"}}}}");
        File.WriteAllText(repository.SolutionPath, "<Solution />");
        var service = new RepositoryDiagnosisService(
            new ProjectDependencyScanner(),
            new RecordingLockedRestoreRunner(0));

        var report = service.Diagnose(repository.RootPath);

        report.SolutionParity.Should().BeFalse();
        report.UnresolvedProjectReferences.Should().ContainSingle();
        report.RetiredReferences.Should().ContainSingle(error => error.Code == "TWGOV002");
        report.MissingLockFiles.Should().ContainSingle(path => path.EndsWith("Tw.Sample.csproj", StringComparison.Ordinal));
        report.RetiredLockDependencies.Should().ContainSingle(item => item.PackageId == "Tw.Http.Client");
        report.ExitCode.Should().Be(1);
    }

    /// <summary>
    /// 验证 locked restore 的非零退出码由诊断命令原样传播
    /// </summary>
    [Fact]
    public void Diagnose_PropagatesLockedRestoreFailureExitCode()
    {
        using var repository = TestRepository.Create();
        var service = new RepositoryDiagnosisService(
            new ProjectDependencyScanner(),
            new RecordingLockedRestoreRunner(23));

        var report = service.Diagnose(repository.RootPath);

        report.LockedRestoreExitCode.Should().Be(23);
        report.ExitCode.Should().Be(23);
    }

    /// <summary>
    /// 验证缺失仓库不会触发 restore 且返回非零退出码
    /// </summary>
    [Fact]
    public void Diagnose_FailsWhenRepositoryDoesNotExist()
    {
        var restoreRunner = new RecordingLockedRestoreRunner(0);
        var service = new RepositoryDiagnosisService(new ProjectDependencyScanner(), restoreRunner);
        var missingRepository = Path.Combine(Path.GetTempPath(), $"tw-cli-missing-{Guid.NewGuid():N}");

        var report = service.Diagnose(missingRepository);

        report.ExitCode.Should().Be(1);
        report.RepositoryExists.Should().BeFalse();
        restoreRunner.SolutionPath.Should().BeNull();
    }

    /// <summary>
    /// 验证 CLI 对诊断 restore 失败和依赖审计失败返回非零退出码
    /// </summary>
    [Fact]
    public void CliApplication_ReturnsCommandExitCodes()
    {
        using var repository = TestRepository.Create();
        var diagnosisService = new RepositoryDiagnosisService(
            new ProjectDependencyScanner(),
            new RecordingLockedRestoreRunner(19));
        var application = new CliApplication(new ProjectDependencyScanner(), diagnosisService);
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();

        var diagnoseExitCode = application.Run(
            ["diagnose", "--repository", repository.RootPath],
            standardOutput,
            standardError);
        File.WriteAllText(
            repository.RuntimeProjectPath,
            "<Project><ItemGroup><PackageReference Include=\"Tw.Http.Client\" /></ItemGroup></Project>");
        var auditExitCode = application.Run(
            ["audit", "dependencies", "--repository", repository.RootPath],
            standardOutput,
            standardError);
        var unknownExitCode = application.Run(["unknown"], standardOutput, standardError);

        diagnoseExitCode.Should().Be(19);
        auditExitCode.Should().Be(1);
        unknownExitCode.Should().Be(2);
    }

    /// <summary>
    /// 验证未执行仓库检查的命令不会宣称事实已经检查
    /// </summary>
    [Fact]
    public void CliApplication_DoesNotClaimUninspectedRepositoryFacts()
    {
        var application = new CliApplication(
            new ProjectDependencyScanner(),
            new RepositoryDiagnosisService(new ProjectDependencyScanner(), new RecordingLockedRestoreRunner(0)));
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();

        application.Run(["validate", "contracts"], standardOutput, standardError);

        standardOutput.ToString().Should().NotContainAny("available", "not detected", "checked");
    }

    /// <summary>
    /// 记录诊断服务请求的解决方案路径并返回可控进程结果
    /// </summary>
    private sealed class RecordingLockedRestoreRunner : ILockedRestoreRunner
    {
        /// <summary>
        /// 构造具有固定退出码的进程替身
        /// </summary>
        /// <param name="exitCode">运行 restore 时返回的退出码</param>
        public RecordingLockedRestoreRunner(int exitCode)
        {
            ExitCode = exitCode;
        }

        /// <summary>
        /// restore 调用应返回的进程退出码
        /// </summary>
        private int ExitCode { get; }

        /// <summary>
        /// 最近一次 restore 请求的解决方案路径
        /// </summary>
        public string? SolutionPath { get; private set; }

        /// <summary>
        /// 记录 restore 请求并返回固定结果
        /// </summary>
        /// <param name="solutionPath">需要以 locked mode 还原的解决方案路径</param>
        /// <param name="workingDirectory">子进程工作目录</param>
        /// <returns>包含固定退出码的进程结果</returns>
        public LockedRestoreResult Run(string solutionPath, string workingDirectory)
        {
            SolutionPath = solutionPath;
            return new LockedRestoreResult(ExitCode, string.Empty, string.Empty);
        }
    }

    /// <summary>
    /// 提供可回收的最小 .NET 仓库诊断夹具
    /// </summary>
    private sealed class TestRepository : IDisposable
    {
        /// <summary>
        /// 初始化最小仓库夹具路径
        /// </summary>
        /// <param name="rootPath">临时仓库根目录</param>
        private TestRepository(string rootPath)
        {
            RootPath = rootPath;
            SolutionPath = Path.Combine(rootPath, "backend", "dotnet", "Tw.SmartPlatform.slnx");
            RuntimeProjectPath = Path.Combine(
                rootPath,
                "backend",
                "dotnet",
                "BuildingBlocks",
                "src",
                "Sample",
                "Tw.Sample",
                "Tw.Sample.csproj");
            TestProjectPath = Path.Combine(
                rootPath,
                "backend",
                "dotnet",
                "BuildingBlocks",
                "tests",
                "Sample",
                "Tw.Sample.Tests",
                "Tw.Sample.Tests.csproj");
        }

        /// <summary>
        /// 临时仓库根目录
        /// </summary>
        public string RootPath { get; }

        /// <summary>
        /// 诊断服务使用的解决方案路径
        /// </summary>
        public string SolutionPath { get; }

        /// <summary>
        /// 夹具中的运行时项目路径
        /// </summary>
        public string RuntimeProjectPath { get; }

        /// <summary>
        /// 夹具中的测试项目路径
        /// </summary>
        public string TestProjectPath { get; }

        /// <summary>
        /// 创建包含拓扑清单、解决方案、项目和锁文件的最小仓库
        /// </summary>
        /// <returns>需要在用例结束时释放的仓库夹具</returns>
        public static TestRepository Create()
        {
            var repository = new TestRepository(Path.Combine(Path.GetTempPath(), $"tw-cli-tests-{Guid.NewGuid():N}"));
            Directory.CreateDirectory(Path.GetDirectoryName(repository.RuntimeProjectPath)!);
            Directory.CreateDirectory(Path.GetDirectoryName(repository.TestProjectPath)!);
            File.WriteAllText(repository.RuntimeProjectPath, "<Project />");
            File.WriteAllText(repository.TestProjectPath, "<Project />");
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(repository.RuntimeProjectPath)!, "packages.lock.json"), EmptyLockFile());
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(repository.TestProjectPath)!, "packages.lock.json"), EmptyLockFile());
            File.WriteAllText(
                repository.SolutionPath,
                "<Solution>"
                + "<Project Path=\"BuildingBlocks/src/Sample/Tw.Sample/Tw.Sample.csproj\" />"
                + "<Project Path=\"BuildingBlocks/tests/Sample/Tw.Sample.Tests/Tw.Sample.Tests.csproj\" />"
                + "</Solution>");
            var topologyPath = Path.Combine(
                repository.RootPath,
                "backend",
                "dotnet",
                "BuildingBlocks",
                "building-blocks-topology.json");
            File.WriteAllText(
                topologyPath,
                "{"
                + "\"schemaVersion\":1,"
                + "\"runtimeProjects\":[{\"path\":\"Sample/Tw.Sample/Tw.Sample.csproj\",\"rootNamespace\":\"Tw.Sample\"}],"
                + "\"testProjects\":[{\"path\":\"Sample/Tw.Sample.Tests/Tw.Sample.Tests.csproj\"}],"
                + "\"toolProjects\":[],\"independentContractPackages\":[],"
                + "\"retiredPackages\":[{\"packageId\":\"Tw.Http.Client\",\"replacementPackageId\":\"Tw.Sample\","
                + "\"runtimeProjectPath\":null,\"testProjectPath\":null,\"replacementTestProjectPath\":null,"
                + "\"retiredNamespaces\":[\"Tw.Http.Client\"]}]}" );
            return repository;
        }

        /// <summary>
        /// 删除测试创建的临时仓库
        /// </summary>
        public void Dispose()
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }

        /// <summary>
        /// 返回不包含依赖的 NuGet 锁文件文本
        /// </summary>
        /// <returns>有效的最小 packages.lock.json</returns>
        private static string EmptyLockFile()
        {
            return "{\"version\":2,\"dependencies\":{\"net10.0\":{}}}";
        }
    }
}
