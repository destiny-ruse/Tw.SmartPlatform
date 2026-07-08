# Tw .NET 微服务框架最终设计蓝图

## 目标

本文定义 `Tw.SmartPlatform` 的 `.NET 10+` 企业级微服务底层框架最终决策。框架以 `Tw.*` 共享包提供公司内部微服务开发底座，覆盖应用层管道、工作单元、数据访问、事件总线、最终一致性、认证授权、OpenAPI、gRPC、AOP、SqlSugar、依赖注入、多租户、分片、文本模板、Excel、后台调度、本地 Aspire 开发、网关、测试和工程治理。

本文只记录最终决策、边界规则和实施约束。被禁止的包名、旧命名和旧入口不得作为兼容层保留。

## 基础决策

- 包名、程序集名和根命名空间使用 `Tw.*`。
- 自有接口、类、枚举、属性、字段、方法、扩展方法、包内部文件名和包内部功能文件夹名不得使用 `Tw`、`Abp`、`Furion` 等框架名前缀。
- `TwException` 是唯一保留 `Tw` 前缀的自有异常基类。
- 第三方技术集成入口可以使用第三方名称，例如 `UseAutofac`、`AddScriban`、`AddMiniExcel`。
- 包重命名、类型重命名和方法重命名直接采用破坏性变更。
- 删除被禁用名称后不保留转发类型、Obsolete 壳、兼容别名和空实现。
- 注释只解释当前代码功能、契约、风险和约束，不记录改名过程或重构叙述。
- 不创建动态 API 包，不提供 ABP 或 Furion 风格的动态 Controller 生成功能。
- 不使用 MassTransit。
- OpenAPI 使用 Swashbuckle。
- JSON 统一使用 Newtonsoft.Json。
- ORM 统一使用 SqlSugar。
- 事件总线统一使用 CAP。
- DI 运行时使用 Autofac，AOP 使用 Castle DynamicProxy。
- 分布式 ID 使用 `Yitter.IdGenerator`。
- API 模型中 ID 保持 `long`，HTTP JSON 由全局格式化配置输出为字符串。
- 文件与对象存储不作为框架级包，由独立文件存储服务承载。
- 同一能力族的包放在同一物理文件夹与解决方案文件夹中，例如 `Web`、`Data`、`EventBus`、`TestBase`。

## 最终包清单

### 基础运行时包

| 包 | 职责 |
| --- | --- |
| `Tw.Core` | 模块基础、通用标识、错误模型、类型工具、轻量基础原语和公共约定 |
| `Tw.Threading` | 取消令牌、异步辅助、后台执行上下文和线程安全工具 |
| `Tw.Timing` | 时钟、时区、时间标准化和时间上下文 |
| `Tw.ExceptionHandling` | 异常分类、错误码映射、异常到错误模型转换 |
| `Tw.Security` | 加密哈希、安全随机、敏感字段标记、脱敏和写回保护 |
| `Tw.Json.Abstractions` | JSON 序列化抽象、序列化选项和长整型 ID 输出约定 |
| `Tw.Json.Newtonsoft` | Newtonsoft.Json 默认序列化实现 |
| `Tw.Validation.Abstractions` | 验证错误模型、字段路径、绑定错误和验证异常 |
| `Tw.DependencyInjection.Abstractions` | 服务生命周期、自动注册、Options 绑定、拦截元数据和服务暴露抽象 |
| `Tw.DependencyInjection` | 程序集发现、服务注册规划、Options 自动绑定和容器中立注册执行 |
| `Tw.DependencyInjection.Autofac` | Autofac 容器接管、Autofac 服务注册和宿主集成 |
| `Tw.Castle.Core` | Castle DynamicProxy、方法拦截器、拦截管道和代理诊断 |
| `Tw.Uow` | 工作单元抽象、事务作用域、提交回调、失败回调和嵌套 UoW 规则 |
| `Tw.Data` | 数据源描述、连接解析、仓储基础抽象、审计字段、软删除和并发契约 |
| `Tw.Data.SqlSugar` | SqlSugar 适配、连接工厂、仓储实现、并发检查和 SqlSugar UoW 适配 |
| `Tw.MultiTenancy.Abstractions` | 租户标识、当前租户、租户解析结果和租户上下文抽象 |
| `Tw.MultiTenancy` | 租户解析、租户数据源目录、租户作用域和 SaaS 运行模式 |
| `Tw.Sharding.Abstractions` | 分片键、分片上下文、分片规则和分片边界抽象 |
| `Tw.Sharding` | 业务分片解析、分片切换、跨分片读取和分片治理 |
| `Tw.IdGeneration` | 分布式 ID 抽象、ID 生成器契约、WorkerId 管理和时钟回拨策略 |
| `Tw.IdGeneration.Yitter` | Yitter.IdGenerator 默认实现 |
| `Tw.TextTemplating` | 文本模板抽象、模板源、渲染上下文、模板缓存和错误诊断 |
| `Tw.TextTemplating.Scriban` | Scriban 模板解析与渲染实现 |
| `Tw.Excel` | Excel 导入导出抽象、列模型、多级表头、模板定义和校验错误模型 |
| `Tw.Excel.MiniExcel` | MiniExcel 流式读写适配、OpenXML 后处理、固定下拉选项和空白模板导出 |

### 应用能力包

| 包 | 职责 |
| --- | --- |
| `Tw.Domain.Shared` | 领域共享常量、错误码、枚举、基础值对象和跨层共享领域契约 |
| `Tw.Domain` | 实体、聚合根、领域服务、领域事件和领域规则基础能力 |
| `Tw.Application.Contracts` | Command、Query、DTO、应用服务契约、分页模型和客户端共享契约 |
| `Tw.Application` | MediatR 集成、Command/Query Handler、Pipeline Behavior、验证、授权、幂等和 UoW 编排 |
| `Tw.Authorization.Abstractions` | Permission 定义、授权上下文、授权结果和 Grant Store 抽象 |
| `Tw.Authorization` | Permission Checker、Grant Store、权限缓存和资源授权边界 |
| `Tw.Features` | Feature 定义、读取、缓存、刷新和作用域规则 |
| `Tw.Settings` | Setting 定义、读取、缓存、刷新和作用域规则 |
| `Tw.Identity.OpenIddict` | 统一身份中心实现、OpenIddict 集成、OIDC/OAuth2、Token 签发与验证 |
| `Tw.Localization` | JSON 本地化资源、文化解析、回退链和 Microsoft `IStringLocalizer` 适配 |

### 协议与宿主包

| 包 | 职责 |
| --- | --- |
| `Tw.AspNetCore.Abstractions` | HTTP 请求上下文、协议错误模型、认证方案和 ASP.NET Core 适配抽象 |
| `Tw.AspNetCore` | HTTP 宿主集成、中间件、异常处理、认证、限流和健康端点 |
| `Tw.AspNetCore.Mvc` | MVC Filter、Endpoint Filter、统一响应、模型绑定错误、CSRF/XSRF 和防伪校验 |
| `Tw.AspNetCore.Mvc.NewtonsoftJson` | ASP.NET Core MVC Newtonsoft.Json 配置和 JSON 契约适配 |
| `Tw.AspNetCore.Swashbuckle` | Swashbuckle 注册、OpenAPI 分组、JWT 定义、XML 注释、Schema 和 Operation 扩展 |
| `Tw.AspNetCore.Grpc` | gRPC 服务端拦截器、ASP.NET Core 服务端注册和 gRPC 宿主治理 |
| `Tw.AspNetCore.Localization` | HTTP 请求文化解析、本地化中间件和 MVC 本地化适配 |
| `Tw.Http.Abstractions` | HTTP 远程调用契约、Header 传播、错误映射和客户端上下文抽象 |
| `Tw.Http` | HTTP 通用模型、远程服务约定、序列化约定和错误响应处理 |
| `Tw.Http.Client` | HttpClientFactory、服务发现、韧性策略、Newtonsoft 序列化和 NSwag 客户端集成 |
| `Tw.Grpc` | gRPC 契约约定、客户端工厂、Deadline、元数据传播和错误映射 |

