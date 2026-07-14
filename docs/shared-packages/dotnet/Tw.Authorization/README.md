# Tw.Authorization

`Tw.Authorization` 在单一 provider-neutral 包中提供权限定义、授权上下文、授权结果、grant 存储与缓存边界，以及默认权限检查器 `PermissionChecker`。所有公开类型均位于 `Tw.Authorization` 命名空间。

## DI 注册

```csharp
services.AddScoped<IPermissionChecker, PermissionChecker>();
services.AddScoped<IGrantStore, ServiceGrantStore>();
services.AddScoped<IPermissionGrantCache, ServicePermissionGrantCache>();
```

`IGrantStore` 与 `IPermissionGrantCache` 由具体服务或基础设施包实现，本包不绑定持久化和缓存 provider。

## 使用权限契约

```csharp
var context = new AuthorizationContext(
    SubjectId: "user-1",
    TenantId: "tenant-1",
    Permission: "orders.approve",
    ResourceType: "Order",
    ResourceId: "order-1",
    Roles: roles);

var result = await permissionChecker.CheckAsync(context, cancellationToken);
```

`PermissionDefinition` 使用稳定 `Name` 与显示用 `DisplayName` 描述权限。`PermissionGrantCacheKey` 包含 subject、tenant、permission、resource type 和 resource id，隔离跨租户与跨资源缓存条目。

## 默认检查行为

`PermissionChecker` 先读取 grant cache：

- 缓存命中时直接返回允许或拒绝结果，不读取 grant store
- 缓存未命中时通过 `IGrantStore` 判断权限，并把允许或拒绝状态写回缓存
- grant store 不存在匹配记录时返回错误码 `AUTHORIZATION:000001`
- 取消与存储、缓存异常原样传播，不转换为授权拒绝

## 注意事项

- 本包不实现 grant 持久化、分布式缓存、JWT 校验或 OpenIddict 身份中心
- 调用方必须把同一个 `CancellationToken` 传递到权限检查入口
- 权限结果码必须保持稳定，供协议层映射
