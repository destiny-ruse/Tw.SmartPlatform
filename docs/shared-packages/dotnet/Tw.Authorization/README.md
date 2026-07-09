# Tw.Authorization

`Tw.Authorization` 提供基于 `IGrantStore` 和 `IPermissionGrantCache` 的默认权限检查器 `PermissionChecker`。

## DI 注册

```csharp
services.AddScoped<IPermissionChecker, PermissionChecker>();
services.AddScoped<IGrantStore, ServiceGrantStore>();
services.AddScoped<IPermissionGrantCache, ServicePermissionGrantCache>();
```

`PermissionChecker` 先读取 grant cache；未命中时通过 `IGrantStore` 判断权限并写回缓存。

## 注意事项

- 拒绝访问返回稳定错误码 `AUTHORIZATION:000001`
- 本包不实现 grant 持久化、分布式缓存、JWT 校验或 OpenIddict 身份中心
- `IGrantStore` 和 `IPermissionGrantCache` 由服务或基础设施包提供实现
