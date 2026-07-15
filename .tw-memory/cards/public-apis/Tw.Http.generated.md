# Public API: Tw.Http

标识：Tw.Http / backend/dotnet/BuildingBlocks/src/Http/Tw.Http

公开能力边界：
- Tw.Http
- Tw.Http.HeaderPropagation

实现公开命名空间：
- Tw.Http
- Tw.Http.HeaderPropagation

公开类型：
- static class HttpHeaderNames - Tw.Http (backend/dotnet/BuildingBlocks/src/Http/Tw.Http/HttpHeaderNames.cs:6)
- sealed record HeaderPropagationOptions - Tw.Http.HeaderPropagation (backend/dotnet/BuildingBlocks/src/Http/Tw.Http/HeaderPropagation/HeaderPropagationOptions.cs:8)
- static class HeaderPropagationPolicy - Tw.Http.HeaderPropagation (backend/dotnet/BuildingBlocks/src/Http/Tw.Http/HeaderPropagation/HeaderPropagationPolicy.cs:24)
- enum HeaderTrustLevel - Tw.Http.HeaderPropagation (backend/dotnet/BuildingBlocks/src/Http/Tw.Http/HeaderPropagation/HeaderPropagationPolicy.cs:8)

DI 注册入口：
- none

包参考文档：
- docs/shared-packages/dotnet/Tw.Http/README.md

契约关联：
- none

消费提示：
- 公开能力来自 package-charter.yaml
- 实现公开 API 来自当前包源码
- 关联文档来自 docs/shared-packages

source_refs:
- charter:package-charter:Tw.Http
