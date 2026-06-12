# DI、配置装载与 AOP 设计规格

## 目标

本设计定义自研依赖注入自动注册、配置与 Options 自动装载、AOP 抽象，以及 ASP.NET Core MVC 和 gRPC 包边界。设计参考外部框架源码中的成熟思想，但公开 API、命名空间、类型名、配置名和文档示例均使用 `Tw` 与能力语义命名，不使用参考框架原名作为代码命名。

设计目标如下：

- 使用 Autofac 接管 .NET 原生依赖注入容器
- 默认扫描 `Tw.` 前缀程序集，并支持扫描白名单与黑名单
- 基于程序集依赖关系执行拓扑计算
- 支持显式暴露服务、默认暴露规则、keyed service、泛型接口自动注册
- 非 keyed 服务契约采用单实现模型，禁止同一非 keyed 契约在运行时存在多个匿名实现
- 放弃 `Replace = true` 显式替换开关，最终实现仅由拓扑基础值、程序集优先级和类型优先级决定
- 自动发现、绑定和校验配置 Options
- 使用 Castle 实现服务方法动态代理，并抽象统一拦截器模型
- 支持 Castle 与 MVC Filter 执行同一套拦截器功能
- 放弃 Middleware 与 gRPC 对统一 `IInterceptor` 的承载适配
- 保留独立 `Tw.AspNetCore.Grpc` 包边界，但该包不把 gRPC 原生拦截器映射到统一 AOP 管线

本设计放弃“并入现有 `Tw.Core` / `Tw.AspNetCore` 且 Autofac/Castle 直接进入 `Tw.Core` 依赖”的旧决策。最终包边界与依赖方向为：`Tw.Core` 只承载框架无关抽象；`Tw.DependencyInjection` 引用 `Tw.Core`，承载 Autofac、Castle、扫描、规划、注册、Options 与 AOP 执行；`Tw.AspNetCore` 是跨协议宿主启动包，引用 `Tw.DependencyInjection` 与 `Tw.Core`，承载 webapi、控制台后台服务、gRPC 等入口的通用宿主封装，并把容器接管与服务注册再封装为统一聚合启动入口；`Tw.AspNetCore.Mvc` 引用 `Tw.AspNetCore`，承载仅 web/webapi 适用的共享能力，包含 MVC Filter 适配；`Tw.AspNetCore.Grpc` 引用 `Tw.AspNetCore`，承载 gRPC 专属共享能力与包边界，不参与本规格的统一 AOP 适配。`Tw.AspNetCore` 处于 `experimental` 阶段，本设计按采纳前破坏性整改收窄其职责：原 charter 中“中间件与过滤器”“Web 层模型绑定与结果封装”“Web 层横切关注点”等 web 专属能力（含现有 `HttpContextCancellationTokenProvider`）下移到 `Tw.AspNetCore.Mvc`，host 包只保留跨协议宿主启动与聚合入口。

### 关键设计决策

- `Tw.Core` 不引用 Autofac、Castle、ASP.NET Core MVC 或 gRPC 运行实现，只引用必要的 Microsoft.Extensions.* 契约包
- `Tw.DependencyInjection` 是执行引擎包，直接引用 Autofac、Autofac.Extensions.DependencyInjection、Autofac.Extras.DynamicProxy、Castle.Core、Castle.Core.AsyncInterceptor 和 Microsoft.Extensions.* 实现包
- 非 keyed 契约最终只注册一个默认实现；需要多个实现时必须使用 keyed service，并为每个实现声明稳定 key
- `[ServiceRegistration]` 不提供 `Replace` 属性；代码中不得出现 `Replace = true` 服务注册语义
- 同一非 keyed 契约存在多个候选时，按最终优先级仲裁唯一实现；优先级完全相同或语义平级无法判定时启动失败
- 每个候选排序键为 `TopologyBaseValue + AssemblyPriority + TypePriority`，`DiscoveryOrder` 只用于稳定诊断输出
- 同一程序集内通过类型优先级显式区分候选；不同平级程序集通过程序集优先级或类型优先级显式区分候选
- 两个候选的唯一区分项是拓扑基础值且两程序集之间无依赖可达关系时启动失败
- `UseAutofac()` 与 `AddServiceRegistration(IConfiguration)` 是 `Tw.DependencyInjection` 提供的引擎级启动原语；`Tw.AspNetCore` host 包把二者再封装为统一聚合启动入口，webapi、控制台后台服务与 gRPC 宿主共用，引擎级原语仍保留以便单独测试与按需组合
- AOP 只承载方法级调用上下文，`IInvocationContext.Method` 保持非空 `MethodInfo`
- Middleware 与 gRPC 不适配统一 `IInterceptor`；依赖 HTTP pipeline、gRPC metadata 或 streaming 语义的能力使用对应平台原生机制
- 同步拦截器不做规划阶段静态检测；误用于异步目标时由 `Proceed()` 在运行期抛出明确异常
- Options 校验始终开启，不按环境门控
- 按 key 解析复用 .NET 原生 `[FromKeyedServices]`，不自研按 key 注入特性

## 架构边界

采用“抽象在核心包，执行在引擎包，承载在专用 Web/gRPC 包”的划分，配合“元数据规划器 + Autofac 执行器”模型。

### Tw.Core：框架无关抽象

`Tw.Core` 保持无框架实现依赖定位，只承载业务代码需要标注或实现的纯抽象。包括：

