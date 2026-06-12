# 方法级动态代理拦截

## 能力定位

`Tw.DependencyInjection` 在 P4 阶段通过 Autofac 原生注册路径承载 Castle 动态代理。业务拦截器只依赖 `Tw.Core` 中的 `Tw.DynamicProxy.Abstractions.IInterceptor`，业务服务用 `[Intercept]`、`[DisableInterception]` 与 `[InterceptorOrder]` 声明方法级拦截规则。

Castle 承载只处理通过 Autofac native `ContainerBuilder.AddServiceRegistration(...)` 自动注册、并满足代理条件的服务方法调用。手写 Autofac 注册不会自动进入本包 AOP 规划、`InterceptionReport` 诊断和 Castle proxy 启用流程。Middleware、Minimal API 和 gRPC 不进入统一 AOP；这些入口仍按各自框架模型使用 middleware、endpoint filter、filter 或 interceptor。

## 注册入口

启用 AOP 必须使用 Autofac native `ContainerBuilder.AddServiceRegistration(...)` 路径。以下代码是 ASP.NET Core/Autofac 组合根片段：宿主接管 Autofac 后，在 `ConfigureContainer<ContainerBuilder>` 中调用注册入口。

```csharp
using Autofac;
using Tw.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseAutofac();
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    containerBuilder.AddServiceRegistration(builder.Configuration);
});
```

`IServiceCollection.AddServiceRegistration(...)` 保留为无 AOP 路径。它会继续执行程序集扫描、服务注册和 Options 自动装载，但不会启用 Castle proxy，也不会生成 `InterceptionReport`。

## 编写拦截器

业务拦截器实现 `Tw.DynamicProxy.Abstractions.IInterceptor`，并按普通服务注册规则注册到 DI。最直接的方式是实现生命周期标记接口：

```csharp
using Tw.DependencyInjection.Abstractions;
using Tw.DynamicProxy.Abstractions;

[InterceptorOrder(-10)]
public sealed class AuditInterceptor : IInterceptor, IScopedDependency
{
    public async ValueTask InterceptAsync(IInvocationContext context)
    {
        await context.ProceedAsync();

        if (context.ReturnValue is string value)
        {
            context.ReturnValue = $"audited:{value}";
        }
    }
}
```

`IInvocationContext` 提供 `Method`、`Target`、`Arguments`、`ArgumentsByName` 与 `ReturnValue`。拦截器可以在 `ProceedAsync()` 前改写 `Arguments`，也可以在目标方法返回后改写 `ReturnValue`。需要短路目标方法时，不调用 `ProceedAsync()` 并直接设置 `ReturnValue`。

## 标记要拦截的服务

`[Intercept]` 可以标注在接口、实现类或方法上，参数是实现 `IInterceptor` 的拦截器类型：

```csharp
using Tw.DependencyInjection.Abstractions;
using Tw.DynamicProxy.Abstractions;

public interface IOrderService
{
    Task<string> SubmitAsync(string orderId);

    Task<string> PreviewAsync(string orderId);
}

[Intercept(typeof(AuditInterceptor))]
public sealed class OrderService : IOrderService, IScopedDependency
{
    public Task<string> SubmitAsync(string orderId)
    {
        return Task.FromResult(orderId);
    }

    public Task<string> PreviewAsync(string orderId)
    {
        return Task.FromResult(orderId);
    }
}
```

方法级标记适合只拦截个别方法：

```csharp
public sealed class OrderService : IOrderService, IScopedDependency
{
    [Intercept(typeof(AuditInterceptor))]
    public Task<string> SubmitAsync(string orderId)
    {
        return Task.FromResult(orderId);
    }

    public Task<string> PreviewAsync(string orderId)
    {
        return Task.FromResult(orderId);
    }
}
```

同一个方法最终选中的拦截器类型会去重。多个拦截器按 `[InterceptorOrder]` 的 `Order` 从小到大执行；未标记顺序时视为 `0`，顺序相同时按拦截器类型全名稳定排序。

## 关闭拦截

`[DisableInterception]` 可以标注在类或方法上。标注在类上会关闭该类全部方法的拦截；标注在方法上只关闭该方法：

```csharp
[Intercept(typeof(AuditInterceptor))]
public sealed class OrderService : IOrderService, IScopedDependency
{
    public Task<string> SubmitAsync(string orderId)
    {
        return Task.FromResult(orderId);
    }

    [DisableInterception]
    public Task<string> PreviewAsync(string orderId)
    {
        return Task.FromResult(orderId);
    }
}
```

## 代理边界

接口契约优先使用 Castle interface proxy。业务服务实现接口并通过接口解析时，不要求实现类方法为 `virtual`：

```csharp
public sealed class OrderService : IOrderService, IScopedDependency
{
    public Task<string> SubmitAsync(string orderId)
    {
        return Task.FromResult(orderId);
    }

    public Task<string> PreviewAsync(string orderId)
    {
        return Task.FromResult(orderId);
    }
}
```

没有接口契约的 class-only 服务使用 Castle class proxy。实现类型必须是 public、非 sealed，目标方法必须是 public virtual：

```csharp
[Intercept(typeof(AuditInterceptor))]
public class OrderWorkflow : IScopedDependency
{
    public virtual Task<string> SubmitAsync(string orderId)
    {
        return Task.FromResult(orderId);
    }
}
```

开放泛型 class-only 服务当前不承载 Castle class proxy。开放泛型通过接口契约暴露时仍按接口代理路径处理。

## 查看诊断报告

Autofac native 注册路径会注册 `Tw.DependencyInjection.Diagnostics.InterceptionReport`。以下代码是 ASP.NET Core/Autofac 组合根中的诊断读取片段，可以在容器构建后解析它，检查哪些方法启用了代理、哪些方法被跳过：

```csharp
using Autofac;
using Tw.DependencyInjection.Diagnostics;

var report = container.Resolve<InterceptionReport>();

foreach (var item in report.Items)
{
    Console.WriteLine($"{item.Status}: {item.ServiceTypeName}.{item.MethodName} ({item.Carrier})");
}
```

`Status` 为 `enabled` 表示方法已启用 Castle 承载；`skipped` 表示存在拦截声明但当前服务或方法不满足代理条件，`Reason` 会说明原因。报告只包含类型名、方法名、承载方式、拦截器类型名和原因，不输出方法参数值或配置值。

## 注意事项

- 拦截器类型必须能从 DI 解析，通常让拦截器实现 `ITransientDependency`、`IScopedDependency` 或 `ISingletonDependency`，也可以在组合根显式注册为自身类型。
- 被 `[Intercept]` 或 selector 命中但未注册的拦截器类型会在容器构建阶段触发 `ServiceRegistrationException` 启动失败，不会拖到首次调用才暴露。
- AOP 只覆盖通过 Autofac native 自动注册、满足代理条件并由 Autofac 容器解析出的服务调用；同类内部 `this.Method()` 调用不会重新经过代理。
- 手写 Autofac 注册不会自动进入本包 AOP 规划、诊断报告和 Castle proxy 启用流程。
- 通过 `IServiceCollection.AddServiceRegistration(...)` 组合出的服务不会启用 Castle proxy。
- Middleware、Minimal API 和 gRPC 不进入统一 AOP。
