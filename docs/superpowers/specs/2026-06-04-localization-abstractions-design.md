# 多语言系统抽象设计

## 状态

已确认，进入实现计划前的规格文档

## 目标

设计一套独立可选的多语言共享包，同时覆盖系统文案本地化和业务实体内容翻译。多语言能力不进入 `Tw.Core` 和 `Tw.AspNetCore` 内置能力，由 `Tw.Localization` 承载框架无关、ORM 无关的接口、模型、默认静态 JSON 支持和运行时编排，由 `Tw.Localization.AspNetCore` 承载可选 Web 宿主适配；业务应用负责数据库实现、权限、审计、管理端 API 和迁移。

设计必须满足以下约束：

- 代码命名不得出现参考框架名称
- `Tw.Localization` 不依赖 ASP.NET Core、EF Core 或具体 ORM
- `Tw.Localization.AspNetCore` 只提供 Web 本地化宿主适配
- `Tw.Core` 和 `Tw.AspNetCore` 不内置多语言实现
- 多租户作为一等维度支持，单租户应用传空租户
- 取消令牌与既有 `ICancellationTokenProvider` 集成，不新增独立取消令牌抽象
- 系统文案和业务实体翻译共享语言上下文与回退策略，但使用不同存储接口

## 非目标

- 不提供 EF Core 表模型、DbContext、迁移或默认数据库实现
- 不提供管理后台页面
- 不把业务实体翻译写入系统文案 key-value 资源表
- 不通过全局静态入口或 Service Locator 访问本地化能力
- 不自动修改或持久化领域实体原字段

## 参考源码结论

本地参考源码调研后的设计取舍如下：

- 资源、贡献源和回退链模型适合系统文案本地化，可用于组合静态 JSON 与动态覆盖
- JSON 静态资源支持嵌套 key 展开、拆分文件合并和稳定覆盖顺序
- 外部动态源应作为覆盖层，而不是替代静态资源定义
- 请求语言解析和 ASP.NET Core 官方本地化生态适配应放在 Web 边界
- 业务实体内容翻译与系统文案生命周期不同，应单独建模和批量查询

## 包边界

### `Tw.Core`

保留基础原语与上下文能力，作为 `Tw.Localization` 的基础依赖。职责包括：

- `ICancellationTokenProvider`
- `Check` 入参校验
- `TwConfigurationException` 等基础异常
- 通用值对象、扩展和工具

`Tw.Core` 不新增 `Tw.Localization` 命名空间，不承载多语言模型、接口、JSON 资源解析或翻译编排。

### `Tw.Localization`

新增独立共享包 `backend/dotnet/BuildingBlocks/src/Tw.Localization`，公开能力命名空间 `Tw.Localization`，职责包括：

- 语言信息与语言上下文
- culture 校验和回退链
- 系统文案资源模型
- JSON 静态资源贡献源
- 动态系统文案仓储接口
- 业务实体翻译仓储接口
- 本地化缓存失效契约
- 与 `ICancellationTokenProvider` 的执行上下文集成

`Tw.Localization` 依赖 `Tw.Core`，不引用 `Microsoft.AspNetCore.*` 和 `Microsoft.EntityFrameworkCore*`。

### `Tw.AspNetCore`

保留 ASP.NET Core 宿主通用集成能力，作为 `Tw.Localization.AspNetCore` 的基础依赖。职责包括：

- HTTP 请求取消令牌 provider
- Web 通用中间件、过滤器、模型绑定和结果封装
- 宿主启动与通用依赖注入扩展
- `AddWebIntegration(...)` 聚合入口

`Tw.AspNetCore` 不新增 `Tw.AspNetCore.Localization` 命名空间，不承载请求语言解析、`IStringLocalizer` 适配或本地化 DTO。

### `Tw.Localization.AspNetCore`

新增独立可选 Web 适配包 `backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore`，公开能力命名空间 `Tw.Localization.AspNetCore`，职责包括：

- 请求语言解析中间件
- Web 请求语言上下文写入
- `IStringLocalizer` 和 `IStringLocalizer<T>` 适配
- MVC、DataAnnotations 和视图本地化接入
- 运行时本地化导出 DTO 契约建议