- 生命周期标记接口：`ITransientDependency`、`IScopedDependency`、`ISingletonDependency`
- 注册与暴露特性：`[ServiceRegistration]`、`[DisableServiceRegistration]`、`[ExposeServices]`、`[ExposeKeyedService]`、`[ServicePriority]`、`[TwAssemblyPriority]`
- keyed 枚举契约：`KeyedServiceEntry<TService>`
- AOP 契约与基类：`IInterceptor`、`IInvocationContext`、`InterceptorBase`、`SyncInterceptorBase`、`[Intercept]`、`[DisableInterception]`、`[InterceptorOrder]`
- Options 契约与特性：`IConfigurableOptions`、`IConfigurableOptions<TOptions>`、`[OptionsSection]`、`[OptionsName]`、`[DisableOptionsBinding]`、`[SensitiveConfiguration]`、`[OptionsValidator]`

核心抽象命名空间为 `Tw.DependencyInjection.Abstractions`、`Tw.Configuration.Abstractions`、`Tw.DynamicProxy.Abstractions`、`Tw.Reflection`。`Tw.Core.csproj` 已声明 `RootNamespace` 为 `Tw`，命名空间与文件夹一一对应：DI 抽象位于 `DependencyInjection/Abstractions`，Options 抽象位于 `Configuration/Abstractions`，AOP 抽象位于 `DynamicProxy/Abstractions`，反射工具位于 `Reflection`。

核心包与引擎包不得向同一命名空间贡献类型。DI、配置、动态代理三个能力域同时存在核心抽象与引擎执行，核心侧统一加 `.Abstractions` 后缀，引擎侧使用以引擎包根命名空间 `Tw.DependencyInjection` 为前缀的执行命名空间（与「命名空间 = RootNamespace + 文件夹」规范一致），与核心侧 `.Abstractions` 命名空间互斥。反射能力（`ITypeFinder`、`TypeFinder`、`ReflectionCache`）全部归 `Tw.Core` 的 `Tw.Reflection`，引擎不向该命名空间贡献类型。

现存 `Tw.Core.Configuration` 与 `Tw.Core.Reflection` 是违背 `RootNamespace` 默认、显式写死的历史命名空间。本设计将 `Tw.Core.Configuration` 迁移为 `Tw.Configuration.Abstractions`，将 `Tw.Core.Reflection` 迁移为 `Tw.Reflection`。迁移必须同步更新 `Tw.Core/package-charter.yaml` 的 `public_capabilities`，把 `Tw.Core.Configuration`、`Tw.Core.Reflection` 替换为 `Tw.Configuration.Abstractions`、`Tw.Reflection`，并新增 `Tw.DependencyInjection.Abstractions`、`Tw.DynamicProxy.Abstractions`。

`Tw.Core` 允许引用 Microsoft.Extensions.DependencyInjection.Abstractions、Microsoft.Extensions.Configuration.Abstractions、Microsoft.Extensions.Options 这类轻量契约包，用于 `ServiceLifetime`、`IConfiguration` 与 Options 契约；不得引用 Autofac、Castle、ASP.NET Core MVC、Grpc.AspNetCore 等框架实现。

### Tw.DependencyInjection：框架绑定执行引擎

新增共享包 `Tw.DependencyInjection` 承载全部框架绑定执行实现，直接引用 Autofac、Autofac.Extensions.DependencyInjection、Autofac.Extras.DynamicProxy、Castle.Core、Castle.Core.AsyncInterceptor 和 Microsoft.Extensions.* 实现包。执行类型归入以 `Tw.DependencyInjection` 为根、按文件夹划分的引擎命名空间（`Tw.DependencyInjection`、`Tw.DependencyInjection.Configuration`、`Tw.DependencyInjection.DynamicProxy`、`Tw.DependencyInjection.Diagnostics` 等）。它提供：

- 程序集发现、拓扑排序、注册规划、单实现仲裁
- keyed service 与泛型接口注册执行
- Options 自动发现、绑定、校验与后置配置
- Castle 动态代理、拦截器管线与选择器执行
- Autofac 接管入口与聚合注册扩展

`Tw.DependencyInjection` 引用 `Tw.Core` 消费其抽象。只有组合根（宿主启动）引用 `Tw.DependencyInjection`，业务服务只依赖 `Tw.Core` 即可参与注册、Options 绑定与拦截。

### Tw.AspNetCore：跨协议宿主启动包

`Tw.AspNetCore` 是既有共享包，本设计将其定位为跨协议宿主启动包。它引用 `Tw.DependencyInjection` 与 `Tw.Core`，承载 webapi、控制台后台服务、gRPC 等入口的通用宿主封装，并把 `UseAutofac()` 与 `AddServiceRegistration(IConfiguration)` 再封装为统一聚合启动入口，使业务宿主只依赖 `Tw.AspNetCore` 即可完成容器接管与服务、Options、拦截注册。

`Tw.AspNetCore` 当前 `stability` 为 `experimental`，未被任何具体微服务引用，处于采纳前阶段，允许直接做破坏性边界整改。本设计按下述方式收窄其职责：

- host 包只保留跨协议宿主启动、聚合入口和与具体协议无关的宿主能力
- 原 charter 的 web 专属能力（“中间件与过滤器”“Web 层模型绑定与结果封装”“Web 层横切关注点”）下移到 `Tw.AspNetCore.Mvc`
- 现有 HTTP 专属能力 `HttpContextCancellationTokenProvider` 及其注册扩展从 host 包迁移到 `Tw.AspNetCore.Mvc`；控制台等无 `HttpContext` 的宿主不再被动获得 HTTP 取消令牌能力

