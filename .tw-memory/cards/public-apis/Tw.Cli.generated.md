# Public API: Tw.Cli

标识：Tw.Cli / backend/dotnet/tools/src/Tw.Cli

公开能力边界：
- tw new
- tw add capability
- tw validate contracts
- tw audit dependencies
- tw diagnose

实现公开命名空间：
- Tw.Cli
- Tw.Cli.Commands
- Tw.Cli.Governance

公开类型：
- sealed class CliApplication - Tw.Cli (backend/dotnet/tools/src/Tw.Cli/CliApplication.cs:9)
- static class DiagnoseCommand - Tw.Cli.Commands (backend/dotnet/tools/src/Tw.Cli/Commands/DiagnoseCommand.cs:11)
- sealed class DotnetLockedRestoreRunner - Tw.Cli.Commands (backend/dotnet/tools/src/Tw.Cli/Commands/DiagnoseCommand.cs:347)
- interface ILockedRestoreRunner - Tw.Cli.Commands (backend/dotnet/tools/src/Tw.Cli/Commands/DiagnoseCommand.cs:333)
- sealed record LockedRestoreResult - Tw.Cli.Commands (backend/dotnet/tools/src/Tw.Cli/Commands/DiagnoseCommand.cs:479)
- sealed class RepositoryDiagnosisReport - Tw.Cli.Commands (backend/dotnet/tools/src/Tw.Cli/Commands/DiagnoseCommand.cs:491)
- sealed class RepositoryDiagnosisService - Tw.Cli.Commands (backend/dotnet/tools/src/Tw.Cli/Commands/DiagnoseCommand.cs:22)
- sealed record RetiredLockDependency - Tw.Cli.Commands (backend/dotnet/tools/src/Tw.Cli/Commands/DiagnoseCommand.cs:486)
- sealed record DependencyScanError - Tw.Cli.Governance (backend/dotnet/tools/src/Tw.Cli/Governance/ProjectDependencyScanner.cs:673)
- sealed class DependencyScanResult - Tw.Cli.Governance (backend/dotnet/tools/src/Tw.Cli/Governance/ProjectDependencyScanner.cs:659)
- sealed class ForbiddenPackageCatalog - Tw.Cli.Governance (backend/dotnet/tools/src/Tw.Cli/Governance/ForbiddenPackageCatalog.cs:8)
- sealed class GovernanceConfigurationException - Tw.Cli.Governance (backend/dotnet/tools/src/Tw.Cli/Governance/ForbiddenPackageCatalog.cs:151)
- sealed class ProjectDependencyScanner - Tw.Cli.Governance (backend/dotnet/tools/src/Tw.Cli/Governance/ProjectDependencyScanner.cs:8)
- sealed record RetiredPackageRule - Tw.Cli.Governance (backend/dotnet/tools/src/Tw.Cli/Governance/ForbiddenPackageCatalog.cs:146)

DI 注册入口：
- none

包参考文档：
- docs/shared-packages/dotnet/Tw.Cli/README.md

契约关联：
- none

消费提示：
- 公开能力来自 package-charter.yaml
- 实现公开 API 来自当前包源码
- 关联文档来自 docs/shared-packages

source_refs:
- charter:package-charter:Tw.Cli
