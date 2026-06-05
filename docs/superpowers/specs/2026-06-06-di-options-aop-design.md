# DI、配置装载与 AOP 设计规格

## 目标

本设计定义 `Tw.Core` 与 `Tw.AspNetCore` 中自研依赖注入自动注册、配置与选项自动装载、AOP 抽象和承载适配能力。

设计目标如下：

- 使用 Autofac 接管 .NET 原生依赖注入容器
- 默认扫描 `Tw.` 前缀程序集，并支持扫描白名单与黑名单
- 基于程序集依赖关系执行拓扑计算
- 支持显式暴露服务、默认暴露规则、keyed service、泛型接口自动注册
- 支持基于拓扑基础值、程序集优先级、类型优先级和替换标记的服务替换仲裁
- 自动发现、绑定和校验配置选项
- 使用 Castle 实现动态代理，并抽象统一拦截器模型
- 支持 Castle、MVC Filter、Middleware、gRPC Interceptor 复用同一套拦截器功能

设计不引入新的共享包。核心能力并入 `Tw.Core`，Web 与 gRPC 承载适配并入 `Tw.AspNetCore`。

## 架构边界

采用“元数据规划器 + Autofac 执行器”架构。

`Tw.Core` 负责框架核心能力，并直接引用 Autofac、Autofac.Extensions.DependencyInjection、Autofac.Extras.DynamicProxy、Castle.Core、Castle.Core.AsyncInterceptor。核心能力命名空间包括：

- `Tw.DependencyInjection`
- `Tw.Configuration`
- `Tw.DynamicProxy`
- `Tw.Reflection`

`Tw.Core` 提供程序集发现、拓扑排序、注册规划、服务替换仲裁、Options 自动绑定、Castle 动态代理、拦截器抽象和 Autofac 接管入口。

`Tw.AspNetCore` 负责 Web 承载适配，不重新实现 DI 决策。它消费 `Tw.Core` 生成的注册计划和 AOP 元数据，提供 MVC Filter、Middleware、gRPC Interceptor，以及 Web 类型默认避开 Castle class proxy 的策略。

服务应用入口使用聚合注册：

```csharp
builder.Host.UseTwAutofac();
builder.Services.AddServiceRegistration();
```

功能级注册仍然保留，便于单独测试和按需组合。

`Tw.Core/package-charter.yaml` 必须同步扩展职责、公开能力和依赖边界，因为 Autofac 和 Castle 会进入核心共享包依赖范围。

## DI 自动注册

### 程序集发现

默认扫描运行时已加载程序集和依赖上下文中的 `Tw.` 前缀程序集。

配置项支持：

- `IncludeAssemblies`
- `ExcludeAssemblies`
- `IncludeAssemblyPrefixes`
- `ExcludeAssemblyPrefixes`

黑名单优先于白名单。扫描结果按程序集引用关系计算拓扑顺序。被依赖程序集排在前，依赖方排在后。发现循环引用时启动失败，并输出完整环路链路。

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

### Keyed Service

通过 `[ExposeKeyedService]` 或 `[ServiceKey]` 生成 Autofac keyed 注册：

```csharp
[ExposeKeyedService(typeof(IPaymentProvider), "wechat")]
public sealed class WechatPaymentProvider : IPaymentProvider, IScopedDependency
{
}
```

普通服务和 keyed service 可以同时存在。key 为空时启动失败。同一 key 指向多个单实现候选且无法通过优先级仲裁时启动失败。

### 优先级与服务替换

每个注册候选生成排序键：

```text
FinalPriority =
  TopologyBaseValue
  + AssemblyPriority
  + TypePriority
  + ReplaceWeight
```

`DiscoveryOrder` 只用于稳定输出顺序，不参与解决单实现冲突。

`TopologyBaseValue` 根据程序集拓扑顺序生成基础值。被依赖程序集基础值低，依赖方基础值高。基础值步长大于允许的显式优先级范围，避免显式优先级反向覆盖架构层级。拓扑层级步长采用 `1_000_000`，程序集与类型显式优先级范围采用 `-100_000..100_000`。

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

`ReplaceWeight` 表示显式替换意图。候选声明 `Replace = true` 时获得固定权重。该权重不跨越拓扑基础值边界。

