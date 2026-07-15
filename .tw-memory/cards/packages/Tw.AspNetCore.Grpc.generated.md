# Package: Tw.AspNetCore.Grpc

标识：Tw.AspNetCore.Grpc / backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Grpc / platform-team
职责：ASP.NET Core gRPC 服务端专属集成构建块，承载 gRPC 服务端注册入口、 gRPC 原生 interceptor 使用边界，以及 gRPC 包治理入口。

适用范围：
- ASP.NET Core gRPC 服务端注册
- gRPC 服务宿主集成
- gRPC 协议边界适配器
- gRPC 服务端注册
- gRPC 原生 interceptor 使用边界
- gRPC 包治理入口

不适用范围：
- 统一拦截 pipeline 适配器
- MVC Filter
- HTTP 中间件
- Minimal API endpoint filter
- Razor 与 MVC 能力
- 业务 proto 契约

依赖边界：
- forbid: none
- allow: Tw.AspNetCore, Grpc.AspNetCore, Microsoft.Extensions.*

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.AspNetCore.Grpc
