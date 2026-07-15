namespace Tw.Cli;

using Tw.Cli.Commands;
using Tw.Cli.Governance;

/// <summary>
/// 将命令行参数路由到可注入的依赖审计和仓库诊断服务
/// </summary>
public sealed class CliApplication
{
    /// <summary>
    /// 项目依赖审计服务
    /// </summary>
    private readonly ProjectDependencyScanner _dependencyScanner;

    /// <summary>
    /// 仓库事实诊断服务
    /// </summary>
    private readonly RepositoryDiagnosisService _diagnosisService;

    /// <summary>
    /// 初始化 CLI 应用服务
    /// </summary>
    /// <param name="dependencyScanner">执行项目依赖治理检查的服务</param>
    /// <param name="diagnosisService">执行仓库事实和 locked restore 检查的服务</param>
    public CliApplication(
        ProjectDependencyScanner dependencyScanner,
        RepositoryDiagnosisService diagnosisService)
    {
        _dependencyScanner = dependencyScanner ?? throw new ArgumentNullException(nameof(dependencyScanner));
        _diagnosisService = diagnosisService ?? throw new ArgumentNullException(nameof(diagnosisService));
    }

    /// <summary>
    /// 执行 CLI 命令并返回稳定退出码
    /// </summary>
    /// <param name="args">命令行参数</param>
    /// <param name="standardOutput">普通命令输出目标</param>
    /// <param name="standardError">错误和治理违规输出目标</param>
    /// <returns>成功为零、治理或 restore 失败为非零、未知命令为二</returns>
    public int Run(string[] args, TextWriter standardOutput, TextWriter standardError)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(standardOutput);
        ArgumentNullException.ThrowIfNull(standardError);

        if (args.Length == 0 || IsHelp(args[0]))
        {
            PrintUsage(standardOutput);
            return 0;
        }

        var repository = GetOptionValue(args, "--repository") ?? Directory.GetCurrentDirectory();

        if (args[0].Equals("diagnose", StringComparison.OrdinalIgnoreCase))
        {
            var report = _diagnosisService.Diagnose(repository);
            PrintDiagnosis(report, standardOutput, standardError);
            return report.ExitCode;
        }

        if (args.Length >= 2
            && args[0].Equals("audit", StringComparison.OrdinalIgnoreCase)
            && args[1].Equals("dependencies", StringComparison.OrdinalIgnoreCase))
        {
            var result = _dependencyScanner.ScanRepository(repository);
            foreach (var error in result.Errors)
            {
                standardError.WriteLine($"{error.Code}: {error.Message} ({error.ProjectPath})");
            }

            return result.Errors.Count == 0 ? 0 : 1;
        }

        if (args.Length >= 2
            && args[0].Equals("validate", StringComparison.OrdinalIgnoreCase)
            && args[1].Equals("contracts", StringComparison.OrdinalIgnoreCase))
        {
            standardOutput.WriteLine("Use the repository contract validators for contract validation.");
            return 0;
        }

        if (args[0].Equals("new", StringComparison.OrdinalIgnoreCase))
        {
            standardOutput.WriteLine("Use dotnet new tw-service, tw-gateway, tw-building-block, or tw-contract-package.");
            return 0;
        }

        if (args.Length >= 2
            && args[0].Equals("add", StringComparison.OrdinalIgnoreCase)
            && args[1].Equals("capability", StringComparison.OrdinalIgnoreCase))
        {
            standardOutput.WriteLine("capability add: no changes requested");
            return 0;
        }

        standardError.WriteLine($"Unknown command: {string.Join(' ', args)}");
        PrintUsage(standardError);
        return 2;
    }

    /// <summary>
    /// 输出诊断报告的事实计数和每条违规详情
    /// </summary>
    /// <param name="report">仓库诊断服务返回的结构化报告</param>
    /// <param name="standardOutput">事实计数输出目标</param>
    /// <param name="standardError">违规详情输出目标</param>
    private static void PrintDiagnosis(
        RepositoryDiagnosisReport report,
        TextWriter standardOutput,
        TextWriter standardError)
    {
        standardOutput.WriteLine($"Repository: {report.RepositoryPath}");
        standardOutput.WriteLine($"source projects: {report.SourceProjectCount}");
        standardOutput.WriteLine($"test projects: {report.TestProjectCount}");
        standardOutput.WriteLine($"solution parity: {(report.SolutionParity ? "pass" : "fail")}");
        standardOutput.WriteLine($"unresolved project references: {report.UnresolvedProjectReferences.Count}");
        standardOutput.WriteLine($"retired references: {report.RetiredReferences.Count}");
        standardOutput.WriteLine($"missing lock files: {report.MissingLockFiles.Count}");
        standardOutput.WriteLine($"retired lock dependencies: {report.RetiredLockDependencies.Count}");
        standardOutput.WriteLine($"locked restore exit code: {report.LockedRestoreExitCode}");

        foreach (var unresolvedReference in report.UnresolvedProjectReferences)
        {
            standardError.WriteLine($"unresolved ProjectReference: {unresolvedReference}");
        }

        foreach (var retiredReference in report.RetiredReferences)
        {
            standardError.WriteLine($"{retiredReference.Code}: {retiredReference.Message} ({retiredReference.ProjectPath})");
        }

        foreach (var missingLockFile in report.MissingLockFiles)
        {
            standardError.WriteLine($"missing lock file: {missingLockFile}");
        }

        foreach (var retiredLockDependency in report.RetiredLockDependencies)
        {
            standardError.WriteLine(
                $"retired lock dependency: {retiredLockDependency.PackageId} ({retiredLockDependency.LockFilePath})");
        }

        foreach (var inspectionError in report.InspectionErrors)
        {
            standardError.WriteLine($"diagnosis error: {inspectionError}");
        }

        if (!string.IsNullOrWhiteSpace(report.LockedRestoreStandardError))
        {
            standardError.Write(report.LockedRestoreStandardError);
        }
    }

    /// <summary>
    /// 判断参数是否请求帮助信息
    /// </summary>
    /// <param name="value">首个命令行参数</param>
    /// <returns>参数为标准帮助别名时返回 <see langword="true"/></returns>
    private static bool IsHelp(string value)
    {
        return value is "-h" or "--help" or "help";
    }

    /// <summary>
    /// 查找命令行选项后紧邻的参数值
    /// </summary>
    /// <param name="args">完整命令行参数</param>
    /// <param name="optionName">需要查找的选项名称</param>
    /// <returns>找到时返回选项值，否则返回 <see langword="null"/></returns>
    private static string? GetOptionValue(string[] args, string optionName)
    {
        for (var index = 0; index < args.Length - 1; index++)
        {
            if (args[index].Equals(optionName, StringComparison.OrdinalIgnoreCase))
            {
                return args[index + 1];
            }
        }

        return null;
    }

    /// <summary>
    /// 输出 CLI 支持的稳定命令入口
    /// </summary>
    /// <param name="writer">帮助文本输出目标</param>
    private static void PrintUsage(TextWriter writer)
    {
        writer.WriteLine("tw diagnose --repository <path>");
        writer.WriteLine("tw audit dependencies --repository <path>");
        writer.WriteLine("tw validate contracts --repository <path>");
    }
}
