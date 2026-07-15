# Tw.Caching.FusionCache

`Tw.Caching.FusionCache` 是 `Tw.Caching` 的 FusionCache provider 适配包。

## 当前能力

当前公开实现 `FusionCacheAdapter` 只标识 provider，并未形成完整缓存注册、读写、失效或故障处理契约。

## 稳定性

本包处于 `experimental` 阶段。进入 `stable` 前必须实现并验证真实 FusionCache 绑定、缓存失效、并发填充、超时、降级和依赖失败行为；不得把当前占位能力描述为生产就绪实现。
