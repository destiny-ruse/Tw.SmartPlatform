# 文本本地化使用指南

## 能力定位

`ITextLocalizer`（命名空间 `Tw.Localization`）提供基于资源名称的文本键查找能力，支持静态 JSON 资源和动态文本覆盖两个来源。静态资源在启动时加载；动态来源通过实现 `IDynamicTextStore` 接入，优先级高于静态 JSON。

## DI 注册

```csharp
using Microsoft.Extensions.DependencyInjection;
using Tw.Localization;

services.AddLocalization(options =>
{
    options.DefaultCulture = "zh-Hans";
    options.SupportedCultures.Add("zh-Hans");
    options.SupportedCultures.Add("en-US");
    options.JsonResourcePaths.Add("Resources/app.zh-Hans.json");
    options.JsonResourcePaths.Add("Resources/app.en-US.json");
});
```

`LocalizationOptions` 的主要配置项：

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `DefaultCulture` | `string` | `"en-US"` | 所有回退均失败时使用的文化（BCP 47）|
| `SupportedCultures` | `List<string>` | 空列表 | 支持的 BCP 47 文化名称列表（必填）|
| `JsonResourcePaths` | `List<string>` | 空列表 | JSON 资源文件的绝对或相对路径列表 |
| `FallbackToParentCultures` | `bool` | `true` | 是否在目标文化缺失时回退到父文化 |
| `FallbackToDefaultCulture` | `bool` | `true` | 是否在父文化回退后仍缺失时再回退到默认文化 |
| `MissingTextBehavior` | `MissingTextBehavior` | `ReturnKey` | 键缺失时的行为 |
| `AllowDuplicateResourceKeys` | `bool` | `true` | 是否允许不同资源文件出现同名键 |

`Validate()` 在 `AddLocalization` 内自动调用：`DefaultCulture` 必须是合法的 BCP 47 名称，`SupportedCultures` 不能为空，且 `DefaultCulture` 必须包含在 `SupportedCultures` 中。

## JSON 资源文件格式

每个 JSON 文件对应一种文化，根对象包含两个必填字段：

- `culture`（字符串）：文件所属 BCP 47 文化名称。
- `texts`（对象）：文本键值对；支持嵌套对象，嵌套层级以 `__` 拼接为扁平键。

叶子值必须是字符串；非字符串叶子值（数字、布尔、数组等）会在解析时抛出 `TwConfigurationException`。

```json
{
  "culture": "zh-Hans",
  "texts": {
    "Common": {
      "Save": "保存",
      "Cancel": "取消"
    },
    "Menu": {
      "Home": "首页",
      "Settings": "设置"
    },
    "Validation__Required": "此字段不能为空"
  }
}
```

上述 JSON 展开后得到如下键：

| 扁平键 | 值 |
|--------|----|
| `Common__Save` | 保存 |
| `Common__Cancel` | 取消 |
| `Menu__Home` | 首页 |
| `Menu__Settings` | 设置 |
| `Validation__Required` | 此字段不能为空 |

### 资源名称推断规则

资源名称（`resourceName`）由文件名在**第一个 `.` 之前**的部分决定。例如：

| 文件路径 | 推断的资源名称 |
|----------|---------------|
| `Resources/app.zh-Hans.json` | `app` |
| `Resources/orders.en-US.json` | `orders` |
| `Resources/common.json` | `common` |

查询时传入的 `resourceName` 必须与此推断值精确匹配（区分大小写）。

## 使用 ITextLocalizer 查找文本

### 注入与上下文构造

```csharp
using Tw.Localization;

public class MyService(ITextLocalizer localizer)
{
    public async ValueTask<string> GetLabelAsync(string key, string cultureName)
    {
        var context = new LocalizationContext(cultureName);
        var result = await localizer.GetAsync("app", key, context);

        if (result.ResourceNotFound)
        {
            // 未找到，result.Value == key
        }

        return result.Value;
    }
}
```

`LocalizationContext` 构造参数：

| 参数/属性 | 类型 | 说明 |
|-----------|------|------|
| `cultureName`（构造参数） | `string` | 目标 BCP 47 文化名称，必填 |
| `TenantId`（init 属性） | `string?` | 租户标识；`null` 表示全局范围 |
| `FallbackToParentCultures`（init 属性） | `bool` | 是否允许回退到父文化，默认 `true` |
| `FallbackToDefaultCulture`（init 属性） | `bool` | 是否允许再回退到默认文化，默认 `true` |

