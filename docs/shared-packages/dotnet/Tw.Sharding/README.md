# Tw.Sharding

`Tw.Sharding` 提供 provider-neutral 的分片描述契约，并在异步调用链中保存当前分片选择。包内不执行数据库连接路由，也不决定分片策略。

## 公开能力

- `ShardDescriptor`：由策略标识和不透明分片键组成的值对象，`None` 表示未选择具体分片
- `IShardContext`：读取当前分片描述的契约
- `ShardContext`：基于异步调用链保存并按作用域恢复分片描述

## 使用方式

该包不提供 DI 注册入口。宿主可以按所需生命周期注册 `ShardContext`，并将同一实例暴露为 `IShardContext`。调用方使用 `Change` 创建作用域：

```csharp
using var scope = shardContext.Change(
    new ShardDescriptor("month", "orders-2026"));
```

嵌套作用域释放后恢复外层描述；作用域可以重复释放且只恢复一次。`Change` 不接受 `null`。

## 能力边界

- 包保持 provider-neutral，不依赖 ASP.NET Core、SqlSugar、CAP 或数据库 provider
- 分片键计算、路由策略和数据库连接选择由具体 provider 负责
- 包不提供 shard router、database selector 或租户存储 API
- `ShardDescriptor.Key` 是不透明值，调用方不得依赖包解析其结构
