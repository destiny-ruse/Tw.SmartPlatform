# Tw .NET 微服务框架最终设计蓝图

日期：2026-07-08
文档类型：Explanation + Reference
目标读者：负责实现、评审和使用 `Tw.*` .NET 微服务底层框架的后端开发人员、Tech Lead、架构师和测试负责人
状态：已确认设计

## 目标

本文定义 `Tw.SmartPlatform` 的 `.NET 10+` 企业级微服务底层框架。框架以 `Tw.*` 共享包提供微服务开发底座，覆盖 CQRS、缓存、工作单元、事件总线、最终一致性、认证授权、Swagger、日志、gRPC、AOP、SqlSugar、自动依赖注入、链路追踪、多租户、业务分片、后台调度、本地 Aspire 开发、网关、测试和工程治理。

框架采用可组合构建块，不提供单体大包。每个运行时包都拥有清晰职责、公开能力、依赖边界和 `package-charter.yaml`。业务服务按自身画像组合引用底层能力，不能绕过框架直接依赖底层基础设施实现。

## 架构参考来源

本设计参考本地源码：

- `D:\SourceCode\abp`：参考 ABP vNext 的模块边界、Unit of Work、多租户、Setting、Feature、Permission、Autofac、AOP、后台任务和事件总线组织方式
- `D:\SourceCode\Furion`：参考 Furion 的 Swagger 封装、Web 快速开发体验、模板、统一响应、JSON、本地化、调度和测试工程体验

参考源码只作为架构组织和封装习惯输入。`Tw.*` 不照搬 ABP 或 Furion 的运行时大包结构，不引入 Furion 作为依赖。

## 基础决策

- 运行时目标为 `.NET 10+`
- 本地开发支持 Aspire，编排项目位于 `backend/dotnet/Aspire`
- 生产运行支持 Kubernetes 与非 Kubernetes 两种部署方式
- 包名和程序集统一使用 `Tw.*`
- 对外接口和类名不重复 `Tw` 前缀，例如缓存接口命名为 `ICache`
- `Tw.Context` 合并到 `Tw.Core`
- 不创建 `Tw.Infrastructure` 大包
- 不创建 `Tw.ExecutionPipeline` 包，共用执行管道放入 `Tw.Core` 的内部执行能力
- 不使用 MassTransit
- Swagger 完全使用 Swashbuckle
- JSON 统一使用 Newtonsoft.Json
- 对象映射统一使用 Mapperly
- 分布式 ID 默认使用 `Yitter.IdGenerator`
- API 模型中 ID 保持 `long`，HTTP JSON 由全局格式化配置输出为字符串
- 文件与对象存储不作为框架级包，由独立文件存储服务承载

## 最终包清单

### 运行时包

