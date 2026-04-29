# DI 自动注册与 AOP 框架设计文档

**日期**：2026-04-29  
**技术栈**：.NET 10+、Autofac、Autofac.Extensions.DependencyInjection、Autofac.Extras.DynamicProxy、Castle.Core、Castle.Core.AsyncInterceptor、Microsoft.Extensions.Options、gRPC for .NET（P2）  
**目标**：封装公司内部通用的 DI 自动注册与 AOP 拦截能力，支持 Web API、gRPC、CAP 消息队列消费、后台服务等多种程序入口；首批执行计划以 DI 自动注册、Options 自动注册、Castle Service AOP 与 MVC/Web API Adapter 为交付边界，gRPC Adapter 作为 P2 阶段，CAP/Worker Adapter 保留在同一总体设计内分期落地。

---

## Status

待审批。设计稿已完成第二轮审查（基于 ABP vNext 对比分析与架构评审），本轮修订内容包括：Entry/Service 双链并行架构澄清（§ 5.6）、删除 `InterceptorScope.All`、`IAutoMatchInterceptor` 新增 `MatchEntry` 方法（§ 3.6）、`[Intercept]` 与 Controller 边界说明（§ 6.5）、`ConfigurationSectionAttribute.AllowMultiple` 变更影响评估（§ 3.4）、低拓扑替换与高拓扑普通实现的交叉边界规则（§ 5.3）、`TypeFinder`/`ReflectionCache` 复用策略（§ 4.1）；等待最终审批后进入实现计划阶段。

---

## Standards

本设计遵循以下工程规范：

1. `rules.naming-dotnet#rules`，版本 `1.1.0`，`docs/standards/rules/naming-dotnet.md`：类型、成员、异步方法命名遵循 .NET 约定。  
   **豁免**：`InterceptorBase.InterceptAsyncEnumerable<T>()` 返回 `IAsyncEnumerable<T>`（惰性流，非 Task/ValueTask），不属于规则 4 所定义的"异步方法"，名称中的 `Async` 描述流的异步特性而非方法返回形态，属于合理命名豁免。
2. `rules.comments-dotnet#rules`，版本 `1.3.0`，`docs/standards/rules/comments-dotnet.md`：公共 API 需提供说明用途和关键契约的 XML 注释。
3. `rules.repo-layout#rules`，版本 `1.1.0`，`docs/standards/rules/repo-layout.md`：源码与测试目录边界清晰，测试路径与被测源码对应。
4. `rules.test-strategy#rules`，版本 `1.1.0`，`docs/standards/rules/test-strategy.md`：验证需声明测试层级与未覆盖风险。
5. `rules.dependency-policy#rules`，版本 `1.1.0`，`docs/standards/rules/dependency-policy.md`：依赖必须可信、锁版本、可溯源。
6. `rules.configuration#rules`，版本 `1.1.0`，`docs/standards/rules/configuration.md`：配置来源、必填校验、敏感信息输出和启动失败语义必须明确。
7. `rules.error-handling#rules`，版本 `1.2.0`，`docs/standards/rules/error-handling.md`：框架显式抛出的异常必须分类清楚、消息安全且使用简体中文。
8. `processes.dependency-onboarding#flow` / `processes.dependency-onboarding#rules`，版本 `1.0.0`，`docs/standards/processes/dependency-onboarding.md`：新增运行时依赖的关键结论、评审和后续责任必须可追踪。

---

## Dependency Admission

本设计会为 BuildingBlocks 首次引入 Autofac/Castle 相关运行时依赖。实现计划必须在修改 `Directory.Packages.props` 前完成依赖准入记录，并把结论写入实现计划或 PR 描述。所有新增包版本必须通过中央包管理固定，并通过 `packages.lock.json` 参与可复现构建；不得使用无上界浮动版本作为生产依赖。

| 依赖 | 用途 | 替代方案与取舍 | 准入要求 |
|---|---|---|---|
| `Autofac` | 最终 DI 容器、生命周期映射、Keyed Service 与注册元数据执行 | 原生 DI 可减少依赖，但拦截器集成、复杂裁决和 Autofac 生态适配不足 | 由 platform 负责版本维护；固定版本；完成许可证、漏洞和破坏性变更检查 |
| `Autofac.Extensions.DependencyInjection` | 提供 `AutofacServiceProviderFactory` 与 .NET Host 集成 | 手写 ServiceProviderFactory 成本高且风险大 | 与 `Autofac` 主版本兼容；集成测试覆盖 Host 启动 |
| `Autofac.Extras.DynamicProxy` | 在 Autofac 注册阶段启用 Castle interface/class interceptor | 自研代理绑定会增加框架代码量和兼容性风险 | 与 `Castle.Core` 版本成组锁定；集成测试覆盖接口代理、类代理禁用与空链跳过 |
| `Castle.Core` | DynamicProxy 代理生成基础能力 | `DispatchProxy` 可减少依赖，但性能、class proxy 和 Autofac 集成能力不足 | 固定版本；确认许可证、维护状态和安全扫描结果 |
| `Castle.Core.AsyncInterceptor` | 提供 Castle DynamicProxy 的异步方法拦截分派，覆盖 sync、`Task`、`Task<T>` 等常见形态 | 自研异步分派胶水代码可减少一个依赖，但容易误处理异常传播、返回值、重复 await 和兼容性细节；优先使用稳定开源包 | 固定版本；确认 Apache-2.0 许可证、维护状态、下载量、下游依赖和漏洞扫描结果；若准入失败才回退内部适配层 |
| `Grpc.AspNetCore` / `Grpc.Core.Api`（P2） | gRPC server interceptor、`GrpcServiceOptions`、`ServerCallContext` 与 streaming RPC 入口适配 | 仅用 ASP.NET Core middleware 可减少依赖面，但无法直接操作 gRPC 层的反序列化消息、返回值和 `ServerCallContext` | P2 阶段准入；固定版本；确认与 .NET 10、ASP.NET Core Host 和许可证策略兼容 |

Microsoft 官方配置能力（`Microsoft.Extensions.Options`、`ConfigurationBinder`、DataAnnotations 验证）沿用 .NET 标准生态，版本随目标框架与中央包管理策略统一控制。所有依赖准入结论必须说明用途、影响范围、替代方案、验证命令和后续维护责任。

---

## Knowledge Alignment

核心能力归属：

- `backend.dotnet.building-blocks.core`（`Tw.Core`）：生命周期接口、注册控制特性、`ICancellationTokenProvider`、`ICurrentCancellationTokenAccessor`、`IInvocationContext` 体系、`InterceptorBase`、自动注册引擎、Castle 代理集成。
- `backend.dotnet.building-blocks.asp-net-core`（`Tw.AspNetCore`）：MVC 入口 Adapter、`HttpContextCancellationTokenProvider`、`AddTwAspNetCoreInfrastructure()` 统一初始化入口，以及 P2 阶段的 gRPC 入口 Adapter 与 `AddGrpcInterceptors()` 扩展。

---

## 一、设计决策概览