`Tw.Localization.AspNetCore` 依赖 `Tw.Localization` 和 `Tw.AspNetCore`，不提供管理 API 和数据库实现，不引用 `Microsoft.EntityFrameworkCore*`。

### 业务应用

业务应用实现以下职责：

- 实现动态系统文案仓储
- 实现业务实体翻译仓储
- 定义数据库表、索引、审计字段、租户字段和迁移
- 实现权限、审计、管理端 API 和导入导出
- 决定动态覆盖和业务翻译的缓存策略

## 服务注册与命名规则

本能力的 `IServiceCollection` 扩展按功能点命名，不使用公司前缀、包名前缀、程序集名前缀或参考框架名称表达注册入口。

命名规则：

- 扩展类命名空间使用当前程序集根命名空间或其功能命名空间，例如 `Tw.Localization`、`Tw.Localization.AspNetCore`、`Tw.Context`、`Tw.AspNetCore.Context`
- 自有扩展类不放入 `Microsoft.Extensions.DependencyInjection` 命名空间
- 扩展类按功能拆分，例如 `LocalizationServiceCollectionExtensions`、`CancellationTokenServiceCollectionExtensions`、`WebIntegrationServiceCollectionExtensions`
- 功能级注册方法使用能力名称，例如 `AddLocalization(...)`、`AddCancellationTokenProvider(...)`
- `Tw.AspNetCore` 提供通用 Web 集成聚合注册方法 `AddWebIntegration(...)`，不包含可选多语言注册
- `Tw.Localization.AspNetCore` 提供 Web 多语言入口注册方法 `AddLocalization(...)`，内部调用 `Tw.Localization` 核心注册以及 `Tw.AspNetCore` 所需通用 Web 集成，避免业务应用必须了解多个功能注册顺序
- 聚合入口不替代功能级注册方法；功能级注册方法必须能够单独测试和按需组合
- 当功能方法名与 .NET 官方扩展同名时，通过本项目功能命名空间隔离，不通过 `Microsoft.Extensions.DependencyInjection` 命名空间抢占官方扩展

本次实现同时整改既有不规范命名：

- `Tw.Core` 当前 `AddTwCore()` 拆分为功能级注册入口，取消令牌 provider 注册归入 `Tw.Context.CancellationTokenServiceCollectionExtensions.AddCancellationTokenProvider(...)`
- `Tw.AspNetCore` 当前 `AddTwAspNetCore()` 拆分为功能级注册入口，HTTP 请求取消令牌 provider 注册归入 `Tw.AspNetCore.Context.CancellationTokenServiceCollectionExtensions.AddHttpContextCancellationTokenProvider(...)`
- `Tw.AspNetCore` 新增入口聚合注册 `Tw.AspNetCore.WebIntegrationServiceCollectionExtensions.AddWebIntegration(...)`
- `TwCoreServiceCollectionExtensions`、`TwAspNetCoreServiceCollectionExtensions` 这类宽泛扩展类名退出目标 API
- 自有扩展类从 `Microsoft.Extensions.DependencyInjection` 命名空间迁移到对应程序集或功能命名空间

## 计划文件对齐结论

当前 `docs/superpowers/plans` 中的多语言计划审查结论：

- `2026-06-04-localization-1-di-naming-remediation.md` 与本设计中的 DI 命名整改一致，保留为前置计划
- `2026-06-04-localization-2-core.md` 把多语言核心落入 `Tw.Core`，与独立可选包边界不一致，不再作为实施依据
- `2026-06-04-localization-3-aspnetcore.md` 把 Web 多语言适配落入 `Tw.AspNetCore`，与独立可选包边界不一致，不再作为实施依据

实现计划阶段采用以下新计划结构：

- 新 Plan 2：`Tw.Localization` 核心包实现
- 新 Plan 3：`Tw.Localization.AspNetCore` Web 可选适配包实现

## 核心模型

### `LanguageInfo`

表示可用语言。

字段：

- `CultureName`：标准 culture 名称，例如 `zh-Hans`、`en-US`
- `UiCultureName`：界面 culture 名称，缺省等于 `CultureName`
- `DisplayName`：展示名称
- `IsEnabled`：是否启用
- `SortOrder`：排序值