| 包 | 职责 |
| --- | --- |
| `Tw.Core` | 基础类型、错误模型、执行上下文、当前用户、当前租户、关联标识、时钟、JSON、脱敏、验证错误模型、内部执行管道 |
| `Tw.DependencyInjection` | 容器中立注册模型、自动依赖注入元数据、AOP 元数据、服务暴露规则、Options 绑定规则 |
| `Tw.DependencyInjection.Autofac` | Autofac 运行时适配、Castle DynamicProxy、拦截器注册、AOP 忽略规则 |
| `Tw.UnitOfWork` | 工作单元抽象、事务作用域、提交回调、失败回调、嵌套 UoW 规则 |
| `Tw.Data.Abstractions` | 数据源描述、连接解析、仓储基础抽象、连接目录抽象 |
| `Tw.Data.SqlSugar` | SqlSugar 适配、连接工厂、仓储实现、审计字段、软删除、SqlSugar UoW 适配 |
| `Tw.MultiTenancy` | 多租户抽象、租户上下文、租户解析、租户数据源目录、SaaS 运行模式 |
| `Tw.Sharding` | 业务分片抽象、分片上下文、分片规则、分片切换、跨分片边界 |
| `Tw.Snowflake` | 分布式 ID 抽象、Yitter.IdGenerator 默认实现、WorkerId 管理、时钟回拨处理 |
| `Tw.Cqrs` | MediatR 集成、Command、Query、Pipeline Behavior、验证、授权、幂等、UoW 调用顺序 |
| `Tw.Authorization` | Permission 定义、授权检查、Grant Store、权限缓存、资源授权边界 |
| `Tw.Identity.OpenIddict` | 统一身份中心实现、OpenIddict 集成、OIDC/OAuth2、Token 签发与验证 |
| `Tw.ApplicationConfiguration` | Setting 与 Feature 的定义、读取、缓存、动态刷新和作用域规则 |
| `Tw.AspNetCore` | HTTP 宿主集成、MVC Filter、异常处理、统一响应、Swagger、API Versioning、认证、限流、健康端点 |
| `Tw.Localization` | JSON 本地化资源、文化解析、Microsoft `IStringLocalizer` 适配 |
| `Tw.EventBus.Abstractions` | 集成事件契约、事件发布抽象、事件订阅元数据、事件幂等契约 |
| `Tw.EventBus.Cap` | CAP 集成、RabbitMQ 传输、SqlSugar CAP 存储、Outbox/Inbox、消费过滤器、清理任务 |
| `Tw.Caching` | 多级缓存抽象、缓存键、TTL、标签、空值缓存、失效事件、防击穿 |
| `Tw.DistributedLock` | 分布式锁、租约、超时、Redis 锁实现、锁键规范 |
| `Tw.Idempotency` | 幂等键、幂等窗口、请求去重、消息去重、冲突响应 |
| `Tw.Resilience` | 超时、重试、熔断、限流、隔离、降级策略封装 |
| `Tw.Http.Client` | HttpClientFactory、服务发现、韧性策略、Newtonsoft 序列化、NSwag 客户端集成 |
| `Tw.Grpc` | gRPC 服务端拦截器、客户端工厂、Deadline、元数据传播、错误映射 |
| `Tw.BackgroundJobs` | Quartz.NET 调度中心、任务定义、任务控制 API、任务执行管道 |
| `Tw.Observability` | Serilog、OpenTelemetry、日志上下文、Trace、Metrics、健康端点 |
| `Tw.Auditing` | 审计事件、审计存储抽象、审计日志、敏感操作记录 |
| `Tw.Configuration` | 配置治理抽象、配置校验、动态配置热更新边界、配置变更审计 |
| `Tw.Configuration.Nacos` | Nacos 配置源、Nacos 非 Kubernetes 服务发现桥接 |
| `Tw.Gateway` | YARP 网关封装、路由、认证校验、Header 治理、限流、基础韧性、动态路由 |
| `Tw.Testing` | 测试辅助、测试上下文、WebApplicationFactory、Aspire Testing、Testcontainers、契约测试 |

### 工具包

| 包 | 职责 |
| --- | --- |
| `Tw.Templates` | `dotnet new` 模板包 |
| `Tw.Cli` | `tw` 命令行工具、生成、能力启用、工程校验、契约校验 |
| `Tw.Analyzers` | Roslyn Analyzer，编译期架构边界和禁止规则检查 |

### 禁止创建的包

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
- 任何 `MassTransit` 相关包

## 包依赖规则

`Tw.Core` 是底层基础包，不依赖 ASP.NET Core、SqlSugar、CAP、Autofac、Quartz、OpenIddict、YARP、Redis、FusionCache、MediatR 和测试库。

`Tw.UnitOfWork` 只定义工作单元抽象，不依赖 SqlSugar、CAP、ASP.NET Core、Autofac、MediatR 和 Quartz。

`Tw.Data.SqlSugar` 依赖 `Tw.Data.Abstractions`、`Tw.UnitOfWork`、`Tw.Core`。业务项目不能直接使用 SqlSugar 的 `ChangeDatabase`。

`Tw.MultiTenancy` 和 `Tw.Sharding` 独立拆包。租户解析、分片规则和连接选择通过抽象协作，业务分片必须由业务契约显式提供分片键。

`Tw.EventBus.Cap` 依赖 CAP 与 `Tw.EventBus.Abstractions`，CAP 存储使用框架自定义 SqlSugar 存储适配。业务服务只依赖 `Tw.EventBus.Abstractions` 发布集成事件。

`Tw.AspNetCore` 汇聚 HTTP 宿主能力。Swagger、API Versioning、Rate Limiting、Health Checks 都作为 `Tw.AspNetCore` 内部能力，不拆独立包。

`Tw.Gateway` 不能依赖 `Tw.Data.*`、`Tw.UnitOfWork`、`Tw.Cqrs`、`Tw.EventBus.*`、`Tw.BackgroundJobs`、`Tw.MultiTenancy`、`Tw.Sharding`。

`Tw.Testing` 只能被测试项目引用，生产项目禁止引用。

## 服务项目结构

