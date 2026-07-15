# Tw.IdGeneration.Yitter

`Tw.IdGeneration.Yitter` 实现 `IIdGenerator`，并通过 `AddYitterIdGeneration` 在组合根按 `workerId` 注册 Yitter 生成器。

## 稳定性

本包处于 `experimental` 阶段。进入 `stable` 前必须完成 workerId 分配、ID 位布局、并发唯一性、时钟回拨、跨节点冲突和持久数据兼容性验证。

## 边界

- Yitter SDK 类型不进入 `Tw.IdGeneration` 公共契约
- workerId 来源和生命周期由部署环境负责
- 已进入持久数据的 ID 规则不得原地切换
