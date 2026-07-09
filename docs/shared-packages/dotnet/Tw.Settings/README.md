# Tw.Settings

`Tw.Settings` 提供按作用域读取配置项的契约和默认读取服务。默认解析顺序为 user、tenant、service、definition default。

## DI 注册

```csharp
services.AddScoped<ISettingProvider>(provider =>
    new SettingProvider(
        provider.GetRequiredService<ISettingStore>(),
        provider.GetRequiredService<ISettingCache>(),
        settingDefinitions));
```

读取配置：

```csharp
var pageSize = await provider.GetAsync(
    "orders.page-size",
    tenantId,
    serviceName,
    userId,
    cancellationToken);
```

## 注意事项

- 未找到值且没有定义默认值时返回 `null`
- 缓存键包含 setting name、scope 和 scope key，避免跨租户或跨用户泄漏
- 本包不实现配置持久化、分布式缓存或配置中心同步