服务模板固定生成以下项目：

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

支持自定义项目名前缀和 `RootNamespace`。层后缀固定，保证模板、CLI、Analyzer、项目引用和契约治理能够稳定识别。

各项目职责如下：

| 项目 | 职责 |
| --- | --- |
| `Contracts` | DTO、Command/Query 合约、事件合约、错误码、公开常量、客户端共享契约 |
| `Domain` | 实体、值对象、领域服务、领域规则、领域事件 |
| `Application` | 用例编排、CQRS Handler、权限检查、UoW 边界、事件发布、缓存与幂等协调 |
| `Infrastructure` | 数据库、缓存、锁、第三方服务、仓储实现、外部适配器 |
| `HttpApi` | Controller、HTTP 参数绑定、Swagger 元数据、HTTP Filter |
| `HttpApi.Client` | HTTP SDK、NSwag 生成客户端、服务调用封装 |
| `Host` | 宿主入口、运行角色组合、配置、依赖注入、HTTP/gRPC/CAP/Jobs 启用 |
| `UnitTests` | 单元测试 |
| `IntegrationTests` | 集成测试 |
| `ContractTests` | HTTP、gRPC、CAP 事件和错误码契约测试 |

`Contracts` 和 `HttpApi.Client` 可以发布为 NuGet。`Host` 生成镜像或可执行程序。测试项目不发布。

## 业务项目默认引用

| 项目 | 默认引用 |
| --- | --- |
| `Contracts` | `Tw.Core`，事件合约存在时引用 `Tw.EventBus.Abstractions` |
| `Domain` | `Tw.Core` |
| `Application` | `Domain`、`Contracts`、`Tw.Core`、`Tw.Cqrs`、`Tw.Authorization`、`Tw.UnitOfWork` |
| `Infrastructure` | `Application`、`Domain`、`Contracts`、`Tw.Data.SqlSugar`，按能力引用缓存、锁、幂等、Http Client、Snowflake |
| `HttpApi` | `Application`、`Contracts`、`Tw.AspNetCore`、`Tw.Cqrs` |
| `HttpApi.Client` | `Contracts`、`Tw.Http.Client` |
| `Host` | 服务启用的所有运行时实现包 |
| `UnitTests` | 被测项目、`Tw.Testing`、xUnit、NSubstitute、AwesomeAssertions |
| `IntegrationTests` | `Host` 或相关适配项目、`Tw.Testing`、Testcontainers |
| `ContractTests` | `Contracts`、`HttpApi.Client`、`Tw.Testing` |

`Domain` 禁止引用 `Contracts`、`AspNetCore`、`Cqrs`、`Data`、`Cache`、`EventBus`、`Http.Client`。`HttpApi` 禁止引用 `Data`、`EventBus.Cap`、`BackgroundJobs`、`Grpc`、`Infrastructure`。

## 上下文与执行管道

`Tw.Core` 提供：

- `ICurrentUser`
- `ICurrentTenant`
- `ICurrentCulture`
- `ICorrelationContext`
- `IExecutionContext`
- `TimeProvider`
- `IJsonSerializer`
- `IDataMasker`
- `ValidationException`
- `ValidationError`
- 内部 `ExecutionPipeline`

HTTP、gRPC、CAP Consumer 和后台任务优先使用各自宿主原生管道：

- HTTP 使用 Middleware、MVC Filter、Endpoint Filter
- gRPC 使用 Interceptor
- CAP Consumer 使用 CAP Filter
- Quartz Job 使用 Job Listener 或 Job Pipeline

这些宿主适配共享 `Tw.Core` 的内部执行管道模型，使日志、审计、UoW、授权、幂等、验证和异常分类只实现一套核心逻辑。

## DI、Autofac 与 AOP

`Tw.DependencyInjection` 定义容器中立的服务注册模型、自动注册规则、生命周期、服务暴露规则和 AOP 元数据。

`Tw.DependencyInjection.Autofac` 是默认运行时适配，使用 Autofac 与 Castle DynamicProxy。AOP 能力参考 ABP 的忽略模式：

- Controller、gRPC Service、CAP Consumer、Quartz Job 优先走宿主原生 Pipeline
- 普通应用服务、领域服务、基础设施服务通过 AOP 拦截
- 已由宿主管道处理的能力不重复套 AOP
- `DisableInterception` 和类似标记可以禁用拦截
- 拦截器只处理跨切面能力，业务规则不能写入拦截器

