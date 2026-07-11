# Tw.DependencyInjection.Abstractions

`Tw.DependencyInjection.Abstractions` 提供依赖注入元数据、生命周期标记、服务暴露特性、keyed service 元数据和 Options 绑定特性。

## 能力

- `DependencyLifetime`
- `ITransientDependency`
- `IScopedDependency`
- `ISingletonDependency`
- `ExposeServicesAttribute`
- `ExposeKeyedServiceAttribute`
- `ServiceRegistrationAttribute`
- `DisableServiceRegistrationAttribute`
- `ServicePriorityAttribute`
- `AssemblyRegistrationPriorityAttribute`
- `KeyedServiceEntry<TService>`
- `IConfigurableOptions`
- `IConfigurableOptions<TOptions>`
- `OptionsSectionAttribute`
- `OptionsNameAttribute`
- `OptionsValidatorAttribute`
- `DisableOptionsBindingAttribute`
- `SensitiveConfigurationAttribute`

## 边界

`Tw.DependencyInjection.Abstractions` 不扫描程序集，不注册服务，不接管容器，不创建通用动态代理，也不提供 ASP.NET Core 宿主集成。

运行时服务注册由 [`Tw.DependencyInjection`](../Tw.DependencyInjection/README.md) 提供，并使用 Microsoft DI 默认容器。横切关注点由各宿主框架的原生扩展点处理。
