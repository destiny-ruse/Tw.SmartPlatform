# Public API: Tw.DependencyInjection.Abstractions

标识：Tw.DependencyInjection.Abstractions / backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection.Abstractions

公开能力边界：
- Tw.DependencyInjection.Abstractions

实现公开命名空间：
- Tw.DependencyInjection.Abstractions
- Tw.DependencyInjection.Abstractions.Configuration

公开类型：
- sealed class AssemblyRegistrationPriorityAttribute - Tw.DependencyInjection.Abstractions (backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection.Abstractions/AssemblyRegistrationPriorityAttribute.cs:8)
- enum DependencyLifetime - Tw.DependencyInjection.Abstractions (backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection.Abstractions/DependencyLifetime.cs:11)
- sealed class DisableServiceRegistrationAttribute - Tw.DependencyInjection.Abstractions (backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection.Abstractions/DisableServiceRegistrationAttribute.cs:7)
- sealed class ExposeKeyedServiceAttribute - Tw.DependencyInjection.Abstractions (backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection.Abstractions/ExposeKeyedServiceAttribute.cs:7)
- sealed class ExposeServicesAttribute - Tw.DependencyInjection.Abstractions (backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection.Abstractions/ExposeServicesAttribute.cs:7)
- interface IScopedDependency - Tw.DependencyInjection.Abstractions (backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection.Abstractions/IScopedDependency.cs:6)
- interface ISingletonDependency - Tw.DependencyInjection.Abstractions (backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection.Abstractions/ISingletonDependency.cs:6)
- interface ITransientDependency - Tw.DependencyInjection.Abstractions (backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection.Abstractions/ITransientDependency.cs:6)
- readonly record struct KeyedServiceEntry - Tw.DependencyInjection.Abstractions (backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection.Abstractions/KeyedServiceEntry.cs:13)
- sealed class ServicePriorityAttribute - Tw.DependencyInjection.Abstractions (backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection.Abstractions/ServicePriorityAttribute.cs:7)
- sealed class ServiceRegistrationAttribute - Tw.DependencyInjection.Abstractions (backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection.Abstractions/ServiceRegistrationAttribute.cs:10)
- interface IConfigurableOptions - Tw.DependencyInjection.Abstractions.Configuration (backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection.Abstractions/Configuration/IConfigurableOptionsOfT.cs:13)

DI 注册入口：
- none

包参考文档：
- docs/shared-packages/dotnet/Tw.DependencyInjection.Abstractions/README.md

契约关联：
- none

消费提示：
- 公开能力来自 package-charter.yaml
- 实现公开 API 来自当前包源码
- 关联文档来自 docs/shared-packages

source_refs:
- charter:package-charter:Tw.DependencyInjection.Abstractions