内置 DI 保留为基础兼容能力。默认运行时选 Autofac，因为框架需要成熟的动态代理、拦截器和模块化注册能力。

## CQRS 与 MediatR

`Tw.Cqrs` 直接使用 MediatR，不额外封装 MediatR 包。默认版本锁定为 `MediatR 12.5.0`。新版本 MediatR 需要许可证 Key 配置，框架不采用该线作为默认依赖。

Pipeline 顺序固定：

```text
ExecutionContext
 -> Feature
 -> Authorization
 -> Validation
 -> Idempotency
 -> Sharding
 -> UnitOfWork
 -> Auditing
 -> Handler
 -> Completed Hooks
```

HTTP、gRPC、CAP Consumer、后台任务进入业务用例时统一调用 `ISender.Send(...)`，复用同一应用层行为。

验证使用 `FluentValidation 12.1.1`。不使用 `FluentValidation.AspNetCore`。业务验证不使用 `DataAnnotations`。

## Unit of Work

`Tw.UnitOfWork` 独立抽象工作单元，参考 ABP 的 UoW 行为：

- 支持 required、requires new、suppress 语义
- 支持事务与非事务 UoW
- 支持提交前、提交后、失败后回调
- 支持当前 UoW 上下文
- 支持取消令牌传递
- 不包含具体 ORM 和消息实现

`Tw.Data.SqlSugar` 实现 SqlSugar UoW。读操作不默认开启事务。写操作由 `Tw.Cqrs` 或宿主 Pipeline 建立事务边界。

## 多租户与 SaaS

包命名保留 `Tw.MultiTenancy`。SaaS 是多租户能力的运行模式，不作为包名。

租户来源：

- HTTP：域名、路径、可信 Header、Token Claim
- gRPC：Metadata、Token Claim
- CAP：消息 Header、事件元数据
- BackgroundJobs：任务参数、任务上下文

数据访问层不得依赖 `HttpContext`。启用 SaaS 时，HTTP 上下文只负责解析租户并写入 `ICurrentTenant`。SqlSugar 连接对象由 `ICurrentTenant`、数据源目录和分片上下文解析。

### 组织机构服务

该服务不启用分片。

关闭 SaaS：

```text
appsettings 当前服务业务库连接串
 -> 当前服务业务数据库
```

启用 SaaS：

```text
入口上下文
 -> TenantId
 -> SaaS 主库
 -> 当前租户的组织机构服务业务库连接串
 -> 当前租户业务数据库
```

业务代码不感知连接串来源。

### 收费业务服务

该服务按住宅、商业小区等业务规则分片。

关闭 SaaS：

```text
appsettings 当前服务主数据库连接串
 -> 当前服务主数据库
 -> 分片目录
 -> 业务分片键
 -> 业务分片库
```

启用 SaaS：

```text
入口上下文
 -> TenantId
 -> SaaS 主库
 -> 当前租户的收费服务数据源目录
 -> 业务分片键
 -> 业务分片库
```

分片能力与业务规则深度绑定。业务规划时确定分片策略，业务请求必须显式携带分片键。

## 分片

`Tw.Sharding` 提供业务显式分片能力，不做全局透明分片。

业务进入分片库的方式：

```csharp
using var scope = _shardContext.Use("Community", communityId);
await _billingRepository.CreateAsync(entity, cancellationToken);
```

或通过 CQRS 契约：

```csharp
public sealed record CreateBillCommand(long CommunityId, decimal Amount) : ICommand, IShardedRequest;
```

规则：

- 未启用分片时，当前作用域只有一个业务连接对象
- 启用分片时，业务必须提供分片策略和分片键
- UoW 开启后禁止切换分片连接对象
- 跨分片读必须通过显式 Fan-out Read API
- 跨分片写使用 CAP 事件和补偿，不能伪装成单个本地事务

## SqlSugar 与连接解析

`Tw.Data.SqlSugar` 是 SqlSugar 的纯适配层。业务代码通过仓储、UoW 或受控连接访问器使用数据库。

连接解析顺序：

```text
ICurrentTenant
 -> IServiceDataSourceResolver
 -> IShardContext
 -> IConnectionConfigResolver
 -> ISqlSugarClient
```

连接串来源：

- SaaS 关闭且分片关闭：来自配置文件
- SaaS 开启且分片关闭：来自 SaaS 主库
- SaaS 关闭且分片开启：来自当前服务主库分片目录
- SaaS 开启且分片开启：来自 SaaS 主库中的租户服务数据源目录

