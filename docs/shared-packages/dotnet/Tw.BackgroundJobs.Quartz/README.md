# Tw.BackgroundJobs.Quartz

`Tw.BackgroundJobs.Quartz` 将 `Tw.BackgroundJobs.Abstractions` 的任务契约适配到 Quartz，提供 Cron 校验、调度器适配、任务控制和存储选项。

## 稳定性

本包处于 `experimental` 阶段。进入 `stable` 前必须补齐真实 Quartz 调度、JobKey、misfire、并发控制、持久化、失败恢复和取消语义的集成验证。

## 边界

- 业务任务处理器属于消费方
- Quartz 类型不得进入 provider-neutral 的后台任务契约
- 调度器与存储选择由宿主组合根负责
