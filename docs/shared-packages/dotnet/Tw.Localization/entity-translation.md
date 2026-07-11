# 实体翻译使用指南

## 能力定位

`IEntityTranslationService`（命名空间 `Tw.Localization`）提供实体字段翻译的高层查询能力，支持单字段查找和批量字段查询。翻译数据来源由 `IEntityTranslationStore` 提供；框架注册一个默认的空存储，业务应用通过实现 `IEntityTranslationStore` 并替换默认注册来接入实际数据。

框架**不**自动将翻译结果回写到实体原字段；是否用翻译值覆盖原字段由调用方决定。

## DI 注册

调用 `AddLocalization` 即注册 `IEntityTranslationService`（Singleton），无需额外配置。若业务应用提供了自定义 `IEntityTranslationStore`，必须在 `AddLocalization` **之前**以 Singleton 方式注册，否则默认空存储会保留。

```csharp
using Microsoft.Extensions.DependencyInjection;
using Tw.Localization;

// 先注册自定义存储（Singleton）
services.AddSingleton<IEntityTranslationStore, MyEntityTranslationStore>();

// 再调用 AddLocalization
services.AddLocalization(options =>
{
    options.DefaultCulture = "zh-Hans";
    options.SupportedCultures.Add("zh-Hans");
    options.SupportedCultures.Add("en-US");
});
```

> `IEntityTranslationStore` 必须以 Singleton 方式注册。若注册为 Scoped，单例的 `IEntityTranslationService` 将在整个应用生命周期内持有同一个 Scoped 实例，导致作用域语义失效。

## 实现 IEntityTranslationStore

`IEntityTranslationStore`（命名空间 `Tw.Localization`）定义单个方法，框架通过此方法向存储层发出批量查询请求：

```csharp
ValueTask<IReadOnlyList<EntityTranslation>> GetListAsync(
    EntityTranslationQuery query,
    CancellationToken cancellationToken = default);
```

`EntityTranslationQuery`（`Tw.Localization.Requests`）包含：

| 属性 | 类型 | 说明 |
|------|------|------|
| `Keys` | `IReadOnlyList<EntityTranslationKey>` | 要查询的实体字段复合键列表 |
| `Context` | `LocalizationContext` | 目标文化和租户信息 |
| `CandidateCultureNames` | `IReadOnlyList<string>` | 由框架按回退策略展开的候选文化链，按优先级从高到低排列 |

`EntityTranslationKey`（`Tw.Localization.Requests`）由三个字符串字段构成：

```csharp
record EntityTranslationKey(string EntityType, string EntityId, string FieldName);
```

实现示例（伪代码，需替换为实际数据访问逻辑）：

```csharp
using Tw.Localization;
using Tw.Localization.Requests;

public sealed class MyEntityTranslationStore : IEntityTranslationStore
{
    public async ValueTask<IReadOnlyList<EntityTranslation>> GetListAsync(
        EntityTranslationQuery query,
        CancellationToken cancellationToken = default)
    {
        // 使用 query.Keys、query.Context.TenantId 和 query.CandidateCultureNames
        // 向数据库发出一次批量查询，避免 N+1
        var results = await _db.QueryTranslationsAsync(
            query.Keys,
            query.CandidateCultureNames,
            query.Context.TenantId,
            cancellationToken);

        return results;
    }
}
```

`GetListAsync` 无结果时返回空列表，不返回 `null`。

## 使用 IEntityTranslationService 查询翻译

### 注入

```csharp
using Tw.Localization;
using Tw.Localization.Requests;

public class ProductDisplayService(IEntityTranslationService translationService)
{
    // ...
}
```

顶层实体翻译查询要求调用方显式传递 `CancellationToken`。HTTP 入口可使用 action 或 endpoint 参数，后台任务和消息消费者应将其入口令牌逐层传入。

### GetFieldAsync：单字段查找