| 决策点 | 结论 |
|---|---|
| 程序集归属 | 核心能力在 `Tw.Core`；首批 MVC Adapter、P2 gRPC Adapter 与初始化入口在 `Tw.AspNetCore` |
| Autofac 接入方式 | 使用 `AutofacServiceProviderFactory` 接管最终容器；`IServiceCollection` 阶段只保存配置与注册框架辅助服务，Autofac 注册在 `ContainerBuilder` 阶段执行 |
| 模块化方式 | 禁止业务侧显式声明 Autofac Module，仅框架内部使用 |
| 自动注册触发 | 实现生命周期接口（Transient / Scoped / Singleton） |
| 多实现集合注册 | 默认同服务多实现视为冲突；显式 `[CollectionService]` 才作为 `IEnumerable<T>` 合法集合注册 |
| 配置与选项自动注册 | 优先复用 `IConfigurableOptions` 与 `ConfigurationSectionAttribute`；启动入口程序集支持 `*Options` / `*Settings` 约定扫描；类库程序集需显式标记或实现配置抽象 |
| 程序集扫描范围 | 扫描名称以 `Tw.` 开头的已加载程序集，并默认加入启动入口程序集兜底 |
| 默认暴露规则 | 所有非系统命名空间、非生命周期标记接口；无业务接口时暴露具体类型 |
| Keyed Service Key 类型 | `object`（string / enum / Type 均可） |
| 服务替换优先级 | 拓扑顺序高的程序集胜出；同程序集用 Order 数字；仍冲突则报错 |
| 无显式替换标记的多实现 | 跨程序集：拓扑胜出 + 启动警告；同程序集或不同程序集同层：启动报错 |
| AOP 自动启用 | 全局拦截器 + 拦截器自声明匹配规则（两者并存） |
| 拦截器适用范围 | 拦截器声明 `Entry` 或 `Service` 作用域；两条链严格隔离，同一拦截器只能作用于其中一层 |
| 入口适配方式 | 首批提供 MVC/Web API 入口 Adapter；gRPC Adapter 作为 P2；CAP/Worker Adapter 保留在同一设计稿内，但放入后续执行计划 |
| Castle 代理覆盖 | 默认仅保证接口代理；无业务接口或仅暴露具体类时启动警告，类代理需显式开启且方法必须为 `virtual` |
| 异步拦截 | 按 sync / Task / Task<T> / ValueTask / ValueTask<T> / stream 分派 |
| 全局拦截器抑制 | 通过 `[IgnoreInterceptors]` 特性在类或接口上声明 |
| CancellationToken 传递 | `ICancellationTokenProvider` 可注入任意服务；框架自动从方法参数、Adapter 上下文或 AsyncLocal 填充 |
| 调用上下文扩展 | `IInvocationContext` 通过 `GetFeature<T>()` 提供场景特定数据；`Items` 可写，供拦截器间通信 |
| Options 直接注入 | 初版只支持 Monitor 语义（`DirectInject = true`） |

---

## 二、公共入口 API

### 2.1 Tw.AspNetCore 基础设施入口（推荐）

`AddTwAspNetCoreInfrastructure()` 是 `Tw.AspNetCore` 提供的统一初始化方法。该名称明确表达它初始化 Tw 平台在 ASP.NET Core 中需要的基础设施，而不是代替所有 ASP.NET Core 能力。方法内部依次完成：接管 Autofac 容器、注册 `HttpContextCancellationTokenProvider`、读取 `IHostEnvironment` 配置 `ValidateOnStart` 默认策略、挂载 `AutoRegistrationModule`，并通过 `IConfigureOptions<MvcOptions>` 注册 MVC/Web API 全局入口 Adapter。

```csharp
// 推荐：使用 Tw.AspNetCore 基础设施入口
builder.AddTwAspNetCoreInfrastructure(options =>
{
    options.AddAssemblyOf<Program>();           // 可选；默认已加入启动入口程序集兜底
    options.EnableOptionsAutoRegistration();    // 可选；默认开启，可显式关闭
    options.AddGlobalInterceptor<LoggingInterceptor>(scope: InterceptorScope.Entry);
});
```

公开扩展方法签名：

```csharp
public static WebApplicationBuilder AddTwAspNetCoreInfrastructure(
    this WebApplicationBuilder builder,
    Action<AutoRegistrationOptions>? configure = null);
```

MVC/Web API Adapter 不提供 `UseInterceptors()` 管道阶段 API。Filter 必须在服务注册阶段进入 `MvcOptions.Filters`，避免应用构建完成后才尝试修改 MVC Filter 管线。

### 2.2 P2 gRPC 入口

gRPC Adapter 在 P2 阶段提供独立扩展方法，并在 `AddGrpc` 配置阶段启用：

```csharp
builder.Services.AddGrpc(grpcOptions =>
{
    grpcOptions.AddGrpcInterceptors();
});
```

公开扩展方法签名：

```csharp
public static GrpcServiceOptions AddGrpcInterceptors(this GrpcServiceOptions options);
```

### 2.3 手动组合（非 ASP.NET Core 场景）

`AddAutoRegistration` 与 `UseAutoRegistration` 方法名固定，供非 Web 项目或需要精细控制的场景使用：

```csharp
// Host 阶段 —— 必须使用 Autofac 接管最终容器
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

// 注册阶段 —— 扫描程序集、注册服务、配置拦截器
// 此阶段保存配置与注册框架辅助服务，不直接执行 Autofac 专属注册。
// 必须在 ConfigureContainer 之前调用，否则 UseAutoRegistration 读取配置时将抛出异常。
builder.Services.AddAutoRegistration(options =>
{
    options.AddAssemblyOf<Program>();
    options.EnableOptionsAutoRegistration();
    options.AddGlobalInterceptor<LoggingInterceptor>(scope: InterceptorScope.Service);
});

// Autofac ContainerBuilder 阶段 —— 框架内部挂载 AutoRegistrationModule
// 业务侧不直接声明 Autofac Module，只调用框架提供的扩展方法。
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    containerBuilder.UseAutoRegistration(builder.Services);
});
```

> 若项目未使用 `AutofacServiceProviderFactory`，`AddAutoRegistration` 在启动期抛出明确异常，避免悄悄退化为原生 DI 行为。

---

## 三、核心抽象层

> **命名约定**：以下所有类名、接口名、特性名均为最终命名，`AddTwAspNetCoreInfrastructure`、`AddAutoRegistration`、`UseAutoRegistration`、`AddGrpcInterceptors` 方法名固定。

### 3.1 CancellationToken 基础设施

`ICancellationTokenProvider` 是全局基础设施原语，**与 AOP 框架无直接绑定**，业务服务和框架组件均可注入使用。

```csharp
// 业务服务注入此接口，随处可取当前调用链的 CancellationToken
public interface ICancellationTokenProvider
{
    CancellationToken Token { get; }
}

// 框架与入口层用此接口切换当前 token；Use() 返回 IDisposable，Dispose 时恢复原值
public interface ICurrentCancellationTokenAccessor
{
    IDisposable Use(CancellationToken token);
}
```

**实现层次**：

| 实现类 | 位置 | 说明 |
|---|---|---|
| `NullCancellationTokenProvider` | `Tw.Core` | 默认注册，返回 `CancellationToken.None` |
| `HttpContextCancellationTokenProvider` | `Tw.AspNetCore` | 覆盖注册，读取 `HttpContext.RequestAborted` |

内部使用 `AsyncLocal<CancellationToken?>` 维护 ambient token。`Use()` 替换当前值并在 `Dispose` 时恢复，天然支持异步调用链跨 `await` 传播。

**Token 填充优先级**（框架自动处理，业务无感知）：

```
优先级 1：方法签名中存在 CancellationToken 参数
          → 框架在执行拦截链前调用 accessor.Use(methodArgToken)
          → 方法参数含多个 CancellationToken 时取第一个，启动时记录警告
优先级 2：Entry Adapter 层
          → MVC：HttpContext.RequestAborted（由 HttpContextCancellationTokenProvider 提供）
          → gRPC：ServerCallContext.CancellationToken（由 gRPC Adapter 调用 accessor.Use() 设置）
          → Worker/CAP（后续）：accessor.Use(stoppingToken) 在 item 处理入口调用
优先级 3：CancellationToken.None 兜底（NullCancellationTokenProvider）
```

> **不要直接注入 `CancellationToken` 到构造函数**：`CancellationToken` 是值类型，注入到 Singleton 时在构造时捕获并永久固化，Scoped 服务也只在 scope 创建时固化，均无法感知调用链变化。

### 3.2 生命周期接口

```csharp
public interface ITransientDependency { }
public interface IScopedDependency    { }
public interface ISingletonDependency { }
```

### 3.3 注册控制特性

```csharp
// 覆盖默认暴露规则，显式指定暴露哪些服务
// IncludeSelf = true 时同时暴露具体实现类型
[ExposeServices(typeof(IFoo), typeof(IBar))]
[ExposeServices(typeof(IFoo), IncludeSelf = true)]

// 注册为 Keyed Service，Key 为 object（string / enum / Type 均可）
[KeyedService("premium")]
[KeyedService(OrderType.Premium)]

// 声明替换已有注册
// Order 仅在同程序集冲突时作为数字排序依据（越大越优先）
[ReplaceService]
[ReplaceService(Order = 10)]

// 退出自动注册
[DisableAutoRegistration]

// 合法多实现集合注册；同一 (ServiceType, Key) 下允许多个实现共存并通过 IEnumerable<T> 解析
[CollectionService]
[CollectionService(Order = 10)]

// 注册开放泛型，如 Repository<T> -> IRepository<T>
[ExposeServices(typeof(IRepository<>))]
```

