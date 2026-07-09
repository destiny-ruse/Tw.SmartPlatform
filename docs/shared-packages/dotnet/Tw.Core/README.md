# Tw.Core

`Tw.Core` 提供跨服务复用的基础原语与无框架依赖工具。本页按功能跳转到使用文档。

## 能力索引

- [取消令牌 Provider](context/cancellation-token-provider.md)：统一的执行上下文取消令牌能力。
- `Tw.Reflection`：类型查找与反射缓存（由 `Tw.Core.Reflection` 迁移）。

## 边界

DI 与 Options 元数据由 [`Tw.DependencyInjection.Abstractions`](../Tw.DependencyInjection.Abstractions/README.md) 承载。方法级拦截抽象与 Castle adapter 由 [`Tw.Castle.Core`](../Tw.Castle.Core/README.md) 承载。