### 基础设施适配包

| 包 | 职责 |
| --- | --- |
| `Tw.EventBus.Abstractions` | 集成事件契约、事件发布抽象、事件订阅元数据和事件幂等契约 |
| `Tw.EventBus` | 事件总线编排、事件处理器发现、本地事件和分布式事件基础能力 |
| `Tw.EventBus.Cap` | CAP 集成、RabbitMQ 传输、SqlSugar CAP 存储、Outbox/Inbox、消费过滤器和清理任务 |
| `Tw.Caching` | 多级缓存抽象、缓存键、TTL、标签、空值缓存、失效事件和防击穿策略 |
| `Tw.Caching.FusionCache` | FusionCache L1/L2 缓存、Backplane、Fail-safe 和 Stampede protection |
| `Tw.DistributedLocking.Abstractions` | 分布式锁、租约、锁键、等待策略和锁失败语义抽象 |
| `Tw.DistributedLocking` | 分布式锁编排、锁键规范、租约治理和锁失败映射 |
| `Tw.DistributedLocking.Redis` | Redis/Valkey 分布式锁实现 |
| `Tw.Idempotency` | 幂等键、幂等窗口、请求去重、消息去重和冲突响应 |
| `Tw.Resilience` | 超时、重试、熔断、限流、隔离和降级策略封装 |
| `Tw.BackgroundJobs.Abstractions` | 后台任务定义、任务参数、调度描述和任务执行上下文抽象 |
| `Tw.BackgroundJobs` | 后台任务编排、任务控制 API、任务执行管道和任务审计 |
| `Tw.BackgroundJobs.Quartz` | Quartz.NET 调度中心、集群调度、持久化任务和调度治理 |
| `Tw.Configuration` | 配置治理抽象、配置校验、动态配置热更新边界和配置变更审计 |
| `Tw.Configuration.Json` | 多 JSON 配置文件加载、环境叠加、文件清单和路径安全校验 |
| `Tw.Configuration.Nacos` | Nacos 配置源、Nacos 非 Kubernetes 服务发现桥接 |
| `Tw.Gateway` | 网关抽象、路由模型、Header 治理、限流策略和动态路由校验 |
| `Tw.Gateway.Yarp` | YARP 网关适配、转发管道、服务发现、灰度权重、WebSocket/SSE/gRPC 透传 |

### 观测、审计与测试基础包

| 包 | 职责 |
| --- | --- |
| `Tw.Observability` | 日志上下文、Trace/Metrics 抽象、健康状态模型和观测字段约定 |
| `Tw.Observability.Serilog` | Serilog 注册、结构化日志、日志脱敏和输出管道 |
| `Tw.Observability.OpenTelemetry` | OpenTelemetry Trace、Metrics、OTLP Exporter 和 Aspire Dashboard 集成 |
| `Tw.Auditing.Contracts` | 审计事件契约、审计动作、审计主体和审计存储抽象 |
| `Tw.Auditing` | 审计事件收集、审计日志、敏感操作记录和审计存储编排 |
| `Tw.TestBase` | 测试时钟、测试 ID、测试当前用户、测试租户、测试文化和契约测试基础 |
| `Tw.AspNetCore.TestBase` | WebApplicationFactory、测试认证、HTTP 契约测试和 ASP.NET Core 测试宿主 |
| `Tw.Data.SqlSugar.TestBase` | SqlSugar 数据库夹具、Respawn 重置和 Testcontainers 数据库支持 |
| `Tw.EventBus.Cap.TestBase` | CAP、RabbitMQ、Outbox/Inbox 和消费幂等测试支持 |

### 工具包

| 包 | 职责 |
| --- | --- |
| `Tw.Templates` | `dotnet new` 模板、服务模板、网关模板、BuildingBlock 模板、契约包模板 |
| `Tw.Cli` | `dotnet tool`，项目创建、能力添加、契约校验、依赖审计和诊断命令 |
| `Tw.Analyzers` | Roslyn Analyzer，编译期架构边界和禁止规则检查 |

## 物理目录与解决方案文件夹

```text
backend/dotnet/BuildingBlocks/src
|-- Foundation
|   |-- Tw.Core
|   |-- Tw.Threading
|   |-- Tw.Timing
|   |-- Tw.ExceptionHandling
|   |-- Tw.Security
|   |-- Tw.Json.Abstractions
|   |-- Tw.Json.Newtonsoft
|   |-- Tw.Validation.Abstractions
|   |-- Tw.DependencyInjection.Abstractions
|   |-- Tw.DependencyInjection
|   |-- Tw.DependencyInjection.Autofac
|   |-- Tw.Castle.Core
|   `-- Tw.Uow
|-- Data
|   |-- Tw.Data
|   `-- Tw.Data.SqlSugar
|-- MultiTenancy
|   |-- Tw.MultiTenancy.Abstractions
|   `-- Tw.MultiTenancy
|-- Sharding
|   |-- Tw.Sharding.Abstractions
|   `-- Tw.Sharding
|-- IdGeneration
|   |-- Tw.IdGeneration
|   `-- Tw.IdGeneration.Yitter
|-- TextTemplating
|   |-- Tw.TextTemplating
|   `-- Tw.TextTemplating.Scriban
|-- Excel
|   |-- Tw.Excel
|   `-- Tw.Excel.MiniExcel
|-- Application
|   |-- Tw.Domain.Shared
|   |-- Tw.Domain
|   |-- Tw.Application.Contracts
|   |-- Tw.Application
|   |-- Tw.Authorization.Abstractions
|   |-- Tw.Authorization
|   |-- Tw.Features
|   |-- Tw.Settings
|   `-- Tw.Identity.OpenIddict
|-- Localization
|   `-- Tw.Localization
|-- Web
|   |-- Tw.AspNetCore.Abstractions
|   |-- Tw.AspNetCore
|   |-- Tw.AspNetCore.Mvc
|   |-- Tw.AspNetCore.Mvc.NewtonsoftJson
|   |-- Tw.AspNetCore.Swashbuckle
|   |-- Tw.AspNetCore.Grpc
|   `-- Tw.AspNetCore.Localization
|-- Grpc
|   `-- Tw.Grpc
|-- Http
|   |-- Tw.Http.Abstractions
|   |-- Tw.Http
|   `-- Tw.Http.Client
|-- EventBus
|   |-- Tw.EventBus.Abstractions
|   |-- Tw.EventBus
|   `-- Tw.EventBus.Cap
|-- Caching
|   |-- Tw.Caching
|   `-- Tw.Caching.FusionCache
|-- DistributedLocking
|   |-- Tw.DistributedLocking.Abstractions
|   |-- Tw.DistributedLocking
|   `-- Tw.DistributedLocking.Redis
|-- Idempotency
|   `-- Tw.Idempotency
|-- Resilience
|   `-- Tw.Resilience
|-- BackgroundJobs
|   |-- Tw.BackgroundJobs.Abstractions
|   |-- Tw.BackgroundJobs
|   `-- Tw.BackgroundJobs.Quartz
|-- Configuration
|   |-- Tw.Configuration
|   |-- Tw.Configuration.Json
|   `-- Tw.Configuration.Nacos
|-- Gateway
|   |-- Tw.Gateway
|   `-- Tw.Gateway.Yarp
|-- Observability
|   |-- Tw.Observability
|   |-- Tw.Observability.Serilog
|   `-- Tw.Observability.OpenTelemetry
|-- Auditing
|   |-- Tw.Auditing.Contracts
|   `-- Tw.Auditing
`-- TestBase
    |-- Tw.TestBase
    |-- Tw.AspNetCore.TestBase
    |-- Tw.Data.SqlSugar.TestBase
    `-- Tw.EventBus.Cap.TestBase
```

