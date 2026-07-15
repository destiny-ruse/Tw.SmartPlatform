# Package: Tw.Observability.OpenTelemetry

标识：Tw.Observability.OpenTelemetry / backend/dotnet/BuildingBlocks/src/Observability/Tw.Observability.OpenTelemetry / platform-team
职责：提供 OpenTelemetry 与 Aspire Dashboard 的配置模型边界。

适用范围：
- OpenTelemetry 注册选项
- Aspire Dashboard 配置模型

不适用范围：
- OpenTelemetry 服务注册与宿主装配
- OTLP 导出器注册
- ASP.NET Core 与 HTTP 插桩注册
- 默认 gRPC 客户端插桩
- 脱敏引擎

依赖边界：
- forbid: 默认 gRPC 客户端插桩包, SqlSugar*, DotNetCore.CAP*
- allow: Tw.Observability, OpenTelemetry.Extensions.Hosting, OpenTelemetry.Exporter.OpenTelemetryProtocol, OpenTelemetry.Instrumentation.AspNetCore, OpenTelemetry.Instrumentation.Http

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.Observability.OpenTelemetry
