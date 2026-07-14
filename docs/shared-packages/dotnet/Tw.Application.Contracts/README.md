# Tw.Application.Contracts

`Tw.Application.Contracts` 提供 provider-neutral 的 command、query 标记和通用分页契约。它不依赖 MediatR、FluentValidation 或基础设施 provider，也不承载 handler 实现。

## 使用方式

定义不返回业务结果的命令：

```csharp
public sealed record ApproveOrderCommand(string OrderId) : ICommand;
```

定义返回业务结果的查询：

```csharp
public sealed record GetOrderQuery(string OrderId) : IQuery<OrderDto>;
```

分页响应使用 `PagedResult<T>`：

```csharp
var result = new PagedResult<OrderDto>(items, totalCount);
```

## 注意事项

- MediatR handler、FluentValidation validator、事务编排、权限执行和协议适配属于应用实现或基础设施边界
- 具体服务的 DTO、共享枚举、错误码和服务契约必须保留在对应限界上下文内
- 跨边界契约字段变更必须按兼容性要求评估调用方影响
