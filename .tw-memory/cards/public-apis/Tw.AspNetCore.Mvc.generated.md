# Public API: Tw.AspNetCore.Mvc

标识：Tw.AspNetCore.Mvc / backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Mvc

公开能力边界：
- Tw.AspNetCore.Mvc.ModelBinding
- Tw.AspNetCore.Mvc.Responses
- Tw.AspNetCore.Mvc.Security
- Tw.AspNetCore.Mvc.ApiVersioning
- Tw.AspNetCore.Mvc

实现公开命名空间：
- Tw.AspNetCore.Mvc
- Tw.AspNetCore.Mvc.ApiVersioning
- Tw.AspNetCore.Mvc.ModelBinding

公开类型：
- static class MvcIntegrationServiceCollectionExtensions - Tw.AspNetCore.Mvc (backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Mvc/DependencyInjection/MvcIntegrationServiceCollectionExtensions.cs:8)
- static class ApiVersioningServiceCollectionExtensions - Tw.AspNetCore.Mvc.ApiVersioning (backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Mvc/ApiVersioning/ApiVersioningServiceCollectionExtensions.cs:9)
- sealed class LongIdModelBinder - Tw.AspNetCore.Mvc.ModelBinding (backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Mvc/ModelBinding/LongIdModelBinder.cs:8)

DI 注册入口：
- Tw.AspNetCore.Mvc.ApiVersioning.ApiVersioningServiceCollectionExtensions.AddApiVersioningIntegration
- Tw.AspNetCore.Mvc.MvcIntegrationServiceCollectionExtensions.AddMvcIntegration

包参考文档：
- docs/shared-packages/dotnet/Tw.AspNetCore.Mvc/README.md

契约关联：
- none

消费提示：
- 公开能力来自 package-charter.yaml
- 实现公开 API 来自当前包源码
- 关联文档来自 docs/shared-packages

source_refs:
- charter:package-charter:Tw.AspNetCore.Mvc