整改必须同步更新 `Tw.AspNetCore/package-charter.yaml` 与对应使用文档。

### Tw.AspNetCore.Mvc：MVC 承载适配

新增 `Tw.AspNetCore.Mvc` 负责 web/webapi 专属共享能力与 MVC Filter 承载适配，不重新实现 DI 决策。它引用 `Tw.AspNetCore`，并由此传递获得 `Tw.DependencyInjection` 与 `Tw.Core`，消费注册计划和 AOP 元数据，提供 Controller action 与 Razor Page handler 级 Filter 适配，并承接从 host 包下移的 HTTP 取消令牌、模型绑定、结果封装与 web 横切能力。MVC Controller 与 Razor Page 等 Web 边界类型默认不启用 Castle class proxy，改用 MVC/Page Filter。

`Tw.AspNetCore.Mvc` 不提供 Middleware 适配，不把 Minimal API endpoint 纳入统一 `IInterceptor` 适配。需要 HTTP pipeline 级横切能力时，使用 ASP.NET Core 原生 Middleware。

### Tw.AspNetCore.Grpc：gRPC 包边界

新增 `Tw.AspNetCore.Grpc` 作为 gRPC 专属共享包边界，与 `Tw.AspNetCore.Mvc` 对称。它引用 host 包 `Tw.AspNetCore`（并由此传递获得 `Tw.DependencyInjection` 与 `Tw.Core`）与 Grpc.AspNetCore，用于承载 gRPC 专属注册、文档和治理边界；gRPC 通用入口与宿主启动归 `Tw.AspNetCore` host 包。本规格不在该包内实现统一 `IInterceptor` adapter，不把 `Grpc.Core.Interceptors.Interceptor` 映射到 `IInterceptorPipeline`。

gRPC 横切能力使用 gRPC 原生 interceptor 模型实现。该模型与 `Tw.DynamicProxy.Abstractions.IInterceptor` 分离，避免把 unary、client streaming、server streaming、duplex streaming 与 metadata 语义压入方法级调用上下文。

### 聚合注册入口

`Tw.DependencyInjection` 提供引擎级启动原语：

```csharp
builder.Host.UseAutofac();
builder.Services.AddServiceRegistration(builder.Configuration);
```

`UseAutofac()` 与 `AddServiceRegistration(IConfiguration)` 由 `Tw.DependencyInjection` 提供。`AddServiceRegistration` 直接从传入的 `IConfiguration` 读取自身扫描与优先级选项，不经过 Options 自动绑定子系统，避免自举循环。

`Tw.AspNetCore` host 包在引擎原语之上提供统一聚合启动入口，业务宿主只依赖 host 包即可完成接管与注册，无需各自编排 `UseAutofac()` 与 `AddServiceRegistration()` 的调用顺序。聚合入口内部调用引擎级原语，不替代它们；引擎级原语和功能级注册仍然保留，便于单独测试和按需组合。host 聚合入口命名遵循 `docs/engineering-standards/03-project-and-code/language-specific/dotnet-core.md` 的聚合入口命名要求，表达宿主职责，不使用包名或框架名作为宽泛入口名。

### Charter 同步

实现该设计时必须新增或扩展 charter：

- 新增 `Tw.DependencyInjection/package-charter.yaml`，声明引擎包职责、公开能力与依赖边界
- 扩展 `Tw.Core/package-charter.yaml`，把抽象命名空间扩展进公开能力，并在 `dependency_rules` 明确允许 Microsoft.Extensions.*.Abstractions 与 Options 轻量契约包、禁止 Autofac 与 Castle
- 修订现存 `Tw.AspNetCore/package-charter.yaml`，把职责收窄为跨协议宿主启动与聚合入口，从 `in_scope` 移除“中间件与过滤器”“Web 层模型绑定与结果封装”“Web 层横切关注点”，在 `out_of_scope` 声明这些 web 专属能力归 `Tw.AspNetCore.Mvc`，并允许依赖 `Tw.DependencyInjection`
- 新增 `Tw.AspNetCore.Mvc/package-charter.yaml`，声明 web/webapi 专属能力与 MVC Filter 承载职责并允许依赖 `Tw.AspNetCore`
- 新增 `Tw.AspNetCore.Grpc/package-charter.yaml`，声明 gRPC 专属包边界、允许依赖 `Tw.AspNetCore`、禁止声明统一 AOP adapter 能力

各包 `public_capabilities` 命名空间互斥，不出现跨包命名空间重叠：host 包对外能力收敛到宿主启动命名空间，`Tw.AspNetCore.Mvc` 与 `Tw.AspNetCore.Grpc` 各自使用独立子命名空间，host 包不再保留与二者重叠的 web 或 gRPC 专属命名空间。

## DI 自动注册

### 程序集发现

默认扫描运行时已加载程序集和依赖上下文中的 `Tw.` 前缀程序集。

配置项支持：

- `IncludeAssemblies`
- `ExcludeAssemblies`
- `IncludeAssemblyPrefixes`
- `ExcludeAssemblyPrefixes`

黑名单优先于白名单。扫描结果按程序集引用关系计算拓扑顺序。被依赖程序集排在前，依赖方排在后。发现循环引用时启动失败，并输出完整环路链路。

上述选项在 `AddServiceRegistration(IConfiguration)` 入口直接从 `IConfiguration` 读取，绑定路径为 `Tw:DependencyInjection`。

