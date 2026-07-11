# 服务自动注册

## 能力定位

`Tw.DependencyInjection` 提供容器中立的服务自动注册。业务类型只依赖 `Tw.DependencyInjection.Abstractions` 标记接口与特性，组合根调用 `AddServiceRegistration(IConfiguration)` 完成扫描、规划与 Microsoft DI 注册。

## 注册入口

```csharp
using Tw.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddServiceRegistration(builder.Configuration);
```

`AddServiceRegistration` 读取 `Tw:DependencyInjection` 配置节，复用程序集扫描结果，生成 `ServiceRegistrationReport` 并注册为 singleton。

服务注册始终使用 Microsoft DI；keyed service 也通过 Microsoft.Extensions.DependencyInjection 的原生能力注册和解析。

## 生命周期

服务类型通过以下任一方式声明生命周期：

```csharp
public sealed class OrderService : IOrderService, IScopedDependency
{
}

[ServiceRegistration(DependencyLifetime.Singleton)]
public sealed class CacheService : ICacheService
{
}
```

同一类型不得同时实现多个生命周期标记；命中时启动失败。未声明生命周期的类型不会注册。

## 暴露服务

默认暴露实现类自身，以及与实现类命名匹配的接口（实现类名以"接口名去掉前导 `I`"结尾即匹配，例如 `OrderService`、`DefaultOrderService` 均暴露 `IOrderService`）：

```csharp
public interface IOrderService
{
}

public sealed class OrderService : IOrderService, IScopedDependency
{
}
```

显式暴露使用 `[ExposeServices]`：

```csharp
[ExposeServices(typeof(IOrderService), IncludeSelf = true)]
public sealed class CustomOrderService : IOrderService, IScopedDependency
{
}
```

默认规则不暴露所有接口，生命周期标记接口与框架接口不会被暴露为业务服务。

## Keyed Service

同一契约存在多个实现时使用 keyed service：

```csharp
[ExposeKeyedService(typeof(IPaymentProvider), "wechat")]
public sealed class WechatPaymentProvider : IPaymentProvider, IScopedDependency
{
}

public sealed class CheckoutService : IScopedDependency
{
    public CheckoutService([FromKeyedServices("wechat")] IPaymentProvider provider)
    {
        Provider = provider;
    }

    public IPaymentProvider Provider { get; }
}
```

需要枚举某契约的全部 keyed 实现时，注入 `IEnumerable<KeyedServiceEntry<TService>>`：

```csharp
public sealed class PaymentRouter : IScopedDependency
{
    public PaymentRouter(IEnumerable<KeyedServiceEntry<IPaymentProvider>> providers)
    {
        Providers = providers.ToList();
    }

    public IReadOnlyList<KeyedServiceEntry<IPaymentProvider>> Providers { get; }
}
```

`[FromKeyedServices]` 指向未注册的 key 时启动失败。
`[ExposeKeyedService]` 的 key 不得为空；空 key 在注册规划阶段启动失败。

## 单实现仲裁

非 keyed 契约最终只注册一个实现。多个候选通过最终优先级 `拓扑层级 × 1000000 + AssemblyPriority + TypePriority`（拓扑基值 + 程序集优先级 + 类型优先级）仲裁，优先级高者胜出，落选候选记录到 `ServiceRegistrationReport.SupersededCandidates`。

程序集优先级配置（`AssemblyPriorities`，key 为程序集名，value 越大优先级越高）：

```json
{
  "Tw": {
    "DependencyInjection": {
      "AssemblyPriorities": {
        "Tw.Order.Application": 100
      }
    }
  }
}
```

未配置 `AssemblyPriorities` 时，可在程序集级别声明默认优先级：

```csharp
using Tw.DependencyInjection.Abstractions;

[assembly: AssemblyRegistrationPriority(50)]
```

同一程序集配置了 `AssemblyPriorities` 时，配置值优先于 `AssemblyRegistrationPriorityAttribute`。

类型优先级：

```csharp
[ServicePriority(20)]
public sealed class PreferredOrderService : IOrderService, IScopedDependency
{
}
```

显式优先级范围为 `-100000..100000`，拓扑层级步长 `1000000` 始终压倒显式优先级。最终优先级相同，或平级（互不可达）程序集只靠拓扑顺序产生胜者时，启动失败。

## open generic

开放泛型实现按其泛型定义参与注册与仲裁，闭合类型在解析时继承定义级结果：

```csharp
public sealed class Repository<TEntity> : IRepository<TEntity>, IScopedDependency
{
}
```

`Repository<TEntity>` 暴露 `IRepository<TEntity>` 的开放泛型定义，消费方可解析任意闭合 `IRepository<OrderEntity>`。

## 注意事项

- `Replace = true`、`ReplaceServices`、`TryReplace` 不属于本包服务注册模型。
- Options 类型不作为普通服务注册，详见 [配置与 Options 自动装载](options-binding.md)。
- 本包只提供 Microsoft DI 服务注册，不启用通用动态代理。
- 诊断报告只输出类型、契约、key、优先级和原因，不输出配置值或方法参数值。
