# Package: Tw.Observability.Serilog

标识：Tw.Observability.Serilog / backend/dotnet/BuildingBlocks/src/Observability/Tw.Observability.Serilog / platform-team
职责：提供 Serilog 结构化日志适配，并通过 Tw.Security.DataMasking 对敏感标量属性脱敏。

适用范围：
- Serilog enricher
- 敏感日志属性脱敏
- OpenTelemetry sink 边界

不适用范围：
- 脱敏引擎
- 审计存储

依赖边界：
- forbid: SqlSugar*, DotNetCore.CAP*
- allow: Tw.Observability, Tw.Security, Serilog.AspNetCore, Serilog.Sinks.OpenTelemetry

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.Observability.Serilog
