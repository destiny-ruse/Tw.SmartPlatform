# Package: Tw.DependencyInjection.Abstractions

标识：Tw.DependencyInjection.Abstractions / backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection.Abstractions / platform-team
职责：服务生命周期、自动注册、Options 绑定和服务暴露抽象。

适用范围：
- 服务生命周期标记
- 服务暴露元数据
- Options 绑定元数据

不适用范围：
- 程序集扫描执行
- Autofac 容器接管
- 方法级拦截抽象
- Castle DynamicProxy 代理创建

依赖边界：
- forbid: Autofac*, Castle*, Microsoft.AspNetCore.*
- allow: none

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.DependencyInjection.Abstractions