### 3.4 配置与选项控制特性

配置自动注册优先复用仓库已有抽象：

- `IConfigurableOptions` 继续作为可配置 options 类型的轻量标记接口，适合类库程序集声明该类型应参与配置绑定。
- `ConfigurationSectionAttribute` 继续作为配置节名称的稳定声明方式，并在本设计中扩展为 options 自动注册的唯一显式元数据来源。
- 调整后的 `ConfigurationSectionAttribute` 允许同一类型声明多个实例，用于命名选项；默认实例使用 `OptionsName = null`。
- `DirectInject`、`ValidateOnStart` 和 `OptionsName` 均挂在 `ConfigurationSectionAttribute` 上，不再新增独立的 `OptionsAttribute`，避免形成第二套配置标记体系。

> **变更影响评估**：`AllowMultiple` 由 `false` 改为 `true` 属于向后兼容变更，原有单 attribute 声明仍合法。实现前须检索全库 `ConfigurationSectionAttribute` 的所有反射调用点，确认无代码隐式假设"同类型只有一个实例"。

```csharp
// 调整现有特性：AllowMultiple 从 false 改为 true，支持命名选项
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class ConfigurationSectionAttribute(string name) : Attribute
{
    public string Name { get; } = Tw.Core.Check.NotNullOrWhiteSpace(name);
    public string? OptionsName { get; init; }
    public bool? ValidateOnStart { get; init; }
    public bool DirectInject { get; init; }
}

// 现有抽象优先：类库 options 可通过标记接口和配置节特性参与自动注册
[ConfigurationSection("Tw:Redis")]
public sealed class RedisOptions : IConfigurableOptions { }

// 覆盖验证策略
[ConfigurationSection("Tw:Redis", ValidateOnStart = true)]

// 允许构造函数直接注入 TOptions；初版只支持 Monitor 语义
// 直接注入提供"解析时快照"便利，不提供订阅热更新能力
[ConfigurationSection("Tw:Redis", DirectInject = true)]

// 命名选项；OptionsName 非空时不注册 TOptions 本体直接注入
[ConfigurationSection("Redis:Primary", OptionsName = "Primary")]
[ConfigurationSection("Redis:Replica", OptionsName = "Replica")]

// 退出配置/选项自动注册
[DisableOptionsRegistration]
```

同一类型上若声明多个 `ConfigurationSectionAttribute`，`OptionsName = null` 的默认实例至多只能有一个；相同 `OptionsName` 不允许重复声明；违反时启动失败，异常信息包含类型名称和重复的 `OptionsName` 值。

### 3.5 暴露服务默认规则

对每个实现了生命周期接口的类，按以下顺序计算暴露的服务列表：

1. 若标注了 `[ExposeServices(...)]`，使用显式列表
2. 否则，取所有直接声明接口，排除：
   - `System.*` / `Microsoft.*` 命名空间下的接口（如 `IDisposable`）
   - 生命周期标记接口本身
3. 若排除后列表为空，则暴露具体类型本身
4. 若 `IncludeSelf = true`，在上述结果基础上追加具体类型

开放泛型按同一规则处理：若实现类型为开放泛型定义（如 `Repository<>`），且暴露服务也是开放泛型接口（如 `IRepository<>`），则使用 Autofac `RegisterGeneric` 注册。开放泛型与封闭泛型不互相替换；若同一开放泛型服务存在多个实现，仍按冲突裁决或 `[CollectionService]` 处理。

### 3.6 AOP 相关接口与特性

```csharp
// 统一调用上下文，屏蔽 Castle.Core 与各入口 Adapter 的底层差异
public interface IInvocationContext
{
    Type       ServiceType        { get; }
    Type       ImplementationType { get; }
    MethodInfo Method             { get; }
    object?[]  Arguments          { get; }

    // 委托给 ICancellationTokenProvider.Token；框架在执行拦截链前设置 ambient token
    CancellationToken CancellationToken { get; }

    // 拦截器间通信用；框架不直接写入此字典
    IDictionary<string, object?> Items { get; }

    // 获取场景特定数据，由各 Adapter 在执行拦截链前注入
    // MVC → IHttpRequestFeature；gRPC → IGrpcCallFeature；Worker → IWorkerItemFeature（后续）
    // 非对应场景返回 null，拦截器应做 null check
    T? GetFeature<T>() where T : class;
}

// 单次请求/方法调用：覆盖 sync、Task、Task<T>、ValueTask、ValueTask<T>
public interface IUnaryInvocationContext : IInvocationContext
{
    Type?   ResultType  { get; }
    object? ReturnValue { get; set; }
    ValueTask<object?> ProceedAsync();
}

// 异步流：覆盖 IAsyncEnumerable<T> 与可映射为流式结果的入口
public interface IAsyncStreamInvocationContext<T> : IInvocationContext
{
    IAsyncEnumerable<T> ProceedAsyncEnumerable();
}

// 拦截器基类，开发者只写一次逻辑
public abstract class InterceptorBase
{
    public virtual async ValueTask InterceptAsync(IUnaryInvocationContext context)
    {
        await context.ProceedAsync();
    }

    // 返回 IAsyncEnumerable<T>（惰性流），不属于 Task/ValueTask 异步方法；
    // "AsyncEnumerable" 描述流类型而非方法返回形态，属于 naming-dotnet 规则 4 的合理豁免
    public virtual IAsyncEnumerable<T> InterceptAsyncEnumerable<T>(
        IAsyncStreamInvocationContext<T> context)
        => context.ProceedAsyncEnumerable();
}

public enum InterceptorScope
{
    Service,   // Castle.Core 代理的 DI 服务方法调用；默认值
    Entry,     // MVC Action / gRPC RPC 入口边界调用（由 Adapter 全局 Filter 执行）
}

// 拦截器自声明自动匹配规则（无需业务类打特性）
// Order 决定同层内执行顺序：数字越小越靠外层（先执行）；默认 0
public interface IAutoMatchInterceptor
{
    int              Order => 0;
    InterceptorScope Scope => InterceptorScope.Service;

    // Service scope：serviceType 为解析接口，implementationType 为实现类（两者通常不同）
    bool Match(Type serviceType, Type implementationType);

    // Entry scope：controllerType 为 Controller 具体类型
    // 默认 false；拦截器若需参与 Entry 链必须显式重写此方法，
    // 避免 Service scope 的匹配逻辑意外影响 Entry 链
    bool MatchEntry(Type controllerType) => false;
}

// 场景特定 Feature 接口（由对应 Adapter 实现并注入 IInvocationContext）
public interface IHttpRequestFeature  { HttpContext HttpContext { get; } }
public interface IGrpcCallFeature     { ServerCallContext ServerCallContext { get; } }
// IWorkerItemFeature —— 后续执行计划

// 显式标记拦截器的特性声明
// 仅对 Service scope（Castle.Core 代理）有效；标注在 Controller 类上时启动期输出警告并忽略
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface,
    AllowMultiple = true, Inherited = true)]
public sealed class InterceptAttribute(Type interceptorType) : Attribute
{
    public Type            InterceptorType { get; } = interceptorType;
    public InterceptorScope Scope          { get; init; } = InterceptorScope.Service;
}

// 忽略拦截器的特性声明
// 标注在 Controller 类上时同时作用于 Entry 链（从 EntryChainCache 中剔除指定拦截器）
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface,
    AllowMultiple = false, Inherited = true)]
public sealed class IgnoreInterceptorsAttribute(params Type[] interceptorTypes) : Attribute
{
    // 空数组表示忽略所有全局/自动匹配拦截器
    public IReadOnlyList<Type> InterceptorTypes { get; } = interceptorTypes;
}

// 使用示例
[Intercept(typeof(CacheInterceptor))]
[Intercept(typeof(CacheInterceptor), Scope = InterceptorScope.Service)]

[IgnoreInterceptors]                                        // 忽略所有全局/自动匹配拦截器
[IgnoreInterceptors(typeof(UnitOfWorkInterceptor))]         // 忽略指定拦截器
```

---

## 四、程序集扫描与拓扑排序

### 4.1 程序集发现规则

- 从当前 `AppDomain` 已加载程序集中筛选
- 名称以 `Tw.` 开头
- 默认额外加入 `Assembly.GetEntryAssembly()` 作为启动入口程序集兜底，即使名称不以 `Tw.` 开头
- 支持通过 `options.AddAssembly(...)` / `options.AddAssemblyOf<T>()` 显式追加程序集
- 排除名称含 `.Tests` / `.Test` / `.Specs` / `.Spec` 的测试程序集
- **不主动从磁盘加载**未加载的程序集