连接串敏感值不写入仓库。生产环境使用 Secret、环境变量、密钥管理服务或受控配置中心注入。

## CAP 事件总线与最终一致性

事件总线只采用 CAP。框架包为：

- `Tw.EventBus.Abstractions`
- `Tw.EventBus.Cap`

CAP 数据库存储由框架自定义 SqlSugar 存储适配。存储适配只处理 CAP 原始实体、表结构和 SqlSugar 事务行为，不处理队列、不处理数据同步、不改变 CAP 原有实体语义。

CAP 数据库规则：

- CAP 数据库单独配置为静态逻辑连接
- CAP 数据不按租户拆分
- CAP 数据不按分片拆分
- 每个 SaaS 子库、分片子库、业务主库所在数据库服务器都存在对应 CAP 数据库
- CAP 数据库主主同步属于基础设施职责
- CAP 存储适配不感知主主同步

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

- 清理成功且超过保留期的 Published、Received 记录
- 清理前按状态、时间、重试次数分批
- 清理任务可由 `Tw.BackgroundJobs` 调度中心控制
- 清理过程记录审计、指标和失败告警
- 清理不删除未完成、失败待处理和死信待处理记录

## 雪花 ID

`Tw.Snowflake` 使用 `Yitter.IdGenerator` 作为默认依赖项。

规则：

- C# 实体、DTO、Command、Query 中 ID 正常使用 `long`
- HTTP JSON 通过全局 Newtonsoft converter 输出为字符串
- 数据库存储使用 `bigint`
- WorkerId 来自配置、环境变量、数据库或部署平台分配
- WorkerId 禁止随机生成
- WorkerId 冲突启动失败
- 时钟回拨超过配置窗口时拒绝发号并暴露健康异常
- ID 不编码租户、分片、用户和权限信息

公开抽象：

```csharp
public interface IIdGenerator
{
    long NewId();
}
```

字符串输出由序列化层负责，不在业务代码调用 `ToString()` 形成约定。

## ASP.NET Core、Swagger、API Versioning 与响应

Swagger 封装参考 Furion 的使用体验，能力放入 `Tw.AspNetCore.Swagger` 命名空间，不单独拆包。

默认能力：

- Swashbuckle 注册
- Newtonsoft 支持
- JWT Bearer 安全定义
- XML 注释加载
- 枚举、错误码、统一响应描述
- 分组与版本文档
- Operation Filter、Schema Filter 扩展点

API Versioning 使用 URL Segment：

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

- 文件
- Stream
- SSE
- WebSocket
- Swagger/OpenAPI
- Health
- Metrics
- 原始回调
- gRPC

## Newtonsoft.Json

`Tw.Core` 提供 `IJsonSerializer`，默认实现为 `NewtonsoftJsonSerializer`。

默认设置：

- camelCase
- 包含 null
- `ReferenceLoopHandling.Error`
- `TypeNameHandling.None`
- ISO 日期
- decimal 精度保护
- enum 字符串
- long ID HTTP 输出 converter

`Tw.AspNetCore` 使用 `Microsoft.AspNetCore.Mvc.NewtonsoftJson`。`Tw.EventBus.Cap` 使用 Newtonsoft serializer 扩展。

框架代码禁止直接使用 `System.Text.Json` 作为业务序列化入口。

## 本地化

`Tw.Localization` 基于 Microsoft `IStringLocalizer`，资源文件使用 JSON，不使用 `.resx`。

默认资源路径：

```text
Localization/zh-CN.json
Localization/en-US.json
```

默认文化为 `zh-CN`。

文化来源顺序：

- HTTP：`X-Culture`、`Accept-Language`、租户默认文化、服务默认文化
- gRPC：Metadata `culture`
- CAP：Header `culture`
- Job：任务上下文 `culture`

资源 Key 使用扁平结构。资源解析使用 Newtonsoft.Json。

## 验证与输入边界

不创建 `Tw.Validation` 包。验证基础模型放入 `Tw.Core`：

- `ValidationException`
- `ValidationError`
- Binding Error Model
- 字段路径
- 错误码

业务验证由 `Tw.Cqrs` 执行 FluentValidation。协议绑定错误由协议包处理。字段名使用 JSON 契约字段名。

CAP Consumer 和后台任务验证失败属于不可重试错误。

## 脱敏与写回保护

脱敏能力放入 `Tw.Core.DataMasking`。

