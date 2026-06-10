# Tw.DependencyInjection

`Tw.DependencyInjection` 是框架绑定的依赖注入执行引擎，消费 `Tw.Core` 的框架无关抽象，承载程序集发现、拓扑排序、Autofac 容器接管与注册规划诊断。本页按功能跳转到使用文档。

## 能力索引

- [程序集扫描与容器接管](assembly-scanning.md)：扫描白/黑名单、依赖拓扑排序与循环诊断、`UseAutofac()` 启动原语（P1 落地）。
- [服务自动注册](service-registration.md)：生命周期标记、显式暴露、keyed service、open generic 与单实现仲裁（P2 落地）。
