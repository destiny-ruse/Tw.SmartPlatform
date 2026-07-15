# Package: Tw.Grpc

标识：Tw.Grpc / backend/dotnet/BuildingBlocks/src/Grpc/Tw.Grpc / platform-team
职责：提供 gRPC 元数据传播、调用期限契约与契约优先边界辅助能力。

适用范围：
- 元数据传播允许列表
- 客户端调用期限选项
- 契约优先 proto 治理

不适用范围：
- ASP.NET Core gRPC 服务端宿主
- CAP 传输
- 数据访问

依赖边界：
- forbid: SqlSugar*, DotNetCore.CAP*
- allow: none

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.Grpc
