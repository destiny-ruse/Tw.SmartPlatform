# Tw.Core

`Tw.Core` 提供跨服务复用的基础原语与无框架依赖工具。本页按功能跳转到使用文档。

## 能力索引

- [取消令牌 Provider](context/cancellation-token-provider.md)：统一的执行上下文取消令牌能力。
- `Tw.DependencyInjection.Abstractions`：DI 自动注册标记接口与特性（注册引擎执行见 `Tw.DependencyInjection`，P1+ 落地）。
- `Tw.Configuration.Abstractions`：配置 Options 契约与特性（绑定执行见 `Tw.DependencyInjection`，P3 落地）。
- `Tw.DynamicProxy.Abstractions`：AOP 拦截契约与基类（承载执行见 `Tw.DependencyInjection`，P4 落地）。
- `Tw.Reflection`：类型查找与反射缓存（由 `Tw.Core.Reflection` 迁移）。
