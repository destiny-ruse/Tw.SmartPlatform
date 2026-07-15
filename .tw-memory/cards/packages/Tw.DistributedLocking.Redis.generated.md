# Package: Tw.DistributedLocking.Redis

标识：Tw.DistributedLocking.Redis / backend/dotnet/BuildingBlocks/src/DistributedLocking/Tw.DistributedLocking.Redis / platform-team
职责：隔离 Redis 分布式锁依赖并提供实验阶段的 provider 适配边界。

适用范围：
- Redis 分布式锁适配器
- Redis provider 依赖隔离

不适用范围：
- 锁键治理
- 业务锁编排
- 未经真实依赖验证的租约、续租、失锁和 fencing 稳定承诺

依赖边界：
- forbid: SqlSugar*, DotNetCore.CAP*
- allow: Tw.DistributedLocking, StackExchange.Redis, DistributedLock.Redis

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.DistributedLocking.Redis