### 参与注册的类型

具体类型满足以下任一条件时参与自动注册：

- 实现 `ITransientDependency`
- 实现 `IScopedDependency`
- 实现 `ISingletonDependency`
- 标记 `[ServiceRegistration]`

类型标记 `[DisableServiceRegistration]` 时跳过自动注册。

抽象类、接口、未闭合泛型、DTO、Options 类型不作为普通服务注册。

### 生命周期

标记接口优先决定生命周期。`[ServiceRegistration(Lifetime = ...)]` 可以覆盖生命周期。

同一类型声明多个生命周期标记时启动失败。未声明生命周期的类型不注册。

### 暴露服务

显式暴露通过 `[ExposeServices]`：

```csharp
[ExposeServices(typeof(IOrderService), IncludeSelf = true)]
public sealed class OrderService : IOrderService, IScopedDependency
{
}
```

没有显式暴露时，默认暴露：

- 自身类型
- 与实现类命名匹配的接口，例如 `OrderService` 暴露 `IOrderService`
- 泛型实现对应的泛型接口定义，例如 `Repository<TEntity>` 暴露 `IRepository<TEntity>`

默认规则不暴露所有接口，避免把生命周期标记接口、框架接口、横切接口暴露为业务服务。

### 注册模型：非 keyed 单实现

非 keyed 契约采用单实现模型：

- 每个非 keyed 契约最终只注册一个实现
- 同一非 keyed 契约的多个候选按最终优先级仲裁唯一实现
- 仲裁落选候选记入诊断报告，标记为 `superseded`
- 落选候选不进入非 keyed `IEnumerable<T>`，不形成运行时匿名多实现
- 需要同一契约多个实现时，必须声明 keyed service，并使用不同的稳定 key

本模型不提供非 keyed 的 enumerable 多实现解析。非 keyed `IEnumerable<T>` 在自动注册场景下不作为多实现消费入口。

### Keyed Service

`[ExposeKeyedService]` 用于注册：标在实现类上，`[ExposeKeyedService(契约, key)]` 生成 Autofac keyed 注册。按 key 解析复用 .NET 原生 `[FromKeyedServices("key")]`，不自研按 key 注入特性。

```csharp
[ExposeKeyedService(typeof(IPaymentProvider), "wechat")]
public sealed class WechatPaymentProvider : IPaymentProvider, IScopedDependency
{
}

public sealed class CheckoutService : IScopedDependency
{
    public CheckoutService([FromKeyedServices("wechat")] IPaymentProvider provider) { }
}
```

按单个 key 解析复用 .NET 原生 `[FromKeyedServices("key")]`。枚举某契约的全部 keyed 实现使用显式契约，不复用非 keyed `IEnumerable<TService>`，也不依赖运行时是否支持 `KeyedService.AnyKey` 的容器差异。

引擎为每个存在 keyed 注册的契约额外登记带 key 元数据的可枚举条目，消费方注入 `IEnumerable<KeyedServiceEntry<TService>>` 即可遍历该契约的全部 keyed 实现及其 key：

```csharp
namespace Tw.DependencyInjection.Abstractions;

/// <summary>携带 key 元数据的 keyed 服务条目</summary>
public readonly record struct KeyedServiceEntry<TService>(object Key, TService Service)
    where TService : notnull;

public sealed class PaymentRouter(IEnumerable<KeyedServiceEntry<IPaymentProvider>> providers)
    : IScopedDependency
{
    private readonly IReadOnlyList<KeyedServiceEntry<IPaymentProvider>> _providers = providers.ToList();
}
```

`KeyedServiceEntry<TService>` 定义在 `Tw.DependencyInjection.Abstractions`（归 `Tw.Core`），使业务服务无需引用引擎包即可枚举。每个条目的 `Service` 尊重对应 keyed 注册的生命周期，枚举在当前 scope 内解析，不缓存为单例。`Key` 即注册时声明的稳定 key。

非 keyed 单实现注册与 keyed 注册可以同时存在。key 为空时启动失败。同一 key 指向多个候选且无法通过优先级仲裁出唯一实现时启动失败。`[FromKeyedServices]` 指向未注册的 key 时启动失败。

### 优先级与单实现仲裁

每个注册候选生成排序键：

```text
FinalPriority =
  TopologyBaseValue
  + AssemblyPriority
  + TypePriority
```

`DiscoveryOrder` 只用于稳定诊断输出顺序，不参与解决单实现冲突。

`TopologyBaseValue` 根据程序集拓扑顺序生成基础值。被依赖程序集基础值低，依赖方基础值高。基础值步长大于允许的显式优先级范围，避免显式优先级反向覆盖架构层级。拓扑层级步长采用 `1_000_000`，程序集与类型显式优先级范围采用 `-100_000..100_000`。注册规划必须保证 `|AssemblyPriority| + |TypePriority| < 拓扑层级步长`，最坏情况 `100_000 + 100_000 = 200_000 < 1_000_000`。`FinalPriority` 与各分量采用 `long` 计算，避免深拓扑层级累加溢出。

`AssemblyPriority` 表示程序集级显式优先级。支持程序集特性和配置覆盖：

```csharp
[assembly: TwAssemblyPriority(100)]
```

配置优先于程序集特性：

```json
{
  "Tw": {
    "DependencyInjection": {
      "AssemblyPriorities": {
        "Tw.Order.Application": 100,
        "Tw.Order.Infrastructure": 80
      }
    }
  }
}
```