带租户的上下文构造示例：

```csharp
var context = new LocalizationContext("zh-Hans")
{
    TenantId = "tenant-001"
};
```

### GetAsync：单条查找

```csharp
ValueTask<LocalizedText> result = await localizer.GetAsync(
    resourceName: "app",
    name: "Common__Save",
    context: new LocalizationContext("zh-Hans"));
```

返回值 `LocalizedText` 的主要属性：

| 属性 | 类型 | 说明 |
|------|------|------|
| `ResourceName` | `string` | 查询使用的资源名称 |
| `Name` | `string` | 查询使用的键名 |
| `Value` | `string` | 解析到的文本值；未找到时等于 `Name` |
| `CultureName` | `string` | 实际使用的 BCP 47 文化名称 |
| `ResourceNotFound` | `bool` | `true` 表示未找到对应资源 |
| `Source` | `LocalizedTextSource` | 来源渠道（`StaticJson`、`Dynamic`、`ParentCulture` 等）|

### GetAllAsync：获取资源集合内全部文本

```csharp
IReadOnlyList<LocalizedText> allTexts = await localizer.GetAllAsync(
    resourceName: "app",
    context: new LocalizationContext("zh-Hans"));
```

返回该资源名称下当前上下文能解析到的所有文本条目。高优先级来源的同名键会覆盖低优先级来源的同名键。

## 键缺失行为

当所有来源和回退链均未找到目标键时，`GetAsync` 返回一个特殊实例：

- `ResourceNotFound == true`
- `Value == name`（等于传入的键名）
- `Source == LocalizedTextSource.NotFound`

系统级回退行为受 `LocalizationOptions.MissingTextBehavior` 控制（`ReturnKey` / `ReturnEmptyString` / `ReturnKeyAndRecordDiagnostic`），但 `ResourceNotFound` 标志始终为 `true`，方便调用方做额外处理。

## 扩展点：IDynamicTextStore

动态文本存储允许从数据库或其他运行时可写数据源提供文本，其优先级**高于**静态 JSON。框架本身**不**注册 `IDynamicTextStore`；若需启用动态覆盖，业务应用须实现该接口并通过 `DynamicTextContributor` 接入管道。

`IDynamicTextStore` 定义（命名空间 `Tw.Localization`）：

```csharp
// 单条查找
ValueTask<LocalizedText?> FindAsync(TextLookupRequest request, CancellationToken ct = default);

// 批量获取资源集合下全部条目
ValueTask<IReadOnlyList<LocalizedText>> GetListAsync(TextFillRequest request, CancellationToken ct = default);
```

`TextLookupRequest`（`Tw.Localization.Requests`）包含 `ResourceName`、`Name`、`Context`（`LocalizationContext`）和 `CandidateCultureNames`（按优先级排列的候选文化链）。`FindAsync` 返回 `null` 表示动态存储中无对应条目，框架会继续尝试静态 JSON。

将动态存储接入管道的注册示例：

```csharp
using Microsoft.Extensions.DependencyInjection;
using Tw.Localization;

// 先注册自定义动态存储（必须在 AddLocalization 之前）
services.AddSingleton<IDynamicTextStore, MyDynamicTextStore>();

// 再注册本地化核心，并手动将动态贡献者加入管道
services.AddLocalization(options => { /* ... */ });

// 以比 JsonTextResourceContributor (priority=0) 更高的优先级注册动态贡献者
services.AddSingleton<ITextResourceContributor>(sp =>
    new DynamicTextContributor(sp.GetRequiredService<IDynamicTextStore>(), priority: 10));
```

> `DynamicTextContributor`（命名空间 `Tw.Localization`）的 `priority` 数值越大，执行越优先。JSON 贡献者的 `priority` 为 `0`；动态贡献者的 `priority` 设置为正数即可覆盖静态 JSON。

## 注意事项

- `JsonResourcePaths` 中的路径在 `AddLocalization` 调用时立即读取并解析；文件不存在或格式不合规时抛出 `TwConfigurationException`，阻止应用启动。
- 同一资源文件内若出现重复的嵌套展开键，后解析的值覆盖先解析的值；应避免同一文件内出现重复键。
- `GetAllAsync` 的结果不保证顺序；若需有序呈现，调用方自行排序。
