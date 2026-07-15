# Package: Tw.DistributedLocking

标识：Tw.DistributedLocking / backend/dotnet/BuildingBlocks/src/DistributedLocking/Tw.DistributedLocking / platform-team
职责：提供 provider-neutral 分布式锁契约，以及租户与分片感知的稳定锁键构造。

适用范围：
- 分布式锁获取与句柄所有权契约
- 分布式锁键构造器

不适用范围：
- Redis 实现
- 租约续期与 fencing 实现

依赖边界：
- forbid: SqlSugar*, DotNetCore.CAP*
- allow: none

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.DistributedLocking
