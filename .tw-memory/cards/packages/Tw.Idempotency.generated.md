# Package: Tw.Idempotency

标识：Tw.Idempotency / backend/dotnet/BuildingBlocks/src/Idempotency/Tw.Idempotency / platform-team
职责：提供幂等键模型、预留记录、稳定冲突异常、执行器与可信宿主上下文工厂。

适用范围：
- HTTP 幂等键
- gRPC 幂等键
- CAP 消息去重键
- 后台任务触发键
- 幂等执行器

不适用范围：
- SQL 持久化
- Redis 持久化

依赖边界：
- forbid: SqlSugar*, DotNetCore.CAP*
- allow: none

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.Idempotency
