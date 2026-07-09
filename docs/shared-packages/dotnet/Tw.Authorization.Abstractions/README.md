# Tw.Authorization.Abstractions

`Tw.Authorization.Abstractions` 定义权限检查所需的上下文、结果、permission grant 存储和缓存契约。它用于让服务在不依赖具体身份中心实现的前提下表达权限边界。

## 使用方式

```csharp
var context = new AuthorizationContext(
    SubjectId: "user-1",
    TenantId: "tenant-1",
    Permission: "orders.approve",
    ResourceType: "Order",
    ResourceId: "order-1",
    Roles: roles);
```

调用方依赖 `IPermissionChecker` 完成权限判断，具体实现由 `Tw.Authorization` 或服务自身组合根注册。

## 注意事项

- 本包只定义契约，不连接 OpenIddict、数据库、缓存或远程服务
- 缓存键包含 subject、tenant、permission、resource type 和 resource id，避免跨租户或跨资源误用
- 权限错误码必须保持稳定，供协议层映射