默认情况下同一服务允许多实现。当服务契约被声明为单实现，或候选声明 `Replace = true` 时，最终优先级最高者成为默认实现，其余候选保留为 enumerable 或 keyed 注册。

冲突规则如下：

- 最终优先级完全相同且多个候选都要求替换同一默认实现时启动失败
- 同一程序集内类型优先级相同且都要求替换同一默认实现时启动失败
- 不同平级程序集显式优先级相同且都要求替换同一默认实现时启动失败
- 多实现契约不失败，按最终优先级和发现顺序输出 enumerable 顺序

## 配置与 Options 自动装载

`Tw.Core` 复用 `Tw.Core.Configuration.IConfigurableOptions` 作为入口标记，并扩展为完整 Options 契约：

```csharp
public interface IConfigurableOptions;

public interface IConfigurableOptions<TOptions> : IConfigurableOptions
    where TOptions : class, IConfigurableOptions
{
    void PostConfigure(TOptions options, IConfiguration configuration);
}
```

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

类型实现 `IValidateOptions<TOptions>` 或通过 `[OptionsValidator(typeof(...))]` 指定验证器时，自动注册验证器。

生产环境缺失必填配置时启动失败。失败信息输出配置路径和校验原因，不输出敏感值。

### 后置配置

实现 `IConfigurableOptions<TOptions>` 的类型自动执行 `PostConfigure`。

`PostConfigure` 只用于补默认值、组合校验和派生非敏感字段，不使用 Service Locator 解析服务。

### 敏感配置

支持 `[SensitiveOptions]` 和 `[SensitiveConfiguration]` 标记类型或属性。

诊断报告只输出路径、绑定状态和校验结果，不输出值。密钥必须来自 Secret、密钥管理服务、部署平台安全变量或受控配置中心。

### 命名 Options

支持 `[OptionsName("name")]` 和 `[NamedOptions]`。未命名实例使用 `Options.DefaultName`。

同一类型多个名称必须显式配置，避免隐式多实例。

## AOP 抽象与承载

### 核心抽象

`Tw.Core` 提供统一 AOP 抽象：

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

`IInvocationContext` 表示一次调用上下文，可以适配 Castle invocation、MVC action、Middleware 请求和 gRPC 方法。它不命名为 method invocation，避免把上下文限制在 Castle 方法代理场景。

`Tw.DynamicProxy.IInterceptor` 与 `Castle.DynamicProxy.IInterceptor` 同名但命名空间不同。内部适配器引用 Castle 类型时使用命名空间限定或 alias。

### 同步方法拦截

核心接口保留 `InterceptAsync` 作为统一入口。

同步目标方法由 adapter 包装：

- 目标方法为同步方法时，`ProceedAsync()` 直接调用同步方法，写入 `ReturnValue`，返回完成的 `ValueTask`
- 目标方法为 `Task`、`Task<T>`、`ValueTask`、`ValueTask<T>` 时，`ProceedAsync()` 等待完成后写入 `ReturnValue`
- `Proceed()` 只允许同步目标调用，异步目标调用会抛出明确异常

同步拦截器通过基类快速实现：

```csharp
public abstract class SyncInterceptorBase : IInterceptor
{
    public ValueTask InterceptAsync(IInvocationContext context)
    {
        Before(context);
        context.Proceed();
        After(context);
        return ValueTask.CompletedTask;
    }

    protected virtual void Before(IInvocationContext context) { }
    protected virtual void After(IInvocationContext context) { }
}
```

异步拦截器可以继承 `InterceptorBase`，在统一异步管线内调用 `await context.ProceedAsync()`。

### 拦截器管线

`IInterceptorPipeline` 负责执行最终拦截器链。Castle、MVC、Middleware、gRPC adapter 都只负责创建 `IInvocationContext`，然后调用同一个 pipeline。

同一个功能只实现一次 `Tw.DynamicProxy.IInterceptor`。审计、日志、事务、幂等、异常转换等横切能力不为 Castle、MVC、gRPC 重复实现。

只有依赖 HTTP response、MVC model state、gRPC metadata 的功能才使用 Web 或 gRPC 专用 adapter 或专用拦截器。