扫描引擎优先复用 `Tw.Core.Reflection.TypeFinder` 与 `ReflectionCache`；若现有能力不满足需求（如程序集级并发批量收集），在同一命名空间内扩展，不另建独立引擎。

### 4.2 拓扑排序（Kahn 算法）

以程序集引用关系建立有向无环图（DAG），计算拓扑顺序：

```
Tw.Domain  ←── Tw.Application ←── Tw.Api
                               ←── Tw.Worker
```

处理顺序：`Tw.Domain → Tw.Application → Tw.Api / Tw.Worker`

- **越靠近叶子节点（上层）的程序集，拓扑索引越大，优先级越高**
- 叶子程序集的注册会覆盖底层程序集的同类注册
- 若多个不同程序集处于同一拓扑层级，且暴露同一 `(ServiceType, Key)`，视为平级冲突并启动时报错
- 若程序集引用存在循环（DAG 不成立），Kahn 算法处理完成后仍有未处理节点，框架应在启动时抛出异常，异常信息需包含完整的循环路径（如 `Tw.A → Tw.B → Tw.A`）

### 4.3 并发扫描策略

```
并发反射各程序集（Parallel.ForEach）
    ↓
汇总所有 ServiceRegistrationEntry
    ↓
串行拓扑排序
    ↓
串行执行 Autofac 注册
```

反射阶段线程安全，排序与注册阶段单线程，兼顾性能与正确性。

### 4.4 服务注册元数据结构

```csharp
// 每个类型产生一条元数据记录
record ServiceRegistrationEntry(
    Type                              ImplementationType,
    IReadOnlyList<(Type, object?)>    ExposedServices,       // (ServiceType, Key)
    ServiceLifetime                   Lifetime,
    bool                              IsReplacement,
    bool                              IsCollectionService,
    int                               CollectionOrder,       // 默认 0
    int                               ReplacementOrder,      // 默认 0
    int                               AssemblyTopologicalIndex,
    IReadOnlyList<Type>               InterceptorTypes
);
```

---

## 五、Autofac 注册执行

### 5.1 生命周期映射

| 接口 | Autofac 生命周期 |
|---|---|
| `ITransientDependency` | `InstancePerDependency()` |
| `IScopedDependency`    | `InstancePerLifetimeScope()` |
| `ISingletonDependency` | `SingleInstance()` |

### 5.2 冲突裁决规则

扫描完成后先在元数据阶段裁决最终注册集合，再写入 Autofac。框架**只注册裁决后的最终集合**，避免被替换实现继续出现在 `IEnumerable<T>` 中。

对每个 `(ServiceType, Key)` 分组，若存在多个 `IsReplacement = true` 的条目：

1. 取 `AssemblyTopologicalIndex` 最大的（叶子程序集优先）
2. 若 `AssemblyTopologicalIndex` 相同且来自不同程序集：**启动时抛异常**
3. 若来自同一程序集：取 `ReplacementOrder` 最大的
4. 若仍相同：**启动时抛异常**，异常信息包含所有冲突类型名称

**替换链**：不需要显式声明链式替换（A 替换 B、C 替换 A）。裁决逻辑只关注 `(ServiceType, Key)` 维度的最终胜出者——拓扑索引最高的替换者天然成为唯一注册，中间层（如 A）自动被排除，无需额外处理。

### 5.3 无显式替换标记的多实现处理

| 情况 | 行为 |
|---|---|
| 同一分组全部实现均标注 `[CollectionService]` | 该 `(ServiceType, Key)` 分组进入集合模式，保留所有集合实现，按拓扑顺序 + `CollectionOrder` 稳定排序后注册 |
| 同一分组混用集合实现与非集合实现 | **启动时报错**，要求业务显式统一语义 |
| 冲突来自**不同拓扑层级程序集** | 拓扑索引高者胜出，发出**启动警告** |
| 冲突来自**不同程序集但同一拓扑层级** | **启动时报错** |
| 冲突来自**同一程序集** | **启动时报错** |

集合模式只用于业务明确需要多实现的服务，例如策略链、规则、handler、validator。集合服务不会参与默认唯一胜出裁决，也不会被 `[ReplaceService]` 部分替换；如需替换集合中的某个元素，必须通过更具体的业务排序或禁用原实现来表达。

**跨层替换边界**：若同一 `(ServiceType, Key)` 下低拓扑层有 `[ReplaceService]` 实现、高拓扑层同时有无标记的普通实现，高拓扑层普通实现优先胜出，低拓扑层的替换声明被覆盖并发出启动警告（告知存在替换声明被高拓扑层普通实现覆盖的情况）。此时低拓扑层的替换条目不再参与 § 5.2 的裁决流程。

### 5.4 部分替换规则

替换仅作用于替换者与原始实现**暴露服务的交集**：

```
原始暴露：[IOrderService, IQueryService]
替换者暴露：[IOrderService]

结果：
  IOrderService → 替换者
  IQueryService → 原始（未被接管，保持不变）
```

替换者暴露了原始没有的服务 → 视为新增注册，不算替换。

**替换链中间层规则**：替换链中间层（如 A 在 B 替换 A 的 `IOrderService` 后成为中间层）对其他服务类型（非被替换的 `(ServiceType, Key)`）的注册**不受影响**，仍按原始注册关系保留。替换者成为胜出者后，中间层仅从被替换的服务类型映射中退出，其余服务类型映射独立存在。

### 5.5 Keyed Service 替换规则

替换匹配基于 `(ServiceType, Key)` 二元组，Key 不匹配则不产生替换：

| 原始 Key | 替换者 Key | 行为 |
|---|---|---|
| `null` | `null` | 正常替换 |
| `"standard"` | `"standard"` | 正常替换 |
| `null` | `"premium"` | 新增 Keyed 注册，原非命名注册不变，发出启动警告 |
| `"standard"` | `"premium"` | Key 不匹配，新增 Keyed 注册，发出启动警告 |
| `"standard"` | `null` | 替换者 Key 为 null，与原始 `(IOrderService, "standard")` 的 Key 不匹配；视为对 `(IOrderService, null)` 的新增注册，但该非命名注册不存在，发出启动警告 |

### 5.6 注册流水线（按拓扑顺序逐条执行）

```
扫描元数据
    ↓
按 (ServiceType, Key) 分组裁决
    ↓
生成最终 ServiceRegistrationEntry 集合
    ↓
唯一服务仅写入胜出项；集合服务写入全部集合项

对每个 ServiceRegistrationEntry，独立执行以下两步（互不干扰）：

步骤 1 — Service 链注册（Castle.Core 代理，Scope = Service）
  Service 链不为空？
  ├── Yes → Key != null?
  │         ├── Yes → .Keyed(key, serviceType).EnableInterfaceInterceptors().InterceptedBy(...)
  │         └── No  → .As(serviceType).EnableInterfaceInterceptors().InterceptedBy(...)
  └── No  → 跳过代理，直接注册原始实现

步骤 2 — Entry 元数据记录（Scope = Entry，供 Adapter 使用）
  Entry 链不为空？
  ├── Yes → 写入 EntryInterceptorRegistry（启动期全量构建，与 Castle 代理完全独立）
  └── No  → 跳过
```

Controller 类型显式排除在步骤 1 扫描之外，不生成 Castle 代理。Controller 的 Entry 链通过 `EntryChainCache` 在首次 Action 调用时按需填充（见 § 6.2）。

> Autofac 仍由框架接管最终容器，但替换语义不再依赖"最后注册者生效"；最后注册者只作为 Autofac 底层特性，不作为业务规则来源。

### 5.7 内部模块封装

框架内部使用 Autofac `Module` 封装注册逻辑，由 ContainerBuilder 阶段的框架扩展方法挂载，**业务侧不可见、不直接声明 Module**：

```csharp
// 框架内部，业务不可见
internal sealed class AutoRegistrationModule : Module
{
    protected override void Load(ContainerBuilder builder) { ... }
}
```

---

## 六、AOP 架构

### 6.1 拦截器三层叠加顺序

启动阶段对每个服务类型预计算拦截器链并缓存，执行顺序固定：

