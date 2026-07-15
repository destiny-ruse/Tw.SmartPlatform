# Package: Tw.Caching.FusionCache

标识：Tw.Caching.FusionCache / backend/dotnet/BuildingBlocks/src/Caching/Tw.Caching.FusionCache / platform-team
职责：提供缓存运行时集成所需的 FusionCache 适配器。

适用范围：
- FusionCache 绑定

不适用范围：
- 缓存键治理

依赖边界：
- forbid: SqlSugar*, DotNetCore.CAP*
- allow: Tw.Caching, ZiggyCreatures.FusionCache

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.Caching.FusionCache
