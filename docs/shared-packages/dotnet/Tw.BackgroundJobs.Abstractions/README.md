# Tw.BackgroundJobs.Abstractions

`Tw.BackgroundJobs.Abstractions` 定义不依赖调度器的后台任务契约。

## 公开能力

- `BackgroundJobDefinition` 与 `BackgroundJobContext`
- `IBackgroundJob` 任务执行契约
- `BackgroundJobControlCommand` 与 `IBackgroundJobControlService`
- `IBackgroundJobStateStore` 状态存储契约

## 稳定性与边界

本包处于 `experimental` 阶段。Quartz 适配、MediatR 运行管线和具体持久化实现不属于本包；稳定前必须冻结任务标识、控制动作、状态转换和取消语义。
