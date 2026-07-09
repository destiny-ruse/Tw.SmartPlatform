# Tw.Features

`Tw.Features` 提供按作用域读取 Feature 开关的契约和默认检查器。默认解析顺序为 tenant 覆盖 service 默认值，再回退到 `FeatureDefinition` 默认值。

## DI 注册

```csharp
services.AddScoped<IFeatureChecker>(provider =>
    new FeatureChecker(
        provider.GetRequiredService<IFeatureStore>(),
        provider.GetRequiredService<IFeatureCache>(),
        featureDefinitions));
```

刷新指定缓存键：

```csharp
await checker.RefreshAsync(
    new FeatureRefreshRequest("billing.approval", FeatureScope.Tenant, "tenant-a"),
    cancellationToken);
```

## 注意事项

- 禁用 Feature 返回稳定错误码 `FEATURE:000001`
- 缓存键包含 feature name、scope 和 scope key
- 本包不实现持久化存储、分布式缓存或消息刷新订阅
