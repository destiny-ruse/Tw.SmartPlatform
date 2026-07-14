# Tw.DistributedLocking.Redis

`Tw.DistributedLocking.Redis` 隔离 Redis 分布式锁依赖，并实现 `Tw.DistributedLocking` 的 provider 边界。当前稳定性为 `experimental`，不得将其描述或采用为生产就绪的 Redis 锁实现。

## 当前边界

- 可以依赖 `Tw.DistributedLocking`、`StackExchange.Redis` 和 `DistributedLock.Redis`
- 不向 `Tw.DistributedLocking` 公开契约泄露 Redis 类型
- 不承担业务锁编排或锁键治理
- 不提供默认 DI 注册入口，宿主必须在组合根显式选择并注册 provider

## Stable 门禁

进入 `stable` 前必须通过独立 provider spec/plan 完成真实 Redis 依赖集成，并验证租约、续租、失锁、ownership token、fencing、竞争、超时、取消和依赖失败语义。该真实 lease/fencing 验证不属于当前包合并任务。
