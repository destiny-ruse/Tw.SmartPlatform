namespace Tw.Cli.Commands;

using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using Tw.Cli.Governance;

/// <summary>
/// 提供 CLI 中诊断命令的入口描述
/// </summary>
public static class DiagnoseCommand
{
    /// <summary>
    /// CLI 命令在帮助信息中显示的说明文本
    /// </summary>
    public static string Description => "Reports package topology, solution parity, project references, and lock file status.";
}

/// <summary>
/// 收集仓库拓扑、引用和锁文件事实，并用 locked restore 判定依赖图陈旧性
/// </summary>
public sealed class RepositoryDiagnosisService
{
    /// <summary>
    /// 依赖审计服务
    /// </summary>
    private readonly ProjectDependencyScanner _dependencyScanner;

    /// <summary>
    /// locked restore 子进程边界
    /// </summary>
    private readonly ILockedRestoreRunner _lockedRestoreRunner;

    /// <summary>
    /// 初始化可注入的仓库诊断服务
    /// </summary>
    /// <param name="dependencyScanner">扫描退役依赖的服务</param>
    /// <param name="lockedRestoreRunner">执行权威 locked restore 的进程边界</param>
    public RepositoryDiagnosisService(
        ProjectDependencyScanner dependencyScanner,
        ILockedRestoreRunner lockedRestoreRunner)
    {
        _dependencyScanner = dependencyScanner ?? throw new ArgumentNullException(nameof(dependencyScanner));
        _lockedRestoreRunner = lockedRestoreRunner ?? throw new ArgumentNullException(nameof(lockedRestoreRunner));
    }

    /// <summary>
    /// 检查指定仓库并返回可供 CLI 和测试消费的结构化事实
    /// </summary>
    /// <param name="repositoryPath">包含 backend/dotnet 的仓库根目录</param>
    /// <returns>项目库存、引用、锁文件和 locked restore 结果</returns>
    public RepositoryDiagnosisReport Diagnose(string repositoryPath)
    {
        var fullRepositoryPath = Path.GetFullPath(repositoryPath);
        var report = new RepositoryDiagnosisReport(fullRepositoryPath)
        {
            RepositoryExists = Directory.Exists(fullRepositoryPath)
        };
        if (!report.RepositoryExists)
        {
            report.InspectionErrors.Add("Repository path does not exist.");
            return report;
        }

        var dotnetRoot = Path.Combine(fullRepositoryPath, "backend", "dotnet");
        var buildingBlocksRoot = Path.Combine(dotnetRoot, "BuildingBlocks");
        var sourceRoot = Path.Combine(buildingBlocksRoot, "src");
        var testRoot = Path.Combine(buildingBlocksRoot, "tests");
        var solutionPath = Path.Combine(dotnetRoot, "Tw.SmartPlatform.slnx");
        var projectPaths = Directory.Exists(dotnetRoot)
            ? Directory.GetFiles(dotnetRoot, "*.csproj", SearchOption.AllDirectories)
                .Where(path => !IsBuildOutput(path))
                .ToArray()
            : [];

        report.SourceProjectCount = CountProjects(sourceRoot);
        report.TestProjectCount = CountProjects(testRoot);
        report.SolutionParity = HasSolutionParity(solutionPath, dotnetRoot, sourceRoot, testRoot, report.InspectionErrors);
        report.UnresolvedProjectReferences.AddRange(FindUnresolvedProjectReferences(projectPaths, fullRepositoryPath));
        report.MissingLockFiles.AddRange(projectPaths
            .Where(projectPath => !File.Exists(Path.Combine(Path.GetDirectoryName(projectPath)!, "packages.lock.json")))
            .Select(projectPath => RelativePath(fullRepositoryPath, projectPath)));

        var dependencyScan = _dependencyScanner.ScanRepository(fullRepositoryPath);
        report.RetiredReferences.AddRange(dependencyScan.Errors.Where(error => error.Code == "TWGOV002"));
        report.InspectionErrors.AddRange(dependencyScan.Errors
            .Where(error => error.Code == "TWGOV000")
            .Select(error => error.Message));

        try
        {
            var packageCatalog = ForbiddenPackageCatalog.Load(fullRepositoryPath);
            report.RetiredLockDependencies.AddRange(FindRetiredLockDependencies(
                projectPaths,
                fullRepositoryPath,
                packageCatalog,
                report.InspectionErrors));
        }
        catch (GovernanceConfigurationException exception)
        {
            report.InspectionErrors.Add(exception.Message);
        }

        if (!File.Exists(solutionPath))
        {
            report.InspectionErrors.Add($"Solution file does not exist: {RelativePath(fullRepositoryPath, solutionPath)}");
            return report;
        }

        var restoreResult = _lockedRestoreRunner.Run(solutionPath, dotnetRoot);
        report.LockedRestoreExitCode = restoreResult.ExitCode;
        report.LockedRestoreStandardOutput = restoreResult.StandardOutput;
        report.LockedRestoreStandardError = restoreResult.StandardError;
        return report;
    }

