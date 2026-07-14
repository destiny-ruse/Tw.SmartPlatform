# Tw.Sharding

`Tw.Sharding` 是提供方无关的分片描述契约包，并在异步调用链中保存当前分片选择。包内不执行数据库连接路由，也不决定分片策略。

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

嵌套作用域释放后恢复外层描述。同一作用域的释放状态在每个继承的 `ExecutionContext`（异步流）内分别记录：每个异步流首次释放时各自恢复进入作用域前的描述，同一异步流重复释放不产生副作用。子异步流释放只恢复子异步流，不会释放或改写父异步流；父异步流仍需自行释放作用域。`Change` 不接受 `null`。

## 能力边界

- 包保持提供方无关，不依赖 ASP.NET Core、SqlSugar、CAP 或数据库提供方
- 分片键计算、路由策略和数据库连接选择由具体提供方负责
- 包不提供分片路由器、数据库选择器或租户存储 API
- `ShardDescriptor.Key` 是不透明值，调用方不得依赖包解析其结构
