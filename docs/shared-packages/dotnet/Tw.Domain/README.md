# Tw.Domain

`Tw.Domain` 提供不依赖 ORM 或数据库 provider 的实体审计、乐观并发与软删除形状契约。它不承载全局业务 DTO、共享枚举或服务专用领域契约；这些契约必须保留在各自限界上下文内。

## 公开能力

- `Tw.Domain.Auditing.IAuditedEntity`：创建与更新审计字段
- `Tw.Domain.Concurrency.IHasConcurrencyStamp`：不透明乐观并发戳
- `Tw.Domain.Concurrency.IHasVersionStamp`：数字乐观并发版本
- `Tw.Domain.SoftDelete.ISoftDelete`：逻辑删除标记、时间和主体

## 使用方式

服务的领域层项目引用本包后，由领域实体直接实现所需标记契约，持久化适配器负责映射并维护相应字段。当前包不提供 DI 注册入口。

```csharp
public sealed class Order : IHasVersionStamp, ISoftDelete
{
    public long VersionStamp { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }
}
```

## 注意事项

- 不得在本包定义 MediatR handler 或应用 pipeline
- 不得依赖 SqlSugar、CAP、OpenIddict、ASP.NET Core 等基础设施或协议包
- 并发检查异常、仓储和工作单元属于 `Tw.Data`，不属于实体形状契约
- 服务专用 DTO、共享枚举、错误码和领域契约属于具体限界上下文，不得放入全局 Building Block
