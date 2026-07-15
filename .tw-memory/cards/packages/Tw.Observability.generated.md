# Package: Tw.Observability

标识：Tw.Observability / backend/dotnet/BuildingBlocks/src/Observability/Tw.Observability / platform-team
职责：提供共享日志、追踪、指标、健康状态与关联上下文契约，脱敏能力由 Tw.Security.DataMasking 提供。

适用范围：
- 关联上下文
- 追踪上下文
- 指标标签
- 健康状态模型

不适用范围：
- 脱敏引擎
- Serilog sink 注册
- OpenTelemetry 导出器注册

依赖边界：
- forbid: SqlSugar*, DotNetCore.CAP*
- allow: Tw.Security

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.Observability