### `LocalizationContext`

表示一次本地化查询上下文。

字段：

- `CultureName`
- `TenantId`
- `FallbackToParentCultures`
- `FallbackToDefaultCulture`

`TenantId` 采用字符串或轻量值对象，不绑定租户框架。单租户应用传空值。

### `LocalizationOptions`

表示本地化配置。

配置项：

- 默认 culture
- 支持语言列表
- JSON 静态资源路径
- JSON 文件变更监听开关
- 缺失文本策略，取值为返回 key（默认）、返回空串或返回 key 并记录诊断，不含抛异常
- 默认回退策略
- 资源重复 key 策略

启动阶段必须校验默认 culture、支持语言和资源路径。生产环境缺失必需配置时启动失败。

### `LocalizedText`

表示系统文案查询结果。

字段：

- `Name`
- `Value`
- `CultureName`
- `ResourceName`
- `ResourceNotFound`
- `Source`

`Source` 取值表达来源，例如静态 JSON、动态覆盖、父级 culture、默认 culture、基础资源。

### `EntityTranslation`

表示业务实体字段翻译。

字段：

- `EntityType`
- `EntityId`
- `FieldName`
- `CultureName`
- `Value`
- `TenantId`

`EntityId` 是不透明字符串，框架不解析数据库主键结构。

### 请求与查询模型

接口签名使用以下查询模型，均为不可变值对象。

系统文案：

- `TextLookupRequest`：`ResourceName`、`Name`、`LocalizationContext`，以及由 context 展开的候选 culture 集合（当前、父级链、默认）。用于单 key 查询。
- `TextFillRequest`：`ResourceName`、`LocalizationContext`，以及展开的候选 culture 集合。用于批量填充。

业务实体翻译：

- `EntityTranslationKey`：`EntityType`、`EntityId`、`FieldName` 组成的值对象，作为批量结果字典键，定义相等性。
- `EntityTranslationLookup`：单个 `EntityTranslationKey` + `LocalizationContext`。用于单实体单字段查询。
- `EntityTranslationQuery`：`EntityType` + `EntityId` 集合或 `FieldName` 集合 + `LocalizationContext`。供 `IEntityTranslationStore` 批量读取，store 一次返回全部命中翻译，不做回退。
- `EntityTranslationBatchQuery`：`EntityTranslationKey` 集合 + `LocalizationContext`。供 `IEntityTranslationService` 批量查询并执行回退与结果组装。

候选 culture 集合在编排层（`ITextLocalizer`、`IEntityTranslationService`）由 `LocalizationContext` 与 `LocalizationOptions` 回退策略展开后传入 store，store 不重复计算回退。

## 核心接口

### 系统文案接口

```csharp
public interface ITextLocalizer
{
    ValueTask<LocalizedText> GetAsync(
        string resourceName,
        string name,
        LocalizationContext context,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<LocalizedText>> GetAllAsync(
        string resourceName,
        LocalizationContext context,
        CancellationToken cancellationToken = default);
}

public interface ITextResourceContributor
{
    int Priority { get; }

    ValueTask<LocalizedText?> GetOrNullAsync(
        TextLookupRequest request,
        CancellationToken cancellationToken = default);

    ValueTask FillAsync(
        TextFillRequest request,
        IDictionary<string, LocalizedText> texts,
        CancellationToken cancellationToken = default);
}

public interface IDynamicTextStore
{
    ValueTask<LocalizedText?> FindAsync(
        TextLookupRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<LocalizedText>> GetListAsync(
        TextFillRequest request,
        CancellationToken cancellationToken = default);
}
```

`ITextLocalizer` 编排贡献源和回退链。`ITextResourceContributor` 表示静态 JSON、动态覆盖等来源。`IDynamicTextStore` 由业务应用实现。

贡献源遍历方向规则：

- `Priority` 定义唯一规范序，数值约定大者优先
- `GetOrNullAsync` 按优先级从高到低遍历，首个非空命中即返回
- `FillAsync` 按优先级从低到高遍历，高优先级结果覆盖低优先级结果