### 拦截器选择

自动拦截通过 `IInterceptorSelector` 决定。selector 可以按实现类型、服务契约、方法、特性、命名空间、程序集匹配拦截器。

特性拦截通过 `[Intercept(typeof(AuditInterceptor))]` 声明。特性可放在类、接口或方法上。方法级优先于类型级。

`[DisableInterception]` 关闭类或方法拦截。

拦截器顺序通过 `[InterceptorOrder]` 指定。顺序相同按类型名称稳定排序。同一拦截器类型不会重复加入同一调用链。

### Castle 承载

Autofac 执行阶段根据注册计划启用 Castle DynamicProxy。

优先使用 `EnableInterfaceInterceptors()`。没有接口暴露且方法可代理时使用 `EnableClassInterceptors()`。不可代理方法进入诊断报告。

### Web 与 gRPC 替代承载

`Tw.AspNetCore` 提供 Web adapter。

MVC Controller、Razor Page、ViewComponent、Minimal API endpoint 等 Web 边界类型默认不启用 Castle class proxy，改用 MVC Filter 或 Middleware。

gRPC 方法默认不用 Castle 代理。`Tw.AspNetCore` 提供 gRPC adapter，把同一套 `IInterceptor` 语义映射到 `Grpc.Core.Interceptors.Interceptor`。

## 诊断与错误处理

`Tw.Core` 提供统一诊断对象，不直接写文件。

### 诊断报告

`ServiceRegistrationReport` 记录：

- 程序集扫描结果
- 拓扑层级
- 候选服务
- 最终注册
- 替换结果
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
- Web 与 gRPC 替代承载结果

报告默认注册为 singleton，可通过测试或排障代码读取。生产日志只输出摘要。详细报告需要显式启用，并且不输出敏感配置值或方法参数值。

### 启动失败规则

以下场景启动失败：

- 程序集拓扑存在循环依赖
- 严格模式下白名单或黑名单指向不存在的程序集
- 类型同时声明多个生命周期
- 单实现契约或 `Replace = true` 的候选最终优先级完全相同
- keyed service 的 key 为空
- 同 key 单实现候选无法完成优先级仲裁
- Options 必填 section 缺失
- Options 验证失败
- Options 路径重复
- 拦截器类型未注册
- 拦截器生命周期不合法
- 类型不可代理且没有替代承载

## 测试策略

### 单元测试

单元测试覆盖核心决策逻辑：

- 程序集拓扑排序与循环诊断
- 服务暴露规则，包括显式暴露、默认命名匹配、open generic、keyed service
- 最终优先级计算，包括拓扑基础值、程序集优先级、类型优先级、替换权重、发现顺序
- 单实现替换、多实现保留、冲突启动失败
- Options 路径推导、显式路径、命名 Options、DataAnnotations、验证器、敏感标记
- `IInterceptorPipeline` 对同步方法、`Task`、`Task<T>`、`ValueTask`、`ValueTask<T>` 的调用行为

### 集成测试

集成测试覆盖 Autofac、Castle 和 ASP.NET Core：

- `UseTwAutofac()` 接管默认容器
- 普通服务、keyed service、open generic 可以解析
- Castle interface proxy 和 class proxy 执行同一套 `Tw.DynamicProxy.IInterceptor`
- MVC Filter 与 Middleware 执行同一套 `IInterceptorPipeline`
- gRPC adapter 执行同一套 `IInterceptorPipeline`
- 诊断报告在真实 Host 中可读取且不泄露敏感值

## 文档与治理要求

实现该设计时必须同步更新：

- `backend/dotnet/BuildingBlocks/src/Tw.Core/package-charter.yaml`
- `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/package-charter.yaml`
- `docs/shared-packages/dotnet/Tw.Core` 下对应能力文档
- `docs/shared-packages/dotnet/Tw.AspNetCore` 下对应承载适配文档

公开 API 必须提供 XML 文档注释。配置项必须说明路径、用途、类型、默认行为、是否必填、是否敏感和变更影响。

所有代码命名不得包含参考框架原名。设计和实现统一使用 `Tw` 命名空间与能力语义命名。
