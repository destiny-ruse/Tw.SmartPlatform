# Public API: Tw.AspNetCore

标识：Tw.AspNetCore / backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore

公开能力边界：
- Tw.AspNetCore.Authentication
- Tw.AspNetCore.Correlation
- Tw.AspNetCore.Errors
- Tw.AspNetCore.Middleware
- Tw.AspNetCore.Security
- Tw.AspNetCore.RateLimiting
- Tw.AspNetCore.Health
- Tw.AspNetCore

实现公开命名空间：
- Tw.AspNetCore
- Tw.AspNetCore.Authentication
- Tw.AspNetCore.Correlation
- Tw.AspNetCore.Errors
- Tw.AspNetCore.Health
- Tw.AspNetCore.Middleware
- Tw.AspNetCore.RateLimiting

公开类型：
- static class HostStartupBuilderExtensions - Tw.AspNetCore (backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore/DependencyInjection/HostStartupBuilderExtensions.cs:9)
- static class AuthenticationSchemeNames - Tw.AspNetCore.Authentication (backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore/Authentication/AuthenticationSchemeNames.cs:6)
- sealed record RequestCorrelation - Tw.AspNetCore.Correlation (backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore/Correlation/RequestCorrelation.cs:8)
- sealed record ProtocolError - Tw.AspNetCore.Errors (backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore/Errors/ProtocolError.cs:6)
- static class HealthEndpointRouteBuilderExtensions - Tw.AspNetCore.Health (backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore/Health/HealthEndpointRouteBuilderExtensions.cs:10)
- sealed class ExceptionHandlingMiddleware - Tw.AspNetCore.Middleware (backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore/Middleware/ExceptionHandlingMiddleware.cs:9)
- static class RateLimitServiceCollectionExtensions - Tw.AspNetCore.RateLimiting (backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore/RateLimiting/RateLimitServiceCollectionExtensions.cs:9)

DI 注册入口：
- Tw.AspNetCore.RateLimiting.RateLimitServiceCollectionExtensions.AddApplicationRateLimiting

包参考文档：
- docs/shared-packages/dotnet/Tw.AspNetCore/host-startup.md
- docs/shared-packages/dotnet/Tw.AspNetCore/README.md

契约关联：
- none

消费提示：
- 公开能力来自 package-charter.yaml
- 实现公开 API 来自当前包源码
- 关联文档来自 docs/shared-packages

source_refs:
- charter:package-charter:Tw.AspNetCore
