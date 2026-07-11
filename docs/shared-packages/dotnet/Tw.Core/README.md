# Tw.Core

`Tw.Core` 提供跨服务复用的基础原语与无框架依赖工具。本包处于 experimental 阶段，本页按功能跳转到使用文档。

## 能力索引

- [异步释放工具](async/async-disposal.md)：将异步清理委托包装为可重用的 `IAsyncDisposable`。
- `Tw.Reflection`：类型查找与反射缓存（由 `Tw.Core.Reflection` 迁移）。

## 边界

DI 与 Options 元数据由 [`Tw.DependencyInjection.Abstractions`](../Tw.DependencyInjection.Abstractions/README.md) 承载。加密、哈希、密码哈希、密码学安全随机和密钥解析由 [`Tw.Security`](../Tw.Security/README.md) 承载。环境式取消令牌与请求上下文由具体宿主边界显式处理；横切关注点由宿主框架的 middleware、filter、interceptor 或应用管线承载。