公开能力：

- `IDataMasker`
- `IDataMaskingRule`
- `IDataMaskingPolicyProvider`
- `ISensitiveValueDetector`
- `SensitiveDataAttribute`
- `SensitiveDataKind`

脱敏只用于输出、日志、审计、导出和错误响应。脱敏结果不得进入持久化写入。

写回保护规则：

- 前端传回手机号、身份证、邮箱等字段时，框架检测掩码格式
- 检测到掩码值写入敏感字段时拒绝请求
- 拒绝结果返回稳定验证错误码
- 原始敏感值更新必须通过明确输入模型和权限校验

## 认证、授权、Permission、Feature 与 Setting

`Tw.Core` 提供当前用户、Principal 访问器、执行上下文和身份承载模型。

`Tw.Authorization` 提供：

- Permission 定义
- Permission Checker
- Grant Store
- Permission Cache
- 用户、角色、租户、资源授权检查

`Tw.ApplicationConfiguration` 提供：

- Setting 定义、读取、缓存、刷新
- Feature 定义、读取、缓存、刷新
- 租户级、服务级、用户级作用域

`Tw.Identity.OpenIddict` 提供身份中心实现。业务服务验证 JWT 不强依赖身份中心实现。

认证授权边界：

- 网关只做 JWT 验证和粗粒度路由策略
- 服务仍然验证 JWT、Permission、资源所有权和租户权限
- 内部服务调用不天然可信
- Gateway 转发原始 `Authorization`
- Gateway 清除调用方伪造的身份 Header

## 缓存

`Tw.Caching` 默认使用 FusionCache，运行时 Redis 协议使用 Valkey 或 Redis，客户端为 StackExchange.Redis。

缓存支持：

- L1 Memory
- L2 Redis/Valkey
- Backplane
- Fail-safe
- Stampede protection
- Tag invalidation
- 空值缓存
- 随机 TTL
- 指标

缓存键格式：

```text
{system}:{service}:{tenantId}:{shardStrategy}:{shardKey}:{resource}:{id}
```

非 SaaS 使用 `tenantId = default`。无分片使用 `shardStrategy = none`、`shardKey = default`。

写操作完成后通过 CAP 发布缓存失效事件。缓存失效在 UoW 提交后执行。

## 分布式锁

`Tw.DistributedLock` 默认使用 `DistributedLock.Redis`。

规则：

- 锁键包含租户和分片维度
- 锁必须设置等待超时和租约
- 禁止无限等待
- 加锁失败映射为并发冲突或业务拒绝
- 锁释放失败记录告警和审计

## 幂等

`Tw.Idempotency` 独立包。SqlSugar 持久化实现由 `Tw.Data.SqlSugar` 提供。

幂等覆盖：

- HTTP 写请求
- gRPC 写请求
- CAP 消费
- 后台任务命令

幂等键与租户、资源、操作类型和业务唯一性绑定。重复请求返回首次结果或冲突响应。冲突响应必须包含稳定错误码。

## 韧性

`Tw.Resilience` 使用 Polly 8 和 `Microsoft.Extensions.Http.Resilience`。

策略：

- Timeout
- Retry
- Circuit Breaker
- Rate Limiter
- Bulkhead/Concurrency Limiter
- Fallback

非幂等写操作、输入错误、权限错误和契约错误禁止自动重试。所有网络、数据库、缓存、消息和文件调用必须声明超时或 Deadline。

## HTTP Client

`Tw.Http.Client` 不合并到 `Tw.AspNetCore`。

能力：

- HttpClientFactory
- Microsoft.Extensions.ServiceDiscovery
- Microsoft.Extensions.Http.Resilience
- Polly
- Newtonsoft 序列化
- NSwag.MSBuild 生成客户端
- 关联标识传播

Header allowlist：

- `Authorization`
- `traceparent`
- `tracestate`
- `X-Correlation-Id`
- `X-Tenant-Id`
- `X-Culture`
- `Idempotency-Key`

## gRPC

`Tw.Grpc` 使用官方 gRPC 包，采用 contract-first `.proto`。

规则：

- Proto 字段编号长期稳定
- 删除字段保留编号和名称占位
- JSON 字段 camelCase，Proto 字段 snake_case
- 客户端必须设置 Deadline
- 元数据传播 Trace、Correlation、Tenant、Culture、Authorization
- 错误映射到稳定业务错误码

## 后台任务调度中心

