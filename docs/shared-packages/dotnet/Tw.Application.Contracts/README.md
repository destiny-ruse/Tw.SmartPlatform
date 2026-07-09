# Tw.Application.Contracts

`Tw.Application.Contracts` 提供应用层公开契约，包括 command、query、DTO、分页请求和分页结果。它面向客户端共享契约和跨程序集用例调用，不承载 handler 实现。

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

- 本包不得依赖 MediatR 或 FluentValidation
- handler、事务编排、权限执行和协议适配放在其他包或服务实现中
- DTO 和契约字段变更必须按兼容性要求评估调用方影响