`TypePriority` 表示类型级显式优先级。支持 `[ServicePriority]` 或 `[ServiceRegistration(Priority = ...)]`。

同一非 keyed 契约或同一 keyed 契约存在多个候选时，最终优先级最高者成为唯一实现，其余候选记入诊断报告。

仲裁失败时启动失败：

- 最终优先级完全相同
- 同一程序集内类型优先级相同且无其他区分项
- 不同平级程序集显式优先级相同且无其他区分项
- 两个候选的唯一区分项是 `TopologyBaseValue`，且两候选所在程序集之间不存在依赖可达关系（含传递依赖，语义平级）

最后一条要求开发者在语义平级、互不可达的程序集之间用 `[TwAssemblyPriority]`、配置 `AssemblyPriorities` 或 `[ServicePriority]` 显式表达优先级，杜绝仅靠拓扑全序产生的无声胜者。当一候选所在程序集传递可达另一候选所在程序集时，下游（依赖方）拓扑基础值高者直接胜出，符合下游覆盖上游默认的分层语义，不触发失败。

### 放弃显式替换开关

本设计不提供 `Replace`、`ReplaceServices`、`TryReplace` 或等价显式替换开关。服务替换语义由单实现仲裁自然产生：同一契约多个候选经过优先级计算后只注册唯一胜者。

`[ServiceRegistration]` 只承载生命周期、优先级和注册行为所需元数据，不承载替换布尔值。实现与测试中不得出现 `Replace = true` 的服务注册路径。

### 泛型与 keyed 的仲裁参与

open generic 注册按其泛型定义参与同一套单实现仲裁，闭合类型在解析时继承定义级仲裁结果，不单独参与替换。keyed service 的仲裁在同一 key 维度内独立进行：同一 key 的候选按最终优先级仲裁出唯一实现，跨 key 不相互替换。同时声明 keyed 与默认暴露的候选在两个维度分别参与仲裁。

## 配置与 Options 自动装载

`Tw.Core` 复用 `Tw.Configuration.Abstractions.IConfigurableOptions`（由 `Tw.Core.Configuration` 迁移而来）作为入口标记，并扩展为完整 Options 契约：

```csharp
public interface IConfigurableOptions;

public interface IConfigurableOptions<TOptions> : IConfigurableOptions
    where TOptions : class, IConfigurableOptions
{
    void PostConfigure(TOptions options, IConfiguration configuration);
}
```

发现按非泛型 `IConfigurableOptions` 标记判定，候选 Options 只需实现该标记即可参与绑定。`PostConfigure` 只对实现泛型 `IConfigurableOptions<TOptions>` 的类型执行；仅实现非泛型标记的类型不执行后置配置。类型实现泛型契约时，泛型参数 `TOptions` 必须等于自身类型，否则启动失败。

### 发现范围

只扫描已纳入 DI 扫描计划的程序集。Options 类型必须满足：

- 非抽象类
- 具备公共无参构造函数
- 实现 `IConfigurableOptions`
- 未标记 `[DisableOptionsBinding]`

### 配置节路径

默认路径为类型名去掉 `Options` 后缀，例如 `CacheOptions` 绑定 `Cache`。

通过 `[OptionsSection("Tw:Cache")]` 显式指定路径。配置路径必须稳定，不从环境名拼接。

### 绑定与校验

自动调用：

```csharp
services.AddOptions<TOptions>()
    .Bind(configuration.GetSection(path))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

`ValidateOnStart()` 始终开启，不按环境门控，使各环境校验行为一致，缺失必填配置在启动期统一暴露。

类型实现 `IValidateOptions<TOptions>` 或通过 `[OptionsValidator(typeof(...))]` 指定验证器时，自动注册验证器。`Tw.Configuration.Abstractions.OptionsValidatorAttribute` 与 `Microsoft.Extensions.Options` 内置源生成器属性同名但命名空间不同，不构成冲突；同一文件同时引用两者时使用命名空间限定或 alias 区分。

缺失必填配置时启动失败。失败信息输出配置路径和校验原因，不输出敏感值。

### 后置配置

实现 `IConfigurableOptions<TOptions>` 的类型自动执行 `PostConfigure`。引擎通过对绑定 section 的闭包捕获把 `IConfiguration` 传入实例的 `PostConfigure`，不依赖 `Microsoft.Extensions.Options` 原生 `PostConfigure<T>` 签名。

`PostConfigure` 只用于补默认值、组合校验和派生非敏感字段，不使用 Service Locator 解析服务。

### 敏感配置

单一特性 `[SensitiveConfiguration]` 同时支持标记类型和属性：标记类型时整类视为敏感，标记属性时仅该属性视为敏感。

诊断报告只输出路径、绑定状态和校验结果，不输出值。密钥必须来自 Secret、密钥管理服务、部署平台安全变量或受控配置中心。

### 命名 Options

通过 `[OptionsName("name")]` 为 Options 类型声明命名实例，未标记的类型使用 `Options.DefaultName`。同一类型需要多个命名实例时，必须用配置显式声明每个名称，不隐式生成多实例。

## AOP 抽象与承载

### 核心抽象

`Tw.Core` 在 `Tw.DynamicProxy.Abstractions` 命名空间提供统一 AOP 抽象：

```csharp
public interface IInterceptor
{
    ValueTask InterceptAsync(IInvocationContext context);
}