`Tw.BackgroundJobs` 使用 Quartz.NET。

能力：

- 统一后台任务调度中心
- 调度接口
- 创建、暂停、恢复、触发、停止任务
- Cron 校验
- 集群调度
- 静态 Scheduler 数据库
- Job 执行管道
- Job 审计、日志、Trace、Metric

后台任务进入业务逻辑时调用 `ISender.Send(...)`。

## 观测与审计

`Tw.Observability` 使用 Serilog 和 OpenTelemetry。

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

`Tw.Configuration.Nacos` 使用 Nacos 2.5.x 与 `nacos-sdk-csharp 1.3.10`。该包同时提供非 Kubernetes 场景的 Nacos 服务发现桥接。

配置格式只支持 JSON。

配置来源顺序：

```text
framework defaults
 -> appsettings.json
 -> appsettings.{Environment}.json
 -> Nacos shared
 -> Nacos service
 -> Nacos role
 -> User Secrets
 -> environment variables
 -> command line
```

允许动态刷新：

- 日志级别
- 缓存 TTL 和策略
- 限流策略
- HTTP 韧性策略
- Gateway 路由和灰度权重
- Feature
- Setting

禁止热更新：

- 数据库 bootstrap 连接
- CAP 数据库
- Broker Endpoint
- Snowflake WorkerId
- JWT Key
- 加密 Key
- 租户和分片拓扑

动态配置变更先校验，校验失败保留上一份有效配置并记录审计和告警。

密钥来自 User Secrets、Aspire、Kubernetes Secret、环境变量或企业密钥系统，不创建 `Tw.Secrets`。

## Gateway

`Tw.Gateway` 使用 YARP。

职责：

- 路由
- Path Rewrite
- Header 治理
- 负载均衡
- 健康探测
- 灰度权重
- JWT 校验
- CORS
- 请求大小限制
- 限流
- Timeout
- 基础 Circuit Breaker
- WebSocket、SSE、gRPC 透传
- Gateway 自身错误统一响应

禁止：

- 业务编排
- 聚合查询
- 数据库访问
- UoW
- CAP
- CQRS
- 租户数据库切换
- 分片业务决策
- OpenAPI 聚合
- 把 JWT 转换为身份、角色、权限 Header

Kubernetes 场景：

```text
Ingress / LB
 -> Tw.Gateway
 -> Kubernetes DNS service discovery
 -> backend services
```

非 Kubernetes 场景：

