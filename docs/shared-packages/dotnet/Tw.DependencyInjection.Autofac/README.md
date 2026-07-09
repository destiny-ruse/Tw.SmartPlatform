# Tw.DependencyInjection.Autofac

`Tw.DependencyInjection.Autofac` 是 `Tw.DependencyInjection` 的 Autofac 运行时容器适配包。它承载 Autofac 宿主接管、Autofac native 服务注册执行、keyed service 注册和从 Autofac 注册到 `Tw.Castle.Core` 方法级拦截的桥接。

## 能力

- `IHostBuilder.UseAutofac()`
- `ContainerBuilder.AddServiceRegistration(IConfiguration)`
- Autofac keyed service 注册执行
- 基于 `Tw.Castle.Core` 的 Autofac DynamicProxy 注册

## 接管 Autofac 宿主

```csharp
using Tw.DependencyInjection.Autofac;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseAutofac();
```

## 使用 Autofac native 注册

```csharp
using Autofac;
using Tw.DependencyInjection.Autofac;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseAutofac();
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    containerBuilder.AddServiceRegistration(builder.Configuration);
});
```

`ContainerBuilder.AddServiceRegistration(...)` 复用 `Tw.DependencyInjection` 的容器中立发现与规划，再通过 Autofac 执行注册。当方法使用 `Tw.Castle.Core.Abstractions` 拦截特性标记时，该路径也会在满足代理条件时启用 Castle DynamicProxy。

## 边界

本包不定义程序集发现规则、DI 元数据特性、MVC filters、CAP filters、gRPC interceptors、数据访问、后台任务或网关集成。
