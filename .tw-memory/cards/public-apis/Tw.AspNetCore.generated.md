# Public API: Tw.AspNetCore

标识：Tw.AspNetCore / backend/dotnet/BuildingBlocks/src/Tw.AspNetCore

公开能力边界：
- Tw.AspNetCore
- Tw.AspNetCore.Context

实现公开命名空间：
- Tw.AspNetCore
- Tw.AspNetCore.Context

公开类型：
- static class WebIntegrationServiceCollectionExtensions - Tw.AspNetCore (backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/DependencyInjection/WebIntegrationServiceCollectionExtensions.cs:14)
- static class CancellationTokenServiceCollectionExtensions - Tw.AspNetCore.Context (backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Context/CancellationTokenServiceCollectionExtensions.cs:11)
- sealed class HttpContextCancellationTokenProvider - Tw.AspNetCore.Context (backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Context/HttpContextCancellationTokenProvider.cs:13)

DI 注册入口：
- Tw.AspNetCore.Context.CancellationTokenServiceCollectionExtensions.AddHttpContextCancellationTokenProvider
- Tw.AspNetCore.WebIntegrationServiceCollectionExtensions.AddWebIntegration

使用文档：
- docs/shared-packages/dotnet/Tw.AspNetCore/context/http-context-cancellation-token-provider.md
- docs/shared-packages/dotnet/Tw.AspNetCore/README.md

契约关联：
- none

消费提示：
- 公开能力来自 package-charter.yaml
- 实现公开 API 来自当前包源码
- 使用文档来自 docs/shared-packages

source_refs:
- charter:package-charter:Tw.AspNetCore