```
① 全局拦截器      通过 options.AddGlobalInterceptor<T>() 注册，对指定 Scope 内的调用生效
② 自动匹配拦截器  实现 IAutoMatchInterceptor，Match() 返回 true 时生效
③ 显式标记拦截器  通过 [Intercept(typeof(T))] 标注在类或接口上
```

两种 Scope 对应两条独立的链，分别由不同机制执行，互不干扰：

| Scope | 生效位置 | 链的缓存结构 |
|---|---|---|
| `Service` | Castle.Core 代理的 DI 服务方法调用 | `ServiceChainCache`，按 `implementationType` 索引，启动期全量预填充 |
| `Entry` | MVC Action / gRPC RPC 入口边界调用 | `EntryChainCache`，按 `controllerType` / `serviceType` 索引，首次调用时懒填充 |

默认 `Scope = Service`。例如工作单元、缓存只用于服务层（Service）；请求日志、Trace 声明为 Entry。同一拦截器仅在其声明的 Scope 下生效，不存在同时作用于两层的模式。

### 6.2 拦截器链计算流程

两条链独立计算，互不依赖。

**ServiceChainCache（启动期，按 implementationType 全量预填充）**

```
对每个被扫描的 DI 服务实现类型：
1. 收集该类型上所有 [IgnoreInterceptors] 规则（类 + 其实现的接口；接口继承链传递）
2. 从全局 Service 拦截器列表中移除被忽略的条目
3. 执行 Match(serviceType, implementationType)，过滤 Scope = Service 的自动匹配拦截器，
   移除被忽略的条目
4. 追加显式 [Intercept] 标记的拦截器（不受 [IgnoreInterceptors] 影响）
5. 若链为空，跳过 Castle 代理注册
6. 写入 ServiceChainCache[implementationType]
```

**EntryChainCache（首次 Action/RPC 调用时，按 controllerType 懒填充）**

```
启动期预构建 Entry 基线链：
  - 所有全局 Entry 拦截器
  - 所有 IAutoMatchInterceptor（Scope = Entry）

首次调用 controllerType 时：
1. 从基线链出发
2. 收集 controllerType 上的 [IgnoreInterceptors] 规则
3. 从基线链中移除被忽略的条目
4. 执行 MatchEntry(controllerType)，移除返回 false 的自动匹配拦截器
5. 写入 EntryChainCache[controllerType]
```

自动匹配由独立的 singleton 规则对象完成；实际拦截器实例在运行时按 DI 生命周期解析，避免启动期构造依赖 scoped 服务的拦截器。

### 6.3 IgnoreInterceptors 作用域

| 标注位置 | 作用范围 |
|---|---|
| DI 服务实现类上 | 仅当前实现类的 Service 链（ServiceChainCache） |
| Controller 类上 | EntryChainCache 中该 Controller 的 Entry 链 |
| 接口上 | 所有实现该接口的类均继承此规则，**包括接口继承链的传递性**（如 `ISpecificRepo : IReadOnlyRepo`，`[IgnoreInterceptors]` 在 `IReadOnlyRepo` 上，则实现 `ISpecificRepo` 的类同样受约束） |

```csharp
// 示例：查询接口不参与工作单元拦截（传递到所有子接口的实现类）
[IgnoreInterceptors(typeof(UnitOfWorkInterceptor))]
public interface IReadOnlyRepository<T> { }

// 示例：某实现类完全跳过所有全局/自动匹配拦截器（Service 链）
[IgnoreInterceptors]
public class OrderQueryService : IOrderQueryService, IScopedDependency { }

// 示例：某 Controller 跳过审计拦截器（Entry 链）
[IgnoreInterceptors(typeof(AuditInterceptor))]
public class HealthController : ControllerBase { }
```

### 6.4 入口 Adapter vs Castle.Core 模式

| | 入口 Adapter | Castle.Core 模式 |
|---|---|---|
| 触发条件 | 显式启用对应入口 Adapter | 普通 DI Resolve 场景 |
| 适用项目 | MVC/Web API、gRPC | Worker 内部服务、普通应用服务、CAP handler 内部依赖 |
| 拦截粒度 | 入口调用边界 | 通过容器解析出的服务方法调用 |
| 动态代理 | 不生成 Castle 代理 | 生成（首次 Resolve 时懒生成） |

**入口 Adapter 重要限制**：Service 层之间的直接调用链不会被入口 Adapter 拦截。如需 Service 层拦截，使用 Castle.Core 模式。

两种模式拦截的是不同调用栈帧：Adapter 拦截入口边界，Castle.Core 拦截 DI 服务方法。`InterceptorScope` 严格隔离两条链，同一拦截器只能声明一个 Scope，不会同时出现在两条链中。

### 6.5 入口 Adapter 设计

不同入口使用不同 Adapter，统一复用拦截器链计算与 `IInvocationContext`。各 Adapter 在执行拦截链前向 `IInvocationContext` 注入对应 Feature，并通过 `ICurrentCancellationTokenAccessor.Use()` 设置 ambient CancellationToken：

| 入口 | Adapter | Feature | 说明 |
|---|---|---|---|
| ASP.NET Core MVC / Web API | `IAsyncActionFilter` | `IHttpRequestFeature` | 围绕 Controller Action，不能覆盖 Result 序列化之后的行为 |
| ASP.NET Core gRPC | `Grpc.Core.Interceptors.Interceptor` | `IGrpcCallFeature` | 覆盖 unary 与 streaming RPC，保留 `ServerCallContext` |
| CAP 消费者 | 后续执行计划 | `ICapMessageFeature`（后续） | 初版不接管 ack、重试、失败语义；handler 内部服务仍可使用 Castle.Core 模式 |
| Worker / HostedService | 后续执行计划 | `IWorkerItemFeature`（后续） | 初版不包装 `ExecuteAsync` 生命周期；循环内 per-item 拦截由 Castle.Core 模式负责 |

MVC/Web API Adapter 在 `AddTwAspNetCoreInfrastructure()` 中通过 `IConfigureOptions<MvcOptions>` 加入 `MvcOptions.Filters`。gRPC Adapter 通过 `AddGrpcInterceptors()` 在 `AddGrpc` 配置阶段注册：

Entry 链组成（由 `CompositeActionFilter` 在启动期预构建基线、首次调用时按 Controller 类型懒填充）：

- **全局 Entry 拦截器**：通过 `options.AddGlobalInterceptor<T>(scope: InterceptorScope.Entry)` 注册
- **自动匹配 Entry 拦截器**：`IAutoMatchInterceptor`（`Scope = Entry`）中 `MatchEntry(controllerType)` 返回 `true` 的条目
- **`[Intercept]` 在 Controller 上不适用**：Castle.Core 代理不能作用于 Controller（参见 ABP vNext #3409），打此特性时启动期输出警告并忽略

```csharp
// 框架内部，业务不可见
internal sealed class CompositeActionFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var controllerType = context.Controller.GetType();

        // 1. 从 EntryChainCache 按 controllerType 获取链（首次调用时懒填充）
        // 2. accessor.Use(context.HttpContext.RequestAborted) 设置 ambient CT
        // 3. 向 IInvocationContext 注入 IHttpRequestFeature
        // 4. 执行 Entry 链（全局 Entry 拦截器 → MatchEntry 自动匹配拦截器）
    }
}
```

```csharp
// 业务入口
builder.Services.AddGrpc(grpcOptions =>
{
    grpcOptions.AddGrpcInterceptors();
});

// 框架内部，业务不可见
internal sealed class CompositeGrpcInterceptor : Grpc.Core.Interceptors.Interceptor
{
    // 覆盖 unary、client streaming、server streaming、duplex streaming
    // 1. accessor.Use(context.CancellationToken) 设置 ambient CT
    // 2. 向 IInvocationContext 注入 IGrpcCallFeature
    // 3. 执行 Entry scope 拦截器链
}
```

`AddGrpcInterceptors()` 必须是幂等的：同一 `GrpcServiceOptions` 多次调用只注册一次框架 gRPC Adapter。若项目未调用 `AddGrpcInterceptors()`，gRPC 服务不启用入口 Adapter，但服务层依赖仍可通过 Castle.Core 模式拦截。

### 6.6 调用形态分派