public interface IInvocationContext
{
    MethodInfo Method { get; }
    object? Target { get; }
    object?[] Arguments { get; }
    IReadOnlyDictionary<string, object?> ArgumentsByName { get; }
    object? ReturnValue { get; set; }
    ValueTask ProceedAsync();
    void Proceed();
}
```

`IInvocationContext` 表示一次方法级调用上下文，可以适配 Castle invocation 和 MVC action。它不适配 Middleware 请求，也不适配 gRPC 原生 interceptor。

`Arguments` 可写：拦截器在调用 `Proceed`/`ProceedAsync` 前改写元素即可向目标传递修改后的入参。`ArgumentsByName` 是只读视图，仅用于按名读取，不用于写回。Castle 与 MVC action 支持把改写后的 `Arguments` 回写到底层调用。`ReturnValue` 在 `Proceed`/`ProceedAsync` 之后可被拦截器改写。

`Tw.DynamicProxy.Abstractions.IInterceptor` 与 `Castle.DynamicProxy.IInterceptor` 同名但命名空间不同。引擎内部适配器引用 Castle 类型时使用命名空间限定或 alias。

### 同步方法拦截

核心接口保留 `InterceptAsync` 作为统一入口。

同步目标方法由 adapter 包装：

- 目标方法为同步方法时，`ProceedAsync()` 直接调用同步方法，写入 `ReturnValue`，返回完成的 `ValueTask`
- 目标方法为 `Task`、`Task<T>`、`ValueTask`、`ValueTask<T>` 时，`ProceedAsync()` 等待完成后写入 `ReturnValue`
- `Proceed()` 只允许同步目标调用，异步目标调用会抛出明确异常

同步拦截器不做规划阶段静态检测。继承 `SyncInterceptorBase` 的拦截器应只用于同步目标；误用于异步目标时，`Proceed()` 在运行期抛出明确异常，并由调用方所在承载记录异常上下文。

同步拦截器通过基类快速实现：

```csharp
public abstract class SyncInterceptorBase : IInterceptor
{
    public ValueTask InterceptAsync(IInvocationContext context)
    {
        Before(context);
        try
        {
            context.Proceed();
        }
        catch (Exception ex)
        {
            OnException(context, ex);
            throw;
        }
        finally
        {
            After(context);
        }
        return ValueTask.CompletedTask;
    }

