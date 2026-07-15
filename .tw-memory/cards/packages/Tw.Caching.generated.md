# Package: Tw.Caching

标识：Tw.Caching / backend/dotnet/BuildingBlocks/src/Caching/Tw.Caching / platform-team
职责：提供缓存键契约与缓存失效通知契约。

适用范围：
- 租户与分片感知缓存键
- 缓存失效发布契约

不适用范围：
- FusionCache 适配器
- Redis 适配器

依赖边界：
- forbid: SqlSugar*, DotNetCore.CAP*
- allow: none

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.Caching