```csharp
public static async ValueTask<string?> GetProductNameAsync(
    IEntityTranslationService translationService,
    CancellationToken cancellationToken)
{
    var lookup = new EntityTranslationLookup(
        Key: new EntityTranslationKey("Product", "42", "Name"),
        Context: new LocalizationContext("zh-Hans"));

    var translatedName = await translationService.GetFieldAsync(
        lookup,
        cancellationToken);

    if (translatedName is null)
    {
        // 未找到翻译，使用实体原字段值
    }

    return translatedName;
}
```

`GetFieldAsync` 返回 `string?`：找到翻译时返回翻译文本，未找到时返回 `null`。框架不抛出异常也不返回占位值；调用方负责回退到原字段。

`EntityTranslationLookup`（`Tw.Localization.Requests`）构造参数：

| 参数 | 类型 | 说明 |
|------|------|------|
| `Key` | `EntityTranslationKey` | 实体类型、实体 ID、字段名称的复合键 |
| `Context` | `LocalizationContext` | 目标文化、租户标识和回退策略 |

### GetFieldsAsync：批量字段查询

批量查询多个实体字段时，使用 `GetFieldsAsync` 以单次存储访问替代多次 `GetFieldAsync`，避免 N+1：

```csharp
public static async ValueTask<IReadOnlyDictionary<EntityTranslationKey, EntityTranslation>>
    GetDisplayTranslationsAsync(
        IEntityTranslationService translationService,
        CancellationToken cancellationToken)
{
    var keys = new List<EntityTranslationKey>
    {
        new("Product", "1", "Name"),
        new("Product", "1", "Description"),
        new("Product", "2", "Name"),
        new("Category", "10", "Title"),
    };

    var batchQuery = new EntityTranslationBatchQuery(
        Keys: keys,
        Context: new LocalizationContext("zh-Hans"));

    var results = await translationService.GetFieldsAsync(
        batchQuery,
        cancellationToken);

    foreach (var key in keys)
    {
        if (results.TryGetValue(key, out var translation))
        {
            // 使用 translation.Value
        }
        else
        {
            // 该字段无翻译，使用实体原字段值
        }
    }

    return results;
}
```

`EntityTranslationBatchQuery`（`Tw.Localization.Requests`）构造参数：

| 参数 | 类型 | 说明 |
|------|------|------|
| `Keys` | `IReadOnlyList<EntityTranslationKey>` | 要批量查找的复合键列表 |
| `Context` | `LocalizationContext` | 共用的本地化上下文 |

返回值是以 `EntityTranslationKey` 为键的只读字典。**未找到翻译的键不出现在字典中**，需用 `TryGetValue` 判断。

`EntityTranslation` 的主要属性：

| 属性 | 类型 | 说明 |
|------|------|------|
| `EntityType` | `string` | 实体类型名称 |
| `EntityId` | `string` | 实体 ID |
| `FieldName` | `string` | 字段名称 |
| `CultureName` | `string` | 翻译所属的 BCP 47 文化名称 |
| `Value` | `string` | 翻译文本 |
| `TenantId` | `string?` | 翻译所属租户；`null` 表示全局翻译 |

## 批量查询与 N+1

`GetFieldsAsync` 将多个字段的查找合并为对 `IEntityTranslationStore.GetListAsync` 的单次调用，由存储实现在一次数据库往返中返回所有结果。在渲染列表页、导出或批处理场景下，应使用 `GetFieldsAsync` 而非在循环内调用 `GetFieldAsync`。

## 注意事项

- 未找到翻译时，`GetFieldAsync` 返回 `null`，`GetFieldsAsync` 结果字典中不包含对应键；框架不回退到原字段、不抛异常。
- 框架不自动将翻译结果覆盖实体原字段；应用层负责决定何时、如何应用翻译。
- `EntityTranslationKey` 使用值相等（record 语义），可安全用作字典键。
- 租户隔离通过 `LocalizationContext.TenantId` 传递；`null` 表示查询全局翻译。