解决方案文件夹必须与物理能力族一致。项目 `RootNamespace` 等于 `.csproj` 名称，不把物理能力族目录写入命名空间。

## 禁止创建的包

- `Tw.Infrastructure`
- `Tw.Context`
- `Tw.ExecutionPipeline`
- `Tw.Swagger`
- `Tw.ApiVersioning`
- `Tw.Validation`
- `Tw.RateLimiting`
- `Tw.HealthChecks`
- `Tw.ObjectStorage`
- `Tw.Serialization`
- `Tw.Bff`
- `Tw.DynamicApi`
- `Tw.AspNetCore.DynamicApi`
- `Tw.ApplicationConfiguration`
- `Tw.Snowflake`
- `Tw.DistributedLock`
- `Tw.Autofac`
- `Tw.Localization.AspNetCore`
- `Tw.Grpc.AspNetCore`
- `Tw.Cqrs`
- `Tw.UnitOfWork`
- `Tw.Data.Abstractions`
- `Tw.Testing`
- 任何 `*.Testing` 运行时封装包
- 任何 `MassTransit` 相关包
- 任何 Conventional API、Dynamic API 或动态 Controller 生成相关包

## 包依赖规则

`Tw.Core` 是底层基础包，不依赖 ASP.NET Core、SqlSugar、CAP、Autofac、Castle、Quartz、OpenIddict、YARP、Redis、FusionCache、MediatR、Newtonsoft.Json 和测试库。

`Tw.Json.Abstractions` 不依赖 Newtonsoft.Json。`Tw.Json.Newtonsoft` 依赖 Newtonsoft.Json，并只暴露框架 JSON 抽象。

`Tw.DependencyInjection.Abstractions` 不依赖 Autofac 和 Castle。`Tw.DependencyInjection.Autofac` 依赖 Autofac。`Tw.Castle.Core` 依赖 Castle DynamicProxy。

`Tw.TextTemplating` 不依赖 Scriban。`Tw.TextTemplating.Scriban` 依赖 Scriban，并只暴露框架模板抽象。

`Tw.Excel` 不依赖 MiniExcel 和 OpenXML。`Tw.Excel.MiniExcel` 依赖 MiniExcel 与 DocumentFormat.OpenXml，并只暴露框架 Excel 抽象。

`Tw.Uow` 只定义工作单元抽象，不依赖 SqlSugar、CAP、ASP.NET Core、Autofac、MediatR 和 Quartz。

`Tw.Data.SqlSugar` 依赖 `Tw.Data`、`Tw.Uow`、`Tw.Core`。业务项目不能直接使用 SqlSugar 的 `ChangeDatabase`。

`Tw.MultiTenancy` 依赖 `Tw.MultiTenancy.Abstractions`。`Tw.Sharding` 依赖 `Tw.Sharding.Abstractions`。租户解析、分片规则和连接选择通过抽象协作，业务分片必须由业务契约显式提供分片键。

`Tw.Application` 依赖 `Tw.Application.Contracts`、`Tw.Domain`、`Tw.Authorization.Abstractions`、`Tw.Uow`。MediatR 只进入 `Tw.Application`，不进入 `Tw.Application.Contracts`。

`Tw.EventBus.Cap` 依赖 CAP 与 `Tw.EventBus`，CAP 存储使用框架自定义 SqlSugar 存储适配。业务服务只依赖 `Tw.EventBus.Abstractions` 或 `Tw.EventBus` 发布集成事件。

`Tw.AspNetCore` 只承载 HTTP 宿主基础能力。MVC、Newtonsoft.Json、Swashbuckle、gRPC 服务端、本地化分别进入 `Tw.AspNetCore.Mvc`、`Tw.AspNetCore.Mvc.NewtonsoftJson`、`Tw.AspNetCore.Swashbuckle`、`Tw.AspNetCore.Grpc`、`Tw.AspNetCore.Localization`。

`Tw.Caching` 只定义缓存抽象、键规范和失效契约。FusionCache、Redis/Valkey 细节进入 `Tw.Caching.FusionCache`。

`Tw.DistributedLocking.Abstractions` 只定义锁契约。`Tw.DistributedLocking.Redis` 依赖 Redis/Valkey 客户端并实现分布式锁。

`Tw.BackgroundJobs.Abstractions` 只定义任务契约。`Tw.BackgroundJobs` 只定义后台任务编排和执行管道。Quartz.NET 调度实现进入 `Tw.BackgroundJobs.Quartz`。

`Tw.Configuration` 只定义配置治理抽象。JSON 文件加载进入 `Tw.Configuration.Json`，Nacos 集成进入 `Tw.Configuration.Nacos`。

`Tw.Gateway` 只定义网关治理模型。YARP 运行时适配进入 `Tw.Gateway.Yarp`。

`Tw.Observability` 只定义观测字段、上下文和健康状态模型。Serilog 与 OpenTelemetry 分别进入 `Tw.Observability.Serilog`、`Tw.Observability.OpenTelemetry`。

`Tw.Gateway.Yarp` 不能依赖 `Tw.Data.*`、`Tw.Uow`、`Tw.Application`、`Tw.EventBus.*`、`Tw.BackgroundJobs.*`、`Tw.MultiTenancy`、`Tw.Sharding`。

所有 `*TestBase` 包只能被测试项目引用，生产项目禁止引用。`*TestBase` 只表示测试基础封装包，单元测试、集成测试和契约测试项目仍然使用 `*.Tests`、`UnitTests`、`IntegrationTests`、`ContractTests` 命名。

## 命名治理

包名、程序集名和根命名空间保留 `Tw.*` 身份。其他自有标识符不使用框架名前缀。

- 自有类型不使用 `Tw`、`Abp`、`Furion` 前缀，`TwException` 除外。
- 自有接口只使用 `I` 前缀表达接口角色，不使用 `ITwXxx`。
- 自有扩展方法按能力命名，不使用 `AddTwXxx`、`UseTwXxx`。
- 自有包内部文件名、包内部功能文件夹名、属性、字段、参数、枚举值不使用框架名前缀。
- 第三方技术集成可以使用第三方名称，例如 `Autofac`、`Scriban`、`MiniExcel`、`OpenIddict`、`SqlSugar`。
- 类型命名空间等于项目 `RootNamespace` 加类型文件相对项目根目录的文件夹路径。
- 跨程序集不向同一命名空间贡献类型。
- 抽象包与实现包覆盖同一能力域时，抽象包命名空间使用 `.Abstractions` 结尾。

## 服务项目结构

服务模板固定使用以下项目层：

```text
Billing.Contracts
Billing.Domain
Billing.Application
Billing.Infrastructure
Billing.HttpApi
Billing.HttpApi.Client
Billing.Host
Billing.UnitTests
Billing.IntegrationTests
Billing.ContractTests
```

| 项目 | 职责 |
| --- | --- |
| `Contracts` | DTO、Command/Query 合约、事件合约、错误码、公开常量、客户端共享契约 |
| `Domain` | 实体、值对象、领域服务、领域规则、领域事件 |
| `Application` | 用例编排、Command/Query Handler、权限检查、UoW 边界、事件发布、缓存与幂等协调 |
| `Infrastructure` | 数据库、缓存、锁、第三方服务、仓储实现、外部适配器 |
| `HttpApi` | Controller、HTTP 参数绑定、OpenAPI 元数据、HTTP Filter |
| `HttpApi.Client` | HTTP SDK、NSwag 生成客户端、服务调用封装 |
| `Host` | 宿主入口、运行角色组合、配置、依赖注入、HTTP/gRPC/CAP/Jobs 启用 |
| `UnitTests` | 单元测试 |
| `IntegrationTests` | 集成测试 |
| `ContractTests` | HTTP、gRPC、CAP 事件和错误码契约测试 |

`Contracts` 和 `HttpApi.Client` 可以发布为 NuGet。`Host` 生成镜像或可执行程序。测试项目不发布。

