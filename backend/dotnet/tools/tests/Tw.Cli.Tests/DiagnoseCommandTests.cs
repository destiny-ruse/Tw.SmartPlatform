using AwesomeAssertions;
using System.Diagnostics;
using System.Reflection;
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
    /// --repository 缺少值或下一 token 为其他 option 的命令组合
    /// </summary>
    public static IEnumerable<object[]> MissingRepositoryOptionValueCases()
    {
        yield return new object[] { new[] { "diagnose", "--repository" } };
        yield return new object[] { new[] { "diagnose", "--repository", "--help" } };
        yield return new object[] { new[] { "audit", "dependencies", "--repository" } };
        yield return new object[] { new[] { "audit", "dependencies", "--repository", "--help" } };
    }

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
    /// Windows 风格 ProjectReference 在 Unix 与 Windows 宿主上均应解析到现存目标
    /// </summary>
    [Fact]
    public void Diagnose_ResolvesWindowsProjectReferenceOnAnyHost()
    {
        using var repository = TestRepository.Create();
        File.WriteAllText(
            repository.RuntimeProjectPath,
            "<Project><ItemGroup><ProjectReference Include=\"..\\..\\..\\tests\\Sample\\Tw.Sample.Tests\\Tw.Sample.Tests.csproj\" /></ItemGroup></Project>");
        var service = new RepositoryDiagnosisService(
            new ProjectDependencyScanner(),
            new RecordingLockedRestoreRunner(0));

        var report = service.Diagnose(repository.RootPath);

        report.UnresolvedProjectReferences.Should().BeEmpty();
    }

    /// <summary>
    /// 分号分隔且混用 Windows/Unix 分隔符的 ProjectReference 必须逐项解析
    /// </summary>
    [Fact]
    public void Diagnose_ResolvesEachSemicolonSeparatedProjectReference()
    {
        using var repository = TestRepository.Create();
        var projectDirectory = Path.GetDirectoryName(repository.RuntimeProjectPath)!;
        var firstReference = Path.Combine(projectDirectory, "References", "A", "A.csproj");
        var secondReference = Path.Combine(projectDirectory, "References", "B", "B.csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(firstReference)!);
        Directory.CreateDirectory(Path.GetDirectoryName(secondReference)!);
        File.WriteAllText(firstReference, "<Project />");
        File.WriteAllText(secondReference, "<Project />");
        File.WriteAllText(
            repository.RuntimeProjectPath,
            "<Project><ItemGroup><ProjectReference Include=\"References/A/A.csproj;References\\B\\B.csproj\" /></ItemGroup></Project>");
        var service = new RepositoryDiagnosisService(
            new ProjectDependencyScanner(),
            new RecordingLockedRestoreRunner(0));

        var report = service.Diagnose(repository.RootPath);

        report.UnresolvedProjectReferences.Should().BeEmpty();
    }

    /// <summary>
    /// 分号引用中只有缺失项必须产生诊断，现存项不得被合并误报
    /// </summary>
    [Fact]
    public void Diagnose_ReportsOnlyMissingSemicolonSeparatedProjectReference()
    {
        using var repository = TestRepository.Create();
        var projectDirectory = Path.GetDirectoryName(repository.RuntimeProjectPath)!;
        var existingReference = Path.Combine(projectDirectory, "References", "A", "A.csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(existingReference)!);
        File.WriteAllText(existingReference, "<Project />");
        File.WriteAllText(
            repository.RuntimeProjectPath,
            "<Project><ItemGroup><ProjectReference Include=\"References/A/A.csproj;References\\B\\B.csproj\" /></ItemGroup></Project>");
        var service = new RepositoryDiagnosisService(
            new ProjectDependencyScanner(),
            new RecordingLockedRestoreRunner(0));

        var report = service.Diagnose(repository.RootPath);

        report.UnresolvedProjectReferences.Should().ContainSingle()
            .Which.Should().EndWith(" -> References/B/B.csproj");
    }

    /// <summary>
    /// 分号引用中的动态表达式必须逐项 fail closed，不得污染静态现存项
    /// </summary>
    [Fact]
    public void Diagnose_ReportsDynamicSemicolonSeparatedProjectReferencePerItem()
    {
        using var repository = TestRepository.Create();
        var projectDirectory = Path.GetDirectoryName(repository.RuntimeProjectPath)!;
        var existingReference = Path.Combine(projectDirectory, "References", "A", "A.csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(existingReference)!);
        File.WriteAllText(existingReference, "<Project />");
        File.WriteAllText(
            repository.RuntimeProjectPath,
            "<Project><ItemGroup><ProjectReference Include=\"References/A/A.csproj;$(GeneratedRoot)/B.csproj\" /></ItemGroup></Project>");
        var service = new RepositoryDiagnosisService(
            new ProjectDependencyScanner(),
            new RecordingLockedRestoreRunner(0));

        var report = service.Diagnose(repository.RootPath);

        report.UnresolvedProjectReferences.Should().ContainSingle()
            .Which.Should().EndWith(
                " has unresolved ProjectReference expression $(GeneratedRoot)/B.csproj");
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
        ((int?)report.LockedRestoreExitCode).Should().BeNull();
        restoreRunner.SolutionPath.Should().BeNull();
    }

    /// <summary>
    /// 缺失解决方案时不得运行 restore，报告必须显式标记未运行
    /// </summary>
    [Fact]
    public void Diagnose_ReportsLockedRestoreNotRunWhenSolutionIsMissing()
    {
        using var repository = TestRepository.Create();
        File.Delete(repository.SolutionPath);
        var restoreRunner = new RecordingLockedRestoreRunner(0);
        var service = new RepositoryDiagnosisService(new ProjectDependencyScanner(), restoreRunner);

        var report = service.Diagnose(repository.RootPath);

        ((int?)report.LockedRestoreExitCode).Should().BeNull();
        report.ExitCode.Should().Be(1);
        restoreRunner.SolutionPath.Should().BeNull();
    }

    /// <summary>
    /// 未运行 restore 时 CLI 输出必须显示 not run 而不是成功退出码零
    /// </summary>
    [Fact]
    public void CliApplication_PrintsLockedRestoreNotRun()
    {
        var missingRepository = Path.Combine(Path.GetTempPath(), $"tw-cli-missing-{Guid.NewGuid():N}");
        var application = new CliApplication(
            new ProjectDependencyScanner(),
            new RepositoryDiagnosisService(new ProjectDependencyScanner(), new RecordingLockedRestoreRunner(0)));
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();

        var exitCode = application.Run(
            ["diagnose", "--repository", missingRepository],
            standardOutput,
            standardError);

        exitCode.Should().Be(1);
        standardOutput.ToString().Should().Contain("locked restore exit code: not run");
        standardOutput.ToString().Should().NotContain("locked restore exit code: 0");
    }

    /// <summary>
    /// 显式 --repository 缺少值时必须返回稳定 usage 退出码
    /// </summary>
    /// <param name="args">缺少 repository 值的完整命令参数</param>
    [Theory]
    [MemberData(nameof(MissingRepositoryOptionValueCases))]
    public void CliApplication_RejectsMissingRepositoryOptionValue(string[] args)
    {
        var application = new CliApplication(
            new ProjectDependencyScanner(),
            new RepositoryDiagnosisService(new ProjectDependencyScanner(), new RecordingLockedRestoreRunner(0)));
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();

        var exitCode = application.Run(args, standardOutput, standardError);

        exitCode.Should().Be(2);
        standardError.ToString().Should().Contain("--repository requires a path value");
    }

    /// <summary>
    /// 未提供 --repository 时命令应使用当前目录而不是返回缺参 usage 错误
    /// </summary>
    [Fact]
    public void CliApplication_AllowsRepositoryOptionToBeAbsent()
    {
        var application = new CliApplication(
            new ProjectDependencyScanner(),
            new RepositoryDiagnosisService(new ProjectDependencyScanner(), new RecordingLockedRestoreRunner(0)));
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();

        var exitCode = application.Run(["audit", "dependencies"], standardOutput, standardError);

        exitCode.Should().NotBe(2);
        standardError.ToString().Should().NotContain("--repository requires a path value");
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
    /// CLI 必须分别传播 locked restore 的标准输出和标准错误
    /// </summary>
    [Fact]
    public void CliApplication_PropagatesLockedRestoreStandardStreams()
    {
        using var repository = TestRepository.Create();
        var runner = new RecordingLockedRestoreRunner(
            17,
            standardOutput: "restore stdout marker\n",
            standardError: "restore stderr marker\n");
        var application = new CliApplication(
            new ProjectDependencyScanner(),
            new RepositoryDiagnosisService(new ProjectDependencyScanner(), runner));
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();

        var exitCode = application.Run(
            ["diagnose", "--repository", repository.RootPath],
            standardOutput,
            standardError);

        exitCode.Should().Be(17);
        standardOutput.ToString().Should().Contain("restore stdout marker");
        standardError.ToString().Should().Contain("restore stderr marker");
    }

    /// <summary>
    /// locked restore 超时必须返回稳定退出码并终止后代进程
    /// </summary>
    [Fact]
    public void DotnetLockedRestoreRunner_TimesOutAndKillsEntireProcessTree()
    {
        using var directory = new TemporaryProcessDirectory();
        var markerPath = Path.Combine(directory.RootPath, "child-survived.txt");
        var command = OperatingSystem.IsWindows()
            ? $"start \"\" /b cmd.exe /d /s /c \"ping 127.0.0.1 -n 3 >nul & echo survived>{QuoteForWindowsCommand(markerPath)}\" & ping 127.0.0.1 -n 30 >nul"
            : $"(sleep 2; echo survived > {QuoteForPosixShell(markerPath)}) & sleep 30";
        var runner = CreateProcessRunner(
            TimeSpan.FromMilliseconds(250),
            (_, _) => CreateShellStartInfo(command, directory.RootPath));
        var stopwatch = Stopwatch.StartNew();

        var result = runner.Run("ignored.slnx", directory.RootPath);

        stopwatch.Stop();
        result.ExitCode.Should().Be(124);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
        Thread.Sleep(TimeSpan.FromSeconds(3));
        File.Exists(markerPath).Should().BeFalse("the timed-out shell's child must be terminated with the process tree");
    }

    /// <summary>
    /// 成功子进程退出后必须排空异步标准输出和标准错误
    /// </summary>
    [Fact]
    public void DotnetLockedRestoreRunner_DrainsStandardStreamsAfterSuccessfulExit()
    {
        using var directory = new TemporaryProcessDirectory();
        var command = OperatingSystem.IsWindows()
            ? "(for /L %i in (1,1,200) do @echo stdout-%i) & (for /L %i in (1,1,200) do @echo stderr-%i 1>&2)"
            : "i=1; while [ $i -le 200 ]; do echo stdout-$i; echo stderr-$i >&2; i=$((i+1)); done";
        var runner = CreateProcessRunner(
            TimeSpan.FromSeconds(10),
            (_, _) => CreateShellStartInfo(command, directory.RootPath));

        var result = runner.Run("ignored.slnx", directory.RootPath);

        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().Contain("stdout-200");
        result.StandardError.Should().Contain("stderr-200");
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
        public RecordingLockedRestoreRunner(
            int exitCode,
            string standardOutput = "",
            string standardError = "")
        {
            ExitCode = exitCode;
            StandardOutput = standardOutput;
            StandardError = standardError;
        }

        /// <summary>
        /// restore 调用应返回的进程退出码
        /// </summary>
        private int ExitCode { get; }

        /// <summary>
        /// restore 调用应返回的标准输出
        /// </summary>
        private string StandardOutput { get; }

        /// <summary>
        /// restore 调用应返回的标准错误
        /// </summary>
        private string StandardError { get; }

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
            return new LockedRestoreResult(ExitCode, StandardOutput, StandardError);
        }
    }

    /// <summary>
    /// 通过非公开可测试构造函数创建具有可控超时和进程启动信息的 runner
    /// </summary>
    /// <param name="timeout">子进程最大运行时间</param>
    /// <param name="startInfoFactory">测试进程启动信息工厂</param>
    /// <returns>可执行测试进程的 locked restore runner</returns>
    private static ILockedRestoreRunner CreateProcessRunner(
        TimeSpan timeout,
        Func<string, string, ProcessStartInfo> startInfoFactory)
    {
        var constructor = typeof(DotnetLockedRestoreRunner).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [typeof(TimeSpan), typeof(Func<string, string, ProcessStartInfo>)],
            modifiers: null);
        constructor.Should().NotBeNull("the process timeout and process start boundary must be injectable in tests");
        return (ILockedRestoreRunner)constructor!.Invoke([timeout, startInfoFactory]);
    }

    /// <summary>
    /// 创建当前宿主可执行的 shell 子进程
    /// </summary>
    /// <param name="command">shell 命令文本</param>
    /// <param name="workingDirectory">子进程工作目录</param>
    /// <returns>不经 shell execute 的进程启动信息</returns>
    private static ProcessStartInfo CreateShellStartInfo(string command, string workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh",
            WorkingDirectory = workingDirectory
        };
        if (OperatingSystem.IsWindows())
        {
            startInfo.ArgumentList.Add("/d");
            startInfo.ArgumentList.Add("/s");
            startInfo.ArgumentList.Add("/c");
        }
        else
        {
            startInfo.ArgumentList.Add("-c");
        }

        startInfo.ArgumentList.Add(command);
        return startInfo;
    }

    /// <summary>
    /// 为 Windows cmd 重定向目标添加双引号
    /// </summary>
    /// <param name="path">不包含双引号的测试路径</param>
    /// <returns>cmd 可消费的带引号路径</returns>
    private static string QuoteForWindowsCommand(string path)
    {
        return $"\"{path}\"";
    }

    /// <summary>
    /// 为 POSIX shell 路径添加单引号并转义内部单引号
    /// </summary>
    /// <param name="path">测试进程要写入的路径</param>
    /// <returns>POSIX shell 可消费的带引号路径</returns>
    private static string QuoteForPosixShell(string path)
    {
        return $"'{path.Replace("'", "'\\''", StringComparison.Ordinal)}'";
    }

    /// <summary>
    /// 提供可回收的子进程测试目录
    /// </summary>
    private sealed class TemporaryProcessDirectory : IDisposable
    {
        /// <summary>
        /// 初始化并创建唯一临时目录
        /// </summary>
        internal TemporaryProcessDirectory()
        {
            RootPath = Path.Combine(Path.GetTempPath(), $"tw-restore-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(RootPath);
        }

        /// <summary>
        /// 子进程工作目录
        /// </summary>
        internal string RootPath { get; }

        /// <summary>
        /// 删除测试创建的临时目录
        /// </summary>
        public void Dispose()
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
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