`IDynamicTextStore` 调用契约：编排器一次性把回退链所需的全部候选维度（候选 culture 集合、当前租户与全局两层、`ResourceName`、单 key 查询附带 `Name`）封装进 `TextLookupRequest` / `TextFillRequest` 传入，store 一次返回全部命中项，编排器在内存中按回退链优先级裁决。单 key 查询不得对 store 逐级多次往返。

### 业务实体翻译接口

```csharp
public interface IEntityTranslationStore
{
    ValueTask<IReadOnlyList<EntityTranslation>> GetListAsync(
        EntityTranslationQuery query,
        CancellationToken cancellationToken = default);
}

public interface IEntityTranslationService
{
    ValueTask<string?> GetFieldAsync(
        EntityTranslationLookup lookup,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyDictionary<EntityTranslationKey, EntityTranslation>> GetFieldsAsync(
        EntityTranslationBatchQuery query,
        CancellationToken cancellationToken = default);
}
```

`IEntityTranslationStore` 由业务应用实现。`IEntityTranslationService` 负责批量查询、回退和结果组装。

### 执行上下文集成

多语言接口允许调用方显式传入 `CancellationToken`，同时与现有 `ICancellationTokenProvider` 集成。

实现规则：

- 显式传入的非默认 token 优先
- 未传、`default` 或 `CancellationToken.None` 时使用 `ICancellationTokenProvider.Token`
- 实现使用既有扩展统一取值

```csharp
var token = _cancellationTokenProvider.FallbackToProvider(cancellationToken);
```

不新增 `CancellationTokenResolver`、`CancellationTokenContext` 或其他独立取消令牌抽象。

## 系统文案流

### JSON 静态资源

默认 JSON 文件格式：

```json
{
  "culture": "zh-Hans",
  "texts": {
    "Menu": {
      "Dashboard": "控制台"
    },
    "Validation__Required": "必填"
  }
}
```

规则：

- `culture` 必须是有效 culture 名称
- `texts` 叶子值只能是字符串；对象仅用于分组
- 文案值不支持数字、布尔值、空值和数组，解析到非字符串叶子值时按资源格式错误处理
- 嵌套对象扁平化为 `Menu__Dashboard`
- 同一 culture 可拆分多个 JSON 文件
- 文件按稳定顺序合并
- 同 key 后加载值覆盖先加载值
- JSON 文件只作为默认文案，不作为运行期管理数据

### 动态覆盖

动态覆盖通过 `IDynamicTextStore` 读取业务应用持久化的覆盖文案。

覆盖维度：

- `TenantId`
- `ResourceName`
- `CultureName`
- `Name`

动态覆盖优先级高于 JSON 静态文案。动态覆盖的空字符串是合法文本，不表示删除覆盖。恢复默认文案时业务应用删除覆盖记录。

### 回退链

单 key 查询按高到低查找：

1. 当前租户当前 culture 动态覆盖
2. 全局当前 culture 动态覆盖
3. 当前 culture 静态 JSON
4. 当前租户父级 culture 动态覆盖
5. 全局父级 culture 动态覆盖
6. 父级 culture 静态 JSON
7. 当前租户默认 culture 动态覆盖
8. 全局默认 culture 动态覆盖
9. 默认 culture 静态 JSON
10. 基础资源
11. 返回 key，并标记 `ResourceNotFound = true`

批量填充按低到高覆盖：

1. 基础资源
2. 默认 culture 静态 JSON
3. 全局默认 culture 动态覆盖
4. 当前租户默认 culture 动态覆盖
5. 父级 culture 静态 JSON
6. 全局父级 culture 动态覆盖
7. 当前租户父级 culture 动态覆盖
8. 当前 culture 静态 JSON
9. 全局当前 culture 动态覆盖
10. 当前租户当前 culture 动态覆盖

## 业务实体翻译流

业务实体翻译单独建模，不进入系统文案资源表。

标识维度：

- `EntityType`：稳定实体类型名，业务应用注册别名，例如 `Product`、`Article`、`Category`
- `EntityId`：不透明字符串
- `FieldName`：可翻译字段名，例如 `Name`、`Title`、`Summary`
- `CultureName`
- `TenantId`

查询模式：