官方约束：
- Castle DynamicProxy 可代理接口与类，但类代理只能拦截 `virtual` 成员
- `Autofac.Extras.DynamicProxy` 的 interface/class interceptor 必须在 Autofac 注册阶段启用
- `Castle.Core.AsyncInterceptor` 负责 Castle DynamicProxy 下的 sync、`Task`、`Task<T>` 异步分派，框架在其之上适配统一的 `IUnaryInvocationContext`
- `ValueTask` / `ValueTask<TResult>` 需要单次消费，框架不得重复 await 或重复转换
- gRPC streaming 与 `IAsyncEnumerable<T>` 属于流式调用，不适合伪装成单次 `Task` 调用

框架优先使用 `Castle.Core.AsyncInterceptor` 提供的成熟异步拦截模型，避免自行维护容易出错的异步分派胶水代码。`ValueTask` / `ValueTask<T>` 与 `IAsyncEnumerable<T>` 仍由框架包装为内部上下文处理，因为它们有单次消费或惰性枚举语义，不能简单套入 `Task` 分派模型。

框架内部按以下形态分派：

| 返回形态 | 内部处理 | 对外上下文 |
|---|---|---|
| `void` / `T` | 同步执行，包装为已完成 `ValueTask<object?>` | `IUnaryInvocationContext` |
| `Task` | await 原始任务 | `IUnaryInvocationContext` |
| `Task<T>` | await 原始任务并保留结果 | `IUnaryInvocationContext` |
| `ValueTask` | 单次 await 原始 `ValueTask`，返回同形态结果 | `IUnaryInvocationContext` |
| `ValueTask<T>` | 单次 await 原始 `ValueTask<T>`，返回同形态结果 | `IUnaryInvocationContext` |
| `IAsyncEnumerable<T>` | 不提前枚举，返回包装后的 async stream | `IAsyncStreamInvocationContext<T>` |
| gRPC streaming | 由 gRPC Adapter 映射为 stream 上下文 | `IAsyncStreamInvocationContext<T>` 或 gRPC 专用上下文 |

常规拦截器只需重写 `InterceptAsync(IUnaryInvocationContext)`；需要处理流式结果时，再重写 `InterceptAsyncEnumerable<T>()`。

### 6.7 拦截调用契约

`ProceedAsync()` 与 `ProceedAsyncEnumerable()` 是拦截器链推进的唯一入口，框架必须保证以下契约：

- `ProceedAsync()` 只能调用一次。重复调用代表拦截器逻辑错误，框架抛出继承 `TwException` 的框架异常，异常消息使用简体中文并包含拦截器类型与目标方法名称。
- 拦截器可以选择不调用 `ProceedAsync()` 以实现短路。短路时若目标方法存在返回值，必须设置 `ReturnValue`；框架在离开拦截链时验证 `ReturnValue` 可赋值给 `ResultType`，不匹配则抛出框架异常。
- 目标方法或下游拦截器抛出的异常默认原样向上传播。框架不默认包装业务异常，避免改变调用方可观察语义；只有框架契约被破坏时才抛出框架异常。
- `Items` 字典生命周期限定在单次调用上下文内。框架不写入业务键，内置键如需引入必须使用 `Tw.` 前缀，避免与业务拦截器冲突。
- `ValueTask` / `ValueTask<T>` 由框架内部单次消费，不向拦截器暴露原始实例，避免重复 await。
- `IAsyncEnumerable<T>` 保持惰性枚举。框架在创建 stream 上下文时捕获当次调用的 `CancellationToken`，并在包装枚举器的每次 `MoveNextAsync()` 周期内通过 `ICurrentCancellationTokenAccessor.Use()` 恢复 ambient token；不得依赖入口 Adapter 在方法返回前设置的临时 AsyncLocal 状态继续存在。
- `ProceedAsyncEnumerable()` 也只能调用一次。重复调用时抛出框架异常；拦截器如需短路流式结果，应直接返回新的 `IAsyncEnumerable<T>`。

### 6.8 Castle 代理覆盖与启动诊断

Castle.Core 模式默认使用接口代理：
- 暴露服务包含 public interface 时，使用 `.EnableInterfaceInterceptors()`
- 仅暴露具体类型时，默认不生成代理，并输出启动警告
- 若显式开启类代理，使用 `.EnableClassInterceptors()`，但仅 `virtual` 方法可被拦截；非 `virtual` 成员输出启动警告

启动警告只在扫描/注册阶段计算并输出。容器构建完成后，运行时直接使用最终注册表与拦截器链缓存，不再重复执行这些诊断检查。

---

## 七、配置与选项自动注册

### 7.1 自动发现规则

配置类扫描复用程序集发现结果，不额外从磁盘加载程序集。满足以下条件的类型会作为 options 类型候选：

- public / internal 的非抽象 class 或 record class
- 启动入口程序集：类型名以 `Options` 或 `Settings` 结尾，或实现 `IConfigurableOptions`，或显式标注 `[ConfigurationSection(...)]`
- 类库程序集：必须实现 `IConfigurableOptions`，或显式标注 `[ConfigurationSection(...)]`，避免把普通 DTO 误识别为配置类
- 未标注 `[DisableOptionsRegistration]`
- 具有 public set/init 属性，能被 `ConfigurationBinder` 绑定

配置节名称按以下优先级计算：

1. `ConfigurationSectionAttribute.Name`
2. 类型名去后缀得到的默认 section

| 类型名 | 默认 section |
|---|---|
| `RedisOptions` | `Redis` |
| `PaymentSettings` | `Payment` |
| `TwRedisOptions` | `TwRedis` |

不符合约定、需要嵌套路径、命名选项、直接注入或验证策略覆盖时，使用调整后的 `[ConfigurationSection(...)]` 显式声明。

### 7.2 注册行为

每个 options 类型注册以下能力：

```csharp
services.AddOptions<TOptions>()
    .Bind(configuration.GetSection(sectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart(); // 由策略决定是否启用
```

框架自动提供：

- `IOptions<TOptions>`
- `IOptionsSnapshot<TOptions>`
- `IOptionsMonitor<TOptions>`
- 显式声明的 `IValidateOptions<TOptions>` 实现
- 可选的 `TOptions` 本体直接注入（Monitor 语义：解析时读取 `IOptionsMonitor<TOptions>.CurrentValue`）

直接注入 `TOptions` 是解析时快照，不随配置 reload 变更。需要热更新时必须直接依赖 `IOptionsMonitor<TOptions>`。框架在 singleton 依赖 scoped 直接注入时按 DI 容器规则暴露错误，不做静默降级。

### 7.3 验证策略

验证采用"默认安全、可配置放宽"的策略：

- 默认启用 `ValidateDataAnnotations()`
- 若类型实现 `IValidatableObject`，随 DataAnnotations 一起执行类级验证
- 自动扫描并注册实现了 `IValidateOptions<TOptions>` 的验证器；验证器必须满足 singleton 兼容性，不能依赖 scoped 服务
- `ValidateOnStart` 默认在测试与生产环境开启，开发环境可通过全局选项关闭；环境判断由 `AddTwAspNetCoreInfrastructure()` 在注册阶段读取 `IHostEnvironment` 完成，确保时序正确
- `[ConfigurationSection(..., ValidateOnStart = true/false)]` 可覆盖全局策略

验证失败视为启动失败，异常信息包含 options 类型、section 名、失败字段或验证器名称。

### 7.4 命名选项

初版只自动注册默认命名实例（`Options.DefaultName`）。需要多个同类型配置实例时，必须显式声明：

```csharp
[ConfigurationSection("Redis:Primary", OptionsName = "Primary")]
[ConfigurationSection("Redis:Replica", OptionsName = "Replica")]
```

命名选项只保证通过 `IOptionsSnapshot<TOptions>.Get(name)` / `IOptionsMonitor<TOptions>.Get(name)` 访问。命名选项默认不注册 `TOptions` 本体直接注入。

### 7.5 性能与运行时影响

自动发现、section 计算、验证器注册都发生在启动阶段。启动后运行时使用 Microsoft Options 管线：

- `IOptions<T>` 为 singleton 语义，适合启动后不变配置
- `IOptionsMonitor<T>` 为 singleton 语义，支持配置 reload 和变更通知
- `IOptionsSnapshot<T>` 为 scoped 语义，请求内缓存，但每个 scope 首次访问会重新计算

框架默认直接注入走 `IOptionsMonitor<T>.CurrentValue`，避免把 `IOptionsSnapshot<T>` 的 scoped 与重算成本无意带入 singleton 或高频路径。