    /// <summary>
    /// 计算指定目录中的物理项目数量
    /// </summary>
    /// <param name="projectsRoot">运行时或测试项目根目录</param>
    /// <returns>递归发现的非构建输出 csproj 数量</returns>
    private static int CountProjects(string projectsRoot)
    {
        return Directory.Exists(projectsRoot)
            ? Directory.GetFiles(projectsRoot, "*.csproj", SearchOption.AllDirectories).Count(path => !IsBuildOutput(path))
            : 0;
    }

    /// <summary>
    /// 比较解决方案中的 BuildingBlocks 项目和物理项目集合
    /// </summary>
    /// <param name="solutionPath">解决方案文件路径</param>
    /// <param name="dotnetRoot">解决方案项目路径的相对基准</param>
    /// <param name="sourceRoot">BuildingBlocks 运行时项目根目录</param>
    /// <param name="testRoot">BuildingBlocks 测试项目根目录</param>
    /// <param name="inspectionErrors">无法解析解决方案时写入的诊断集合</param>
    /// <returns>物理项目和解决方案项目一一对应时返回 <see langword="true"/></returns>
    private static bool HasSolutionParity(
        string solutionPath,
        string dotnetRoot,
        string sourceRoot,
        string testRoot,
        ICollection<string> inspectionErrors)
    {
        if (!File.Exists(solutionPath))
        {
            return false;
        }

        try
        {
            var actualProjects = new[] { sourceRoot, testRoot }
                .Where(Directory.Exists)
                .SelectMany(root => Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories))
                .Where(path => !IsBuildOutput(path))
                .Select(path => NormalizePath(Path.GetRelativePath(dotnetRoot, path)))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var solutionProjects = XDocument.Load(solutionPath)
                .Descendants()
                .Where(element => element.Name.LocalName == "Project")
                .Select(element => NormalizePath(element.Attribute("Path")?.Value ?? string.Empty))
                .Where(path => path.StartsWith("BuildingBlocks/src/", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith("BuildingBlocks/tests/", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            return solutionProjects.Length == solutionProjects.Distinct(StringComparer.OrdinalIgnoreCase).Count()
                && actualProjects.SetEquals(solutionProjects);
        }
        catch (Exception exception) when (exception is System.Xml.XmlException or InvalidOperationException)
        {
            inspectionErrors.Add("Solution XML is invalid.");
            return false;
        }
    }

    /// <summary>
    /// 查找无法解析为现存项目文件的 ProjectReference
    /// </summary>
    /// <param name="projectPaths">待扫描的项目文件路径</param>
    /// <param name="repositoryPath">输出诊断路径的相对基准</param>
    /// <returns>无法解析引用的可读诊断列表</returns>
    private static IEnumerable<string> FindUnresolvedProjectReferences(
        IEnumerable<string> projectPaths,
        string repositoryPath)
    {
        foreach (var projectPath in projectPaths)
        {
            XDocument? document;
            try
            {
                document = XDocument.Load(projectPath);
            }
            catch (Exception exception) when (exception is System.Xml.XmlException or InvalidOperationException)
            {
                document = null;
            }

            if (document is null)
            {
                yield return $"{RelativePath(repositoryPath, projectPath)} has invalid XML";
                continue;
            }

            foreach (var include in document.Descendants()
                         .Where(element => element.Name.LocalName == "ProjectReference")
                         .Select(element => element.Attribute("Include")?.Value))
            {
                if (string.IsNullOrWhiteSpace(include))
                {
                    yield return $"{RelativePath(repositoryPath, projectPath)} has ProjectReference without Include";
                    continue;
                }

                if (include.Contains("$(", StringComparison.Ordinal)
                    || include.Contains("@(", StringComparison.Ordinal)
                    || include.Contains("%(", StringComparison.Ordinal))
                {
                    yield return $"{RelativePath(repositoryPath, projectPath)} has unresolved ProjectReference expression {include}";
                    continue;
                }

                var referencedPath = Path.GetFullPath(Path.Combine(
                    Path.GetDirectoryName(projectPath)!,
                    MsBuildPath.NormalizeFileSystemPath(include, Path.DirectorySeparatorChar)));
                if (!File.Exists(referencedPath))
                {
                    yield return $"{RelativePath(repositoryPath, projectPath)} -> {NormalizePath(include)}";
                }
            }
        }
    }

    /// <summary>
    /// 查找 NuGet 锁文件顶层依赖中的淘汰包身份
    /// </summary>
    /// <param name="projectPaths">用于定位相邻锁文件的项目路径</param>
    /// <param name="repositoryPath">输出诊断路径的相对基准</param>
    /// <param name="packageCatalog">当前仓库拓扑清单中的淘汰包目录</param>
    /// <param name="inspectionErrors">锁文件损坏时写入的诊断集合</param>
    /// <returns>锁文件路径和淘汰包标识集合</returns>
    private static IEnumerable<RetiredLockDependency> FindRetiredLockDependencies(
        IEnumerable<string> projectPaths,
        string repositoryPath,
        ForbiddenPackageCatalog packageCatalog,
        ICollection<string> inspectionErrors)
    {
        foreach (var lockFilePath in projectPaths
                     .Select(projectPath => Path.Combine(Path.GetDirectoryName(projectPath)!, "packages.lock.json"))
                     .Where(File.Exists)
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(File.ReadAllText(lockFilePath));
            }
            catch (JsonException)
            {
                inspectionErrors.Add($"Lock file is invalid JSON: {RelativePath(repositoryPath, lockFilePath)}");
                continue;
            }

            using (document)
            {
                if (!document.RootElement.TryGetProperty("dependencies", out var frameworks)
                    || frameworks.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                foreach (var framework in frameworks.EnumerateObject())
                {
                    if (framework.Value.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    foreach (var dependency in framework.Value.EnumerateObject())
                    {
                        if (packageCatalog.TryGetRetiredPackage(dependency.Name, out var retiredPackage))
                        {
                            yield return new RetiredLockDependency(
                                RelativePath(repositoryPath, lockFilePath),
                                retiredPackage!.PackageId);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 判断路径是否位于编译输出目录
    /// </summary>
    /// <param name="path">待检查的路径</param>
    /// <returns>路径位于 bin 或 obj 时返回 <see langword="true"/></returns>
    private static bool IsBuildOutput(string path)
    {
        var normalized = NormalizePath(path);
        return normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 将路径分隔符规范化为正斜杠
    /// </summary>
    /// <param name="path">待规范化路径</param>
    /// <returns>使用正斜杠的路径</returns>
    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    /// <summary>
    /// 返回仓库相对的规范化路径
    /// </summary>
    /// <param name="repositoryPath">仓库根目录</param>
    /// <param name="path">待转换路径</param>
    /// <returns>使用正斜杠的仓库相对路径</returns>
    private static string RelativePath(string repositoryPath, string path)
    {
        return NormalizePath(Path.GetRelativePath(repositoryPath, path));
    }
}

/// <summary>
/// 执行解决方案 locked restore 的可注入进程边界
/// </summary>
public interface ILockedRestoreRunner
{
    /// <summary>
    /// 以 locked mode 还原指定解决方案
    /// </summary>
    /// <param name="solutionPath">需要还原的 .slnx 路径</param>
    /// <param name="workingDirectory">dotnet 子进程工作目录</param>
    /// <returns>子进程退出码、标准输出和标准错误</returns>
    LockedRestoreResult Run(string solutionPath, string workingDirectory);
}

/// <summary>
/// 通过本机 dotnet 进程执行权威 locked restore
/// </summary>
public sealed class DotnetLockedRestoreRunner : ILockedRestoreRunner
{
    /// <summary>
    /// locked restore 的稳定超时退出码
    /// </summary>
    private const int TimeoutExitCode = 124;

    /// <summary>
    /// 默认 locked restore 最大执行时间
    /// </summary>
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(10);

    /// <summary>
    /// 子进程最大执行时间
    /// </summary>
    private readonly TimeSpan _timeout;

    /// <summary>
    /// 根据解决方案与工作目录创建子进程启动信息
    /// </summary>
    private readonly Func<string, string, ProcessStartInfo> _startInfoFactory;

    /// <summary>
    /// 使用十分钟默认超时初始化真实 dotnet restore runner
    /// </summary>
    public DotnetLockedRestoreRunner()
        : this(DefaultTimeout, CreateDotnetRestoreStartInfo)
    {
    }

    /// <summary>
    /// 使用可控超时与进程启动边界初始化测试 runner
    /// </summary>
    /// <param name="timeout">子进程最大执行时间</param>
    /// <param name="startInfoFactory">根据解决方案与工作目录创建进程启动信息的工厂</param>
    internal DotnetLockedRestoreRunner(
        TimeSpan timeout,
        Func<string, string, ProcessStartInfo> startInfoFactory)
    {
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "locked restore 超时必须大于零");
        }

        _timeout = timeout;
        _startInfoFactory = startInfoFactory ?? throw new ArgumentNullException(nameof(startInfoFactory));
    }

    /// <summary>
    /// 以 locked mode 执行 dotnet restore 并完整捕获进程输出
    /// </summary>
    /// <param name="solutionPath">需要还原的 .slnx 路径</param>
    /// <param name="workingDirectory">dotnet 子进程工作目录</param>
    /// <returns>子进程退出码、标准输出和标准错误</returns>
    public LockedRestoreResult Run(string solutionPath, string workingDirectory)
    {
        var startInfo = _startInfoFactory(solutionPath, workingDirectory);
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.CreateNoWindow = true;

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException("无法启动 dotnet restore 子进程");
        }

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();
        using var timeoutCancellation = new CancellationTokenSource(_timeout);
        try
        {
            process.WaitForExitAsync(timeoutCancellation.Token).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException) when (timeoutCancellation.IsCancellationRequested)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // 进程恰好在超时边界退出时继续排空输出并返回稳定超时结果
            }

            process.WaitForExit();
            Task.WhenAll(standardOutputTask, standardErrorTask).GetAwaiter().GetResult();
            var timeoutMessage = $"Locked restore timed out after {_timeout}.";
            var standardError = standardErrorTask.Result;
            if (!string.IsNullOrEmpty(standardError) && !standardError.EndsWith(Environment.NewLine, StringComparison.Ordinal))
            {
                standardError += Environment.NewLine;
            }

            return new LockedRestoreResult(
                TimeoutExitCode,
                standardOutputTask.Result,
                standardError + timeoutMessage + Environment.NewLine);
        }

        Task.WhenAll(standardOutputTask, standardErrorTask).GetAwaiter().GetResult();
        return new LockedRestoreResult(process.ExitCode, standardOutputTask.Result, standardErrorTask.Result);
    }

    /// <summary>
    /// 创建生产环境使用的 dotnet restore 启动信息
    /// </summary>
    /// <param name="solutionPath">需要以 locked mode 还原的解决方案路径</param>
    /// <param name="workingDirectory">dotnet 子进程工作目录</param>
    /// <returns>包含 locked mode 参数的进程启动信息</returns>
    private static ProcessStartInfo CreateDotnetRestoreStartInfo(string solutionPath, string workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = workingDirectory
        };
        startInfo.ArgumentList.Add("restore");
        startInfo.ArgumentList.Add(solutionPath);
        startInfo.ArgumentList.Add("--locked-mode");

        return startInfo;
    }
}

/// <summary>
/// 描述 locked restore 子进程结果
/// </summary>
/// <param name="ExitCode">dotnet restore 退出码</param>
/// <param name="StandardOutput">子进程标准输出</param>
/// <param name="StandardError">子进程标准错误</param>
public sealed record LockedRestoreResult(int ExitCode, string StandardOutput, string StandardError);

/// <summary>
/// 描述锁文件中发现的淘汰包依赖
/// </summary>
/// <param name="LockFilePath">仓库相对锁文件路径</param>
/// <param name="PackageId">拓扑清单中的淘汰包标识</param>
public sealed record RetiredLockDependency(string LockFilePath, string PackageId);

/// <summary>
/// 承载诊断命令采集的仓库事实和权威 restore 结果
/// </summary>
public sealed class RepositoryDiagnosisReport
{
    /// <summary>
    /// 使用规范化仓库路径初始化报告
    /// </summary>
    /// <param name="repositoryPath">诊断目标的绝对路径</param>
    public RepositoryDiagnosisReport(string repositoryPath)
    {
        RepositoryPath = repositoryPath;
    }

    /// <summary>
    /// 诊断目标的绝对路径
    /// </summary>
    public string RepositoryPath { get; }

    /// <summary>
    /// 诊断目标目录是否存在
    /// </summary>
    public bool RepositoryExists { get; internal set; }

    /// <summary>
    /// BuildingBlocks 运行时项目发现数量
    /// </summary>
    public int SourceProjectCount { get; internal set; }

    /// <summary>
    /// BuildingBlocks 测试项目发现数量
    /// </summary>
    public int TestProjectCount { get; internal set; }

    /// <summary>
    /// 物理 BuildingBlocks 项目是否与 .slnx 一一对应
    /// </summary>
    public bool SolutionParity { get; internal set; }

    /// <summary>
    /// 无法解析为现存项目的 ProjectReference 诊断
    /// </summary>
    public List<string> UnresolvedProjectReferences { get; } = [];

    /// <summary>
    /// 项目文件中发现的淘汰引用
    /// </summary>
    public List<DependencyScanError> RetiredReferences { get; } = [];

    /// <summary>
    /// 缺少相邻 packages.lock.json 的项目路径
    /// </summary>
    public List<string> MissingLockFiles { get; } = [];

    /// <summary>
    /// NuGet 锁文件中发现的淘汰包依赖
    /// </summary>
    public List<RetiredLockDependency> RetiredLockDependencies { get; } = [];

    /// <summary>
    /// XML、JSON 或拓扑配置无法检查时产生的诊断
    /// </summary>
    public List<string> InspectionErrors { get; } = [];

    /// <summary>
    /// locked restore 子进程退出码，未执行时为 <see langword="null"/>
    /// </summary>
    public int? LockedRestoreExitCode { get; internal set; }

    /// <summary>
    /// locked restore 标准输出
    /// </summary>
    public string LockedRestoreStandardOutput { get; internal set; } = string.Empty;

    /// <summary>
    /// locked restore 标准错误
    /// </summary>
    public string LockedRestoreStandardError { get; internal set; } = string.Empty;

    /// <summary>
    /// 返回命令应传播的退出码
    /// </summary>
    public int ExitCode
    {
        get
        {
            if (LockedRestoreExitCode is int restoreExitCode && restoreExitCode != 0)
            {
                return restoreExitCode;
            }

            return RepositoryExists
                && SolutionParity
                && UnresolvedProjectReferences.Count == 0
                && RetiredReferences.Count == 0
                && MissingLockFiles.Count == 0
                && RetiredLockDependencies.Count == 0
                && InspectionErrors.Count == 0
                ? 0
                : 1;
        }
    }
}