- 单实体单字段
- 单实体多字段
- 多实体同字段
- 多实体多字段

回退链：

1. 当前租户当前 culture 翻译
2. 全局当前 culture 翻译
3. 当前租户父级 culture 翻译
4. 全局父级 culture 翻译
5. 当前租户默认 culture 翻译
6. 全局默认 culture 翻译
7. 返回 `null`

业务实体翻译不自动覆盖实体原字段。框架提供 DTO 映射辅助，例如翻译结果应用器或扩展方法。调用方决定缺失翻译时使用原始实体字段、显示缺失状态或走业务特定降级。

## Web 集成

### 请求语言解析

`Tw.Localization.AspNetCore` 提供：

- `Tw.Localization.AspNetCore.LocalizationServiceCollectionExtensions.AddLocalization(...)`
- `Tw.Localization.AspNetCore.LocalizationApplicationBuilderExtensions.UseLocalization(...)`

默认语言来源顺序：

1. route culture，例如 `/{culture}/...`
2. query，例如 `?culture=zh-Hans`
3. cookie
4. `Accept-Language`
5. `LocalizationOptions.DefaultCulture`

解析结果写入当前 `LocalizationContext` 作用域。业务代码不直接依赖 `HttpContext`。写 cookie 是可选行为，只有 query 或 route 明确切换语言时写入。

### `IStringLocalizer` 适配

`Tw.Localization` 不依赖 `Microsoft.Extensions.Localization`。`Tw.Localization.AspNetCore` 提供适配器：

- `TwStringLocalizerFactory`
- `TwStringLocalizer`
- `TwStringLocalizer<TResource>`

资源名由类型映射得到，可通过特性或 options 注册别名。找不到 key 时返回 key 本身，并设置 `ResourceNotFound = true`，保持 ASP.NET Core 生态预期。

### 运行时导出 API

核心只提供 `ITextLocalizer.GetAllAsync`。`Tw.Localization.AspNetCore` 可提供 DTO 契约和扩展示例，不强制注册控制器。

推荐业务应用暴露只读端点：

```text
GET /api/localization/resources/{resourceName}?culture=zh-Hans
GET /api/localization/resources/{resourceName}?culture=zh-Hans&onlyDynamic=true
```

`onlyDynamic=true` 用于前端先加载静态包，再叠加数据库覆盖。管理 API 由业务应用按权限和审计要求实现。

## 缓存与失效

静态 JSON 资源缓存到内存。开启文件监听时，文件变化清空对应资源缓存。

动态覆盖缓存由业务应用决定。核心提供缓存失效契约：

- `ILocalizationCacheInvalidator`
- `ILocalizationChangeToken`

缓存分层：

- 语言列表缓存
- 资源索引缓存
- 静态文本字典缓存
- 动态覆盖缓存
- 业务实体翻译批量查询缓存

单个动态文本变更不得强制重建全部资源缓存。业务应用实现缓存时必须维护租户边界。

## 错误处理

启动阶段失败：

- 默认 culture 非法
- 支持语言包含非法 culture
- JSON 资源路径缺失且配置为必需
- 资源名重复且策略不允许覆盖

运行阶段行为：

- JSON 格式错误抛出 `TwConfigurationException`，消息包含资源路径和 culture，不输出敏感配置值
- 动态仓储异常不被静默吞掉，保留诊断上下文后交给调用边界处理
- 缺失系统文案不是异常，返回 `ResourceNotFound = true`
- 缺失业务实体翻译不是异常，返回 `null`
- 非法 culture 在 Web 边界返回稳定 4xx 错误，业务应用决定响应结构
- 业务实体翻译查不到时不返回空字符串

## 测试策略

### `Tw.Localization.Tests`

覆盖：

- JSON 解析
- 嵌套 key 展开
- 重复 key 覆盖
- 拆分文件合并
- culture 校验
- 当前 culture 回退
- 父级 culture 回退
- 默认 culture 回退
- 基础资源回退
- 动态覆盖优先级
- 租户覆盖优先级
- 缺失文案结果
- 业务实体翻译批量查询
- 业务实体翻译回退链
- 列表页查询避免 N+1
- 显式 token 与 `ICancellationTokenProvider` 集成