---

## 八、性能优化策略

| 策略 | 说明 |
|---|---|
| 并发反射 | 各程序集独立并发扫描，汇总后串行排序与注册 |
| 单次反射原则 | 每个 Type 只反射一次，所有特性在同一次遍历中读取并缓存 |
| 拦截器链预计算 | 启动时缓存每个类型的完整拦截器链，运行时直接查表 |
| 代理懒生成 | Castle.Core 代理类型在首次 Resolve 时生成，不阻塞启动 |
| 跳过空链类型 | 拦截器链为空的类型不注册代理，直接使用原始实现 |
| 启动诊断输出 | 开发环境默认输出各阶段耗时与警告，生产环境可关闭；诊断不进入运行时热路径 |

```
[AutoRegistration] Scan:     42ms  (12 assemblies, 1,847 types)
[AutoRegistration] Sort:      1ms
[AutoRegistration] Register: 18ms  (324 services)
[AutoRegistration] AOP:       3ms  (87 intercepted)
[AutoRegistration] Total:    64ms
```

---

## 九、典型使用示例

### 9.1 基础自动注册

```csharp
// 自动扫描到，注册为 IOrderService（Scoped）
public class OrderService : IOrderService, IScopedDependency { }

// 无业务接口，注册为具体类型（Transient）
public class PureWorker : ITransientDependency { }
```

### 9.2 Keyed Service

```csharp
[KeyedService("premium")]
public class PremiumOrderService : IOrderService, IScopedDependency { }
```

### 9.3 集合服务

```csharp
// 多个规则实现都保留，可通过 IEnumerable<IOrderRule> 注入
[CollectionService]
public class CreditLimitRule : IOrderRule, IScopedDependency { }

[CollectionService(Order = 10)]
public class InventoryRule : IOrderRule, IScopedDependency { }
```

### 9.4 开放泛型

```csharp
// 注册为 IRepository<T> -> EfRepository<T>
[ExposeServices(typeof(IRepository<>))]
public class EfRepository<T> : IRepository<T>, IScopedDependency { }
```

### 9.5 服务替换

```csharp
// 上层程序集替换底层实现，仅替换 IOrderService，不影响 IQueryService
[ReplaceService]
[ExposeServices(typeof(IOrderService))]
public class EnhancedOrderService : IOrderService, IQueryService, IScopedDependency { }
```

### 9.6 ICancellationTokenProvider 使用

```csharp
// 业务服务无需关心调用来源，通过 provider 取当前调用链的 token
public class OrderService : IOrderService, IScopedDependency
{
    private readonly ICancellationTokenProvider _ct;
    private readonly IOrderRepository _repo;

    public async Task SubmitAsync(Order order)
    {
        await _repo.SaveAsync(order, _ct.Token);
    }
}
```

### 9.7 AOP 使用

```csharp
// 全局注册（Service scope：作用于 Castle.Core 代理的 DI 服务调用）
options.AddGlobalInterceptor<LoggingInterceptor>(scope: InterceptorScope.Service);

// 全局注册（Entry scope：作用于所有 MVC Action 入口边界）
options.AddGlobalInterceptor<TraceInterceptor>(scope: InterceptorScope.Entry);

// 自动匹配 Service scope（无需业务类打特性）
public class AuditInterceptor : InterceptorBase, IAutoMatchInterceptor
{
    public InterceptorScope Scope => InterceptorScope.Service;

    public bool Match(Type serviceType, Type implementationType)
        => implementationType.Namespace?.StartsWith("Tw.Application") == true;

    // MatchEntry 默认 false，不参与 Entry 链（无需重写）

    public override async ValueTask InterceptAsync(IUnaryInvocationContext context)
    {
        await context.ProceedAsync();
    }
}

// 自动匹配 Entry scope（仅参与 Controller 入口链）
public class RequestLoggingInterceptor : InterceptorBase, IAutoMatchInterceptor
{
    public InterceptorScope Scope => InterceptorScope.Entry;

    public bool Match(Type serviceType, Type implementationType)
        => false; // 不参与 Service 链

    public bool MatchEntry(Type controllerType)
        => !controllerType.Namespace?.StartsWith("Tw.Internal") == true; // 排除内部 Controller

    public override async ValueTask InterceptAsync(IUnaryInvocationContext context)
    {
        await context.ProceedAsync();
    }
}

// 显式标记（仅适用于 Service scope / Castle.Core 代理）
[Intercept(typeof(CacheInterceptor))]
public class OrderQueryService : IOrderQueryService, IScopedDependency { }

// 查询场景忽略工作单元拦截器（传递到所有子接口实现类）
[IgnoreInterceptors(typeof(UnitOfWorkInterceptor))]
public interface IReadOnlyRepository<T> { }

// Controller 跳过特定 Entry 拦截器
[IgnoreInterceptors(typeof(AuditInterceptor))]
public class HealthController : ControllerBase { }
```

### 9.8 Feature Collection 使用

```csharp
// 拦截器按需获取 HTTP 上下文，非 HTTP 场景 GetFeature 返回 null
public class RequestLoggingInterceptor : InterceptorBase
{
    public override async ValueTask InterceptAsync(IUnaryInvocationContext context)
    {
        var http = context.GetFeature<IHttpRequestFeature>();
        var traceId = http?.HttpContext.TraceIdentifier
                      ?? context.Items.GetValueOrDefault("TraceId") as string
                      ?? Guid.NewGuid().ToString("N");
        context.Items["TraceId"] = traceId;
        await context.ProceedAsync();
    }
}
```

### 9.9 配置与选项自动注册

```csharp
// 复用现有抽象：类库 options 显式声明配置节并实现标记接口
[ConfigurationSection("Tw:Redis")]
public sealed class RedisOptions : IConfigurableOptions
{
    [Required]
    public string ConnectionString { get; init; } = "";
}

// 覆盖 section，并允许直接注入 PaymentOptions（Monitor 语义）
[ConfigurationSection("Tw:Payment", DirectInject = true, ValidateOnStart = true)]
public sealed class PaymentOptions : IValidatableObject
{
    [Required]
    public string MerchantId { get; init; } = "";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MerchantId.Length < 6)
        {
            yield return new ValidationResult("MerchantId 长度不能小于 6。", [nameof(MerchantId)]);
        }
    }
}

public class PaymentClient : IScopedDependency
{
    // 直接注入是解析时快照；需要热更新改为注入 IOptionsMonitor<PaymentOptions>
    public PaymentClient(PaymentOptions options) { }
}
```

---

## 十、验收标准

验收按层级推进。首批执行计划必须覆盖 P0、P1 与横切质量门禁。P2 为 gRPC Adapter 阶段；P3 为总体设计内的后续 CAP/Worker 入口能力，不阻塞首批交付，但进入对应执行计划时必须补齐验收。

### 10.1 P0：自动注册、配置和容器基础

1. `ITransientDependency` / `IScopedDependency` / `ISingletonDependency` 实现类在启动时自动完成注册，无需手动 `builder.Services.AddXxx()`；生命周期分别映射到 `InstancePerDependency()`、`InstancePerLifetimeScope()`、`SingleInstance()`。
2. 默认暴露规则正确：实现业务接口时暴露接口；仅实现生命周期接口时暴露具体类型；`System.*`、`Microsoft.*` 与生命周期标记接口不会作为业务服务暴露。
3. `[ExposeServices]`、`IncludeSelf`、`[DisableAutoRegistration]`、开放泛型注册对最终注册表的影响符合设计稿描述。
4. `[KeyedService]` 使用 `(ServiceType, Key)` 维度注册；Key 不匹配的替换不会影响原注册，并输出可诊断的启动警告。
5. `[ReplaceService]` 仅替换暴露服务交集；替换链中间层在非被替换服务上的注册保持可解析。
6. `[CollectionService]` 分组保留所有集合实现并稳定排序；集合实现与非集合实现混用时启动失败。
7. 多实现冲突裁决符合设计稿描述：跨拓扑层级按高层胜出并警告；同程序集或同拓扑层不同程序集冲突时启动失败；异常消息包含全部冲突类型名称。
8. 拓扑排序正确：叶子程序集注册覆盖底层程序集同类注册；循环引用在启动时抛出含完整循环路径的异常。
9. 程序集并发扫描结果与单线程扫描结果一致；重复启动测试不出现注册顺序抖动。
10. Options 自动注册优先复用现有 `IConfigurableOptions` 与调整后的 `ConfigurationSectionAttribute`；命名选项、验证覆盖和直接注入均通过 `ConfigurationSectionAttribute` 表达。
11. 同一类型声明多个 `ConfigurationSectionAttribute` 时，默认命名实例只能有一个；多个 `OptionsName = null` 或重复 `OptionsName` 均启动失败。
12. `[ConfigurationSection(..., ValidateOnStart = true)]` 标注的配置类在启动阶段验证失败时终止启动，异常信息包含 options 类型、section 名和失败字段，且不输出敏感值。
13. `[ConfigurationSection(..., DirectInject = true)]` 支持构造函数直接注入 `TOptions`，注入值来自 `IOptionsMonitor<TOptions>.CurrentValue`；命名选项默认不注册 `TOptions` 本体直接注入。

