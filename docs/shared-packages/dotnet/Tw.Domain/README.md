# Tw.Domain

`Tw.Domain` 承载领域实体、值对象、领域服务、领域异常，以及不依赖 ORM 或数据库提供程序的实体形状契约。它可以依赖 `Tw.Domain.Shared` 和 `Tw.Core`，但不承载应用用例编排或数据访问实现。

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
- 跨服务共享前先确认规则确实属于平台领域基础，而不是单个服务私有模型
