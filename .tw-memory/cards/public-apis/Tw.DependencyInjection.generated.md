# Public API: Tw.DependencyInjection

标识：Tw.DependencyInjection / backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection

公开能力边界：
- Tw.DependencyInjection

实现公开命名空间：
- Tw.DependencyInjection
- Tw.DependencyInjection.Diagnostics

公开类型：
- static class ServiceCollectionRegistrationExtensions - Tw.DependencyInjection (backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection/ServiceCollectionRegistrationExtensions.cs:14)
- sealed class ServiceRegistrationException - Tw.DependencyInjection (backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection/ServiceRegistrationException.cs:6)
- sealed class ServiceRegistrationOptions - Tw.DependencyInjection (backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection/ServiceRegistrationOptions.cs:6)
- sealed record AssemblyTopologyEntry - Tw.DependencyInjection.Diagnostics (backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection/Diagnostics/AssemblyTopologyEntry.cs:8)
- sealed record OptionsBindingDiagnostic - Tw.DependencyInjection.Diagnostics (backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection/Diagnostics/OptionsBindingDiagnostic.cs:13)
- sealed class OptionsBindingReport - Tw.DependencyInjection.Diagnostics (backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection/Diagnostics/OptionsBindingReport.cs:6)
- sealed record PlannedServiceRegistrationDiagnostic - Tw.DependencyInjection.Diagnostics (backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection/Diagnostics/PlannedServiceRegistrationDiagnostic.cs:13)
- sealed record ServiceCandidateDiagnostic - Tw.DependencyInjection.Diagnostics (backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection/Diagnostics/ServiceCandidateDiagnostic.cs:18)
- sealed record ServiceConflictDiagnostic - Tw.DependencyInjection.Diagnostics (backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection/Diagnostics/ServiceConflictDiagnostic.cs:10)
- sealed class ServiceRegistrationReport - Tw.DependencyInjection.Diagnostics (backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection/Diagnostics/ServiceRegistrationReport.cs:15)
- sealed record SkippedServiceTypeDiagnostic - Tw.DependencyInjection.Diagnostics (backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection/Diagnostics/SkippedServiceTypeDiagnostic.cs:8)
- sealed record SupersededServiceCandidateDiagnostic - Tw.DependencyInjection.Diagnostics (backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection/Diagnostics/SupersededServiceCandidateDiagnostic.cs:12)

DI 注册入口：
- Tw.DependencyInjection.ServiceCollectionRegistrationExtensions.AddServiceRegistration

包参考文档：
- docs/shared-packages/dotnet/Tw.DependencyInjection/assembly-scanning.md
- docs/shared-packages/dotnet/Tw.DependencyInjection/options-binding.md
- docs/shared-packages/dotnet/Tw.DependencyInjection/README.md
- docs/shared-packages/dotnet/Tw.DependencyInjection/service-registration.md

契约关联：
- none

消费提示：
- 公开能力来自 package-charter.yaml
- 实现公开 API 来自当前包源码
- 关联文档来自 docs/shared-packages

source_refs:
- charter:package-charter:Tw.DependencyInjection