| 项目 | 默认引用 |
| --- | --- |
| `Contracts` | `Tw.Application.Contracts`、`Tw.Core`，事件合约存在时引用 `Tw.EventBus.Abstractions` |
| `Domain` | `Tw.Domain`、`Tw.Core` |
| `Application` | `Domain`、`Contracts`、`Tw.Application`、`Tw.Authorization`、`Tw.Uow` |
| `Infrastructure` | `Application`、`Domain`、`Contracts`、`Tw.Data.SqlSugar`，按能力引用缓存、锁、幂等、Http Client、ID 生成、Excel、文本模板 |
| `HttpApi` | `Application`、`Contracts`、`Tw.AspNetCore`、`Tw.AspNetCore.Mvc`、`Tw.AspNetCore.Swashbuckle` |
| `HttpApi.Client` | `Contracts`、`Tw.Http.Client` |
| `Host` | 服务启用的所有运行时实现包 |
| `UnitTests` | 被测项目、`Tw.TestBase`、xUnit、NSubstitute、AwesomeAssertions |
| `IntegrationTests` | `Host` 或相关适配项目、`Tw.TestBase`、`Tw.AspNetCore.TestBase`、`Tw.Data.SqlSugar.TestBase`、`Tw.EventBus.Cap.TestBase`、Testcontainers |
| `ContractTests` | `Contracts`、`HttpApi.Client`、`Tw.TestBase` |

`Domain` 禁止引用 `Contracts`、`AspNetCore`、`Application`、`Data`、`Cache`、`EventBus`、`Http.Client`。`HttpApi` 禁止引用 `Data`、`EventBus.Cap`、`BackgroundJobs`、`Grpc`、`Infrastructure`。

## 执行上下文与应用管道

基础上下文按能力包拆分：

- `Tw.Security` 提供当前用户、Principal 访问器、敏感字段标记和脱敏能力。
- `Tw.MultiTenancy.Abstractions` 提供当前租户和租户上下文抽象。
- `Tw.Localization` 提供当前文化、文化解析和本地化资源访问。
- `Tw.Observability` 提供关联标识、Trace/Metrics 上下文和日志上下文。
- `Tw.Timing` 提供时钟、时区和时间上下文。
- `Tw.Json.Abstractions` 提供 JSON 序列化抽象。
- `Tw.Validation.Abstractions` 提供验证错误模型、绑定错误和验证异常。
- `Tw.ExceptionHandling` 提供异常分类和错误模型转换。

HTTP、gRPC、CAP Consumer 和后台任务优先使用各自宿主原生管道：

- HTTP 使用 Middleware、MVC Filter、Endpoint Filter。
- gRPC 使用 Interceptor。
- CAP Consumer 使用 CAP Filter。
- Quartz Job 使用 Job Listener 或 Job Pipeline。

宿主适配共享 `Tw.Application` 的应用层 Pipeline 与各能力包抽象，使日志、审计、UoW、授权、幂等、验证和异常分类保持统一执行顺序。

应用层 Pipeline 顺序固定：

```text
ExecutionContext
 -> Feature
 -> Authorization
 -> Validation
 -> Idempotency
 -> Sharding
 -> Uow
 -> Concurrency
 -> Auditing
 -> Handler
 -> Completed Hooks
```

HTTP、gRPC、CAP Consumer、后台任务进入业务用例时统一调用 `ISender.Send(...)`，复用同一应用层行为。

`Tw.Application` 使用 MediatR 12.5.0。业务验证使用 FluentValidation 12.1.1，不使用 `FluentValidation.AspNetCore`，不使用 `DataAnnotations` 执行业务验证。

## DI、Autofac 与 AOP

`Tw.DependencyInjection` 定义容器中立的服务注册模型、自动注册规则、生命周期、服务暴露规则和 AOP 元数据。

`Tw.DependencyInjection.Autofac` 是默认运行时适配，使用 Autofac。`Tw.Castle.Core` 提供 Castle DynamicProxy 拦截能力。

AOP 规则：

- Controller、gRPC Service、CAP Consumer、Quartz Job 优先走宿主原生 Pipeline。
- 普通应用服务、领域服务、基础设施服务通过 AOP 拦截。
- 已由宿主管道处理的能力不重复套 AOP。
- `DisableInterception` 和类似标记可以禁用拦截。
- 拦截器只处理跨切面能力，业务规则不能写入拦截器。

扩展方法命名为 `UseAutofac`、`AddInterception`、`AddServiceRegistration` 等能力名称，不使用 `UseTwAutofac`、`AddTwInterception` 形式。

## 工作单元与并发检查

`Tw.Uow` 定义工作单元抽象：

- 支持 required、requires new、suppress 语义。
- 支持事务与非事务 UoW。
- 支持提交前、提交后、失败后回调。
- 支持当前 UoW 上下文。
- 支持取消令牌传递。
- 不包含具体 ORM 和消息实现。

`Tw.Data.SqlSugar` 实现 SqlSugar UoW。读操作不默认开启事务。写操作由 `Tw.Application` 或宿主 Pipeline 建立事务边界。

`Tw.Data` 定义并发契约，`Tw.Data.SqlSugar` 提供 SqlSugar 乐观锁和悲观锁适配，`Tw.Application` 通过 Pipeline 统一传递期望并发标识。

公开契约：

- `IHasConcurrencyStamp`：实体持有字符串并发戳。
- `IHasVersionStamp`：实体持有数值版本戳。
- `ConcurrencyConflictException`：并发冲突异常，继承 `TwException`。
- `IConcurrencyStampProvider`：生成新并发戳。
- `IConcurrencyCheckContext`：当前命令携带的期望并发值。
- `IPessimisticLock`：悲观锁作用域抽象。

乐观并发规则：

- 新增实体缺失并发戳时自动生成。
- 更新和删除必须使用调用方读取到的旧并发戳或版本戳作为条件。
- 更新成功后生成新并发戳或递增版本戳。
- 更新或删除影响行数为 0 时抛出 `ConcurrencyConflictException`。
- 并发冲突映射为 HTTP `409 Conflict` 和稳定错误码。
- 并发字段不得由普通 DTO 直接覆盖，必须通过框架更新逻辑维护。

悲观锁规则：

- 悲观锁必须在 UoW 事务内使用。
- 锁必须包含租户、分片、资源类型和资源标识。
- 锁等待时间和租约必须有上限。
- 锁获取失败映射为并发冲突或业务拒绝。
- 锁范围不得跨多个物理分片伪装成本地事务。

SqlSugar 适配规则：

- 支持 SqlSugar 原生乐观锁能力的数据库优先使用原生能力。
- 不具备统一原生能力的数据库使用 `where id = @id and concurrency_stamp = @oldStamp` 或版本字段条件更新。
- 悲观锁使用数据库方言支持的行级锁或更新锁能力，并由 `Tw.Data.SqlSugar` 隔离方言差异。
- 仓储、UoW 和 Application Pipeline 必须共享同一并发上下文，不能让业务代码手写并发 SQL。

## 多租户、分片与数据访问

`Tw.MultiTenancy` 定义租户解析、租户上下文、租户数据源目录和 SaaS 运行模式。非 SaaS 模式使用固定租户标识 `default`。

租户来源顺序：

1. 认证票据中的租户声明。
2. 路由、Header、子域名或网关传递的租户标识。
3. 后台任务、CAP 消息或调度上下文。
4. 服务默认租户。

`Tw.Sharding` 定义业务分片上下文、分片规则、分片切换和跨分片边界。业务请求必须显式携带分片键。框架不根据租户 ID 自动推导业务分片。

分片键来源顺序：

1. Command/Query 契约显式字段。
2. HTTP 路由或 Header 中的受控字段。
3. gRPC Metadata 或消息 Header。
4. 后台任务参数。

`Tw.Data.SqlSugar` 是 SqlSugar 适配层。业务代码通过仓储、UoW 或受控连接访问器使用数据库。

连接解析顺序：

