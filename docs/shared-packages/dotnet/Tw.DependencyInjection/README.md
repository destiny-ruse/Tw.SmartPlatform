# Tw.DependencyInjection

`Tw.DependencyInjection` 是容器中立的依赖注入运行时，消费 `Tw.DependencyInjection.Abstractions` 的框架无关抽象，承载程序集发现、拓扑排序、注册规划诊断、Microsoft DI 注册执行与 Options 自动装载。本包不依赖 Autofac 或 Castle。

## 能力索引

- [程序集扫描与注册规划](assembly-scanning.md)：扫描白/黑名单、依赖拓扑排序、循环诊断与 `ServiceRegistrationReport`。
- [服务自动注册入口与规划诊断报告](service-registration.md)：生命周期标记、显式暴露、keyed service、open generic 与单实现仲裁。
- [Options 自动装载与诊断报告](options-binding.md)：发现 `IConfigurableOptions`、绑定配置节、启动校验、命名 Options 与诊断报告。

## 相关包

- [`Tw.DependencyInjection.Abstractions`](../Tw.DependencyInjection.Abstractions/README.md)：DI 与 Options 元数据抽象。
- [`Tw.DependencyInjection.Autofac`](../Tw.DependencyInjection.Autofac/README.md)：Autofac 宿主接管与 Autofac native 注册执行。
- [`Tw.Castle.Core`](../Tw.Castle.Core/README.md)：方法级拦截抽象、pipeline、Castle adapter 与 `InterceptionReport`。