    protected virtual void Before(IInvocationContext context) { }
    protected virtual void After(IInvocationContext context) { }
    protected virtual void OnException(IInvocationContext context, Exception exception) { }
}
```

`After` 在 `finally` 中执行，保证目标方法抛异常时计时、释放等收尾逻辑仍然运行；`OnException` 仅在目标抛异常时触发，默认实现不吞异常。`InterceptorBase` 异步基类提供对称的 `await context.ProceedAsync()` 与 `try/finally` 结构。

### 拦截器管线

`IInterceptorPipeline` 负责执行最终拦截器链。Castle 与 MVC Filter adapter 只负责创建 `IInvocationContext`，然后调用同一个 pipeline。

同一个方法级横切功能只实现一次 `Tw.DynamicProxy.Abstractions.IInterceptor`。审计、日志、事务、幂等、异常转换等方法级横切能力不为 Castle 与 MVC 重复实现。

依赖 HTTP response、MVC model state、gRPC metadata、streaming 调用或 HTTP pipeline 顺序的功能不实现为通用 `IInterceptor`，使用对应平台原生机制。

### 拦截器选择

自动拦截通过 `IInterceptorSelector` 决定。selector 可以按实现类型、服务契约、方法、特性、命名空间、程序集匹配拦截器。

特性拦截通过 `[Intercept(typeof(AuditInterceptor))]` 声明。特性可放在类、接口或方法上。方法级优先于类型级。

`[DisableInterception]` 关闭类或方法拦截。

拦截器顺序通过 `[InterceptorOrder]` 指定。顺序相同按类型名称稳定排序。同一拦截器类型不会重复加入同一调用链。

### Castle 承载

Autofac 执行阶段根据注册计划启用 Castle DynamicProxy。

优先使用 `EnableInterfaceInterceptors()`。没有接口暴露且方法可代理时使用 `EnableClassInterceptors()`。不可代理方法进入诊断报告。

### MVC Filter 承载

`Tw.AspNetCore.Mvc` 提供 MVC/Page Filter adapter。Controller action 与 Razor Page handler 由 Filter 创建 `IInvocationContext` 并调用 `IInterceptorPipeline`。

MVC Filter adapter 只处理 MVC 可定位到方法信息和参数字典的调用。Middleware、Minimal API endpoint 与 gRPC 方法不进入该 adapter。

adapter 以 `ActionDescriptor.Parameters` 的声明顺序建立稳定的「位置 ↔ 参数名」映射：`IInvocationContext.Method` 取自 action 对应 `MethodInfo`，`Arguments` 按该顺序物化自 `ActionExecutingContext.ActionArguments`，`ArgumentsByName` 是其只读按名视图。拦截器在 `Proceed`/`ProceedAsync` 前改写 `Arguments[i]` 后，adapter 用同一映射把元素回写到 `ActionArguments[参数名]`，保证位置数组改写对 MVC action 生效。无法按名定位到 action 参数的调用不进入该 adapter，进入诊断报告。

### gRPC 不接入统一 AOP

`Tw.AspNetCore.Grpc` 不提供 `IInterceptorPipeline` adapter。gRPC 服务需要横切能力时，直接实现或注册 gRPC 原生 interceptor。

该包的文档必须明确：`Tw.DynamicProxy.Abstractions.IInterceptor` 不用于 gRPC 调用链，gRPC interceptor 与 Castle/MVC 方法级拦截器是两套独立承载。

## 诊断与错误处理

`Tw.DependencyInjection` 提供统一诊断对象，不直接写文件。

### 诊断报告

`ServiceRegistrationReport` 记录：

- 程序集扫描结果
- 拓扑层级
- 候选服务
- 最终注册
- 仲裁结果与 superseded 候选
- keyed service 与 open generic 注册
- 跳过原因
- 冲突原因

`OptionsBindingReport` 记录：

- Options 类型
- 配置路径
- 命名实例
- section 是否存在
- 绑定状态
- 验证器
- 启动校验结果
- 敏感标记

`InterceptionReport` 记录：

- 服务与方法
- 承载方式
- 命中的 selector
- 最终拦截器顺序
- 不可代理原因
- MVC Filter 承载结果

常驻报告默认注册为 singleton，只保留摘要。完整候选与拓扑图谱需要显式启用，且不输出敏感配置值或方法参数值。生产日志只输出摘要。

### 启动失败规则

以下场景启动失败：

- 程序集拓扑存在循环依赖
- 严格模式下白名单或黑名单指向不存在的程序集
- 类型同时声明多个生命周期
- 非 keyed 契约多个候选最终优先级完全相同
- 非 keyed 候选唯一区分项为拓扑基础值且所在程序集间无依赖可达关系（含传递依赖）
- keyed service 的 key 为空
- 同 key 候选无法仲裁出唯一实现
- Options 必填 section 缺失
- Options 验证失败
- Options 路径重复
- 拦截器类型未注册（被 `[Intercept]` 或 selector 命中但不在注册计划内）
- `[FromKeyedServices]` 指向未注册的 key

同步拦截器命中异步方法不作为启动失败规则；运行期调用 `Proceed()` 时失败。

不可代理的类型或方法不作为启动失败规则：按「Castle 承载」节进入 `InterceptionReport` 诊断报告，状态记为 `skipped`。拦截器实例的生命周期由其自身注册声明决定，引擎在解析侧不做额外生命周期合法性门控。

## 实现分期

本设计覆盖 DI 自动注册、Options 自动装载、AOP 抽象、宿主聚合、MVC 承载和 gRPC 包边界。它们共享程序集发现与拓扑基础设施，统一在一份设计内描述，但实现必须按依赖顺序切成独立可交付、可单独测试的阶段，每个阶段对应一份独立实现计划。不得在单一计划内一次性实现全部子系统。

| 阶段 | 范围 | 产出包 | 依赖 |
| --- | --- | --- | --- |
| P0 抽象地基 | 生命周期标记接口、注册/暴露/优先级特性、AOP 契约与基类、Options 契约与特性，落在 `.Abstractions` 命名空间；`Tw.Core.Configuration`/`Tw.Core.Reflection` 迁移为 `Tw.Configuration.Abstractions`/`Tw.Reflection`；新增 Configuration.Abstractions 与 Options 包引用；`Tw.Core` charter 更新 | `Tw.Core` | 无 |
| P1 扫描地基 | 程序集发现、白/黑名单、拓扑排序、循环诊断、`UseAutofac()` 接管、`ServiceRegistrationReport` 骨架；新增 `Tw.DependencyInjection` 包与 charter | `Tw.DependencyInjection` | P0 |
| P2 DI 注册 | 参与注册判定、生命周期、默认与显式暴露、keyed service、open generic、非 keyed 单实现仲裁、平级失败规则、`AddServiceRegistration()` | `Tw.DependencyInjection` | P1 |
| P3 Options 装载 | 发现、路径推导、绑定、校验、`PostConfigure`、敏感标记、命名 Options、`OptionsBindingReport` | `Tw.DependencyInjection` | P1 |
| P4 AOP 承载 | `IInterceptorPipeline`、`IInterceptorSelector`、特性拦截、Castle interface/class proxy、`InterceptionReport` | `Tw.DependencyInjection` | P0、P2 |
| P5 宿主聚合 | `Tw.AspNetCore` host 包引用 `Tw.DependencyInjection`，把 `UseAutofac()` 与 `AddServiceRegistration()` 再封装为统一聚合启动入口；更新 host 包使用文档 | `Tw.AspNetCore` | P2、P3 |
| P6 MVC 承载 | MVC Filter 复用同一 pipeline，MVC 边界类型避开 class proxy；新增 `Tw.AspNetCore.Mvc` 包与 charter；把 `HttpContextCancellationTokenProvider` 及 web 横切、模型绑定、结果封装能力从 host 包迁入；收窄 `Tw.AspNetCore` charter | `Tw.AspNetCore.Mvc` | P4、P5 |
| P7 gRPC 包边界 | 新增 `Tw.AspNetCore.Grpc` 包、charter 与使用文档；明确 gRPC 不接入统一 `IInterceptorPipeline` | `Tw.AspNetCore.Grpc` | P5 |

P3 只依赖 P1 的扫描计划，不依赖 P2 的注册仲裁，可与 P2 并行。P5 依赖 P2、P3 的注册与 Options 装载入口。P6 依赖 P4 的 AOP pipeline 与 P5 的 host 聚合，并在本阶段完成 host 包 web 专属能力下移与 charter 收窄，避免 charter 声明与代码归属不一致。P7 依赖 P5 的 host 包，只建立 gRPC 包边界和文档，不依赖 P4。各阶段计划必须包含本阶段对应的使用文档与索引联动，不把全部文档堆到最后一个阶段。

## 测试策略

### 单元测试

单元测试覆盖核心决策逻辑：

- 程序集拓扑排序与循环诊断
- 服务暴露规则，包括显式暴露、默认命名匹配、open generic、keyed service
- 最终优先级计算，包括拓扑基础值、程序集优先级、类型优先级
- 非 keyed 单实现仲裁、superseded 候选记录、优先级相等失败、平级无依赖边失败
- keyed 多 key 注册与同 key 单实现仲裁
- `IEnumerable<KeyedServiceEntry<TService>>` 枚举返回某契约全部 keyed 实现，携带正确 key 且尊重各实现生命周期
- `[FromKeyedServices]` 指向未注册 key 时启动失败
- `[ServiceRegistration]` 不存在替换属性，规划器不存在显式替换分支
- Options 路径推导、显式路径、命名 Options、DataAnnotations、验证器、敏感标记
- `IConfigurableOptions<TOptions>.PostConfigure` 通过闭包获得 `IConfiguration`，补默认值与组合校验生效，且不解析服务
- 泛型 Options 契约 `TOptions` 不等于自身类型时启动失败
- `IInterceptorPipeline` 对同步方法、`Task`、`Task<T>`、`ValueTask`、`ValueTask<T>` 的调用行为
- `Proceed()` 命中异步目标时抛出明确异常

### 集成测试

集成测试覆盖 Autofac、Castle 和 ASP.NET Core MVC：

- `UseAutofac()` 接管默认容器
- 非 keyed 单实现服务、keyed service、open generic 可以解析
- 非 keyed 同契约多候选只注册仲裁胜者
- 同契约多 keyed 实现可通过 `IEnumerable<KeyedServiceEntry<TService>>` 枚举，并按单 key 经 `[FromKeyedServices]` 解析
- `Tw.AspNetCore` host 聚合入口在 webapi 宿主完成容器接管与服务、Options、拦截注册
- Castle interface proxy 和 class proxy 执行同一套 `Tw.DynamicProxy.Abstractions.IInterceptor`
- `Tw.AspNetCore.Mvc` 的 MVC Filter 执行同一套 `IInterceptorPipeline`，拦截器改写 `Arguments` 对 action 生效
- `Tw.AspNetCore.Grpc` 不注册统一 AOP adapter
- 诊断报告在真实 Host 中可读取且不泄露敏感值

## 文档与治理要求

### Charter 与新增包

实现该设计时必须同步新增或扩展 charter：

- 新增 `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/package-charter.yaml`，声明引擎包职责、公开能力与依赖边界
- 扩展 `backend/dotnet/BuildingBlocks/src/Tw.Core/package-charter.yaml` 的抽象公开能力与依赖边界
- 修订 `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/package-charter.yaml`，把职责收窄为跨协议宿主启动与聚合入口，移除 web 专属 `in_scope`、在 `out_of_scope` 声明 web 能力归 `Tw.AspNetCore.Mvc`、允许依赖 `Tw.DependencyInjection`
- 新增 `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore.Mvc/package-charter.yaml`，声明 web/webapi 专属能力与 MVC Filter 承载职责并允许依赖 `Tw.AspNetCore`
- 新增 `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore.Grpc/package-charter.yaml`，声明 gRPC 专属包边界、允许依赖 `Tw.AspNetCore`、明确不承载统一 AOP adapter

### 使用文档

按 `docs/engineering-standards/03-project-and-code/shared-package-charter.md` 的能力使用文档要求，创建以下 How-to Guide 文档：

- `docs/shared-packages/dotnet/Tw.DependencyInjection/service-registration.md`：DI 自动注册
- `docs/shared-packages/dotnet/Tw.DependencyInjection/options-binding.md`：配置与 Options 自动装载
- `docs/shared-packages/dotnet/Tw.DependencyInjection/dynamic-proxy-interception.md`：Castle AOP 拦截
- `docs/shared-packages/dotnet/Tw.AspNetCore/host-startup.md`：跨协议宿主启动与聚合入口
- `docs/shared-packages/dotnet/Tw.AspNetCore.Mvc/mvc-interception.md`：MVC Filter 承载适配
- `docs/shared-packages/dotnet/Tw.AspNetCore.Grpc/grpc-integration.md`：gRPC 包边界与原生 interceptor 使用边界

每篇使用文档必须覆盖能力定位、DI 注册方式、各入口使用方式和注意事项。

### 索引联动

同步更新各层 Reference 索引，保证从总索引可跳转到上述使用文档：

- `docs/shared-packages/README.md`
- `docs/shared-packages/dotnet/README.md`
- 新增 `docs/shared-packages/dotnet/Tw.DependencyInjection/README.md`
- `docs/shared-packages/dotnet/Tw.Core/README.md`
- `docs/shared-packages/dotnet/Tw.AspNetCore/README.md`
- 新增 `docs/shared-packages/dotnet/Tw.AspNetCore.Mvc/README.md`
- 新增 `docs/shared-packages/dotnet/Tw.AspNetCore.Grpc/README.md`

### API 与命名

公开 API 必须提供 XML 文档注释。配置项必须说明路径、用途、类型、默认行为、是否必填、是否敏感和变更影响。

命名空间必须与文件夹路径匹配，遵守 `docs/engineering-standards/03-project-and-code/language-specific/dotnet-core.md` 的命名空间规则。

所有代码命名不得包含参考框架原名。设计和实现统一使用 `Tw` 命名空间与能力语义命名。