```text
ICurrentTenant
 -> IServiceDataSourceResolver
 -> IShardContext
 -> IConnectionConfigResolver
 -> ISqlSugarClient
```

连接串来源：

- SaaS 关闭且分片关闭：来自配置文件。
- SaaS 开启且分片关闭：来自 SaaS 主库。
- SaaS 关闭且分片开启：来自当前服务主库分片目录。
- SaaS 开启且分片开启：来自 SaaS 主库中的租户服务数据源目录。

连接串敏感值不写入仓库。生产环境使用 Secret、环境变量、密钥管理服务或受控配置中心注入。

## CAP 事件总线与最终一致性

事件总线只采用 CAP。框架包为 `Tw.EventBus.Abstractions`、`Tw.EventBus`、`Tw.EventBus.Cap`。

CAP 数据库存储由框架自定义 SqlSugar 存储适配。存储适配只处理 CAP 原始实体、表结构和 SqlSugar 事务行为，不处理队列、不处理数据同步、不改变 CAP 原有实体语义。

CAP 数据库规则：

- CAP 数据库单独配置为静态逻辑连接。
- CAP 数据不按租户拆分。
- CAP 数据不按分片拆分。
- 每个 SaaS 子库、分片子库、业务主库所在数据库服务器都存在对应 CAP 数据库。
- CAP 数据库主主同步属于基础设施职责。
- CAP 存储适配不感知主主同步。

同一 UoW 中写业务数据和写 CAP Outbox 的流程：

```text
解析当前业务库
 -> 解析当前业务库所在服务器对应的 CAP 数据库
 -> SqlSugar 多租户连接配置绑定业务库与 CAP 库
 -> 开启 SqlSugar UoW
 -> 写业务表
 -> 写 CAP Outbox
 -> 提交同一数据库事务行为
```

框架不承诺跨服务器强一致本地事务。一次写 UoW 涉及多个物理业务服务器时，框架拒绝同事务保存，业务必须拆为 CAP 最终一致流程。

CAP 已处理消息清理由 `Tw.EventBus.Cap` 提供调度任务：

- 清理成功且超过保留期的 Published、Received 记录。
- 清理前按状态、时间、重试次数分批。
- 清理任务可由 `Tw.BackgroundJobs.Quartz` 调度中心控制。
- 清理过程记录审计、指标和失败告警。
- 清理不删除未完成、失败待处理和死信待处理记录。

## 分布式 ID

`Tw.IdGeneration` 定义 ID 生成抽象，`Tw.IdGeneration.Yitter` 使用 `Yitter.IdGenerator` 作为默认实现。

- C# 实体、DTO、Command、Query 中 ID 正常使用 `long`。
- HTTP JSON 通过全局 Newtonsoft converter 输出为字符串。
- 数据库存储使用 `bigint`。
- WorkerId 来自配置、环境变量、数据库或部署平台分配。
- WorkerId 禁止随机生成。
- WorkerId 冲突启动失败。
- 时钟回拨超过配置窗口时拒绝发号并暴露健康异常。
- ID 不编码租户、分片、用户和权限信息。

公开抽象：

```csharp
public interface IIdGenerator
{
    long NewId();
}
```

字符串输出由序列化层负责，不在业务代码调用 `ToString()` 形成约定。

## ASP.NET Core、OpenAPI、API Versioning 与响应

`Tw.AspNetCore.Swashbuckle` 提供 OpenAPI 封装：

- Swashbuckle 注册。
- Newtonsoft 支持。
- JWT Bearer 安全定义。
- XML 注释加载。
- 枚举、错误码、统一响应描述。
- 分组与版本文档。
- Operation Filter、Schema Filter 扩展点。

API Versioning 由 `Tw.AspNetCore.Mvc` 统一注册，使用 URL Segment：

```text
/api/v1/orders
```

默认统一响应包裹：

```json
{
  "success": true,
  "code": "SYSTEM:000000",
  "message": "success",
  "data": {},
  "traceId": "...",
  "correlationId": "...",
  "timestamp": "2026-07-08T00:00:00+08:00"
}
```

错误响应保持真实 HTTP 状态码。错误码格式为 `{MODULE}:{000000}`。

以下响应不包裹：

- 文件。
- Stream。
- SSE。
- WebSocket。
- Swagger UI/OpenAPI。
- Health。
- Metrics。
- 原始回调。
- gRPC。

动态 API 规则：

- 不封装动态 API。
- 不提供动态 Controller 生成。
- 不通过应用服务方法名推断 HTTP 路由和动词。
- 不通过运行时扫描把应用服务自动暴露为外部 API。
- API 必须通过 Controller、Endpoint 或 gRPC Proto 显式声明。
- OpenAPI 契约必须来自显式协议层，不得来自动态 API 约定。

## CSRF、XSRF 与防伪

CSRF/XSRF 与防伪系统由 `Tw.AspNetCore.Mvc` 封装。

- Cookie 登录、浏览器表单和浏览器 AJAX 写请求默认启用防伪校验。
- Bearer Token API 默认不强制防伪校验。
- `GET`、`HEAD`、`OPTIONS`、`TRACE` 不执行防伪校验。
- `POST`、`PUT`、`PATCH`、`DELETE` 在启用 Cookie 身份或浏览器交互时执行防伪校验。
- Token Cookie 名称为 `XSRF-TOKEN`。
- 请求 Header 名称为 `X-XSRF-TOKEN`。
- Token Cookie `HttpOnly=false`，允许浏览器前端读取后写入 Header。
- 失败响应使用统一错误结构，不暴露内部防伪实现细节。
- 允许按 Controller、Action、Endpoint Metadata 禁用防伪。
- 允许按认证方案决定自动校验规则。
- 允许自定义 Cookie、Header、SameSite、Secure、Domain 和 Path。
- 防伪失败记录安全审计，包含主体、路径、方法、来源和关联标识。

## JSON

`Tw.Json.Abstractions` 提供 `IJsonSerializer`，`Tw.Json.Newtonsoft` 提供默认实现 `NewtonsoftJsonSerializer`。

默认设置：

- camelCase。
- 包含 null。
- `ReferenceLoopHandling.Error`。
- `TypeNameHandling.None`。
- ISO 日期。
- decimal 精度保护。
- enum 字符串。
- long ID HTTP 输出 converter。

`Tw.AspNetCore.Mvc.NewtonsoftJson` 使用 `Microsoft.AspNetCore.Mvc.NewtonsoftJson`。`Tw.EventBus.Cap` 使用 Newtonsoft serializer 扩展。

框架代码禁止直接使用 `System.Text.Json` 作为业务序列化入口。

## 文本模板

`Tw.TextTemplating` 定义通用文本模板抽象，`Tw.TextTemplating.Scriban` 使用 Scriban 作为默认实现。

- 支持字符串模板、文件模板、嵌入资源模板和配置模板源。
- 支持模板解析缓存和渲染结果缓存。
- 支持模板变量模型、文化信息、时钟、当前租户和当前用户只读注入。
- 支持模板语法错误、变量缺失、成员访问失败和渲染失败的结构化诊断。
- 默认禁用任意文件 include，文件模板只能从注册的模板根目录读取。
- 默认禁用危险成员访问、反射写入、进程、网络和文件系统访问。
- 模板渲染不得直接访问数据库、HTTP、缓存、消息队列和依赖注入容器。

`Tw.Templates` 是 `dotnet new` 工程模板包，不承载运行时文本模板能力。

## Excel 导入导出

`Tw.Excel` 定义 Excel 元数据模型和导入导出契约，`Tw.Excel.MiniExcel` 使用 MiniExcel 作为流式读写内核，并使用 OpenXML 能力处理多级表头、合并单元格、数据验证和下拉选项。

封装原则：

- 业务代码只依赖 `Tw.Excel`。
- 业务代码不得直接使用 MiniExcel API。
- Excel 表结构由框架列模型表达，不由业务代码拼装单元格坐标。
- 模板文件和导出文件不得包含密钥、完整个人敏感信息和未授权数据。
- 导入错误必须保留行号、列标识、字段路径、错误码和安全错误消息。