### `Tw.Localization.AspNetCore.Tests`

覆盖：

- route、query、cookie、header、默认 culture 顺序
- query 和 route 切换语言时的 cookie 写入策略
- 当前语言上下文写入
- `IStringLocalizer` 适配
- `IStringLocalizer<TResource>` 资源名映射
- DataAnnotations 本地化接入
- 非法 culture Web 边界处理

仓储测试使用 Fake 实现，不引入 EF Core 或真实数据库。

## 文档与 charter 更新

实现该设计时必须同步更新：

- `backend/dotnet/BuildingBlocks/src/Tw.Localization/package-charter.yaml`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/package-charter.yaml`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/package-charter.yaml`
- `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/package-charter.yaml`
- `docs/engineering-standards/03-project-and-code/language-specific/dotnet-core.md`
- `docs/shared-packages/dotnet/Tw.Localization/README.md`
- `docs/shared-packages/dotnet/Tw.Localization.AspNetCore/README.md`
- `docs/shared-packages/dotnet/Tw.Core/README.md`
- `docs/shared-packages/dotnet/Tw.AspNetCore/README.md`
- `docs/shared-packages/dotnet/README.md`

新增共享包能力文档：

- `docs/shared-packages/dotnet/Tw.Localization/text-localization.md`
- `docs/shared-packages/dotnet/Tw.Localization/entity-translation.md`
- `docs/shared-packages/dotnet/Tw.Localization.AspNetCore/request-localization.md`

能力使用文档采用 How-to Guide。包索引采用 Reference。

## 实现范围

首轮实现包含：

- `Tw.Core`、`Tw.AspNetCore` 既有 DI 命名整改
- `Tw.Localization` 项目、测试项目、package charter 和共享包文档
- `Tw.Localization` 核心模型、接口和默认编排服务
- JSON 静态资源贡献源
- 内存动态文案 store 测试替身
- 内存实体翻译 store 测试替身
- `Tw.Localization.AspNetCore` 项目、测试项目、package charter 和共享包文档
- `Tw.Localization.AspNetCore` 请求语言解析
- `IStringLocalizer` 适配
- 单元测试和共享包使用文档

首轮实现不包含：

- EF Core 实现
- 数据库迁移
- 管理端页面
- 管理 API
- 前端应用本地化包
- 分布式缓存实现

## 兼容性

新增能力作为 `Tw.Localization` 和 `Tw.Localization.AspNetCore` 的公开能力进入独立可选包。`Tw.Core` 和 `Tw.AspNetCore` 的取消令牌 provider 运行时行为、ASP.NET Core 通用集成行为保持不变。

`Tw.Core` 和 `Tw.AspNetCore` 当前未被任何具体微服务项目引用，也未发布为 NuGet 包。在此采纳前阶段，既有不规范 DI 注册命名直接做破坏性整改：删除 `AddTwCore()`、`AddTwAspNetCore()` 与 `TwCoreServiceCollectionExtensions`、`TwAspNetCoreServiceCollectionExtensions`，并把扩展类迁出 `Microsoft.Extensions.DependencyInjection` 命名空间，不保留 `[Obsolete]` 废弃转发壳。一旦该包被微服务引用或发布 NuGet，再恢复按 charter `compatibility` 承诺处理破坏性变更。

本次整改同步更新相关包的 `package-charter.yaml`：

- `Tw.Core` 保留基础能力 `Tw.Context`，不登记 `Tw.Localization`
- `Tw.AspNetCore` 保留通用 Web 能力 `Tw.AspNetCore`、`Tw.AspNetCore.Context`，不登记 `Tw.Localization.AspNetCore`
- `Tw.Localization` 登记 `Tw.Localization`
- `Tw.Localization.AspNetCore` 登记 `Tw.Localization.AspNetCore`

四个包的 `public_capabilities` 必须互斥。业务应用只有引用并注册 `Tw.Localization` 或 `Tw.Localization.AspNetCore` 时才启用多语言能力。

公共 API 命名使用 `Tw.Localization`、`TextLocalizer`、`TextResource`、`EntityTranslation`、`DynamicTextStore` 等项目自有术语，不使用参考框架名称。