```text
Load Balancer
 -> Tw.Gateway
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
- `Tw.Http.Client`
- `Tw.Resilience`
- `Tw.Caching`
- `Tw.DistributedLock`
- `Tw.Idempotency`

## 测试

`Tw.Testing` 是 test-only 包，不进入生产产物。

能力：

- 测试时钟
- 测试 ID
- 测试当前用户、租户、文化和关联上下文
- `WebApplicationFactory` 封装
- 测试认证
- Aspire AppHost 测试
- Testcontainers 容器编排
- 数据库夹具
- CAP、Redis、RabbitMQ、Nacos 测试支持
- OpenAPI、Proto、CAP 事件、错误码契约验证
- 日志、审计、Trace、Metric 测试采集器
- 脱敏输出和写回保护断言

框架自有包质量门禁：

- 行覆盖率不低于 98%
- 核心逻辑分支覆盖率不低于 90%
- UoW、多租户、分片、CAP、幂等、授权、脱敏、网关、配置、执行管道必须覆盖失败路径

业务服务模板默认门禁：

- 行覆盖率不低于 80%
- 收费、权限、资金、数据一致性相关模块行覆盖率不低于 90%
- 契约测试覆盖 HTTP、gRPC、CAP 事件和错误码兼容性

## 工具链

### Tw.Templates

`Tw.Templates` 发布为 NuGet 模板包，使用官方 `dotnet new` 模板体系。

模板：

- `tw-workspace`
- `tw-service`
- `tw-building-block`
- `tw-gateway`
- `tw-contracts`

模板支持自定义服务名、项目名前缀和 RootNamespace。层后缀固定。

### Tw.Cli

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

### Tw.Analyzers

`Tw.Analyzers` 作为 Roslyn Analyzer NuGet 包安装到模板项目中，`PrivateAssets=all`。

内置规则：

- 禁止引用 `MassTransit`
- 禁止生产项目引用 `Tw.Testing`
- 禁止出现 `Tw.Infrastructure`、`Tw.Context`、`Tw.ExecutionPipeline`
- 禁止业务层直接依赖 SqlSugar、CAP 实现包、ASP.NET Core、Quartz、Gateway 包
- 禁止 `Application`、`Domain`、`Contracts` 使用 SqlSugar `ChangeDatabase`
- 禁止服务注册扩展放入 `Microsoft.Extensions.DependencyInjection` 命名空间
- 禁止扩展方法使用 `AddTwXxx` 形式
- 禁止 `.Result`、`.Wait()` 阻塞异步
- 禁止框架代码直接使用 `System.Text.Json`
- 禁止业务验证依赖 `DataAnnotations`
- 检查敏感字段脱敏策略
- 检查共享包 `package-charter.yaml`
- 检查测试项目命名和生产项目引用边界

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
| Snowflake | `Yitter.IdGenerator` | `1.0.15` |
| Mapping | `Riok.Mapperly` | `4.3.1` |
| JSON | `Newtonsoft.Json` | `13.0.4` |
| ASP.NET JSON | `Microsoft.AspNetCore.Mvc.NewtonsoftJson` | `10.0.9` |
| Swagger | `Swashbuckle.AspNetCore` | `10.2.3` |
| Swagger | `Swashbuckle.AspNetCore.Newtonsoft` | `10.2.3` |
| API Versioning | `Asp.Versioning.Mvc` | `10.0.0` |
| API Versioning | `Asp.Versioning.Mvc.ApiExplorer` | `10.0.0` |
| Cache | `ZiggyCreatures.FusionCache` | `2.6.0` |
| Redis | `StackExchange.Redis` | `3.0.11` |
| Lock | `DistributedLock.Redis` | `1.1.1` |
| Resilience | `Polly` | `8.7.0` |
| Resilience | `Microsoft.Extensions.Http.Resilience` | `10.7.0` |
| Service Discovery | `Microsoft.Extensions.ServiceDiscovery` | `10.7.0` |
| Gateway Discovery | `Microsoft.Extensions.ServiceDiscovery.Yarp` | `10.7.0` |
| Gateway | `Yarp.ReverseProxy` | `2.3.0` |
| CQRS | `MediatR` | `12.5.0` |
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

`OpenTelemetry.Instrumentation.GrpcNetClient` 当前只有 beta 版本，不作为默认依赖。

## 事实来源

- NuGet V3 API：`https://api.nuget.org/v3/index.json`
- `DotNetCore.CAP`：`https://www.nuget.org/packages/DotNetCore.CAP/`
- `SqlSugarCore`：`https://www.nuget.org/packages/SqlSugarCore/`
- `Yitter.IdGenerator`：`https://api.nuget.org/v3-flatcontainer/yitter.idgenerator/index.json`
- `Riok.Mapperly`：`https://www.nuget.org/packages/Riok.Mapperly/`
- `Newtonsoft.Json`：`https://www.nuget.org/packages/Newtonsoft.Json/`
- `Yarp.ReverseProxy`：`https://www.nuget.org/packages/Yarp.ReverseProxy/`
- `ZiggyCreatures.FusionCache`：`https://www.nuget.org/packages/ZiggyCreatures.FusionCache/`
- `Testcontainers`：`https://www.nuget.org/packages/Testcontainers/`
- Microsoft 自定义模板文档：`https://learn.microsoft.com/en-us/dotnet/core/tools/custom-templates`

## 验证清单

- 所有运行时包包含 `package-charter.yaml`
- 所有公开 API 具备 XML 文档注释
- 所有服务注册扩展按功能命名，不放入 `Microsoft.Extensions.DependencyInjection`
- 所有默认依赖进入集中版本管理
- 所有密钥、连接串和 Token 不进入仓库
- `Tw.Analyzers` 阻断禁止引用和架构越界
- `Tw.Testing` 不进入生产项目
- 框架自有包行覆盖率不低于 98%
- 核心逻辑分支覆盖率不低于 90%
- CAP Outbox 与业务写入同事务行为验证通过
- SaaS、非 SaaS、分片、非分片四种连接解析组合验证通过
- HTTP、gRPC、CAP Consumer、后台任务共享同一应用层执行行为
- 日志、审计、错误响应和导出不泄露敏感信息

## 实施输入

本文是 `Tw.*` 微服务底层框架的最终设计输入。实施计划进入 `docs/superpowers/plans`，并按共享包边界、依赖顺序、测试门禁、工具治理和文档同步要求拆分。