静态多级表头：

- 由固定列定义声明表头层级、字段名、数据类型、格式、是否必填和校验规则。
- 导出时生成合并表头和字段列。
- 导入时按表头路径和字段列映射，不依赖列顺序作为唯一依据。

动态多级表头：

- 由运行时列定义声明动态分组、动态字段和业务键。
- 动态列必须具备稳定字段标识，不能只依赖显示名称。
- 导入时将动态列转换为结构化键值集合或业务定义的动态字段模型。
- 动态列数量、层级深度和单元格数量必须配置上限。

空白模板导出：

- 支持导出只有表头、示例行、字段说明和数据验证规则的空白模板。
- 支持固定下拉选项列。
- 下拉选项来自枚举、静态字典、配置或受控业务字典快照。
- 下拉选项写入隐藏 Sheet 或 OpenXML 数据验证区域。
- 选项数量超过 Excel 数据验证限制时，模板必须使用隐藏 Sheet 范围引用。

性能和安全：

- 大文件导入使用流式读取。
- 单次导入必须限制文件大小、Sheet 数、行数、列数和单元格字符长度。
- 导入前校验扩展名、MIME、文件头和压缩包结构。
- 公式单元格默认按文本处理，导出用户输入文本时防止公式注入。
- 导入导出必须记录审计事件，包含操作者、模板、行数、失败数和数据范围。

## 本地化

`Tw.Localization` 基于 Microsoft `IStringLocalizer`，资源文件使用 JSON，不使用 `.resx`。

默认资源路径：

```text
Localization/zh-CN.json
Localization/en-US.json
```

默认文化为 `zh-CN`。

文化来源顺序：

- HTTP：`X-Culture`、`Accept-Language`、租户默认文化、服务默认文化。
- gRPC：Metadata `culture`。
- CAP：Header `culture`。
- Job：任务上下文 `culture`。

资源 Key 使用扁平结构。资源解析使用 Newtonsoft.Json。

## 验证与输入边界

不创建完整实现型 `Tw.Validation` 包。验证基础模型放入 `Tw.Validation.Abstractions`：

- `ValidationException`。
- `ValidationError`。
- Binding Error Model。
- 字段路径。
- 错误码。

业务验证由 `Tw.Application` 执行 FluentValidation。协议绑定错误由协议包处理。字段名使用 JSON 契约字段名。

CAP Consumer 和后台任务验证失败属于不可重试错误。

## 脱敏与写回保护

脱敏能力放入 `Tw.Security`。

公开能力：

- `IDataMasker`。
- `IDataMaskingRule`。
- `IDataMaskingPolicyProvider`。
- `ISensitiveValueDetector`。
- `SensitiveDataAttribute`。
- `SensitiveDataKind`。

脱敏只用于输出、日志、审计、导出和错误响应。脱敏结果不得进入持久化写入。

写回保护规则：

- 前端传回手机号、身份证、邮箱等字段时，框架检测掩码格式。
- 检测到掩码值写入敏感字段时拒绝请求。
- 拒绝结果返回稳定验证错误码。
- 原始敏感值更新必须通过明确输入模型和权限校验。

## 认证、授权、Permission、Feature 与 Setting

`Tw.Security` 提供当前用户、Principal 访问器和身份承载模型。

`Tw.Authorization` 提供：

- Permission 定义。
- Permission Checker。
- Grant Store。
- Permission Cache。
- 用户、角色、租户、资源授权检查。

`Tw.Settings` 提供：

- Setting 定义、读取、缓存、刷新。
- 租户级、服务级、用户级作用域。

`Tw.Features` 提供：

- Feature 定义、读取、缓存、刷新。
- 租户级、服务级、用户级作用域。
- Feature 禁用时的稳定错误码和审计事件。

`Tw.Identity.OpenIddict` 提供身份中心实现。业务服务验证 JWT 不强依赖身份中心实现。

认证授权边界：

- 网关只做 JWT 验证和粗粒度路由策略。
- 服务仍然验证 JWT、Permission、资源所有权和租户权限。
- 内部服务调用不天然可信。
- Gateway 转发原始 `Authorization`。
- Gateway 清除调用方伪造的身份 Header。

## 缓存、分布式锁、幂等与韧性

`Tw.Caching` 定义缓存抽象和键规范。`Tw.Caching.FusionCache` 默认使用 FusionCache，运行时 Redis 协议使用 Valkey 或 Redis，客户端为 StackExchange.Redis。

缓存支持 L1 Memory、L2 Redis/Valkey、Backplane、Fail-safe、Stampede protection、Tag invalidation、空值缓存、随机 TTL 和指标。

缓存键格式：

```text
{system}:{service}:{tenantId}:{shardStrategy}:{shardKey}:{resource}:{id}
```

非 SaaS 使用 `tenantId = default`。无分片使用 `shardStrategy = none`、`shardKey = default`。写操作完成后通过 CAP 发布缓存失效事件。缓存失效在 UoW 提交后执行。

`Tw.DistributedLocking` 定义分布式锁抽象。`Tw.DistributedLocking.Redis` 默认使用 `DistributedLock.Redis`。

- 锁键包含租户和分片维度。
- 锁必须设置等待超时和租约。
- 禁止无限等待。
- 加锁失败映射为并发冲突或业务拒绝。
- 锁释放失败记录告警和审计。

`Tw.Idempotency` 独立包。SqlSugar 持久化实现由 `Tw.Data.SqlSugar` 提供。

幂等覆盖 HTTP 写请求、gRPC 写请求、CAP 消费和后台任务命令。幂等键与租户、资源、操作类型和业务唯一性绑定。重复请求返回首次结果或冲突响应。冲突响应必须包含稳定错误码。

`Tw.Resilience` 使用 Polly 8 和 `Microsoft.Extensions.Http.Resilience`。策略包含 Timeout、Retry、Circuit Breaker、Rate Limiter、Bulkhead/Concurrency Limiter 和 Fallback。非幂等写操作、输入错误、权限错误和契约错误禁止自动重试。所有网络、数据库、缓存、消息和文件调用必须声明超时或 Deadline。

## HTTP Client 与 gRPC

`Tw.Http.Client` 不合并到 `Tw.AspNetCore`。

能力：

- HttpClientFactory。
- Microsoft.Extensions.ServiceDiscovery。
- Microsoft.Extensions.Http.Resilience。
- Polly。
- Newtonsoft 序列化。
- NSwag.MSBuild 生成客户端。
- 关联标识传播。

Header allowlist：

- `Authorization`
- `traceparent`
- `tracestate`
- `X-Correlation-Id`
- `X-Tenant-Id`
- `X-Culture`
- `Idempotency-Key`

`Tw.Grpc` 使用官方 gRPC 包，采用 contract-first `.proto`。

- Proto 字段编号长期稳定。
- 删除字段保留编号和名称占位。
- JSON 字段 camelCase，Proto 字段 snake_case。
- 客户端必须设置 Deadline。
- 元数据传播 Trace、Correlation、Tenant、Culture、Authorization。
- 错误映射到稳定业务错误码。

## 后台任务调度中心

`Tw.BackgroundJobs` 定义后台任务抽象和执行管道。`Tw.BackgroundJobs.Quartz` 使用 Quartz.NET。

能力：

- 统一后台任务调度中心。
- 调度接口。
- 创建、暂停、恢复、触发、停止任务。
- Cron 校验。
- 集群调度。
- 静态 Scheduler 数据库。
- Job 执行管道。
- Job 审计、日志、Trace、Metric。

后台任务进入业务逻辑时调用 `ISender.Send(...)`。

## 观测与审计

`Tw.Observability` 定义观测字段、上下文和健康状态模型。`Tw.Observability.Serilog` 使用 Serilog，`Tw.Observability.OpenTelemetry` 使用 OpenTelemetry。

