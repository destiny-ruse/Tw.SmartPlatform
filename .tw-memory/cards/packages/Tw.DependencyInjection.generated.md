# Package: Tw.DependencyInjection

标识：Tw.DependencyInjection / backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection / platform-team
职责：容器中立的依赖注入运行时：程序集发现、白/黑名单过滤、依赖拓扑排序与循环诊断、 注册规划、Microsoft DI 注册执行与注册规划诊断报告。消费 Tw.DependencyInjection.Abstractions 的框架无关抽象，并提供自动服务注册的参与判定、暴露、仲裁、注册入口与 Options 自动装载能力。

适用范围：
- 程序集发现与白/黑名单过滤
- 程序集依赖拓扑排序与循环诊断
- Microsoft DI 注册执行
- 服务注册规划诊断报告
- 自动服务注册参与判定、生命周期解析与服务暴露
- 非 keyed 单实现仲裁、keyed service 与 open generic 注册
- Options 自动发现、绑定、启动校验、后置配置与诊断报告

不适用范围：
- DI 注册标记、特性、Options 与 AOP 抽象
- 其他依赖注入容器接管与容器专属注册执行
- ASP.NET Core 宿主启动、MVC、Minimal API、Middleware 与 gRPC 承载
- 数据访问、ORM、仓储实现

依赖边界：
- forbid: Microsoft.AspNetCore.*, Microsoft.EntityFrameworkCore*, Autofac*, Castle.*, Tw.Castle.*, Tw.DependencyInjection.Autofac
- allow: Tw.DependencyInjection.Abstractions, Microsoft.Extensions.*

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.DependencyInjection