### 10.2 P1：Service AOP 与 MVC/Web API Adapter

1. `InterceptorBase.InterceptAsync()` 能拦截 sync、`Task`、`Task<T>`、`ValueTask`、`ValueTask<T>` 返回形态，`ProceedAsync()` 正确传递返回值和异常。
2. `ProceedAsync()` 单次调用语义明确：重复调用应抛出框架异常；未调用时允许短路返回，且 `ReturnValue` 必须按目标返回类型校验。
3. `IAsyncEnumerable<T>` 不被提前枚举；`InterceptAsyncEnumerable<T>()` 返回的流在枚举期间保持拦截器包装和取消令牌传播。
4. `ServiceChainCache` 在启动期按 `implementationType` 全量预填充；`EntryChainCache` 在首次 Action 调用时按 `controllerType` 懒填充；两个缓存互相独立。Service 链顺序：全局 Service 拦截器 → `Match()` 自动匹配 → 显式 `[Intercept]`；Entry 链顺序：全局 Entry 拦截器 → `MatchEntry()` 自动匹配；同层内按 `Order` 稳定排序。
5. `InterceptorScope.Service` 和 `Entry` 严格隔离：Service scope 拦截器不出现在 Entry 链中，Entry scope 拦截器不出现在 Castle Service 代理链中。
6. `[IgnoreInterceptors]` 在 DI 服务类/接口上作用于 ServiceChainCache，在 Controller 类上作用于 EntryChainCache；接口继承链的 `[IgnoreInterceptors]` 向下传递到所有实现类；`[IgnoreInterceptors]` 只抑制全局与自动匹配拦截器，不抑制显式 `[Intercept]`。
7. Castle.Core 模式默认使用接口代理；仅暴露具体类型时默认不生成代理并输出启动警告；显式开启类代理时非 `virtual` 成员输出启动警告。
8. 拦截器链为空的类型不注册代理；首次 Resolve 时生成代理不改变服务生命周期。
9. `IInvocationContext.CancellationToken` 优先使用方法参数中的 `CancellationToken`；没有方法参数时使用入口 Adapter 设置的 ambient token；仍不存在时返回 `CancellationToken.None`。
10. `ICancellationTokenProvider.Token` 注入任意生命周期服务后均能正确返回当前调用链 token，包括 Singleton 服务。
11. `AddTwAspNetCoreInfrastructure()` 通过 `IConfigureOptions<MvcOptions>` 注册 MVC/Web API Adapter；`IInvocationContext.GetFeature<IHttpRequestFeature>()` 在 MVC Adapter 执行链中返回非 null，在非 HTTP 场景返回 null。
12. MVC Adapter 覆盖 Controller Action 调用边界，不宣称覆盖 Result 序列化之后的行为。

### 10.3 P2：gRPC Adapter

1. `AddGrpcInterceptors()` 可在 `builder.Services.AddGrpc(options => options.AddGrpcInterceptors())` 中启用 gRPC Adapter，且多次调用保持幂等。
2. gRPC Adapter 覆盖 unary、client streaming、server streaming、duplex streaming RPC；每类调用均保留 `ServerCallContext`。
3. `IInvocationContext.GetFeature<IGrpcCallFeature>()` 在 gRPC Adapter 执行链中返回非 null，在非 gRPC 场景返回 null。
4. gRPC Adapter 使用 `ServerCallContext.CancellationToken` 设置 ambient token；方法参数中存在 `CancellationToken` 时仍按方法参数优先生效。
5. 未调用 `AddGrpcInterceptors()` 的项目不启用 gRPC Entry scope 拦截，但服务层依赖仍可通过 Castle.Core 模式拦截。

### 10.4 横切质量门禁：诊断、依赖和测试

1. 开发环境启动时输出扫描、排序、注册、AOP 预计算耗时与警告；生产环境可关闭诊断输出。
2. 框架显式抛出的异常使用稳定异常类型和简体中文消息；消息不泄露密钥、连接串、堆栈或第三方原始错误。
3. 新增依赖必须在中央包管理中固定版本，并提交锁文件变更；PR 中记录用途、替代方案、许可证、漏洞扫描、维护责任和验证命令。
4. `Castle.Core.AsyncInterceptor` 准入记录必须确认当前版本、许可证、维护状态、下载量或下游依赖、漏洞扫描结果，并用集成测试覆盖 sync、`Task`、`Task<T>` 分派。
5. 所有新增 public API 必须提供 XML 注释，说明用途、调用时序、副作用、异常语义和关键契约。
6. 单元测试覆盖元数据扫描、裁决、链计算、Options 规则和异常分支；集成测试覆盖 Autofac Host 启动、Castle 代理、MVC Adapter 与配置验证；P2 阶段补充 gRPC Adapter 集成测试。
7. 未自动化覆盖的风险必须在实现计划或 PR 中记录原因、影响范围、替代验证和后续跟踪项。

### 10.5 P3：后续 Adapter 验收边界

1. CAP 消费入口 Adapter 进入执行计划前，必须单独定义 ack、重试、失败语义、幂等和消息上下文 Feature 的边界；首批实现不接管这些语义。
2. Worker / HostedService Adapter 进入执行计划前，必须单独定义 `ExecuteAsync` 生命周期、per-item 拦截、`stoppingToken` 传播和异常处理策略；首批实现不包装 `ExecuteAsync` 生命周期。

---

## 十一、风险与处理

| 风险 | 缓解方案 |
|---|---|
| Castle 代理与 Autofac 集成版本兼容性 | 锁定 `Castle.Core`、`Autofac.Extras.DynamicProxy` 版本；集成测试覆盖代理注册与拦截调用 |
| `ValueTask` 单次消费语义被误用 | 框架内部不暴露原始 `ValueTask`，统一通过 `IUnaryInvocationContext.ProceedAsync()` 消费 |
| 拦截器依赖 Scoped 服务但被注册为 Singleton | 拦截器实例延迟至运行时按生命周期解析；若 Singleton 拦截器注入 Scoped 服务，由 DI 容器在运行时暴露错误 |
| 程序集循环引用导致拓扑排序死循环 | Kahn 算法天然检测循环：处理完毕后仍有剩余节点即为循环，启动时抛异常含完整循环路径 |
| `ICancellationTokenProvider` 在非 ASP.NET Core 场景未正确初始化 | 默认注册 `NullCancellationTokenProvider`（返回 `None`）确保兜底；各入口 Adapter 负责覆盖 ambient token |
| 方法参数含多个 `CancellationToken` | 框架取第一个 `CancellationToken` 类型参数；启动时对含多个 CT 参数的方法记录警告 |
| `GetFeature<T>()` 在非对应场景被误用 | 文档明确说明各 Feature 的有效场景；拦截器应做 null check，不假设 Feature 一定存在 |

---

## 十二、不在范围内

- 分布式配置、远程程序集发现
- 配置写回、配置中心管理界面、运行时动态新增 section
- 运行时动态注册（容器构建后修改注册）
- CAP 消费入口 Adapter、Worker / HostedService 生命周期 Adapter 的首批实现
- 方法级别的 `[IgnoreInterceptors]`（当前仅支持类/接口级别）
- 非 `Tw.` 前缀程序集的扫描（启动入口程序集与显式追加程序集除外）
- 开放泛型与 Keyed Service 的交叉场景（如 `[KeyedService("cache")][ExposeServices(typeof(IRepository<>))]`）：Key 绑定语义（Key 绑定到开放泛型定义还是封闭泛型实例）需要额外规则，初版明确不支持