本地 Aspire 使用 Aspire Dashboard 接收 OTLP。生产使用 OTLP Exporter 发送到 Collector。

默认日志字段：

- `service.name`
- `service.version`
- `environment`
- `trace_id`
- `correlation_id`
- `tenant_id`
- `shard_strategy`
- `shard_key`
- `user_id`
- `event_name`

`Tw.Auditing` 单独拆包。审计覆盖登录、登出、权限变更、配置变更、数据导出、敏感数据访问、生产数据修复、批量操作和安全拒绝。

日志、审计和错误响应禁止包含密钥、令牌、完整手机号、完整证件号、密码、原始敏感载荷和完整连接串。

## 配置中心与 Nacos

`Tw.Configuration` 是配置治理抽象，不包含配置中心实现，不包含密钥管理。

`Tw.Configuration.Json` 提供多 JSON 文件加载。配置格式只支持 JSON，不支持 XML、INI 和任意路径扫描。

`Tw.Configuration.Nacos` 使用 Nacos 2.5.x 与 `nacos-sdk-csharp 1.3.10`，并提供非 Kubernetes 场景的 Nacos 服务发现桥接。

配置来源顺序：

```text
framework defaults
 -> appsettings.json
 -> appsettings.{Environment}.json
 -> appsettings.{Role}.json
 -> configs/*.json
 -> Nacos shared
 -> Nacos service
 -> Nacos role
 -> User Secrets
 -> environment variables
 -> command line
```

多 JSON 文件规则：

- 默认加载 `appsettings.json`。
- 按运行环境加载 `appsettings.{Environment}.json`。
- 按运行角色加载 `appsettings.{Role}.json`。
- 按显式清单加载 `configs/*.json`。
- 文件覆盖顺序必须稳定，后加载文件覆盖先加载文件。
- 文件路径必须位于应用内容根目录、配置目录或显式允许的绝对目录。
- 不存在的可选文件可以跳过，必填文件缺失时启动失败。
- JSON 解析错误、重复关键配置冲突和必填配置缺失必须启动失败。
- 日志只输出配置文件路径、配置节和校验结果，不输出敏感值。

允许动态刷新：

- 日志级别。
- 缓存 TTL 和策略。
- 限流策略。
- HTTP 韧性策略。
- Gateway 路由和灰度权重。
- Feature。
- Setting。

禁止热更新：

- 数据库 bootstrap 连接。
- CAP 数据库。
- Broker Endpoint。
- ID WorkerId。
- JWT Key。
- 加密 Key。
- 租户和分片拓扑。

动态配置变更先校验，校验失败保留上一份有效配置并记录审计和告警。

密钥来自 User Secrets、Aspire、Kubernetes Secret、环境变量或企业密钥系统，不创建 `Tw.Secrets`。

## Gateway

`Tw.Gateway` 定义网关治理模型。`Tw.Gateway.Yarp` 使用 YARP。

职责：

- 路由。
- Path Rewrite。
- Header 治理。
- 负载均衡。
- 健康探测。
- 灰度权重。
- JWT 校验。
- CORS。
- 请求大小限制。
- 限流。
- Timeout。
- 基础 Circuit Breaker。
- WebSocket、SSE、gRPC 透传。
- Gateway 自身错误统一响应。

禁止：

- 业务编排。
- 聚合查询。
- 数据库访问。
- UoW。
- CAP。
- Application Pipeline。
- 租户数据库切换。
- 分片业务决策。
- OpenAPI 聚合。
- 把 JWT 转换为身份、角色、权限 Header。

Kubernetes 场景：

```text
Ingress / LB
 -> Tw.Gateway.Yarp
 -> Kubernetes DNS service discovery
 -> backend services
```

非 Kubernetes 场景：

```text
Load Balancer
 -> Tw.Gateway.Yarp
 -> Tw.Configuration.Nacos service discovery
 -> backend services
```

动态路由来自 `Tw.Configuration` 或 Nacos，变更先校验，校验失败保留上一份有效路由。

严格全局限流由外部 Edge、WAF、API Management 或负载均衡承担。Gateway 内置限流为应用级保护。

## 文件存储

框架不创建对象存储包。文件、附件、对象存储、预签名 URL、病毒扫描、文件权限和生命周期由独立文件存储服务实现。

框架提供可复用底座：

- `Tw.Authorization`
- `Tw.Auditing`
- `Tw.Observability`
- `Tw.Observability.Serilog`
- `Tw.Observability.OpenTelemetry`
- `Tw.Http.Client`
- `Tw.Resilience`
- `Tw.Caching`
- `Tw.Caching.FusionCache`
- `Tw.DistributedLocking`
- `Tw.DistributedLocking.Redis`
- `Tw.Idempotency`

## 测试

`Tw.TestBase`、`Tw.AspNetCore.TestBase`、`Tw.Data.SqlSugar.TestBase`、`Tw.EventBus.Cap.TestBase` 是 test-only 包，不进入生产产物。

能力：

- 测试时钟。
- 测试 ID。
- 测试当前用户、租户、文化和关联上下文。
- `WebApplicationFactory` 封装，由 `Tw.AspNetCore.TestBase` 提供。
- 测试认证，由 `Tw.AspNetCore.TestBase` 提供。
- Aspire AppHost 测试。
- Testcontainers 容器编排。
- 数据库夹具，由 `Tw.Data.SqlSugar.TestBase` 提供。
- CAP、RabbitMQ、Outbox/Inbox 测试支持，由 `Tw.EventBus.Cap.TestBase` 提供。
- Redis、Nacos 测试支持按被测包能力引用。
- OpenAPI、Proto、CAP 事件、错误码契约验证。
- 日志、审计、Trace、Metric 测试采集器。
- 脱敏输出和写回保护断言。

框架自有包质量门禁：

- 行覆盖率不低于 98%。
- 核心逻辑分支覆盖率不低于 90%。
- UoW、多租户、分片、CAP、幂等、授权、脱敏、网关、配置、执行管道必须覆盖失败路径。

业务服务模板默认门禁：

- 行覆盖率不低于 80%。
- 收费、权限、资金、数据一致性相关模块行覆盖率不低于 90%。
- 契约测试覆盖 HTTP、gRPC、CAP 事件和错误码兼容性。

## 工具链

`Tw.Templates` 发布为 NuGet 模板包，使用官方 `dotnet new` 模板体系。

模板：

- `tw-workspace`
- `tw-service`
- `tw-building-block`
- `tw-gateway`
- `tw-contracts`

模板支持自定义服务名、项目名前缀和 RootNamespace。层后缀固定。

`Tw.Cli` 发布为 `dotnet tool`，命令名 `tw`。

命令：

- `tw doctor`
- `tw new workspace|service|gateway|building-block`
- `tw add capability`
- `tw add command|query|event|consumer|job|grpc`
- `tw check`
- `tw contract verify`
- `tw package audit`

写文件命令必须可重复执行。目标文件已存在时默认失败，显式 `--force` 才覆盖。

`Tw.Analyzers` 作为 Roslyn Analyzer NuGet 包安装到模板项目中，`PrivateAssets=all`。

内置规则：

- 禁止引用 `MassTransit`。
- 禁止生产项目引用 `Tw.TestBase`、`Tw.AspNetCore.TestBase`、`Tw.Data.SqlSugar.TestBase`、`Tw.EventBus.Cap.TestBase`。
- 禁止出现 `Tw.Infrastructure`、`Tw.Context`、`Tw.ExecutionPipeline`、`Tw.DynamicApi`、`Tw.ApplicationConfiguration`、`Tw.Snowflake`、`Tw.DistributedLock`、`Tw.Cqrs`、`Tw.UnitOfWork`、`Tw.Data.Abstractions`。
- 禁止业务层直接依赖 SqlSugar、CAP 实现包、ASP.NET Core、Quartz、Gateway 包。
- 禁止 `Application`、`Domain`、`Contracts` 使用 SqlSugar `ChangeDatabase`。
- 禁止服务注册扩展放入 `Microsoft.Extensions.DependencyInjection` 命名空间。
- 禁止扩展方法使用 `AddTwXxx` 形式。
- 禁止自有接口、类、枚举、属性、字段、方法和包内部文件名使用 `Tw`、`Abp`、`Furion` 框架名前缀，`TwException` 除外。
- 禁止动态 Controller 生成和应用服务自动暴露为 HTTP API。
- 禁止 `.Result`、`.Wait()` 阻塞异步。
- 禁止框架代码直接使用 `System.Text.Json`。
- 禁止业务验证依赖 `DataAnnotations`。
- 检查敏感字段脱敏策略。
- 检查并发字段不得由普通 DTO 直接写入。
- 检查 Excel 导入导出文件大小、行数、列数和公式注入防护配置。
- 检查文本模板禁止任意文件 include 和危险成员访问。
- 检查共享包 `package-charter.yaml`。
- 检查测试项目命名和生产项目引用边界。

架构边界和禁止依赖为 `error`。命名、脱敏、异步阻塞为 `warning`。可维护性提示为 `info`。

## CI/CD 与发布治理

构建使用 NUKE Build。版本使用 GitVersion。测试使用 xUnit v3、NSubstitute、AwesomeAssertions、coverlet、ReportGenerator、Testcontainers。变异测试使用 Stryker.NET。SBOM 使用 CycloneDX。镜像扫描使用 Trivy。签名使用 Cosign。部署资产使用 Helm，交付使用 Argo CD。

所有依赖版本进入 `Directory.Packages.props` 集中管理。模板生成 `global.json` 固定 SDK。构建脚本必须能从干净检出执行。

SqlSugar 迁移使用 SQL-first 迁移，不使用 EF Migration。

## 默认依赖版本

版本锁定以 NuGet V3 flat container 元数据为准。稳定版优先，预览版不作为默认依赖。

| 能力 | 包 | 版本 |
| --- | --- | --- |
| DI | `Autofac` | `9.3.0` |
| DI | `Autofac.Extensions.DependencyInjection` | `11.0.1` |
| AOP | `Autofac.Extras.DynamicProxy` | `8.0.0` |
| AOP | `Castle.Core` | `5.2.1` |
| EventBus | `DotNetCore.CAP` | `10.0.1` |
| EventBus | `DotNetCore.CAP.RabbitMQ` | `10.0.1` |
| ORM | `SqlSugarCore` | `5.1.4.216` |
| ID Generation | `Yitter.IdGenerator` | `1.0.15` |
| Mapping | `Riok.Mapperly` | `4.3.1` |
| JSON | `Newtonsoft.Json` | `13.0.4` |
| ASP.NET JSON | `Microsoft.AspNetCore.Mvc.NewtonsoftJson` | `10.0.9` |
| OpenAPI | `Swashbuckle.AspNetCore` | `10.2.3` |
| OpenAPI | `Swashbuckle.AspNetCore.Newtonsoft` | `10.2.3` |
| API Versioning | `Asp.Versioning.Mvc` | `10.0.0` |
| API Versioning | `Asp.Versioning.Mvc.ApiExplorer` | `10.0.0` |
| Text Templating | `Scriban` | `7.2.5` |
| Excel | `MiniExcel` | `1.45.0` |
| Excel | `DocumentFormat.OpenXml` | `3.5.1` |
| Cache | `ZiggyCreatures.FusionCache` | `2.6.0` |
| Redis | `StackExchange.Redis` | `3.0.11` |
| Lock | `DistributedLock.Redis` | `1.1.1` |
| Resilience | `Polly` | `8.7.0` |
| Resilience | `Microsoft.Extensions.Http.Resilience` | `10.7.0` |
| Service Discovery | `Microsoft.Extensions.ServiceDiscovery` | `10.7.0` |
| Gateway Discovery | `Microsoft.Extensions.ServiceDiscovery.Yarp` | `10.7.0` |
| Gateway | `Yarp.ReverseProxy` | `2.3.0` |
| Application Pipeline | `MediatR` | `12.5.0` |
| Validation | `FluentValidation` | `12.1.1` |
| Identity | `OpenIddict` | `7.5.0` |
| gRPC | `Grpc.AspNetCore` | `2.80.0` |
| gRPC | `Grpc.Net.Client` | `2.80.0` |
| Proto | `Google.Protobuf` | `3.35.1` |
| Jobs | `Quartz` | `3.18.2` |
| Logging | `Serilog.AspNetCore` | `10.0.0` |
| Logging | `Serilog.Sinks.OpenTelemetry` | `4.2.0` |
| Observability | `OpenTelemetry.Extensions.Hosting` | `1.16.0` |
| Observability | `OpenTelemetry.Exporter.OpenTelemetryProtocol` | `1.16.0` |
| Observability | `OpenTelemetry.Instrumentation.AspNetCore` | `1.16.0` |
| Observability | `OpenTelemetry.Instrumentation.Http` | `1.16.0` |
| Nacos | `nacos-sdk-csharp` | `1.3.10` |
| Nacos | `nacos-sdk-csharp.Extensions.Configuration` | `1.3.10` |
| Nacos | `nacos-sdk-csharp.Extensions.ServiceDiscovery` | `1.3.10` |
| SDK | `NSwag.MSBuild` | `14.7.1` |
| CLI | `System.CommandLine` | `2.0.9` |
| CLI | `Spectre.Console` | `0.57.2` |
| Analyzer | `Microsoft.CodeAnalysis.CSharp` | `5.6.0` |
| Test | `xunit.v3` | `3.2.2` |
| Test | `xunit.runner.visualstudio` | `3.1.5` |
| Test | `Microsoft.NET.Test.Sdk` | `18.7.0` |
| Test | `AwesomeAssertions` | `9.4.0` |
| Test | `NSubstitute` | `5.3.0` |
| Coverage | `coverlet.collector` | `10.0.1` |
| Integration Test | `Testcontainers` | `4.13.0` |
| Integration Test | `Microsoft.AspNetCore.Mvc.Testing` | `10.0.9` |
| Integration Test | `Aspire.Hosting.Testing` | `13.4.6` |
| HTTP Stub | `WireMock.Net` | `2.12.0` |
| DB Reset | `Respawn` | `7.0.0` |
| Report | `ReportGenerator` | `5.5.10` |
| Mutation | `dotnet-stryker` | `4.16.0` |

`OpenTelemetry.Instrumentation.GrpcNetClient` 不作为默认依赖。

## 验证清单

- 所有运行时包包含 `package-charter.yaml`。
- 所有公开 API 具备 XML 文档注释。
- 所有服务注册扩展按功能命名，不放入 `Microsoft.Extensions.DependencyInjection`。
- 所有自有 API 和包内部文件名不使用 `Tw`、`Abp`、`Furion` 框架名前缀，`TwException` 除外。
- 所有被禁用包名不保留旧包、兼容别名和转发类型。
- 所有默认依赖进入集中版本管理。
- 所有密钥、连接串和 Token 不进入仓库。
- `Tw.Analyzers` 阻断禁止引用和架构越界。
- 所有 `*TestBase` 包不进入生产项目。
- 动态 API 和动态 Controller 生成能力不存在。
- 框架自有包行覆盖率不低于 98%。
- 核心逻辑分支覆盖率不低于 90%。
- CAP Outbox 与业务写入同事务行为验证通过。
- SaaS、非 SaaS、分片、非分片四种连接解析组合验证通过。
- HTTP、gRPC、CAP Consumer、后台任务共享同一应用层执行行为。
- CSRF/XSRF 对 Cookie 登录和浏览器写请求生效，对纯 Bearer API 不默认强制。
- 并发冲突稳定映射为 `409 Conflict`。
- Excel 多级表头、动态表头、空白模板和固定下拉选项验证通过。
- Scriban 模板渲染缓存、成员访问限制和错误诊断验证通过。
- 日志、审计、错误响应和导出不泄露敏感信息。
