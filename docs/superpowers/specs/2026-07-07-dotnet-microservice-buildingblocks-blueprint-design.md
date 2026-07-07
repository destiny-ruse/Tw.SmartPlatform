# .NET 微服务 BuildingBlocks 最终设计蓝图

日期：2026-07-07
文档类型：Explanation + Reference
目标读者：负责实现和评审 .NET 微服务 BuildingBlocks 的后端开发人员、Tech Lead 和架构师
状态：已确认设计

## 目标

本设计定义 `Tw.SmartPlatform` .NET 企业级微服务 BuildingBlocks 的最终方向。框架以可组合共享包提供微服务开发底座，覆盖 CQRS、缓存、工作单元、事件总线、最终一致性、认证授权、OpenAPI、日志、gRPC、AOP、SqlSugar、自动依赖注入、链路追踪、多租户、业务分片、本地 Aspire 开发和 Kubernetes 运行。

框架不提供单体大包。每个能力以独立共享包声明职责、依赖边界和公开能力，服务按业务画像组合使用。业务服务通过配置和显式业务契约启用多租户、分片、缓存、事件一致性和观测能力。

## 核心决策

- 采用可组合 BuildingBlocks，不采用单体框架。
- 包命名保留 `MultiTenancy`，不使用 `SaaS` 作为包名。SaaS 是多租户的一种运行模式。
- `Tw.Context` 与 `Tw.Core` 合并。上下文抽象归属 `Tw.Core`，可以保留 `Tw.Context` 命名空间以表达语义。
- 本地开发基准采用 .NET Aspire，生产运行基准采用 Kubernetes。二者共享 `Tw.ServiceDefaults`。
- API 入口支持网关、Controller、Minimal API 和 gRPC；网关能力基于 YARP 封装。
- 数据访问主栈采用 SqlSugar。分片是业务规划后的显式能力，不作为全局透明能力。
- 最终一致性主栈采用 CAP + Outbox/Inbox + 幂等 + 补偿。
- 缓存主栈采用 FusionCache + Redis，多级缓存、分布式锁、缓存失效事件和防击穿由框架统一封装。
- 认证授权采用自建统一身份服务 + 外部身份源接入；OIDC/OAuth2 底层采用 OpenIddict。
- 日志采用 Serilog，追踪和指标采用 OpenTelemetry。
- 韧性策略采用 Polly，统一管理超时、重试、熔断、限流、隔离和降级。

## 包结构

| 包 | 职责 |
| --- | --- |
| `Tw.Core` | 基础类型、结果模型、错误模型、当前用户、当前租户、关联标识、客户端上下文、时钟、ID 生成、审计抽象 |
| `Tw.DependencyInjection` | 自动依赖注入、Options 绑定、AOP 扫描和注册规划 |
| `Tw.Application` | CQRS 抽象、Command/Query Dispatcher、Pipeline Behavior、应用层幂等、验证入口 |
| `Tw.Application.FluentValidation` | FluentValidation 适配 |
| `Tw.AspNetCore` | HTTP 上下文桥接、统一异常、健康检查、ProblemDetails、请求关联、主机集成 |
| `Tw.AspNetCore.Mvc` | Controller、模型绑定、过滤器、OpenAPI 元数据适配 |
| `Tw.AspNetCore.Grpc` | gRPC 上下文桥接、错误映射、追踪传播 |
| `Tw.Gateway.Yarp` | 网关路由、认证转发、租户头治理、关联标识传播 |
| `Tw.ServiceDefaults` | Aspire 与 Kubernetes 共用的服务默认配置、健康检查、观测、服务发现、韧性基线 |
| `Tw.MultiTenancy` | 租户解析抽象、租户上下文、租户数据源目录、租户隔离策略 |
| `Tw.AspNetCore.MultiTenancy` | HTTP/gRPC 租户解析中间件和过滤器 |
| `Tw.Data` | Unit of Work、仓储抽象、数据源描述、连接解析抽象 |
| `Tw.Data.SqlSugar` | SqlSugar 客户端工厂、UoW、仓储基类、审计写入、软删除、查询过滤 |
| `Tw.Data.SqlSugar.MultiTenancy` | 多租户模式下的 SqlSugar 连接解析和租户数据源选择 |
| `Tw.Data.Sharding` | 分片键、分片上下文、分片规则、分片路由、跨分片访问边界 |
| `Tw.Data.SqlSugar.Sharding` | SqlSugar 分片连接切换、分片仓储、分片 UoW 约束 |
| `Tw.Caching` | 缓存抽象、缓存键规范、TTL、标签、空值缓存、缓存策略 |
| `Tw.Caching.FusionCache` | FusionCache 多级缓存实现 |
| `Tw.Caching.Redis` | Redis 二级缓存、Backplane、连接健康检查 |
| `Tw.Caching.HybridCache` | Microsoft HybridCache 兼容适配 |
| `Tw.DistributedLock` | 分布式锁、读写锁、信号量抽象和 Redis/数据库后端适配 |
| `Tw.EventBus` | 集成事件、领域事件出站、订阅、消息契约、幂等消费抽象 |
| `Tw.EventBus.Cap` | CAP Outbox/Inbox、发布订阅、消费重试、死信和补偿入口 |
| `Tw.Data.SqlSugar.Cap` | SqlSugar 本地事务与 CAP 事务边界绑定 |
| `Tw.DistributedTransaction.Dtm` | Saga、TCC、XA、Workflow 复杂事务扩展能力 |
| `Tw.Auth` | 认证授权抽象、权限模型、资源授权、租户授权检查 |
| `Tw.Auth.OpenIddict` | OpenIddict 身份服务和 Token 验证适配 |
| `Tw.OpenApi` | OpenAPI 文档、Swagger UI、认证方案、错误响应描述 |
| `Tw.Logging.Serilog` | Serilog 结构化日志、脱敏、日志上下文字段 |
| `Tw.Observability.OpenTelemetry` | Trace、Metrics、Baggage、Exporter 和采样策略 |
| `Tw.Resilience` | Polly 策略、超时、重试、熔断、限流、隔离、降级 |
| `Tw.Scheduling.Quartz` | 定时任务、持久化任务、集群调度和任务追踪 |
| `Tw.Localization` | 本地化核心能力 |
| `Tw.Localization.AspNetCore` | ASP.NET Core 本地化集成 |

## 多租户与 SaaS 语义

`Tw.MultiTenancy` 负责租户识别、租户上下文、租户数据隔离、租户级数据源目录和租户授权边界。SaaS 模式通过 `MultiTenancy.Enabled = true` 启用，但包名仍为 `MultiTenancy`。

租户来源按入口类型适配：

- HTTP：从域名、路径、可信 Header、Token Claim 或显式租户切换参数解析。
- gRPC：从 Metadata、Token Claim 或调用上下文解析。
- 消息消费：从消息 Header 或事件元数据解析。
- 后台任务：从任务参数、任务上下文或显式租户执行范围解析。

数据访问层不得直接依赖 `HttpContext`。HTTP 中间件只负责解析并写入 `ICurrentTenant`，SqlSugar 连接解析统一依赖 `ICurrentTenant`、`IServiceDataSourceResolver` 和 `IShardContext`。

## 连接串来源规则

连接串来源由多租户开关和分片开关共同决定。

### 单库服务，不启用多租户，不启用分片

组织机构服务在非 SaaS 模式下采用该规则：

```text
appsettings 当前服务业务库连接串
 -> SqlSugarClient
```

服务只需要配置当前服务业务数据库。业务代码注入默认 UoW、仓储或当前 SqlSugar 客户端访问业务库。

### 单库服务，启用多租户，不启用分片

组织机构服务在 SaaS 模式下采用该规则：

```text
入口上下文
 -> TenantId
 -> 多租户主库
 -> 当前租户 + 当前服务业务库连接串
 -> SqlSugarClient
```

框架自动解析当前租户对应的当前服务业务库连接对象。业务代码不处理连接串。

### 分片服务，不启用多租户，启用业务分片

收费业务服务在非 SaaS 模式下采用该规则：

```text
appsettings 当前服务主数据库连接串
 -> 分片目录
 -> 业务分片键
 -> 分片规则
 -> 具体业务库或业务分片库连接串
 -> SqlSugarClient
```

当前服务主数据库保存分片目录和分片规则。业务必须显式提供分片键，例如 `CommunityId`、`ProjectId` 或其他业务规划字段。

### 分片服务，启用多租户，启用业务分片

收费业务服务在 SaaS + 分片模式下采用该规则：

```text
入口上下文
 -> TenantId
 -> 多租户主库
 -> 当前租户 + 当前服务数据源目录
 -> 业务分片键
 -> 分片规则
 -> 具体业务库或业务分片库连接串
 -> SqlSugarClient
```

多租户主库保存租户、服务数据源目录和租户可访问的数据边界。分片键仍由业务显式提供。框架负责把 `TenantId + ServiceName + ShardStrategy + ShardKey` 解析为当前 SqlSugar 连接对象。

## 分片规则

分片是业务能力，不是全局透明能力。框架负责连接切换、UoW 约束、缓存键隔离、日志字段、追踪字段和一致性边界。业务负责声明是否启用分片、使用哪一种分片策略、每次操作使用哪个分片键。

业务显式分片方式包括两类：

```csharp
using var shard = _shardContext.Use("Community", communityId);
await _feeRepository.CreateAsync(entity);
```

```csharp
public sealed record CreateFeeBillCommand(string CommunityId, string CustomerId, decimal Amount)
    : ICommand, IShardedRequest;
```

CQRS Pipeline 在发现 `IShardedRequest` 后设置分片上下文。该自动化只负责连接切换，不推断业务分片键。

## SqlSugar 连接对象选择

框架统一提供 `ITwSqlSugarClientAccessor`、`ITwSqlSugarClientFactory` 和 UoW 集成。默认解析顺序固定为：

```text
ICurrentTenant
 -> IServiceDataSourceResolver
 -> IShardContext
 -> IConnectionConfigResolver
 -> ISqlSugarClient
```

动态切换连接对象遵守以下规则：

- 未启用分片时，当前作用域只有一个业务连接对象。
- 启用分片时，业务通过 `IShardContext` 或 `IShardedRequest` 切换当前连接对象。
- 一个写事务只能绑定一个物理业务库或一个业务分片库。
- UoW 开启后切换分片连接对象属于非法操作，框架抛出明确异常。
- 跨分片读操作通过显式 Fan-out Read API 表达，并标记只读。
- 跨分片写操作通过 CAP 集成事件、补偿或 DTM 扩展能力表达，不通过单个本地事务伪装为强一致事务。

## 数据模型与主库职责

多租户主库负责保存租户、应用、服务数据源目录、租户服务开关、租户授权边界和数据源密钥引用。业务主库负责保存非 SaaS 模式下当前服务的数据源目录和分片规则。业务分片库保存实际业务数据。

多租户主库核心概念：

- `tenant`：租户主体。
- `tenant_application`：租户启用的应用。
- `tenant_service_data_source`：租户在某服务下的数据源目录。
- `tenant_service_feature`：租户级服务开关。
- `tenant_user_binding`：用户与租户的授权关系。
- `connection_secret_ref`：连接串密钥引用。

业务分片目录核心概念：

- `data_source`：业务库或分片库描述。
- `shard_strategy`：分片策略，例如 `Community`。
- `shard_mapping`：业务分片键范围、哈希或枚举值到数据源的映射。
- `shard_migration`：分片迁移记录、校验状态和切换记录。

连接串不得明文提交到仓库。生产连接串通过 Secret、密钥管理服务、部署平台安全变量或受控配置中心注入。主库中保存密文或 `secret_ref`。

## CQRS 与应用层

`Tw.Application` 提供 Command、Query、Handler、Dispatcher 和 Pipeline Behavior。Pipeline 顺序固定为：

```text
Correlation
 -> CurrentUser
 -> CurrentTenant
 -> Authorization
 -> Validation
 -> Idempotency
 -> Sharding
 -> UnitOfWork
 -> EventCollection
 -> Handler
 -> Outbox
 -> Observability
```

Command 默认代表写操作。Query 默认代表读操作。写操作需要声明幂等键策略，关键写操作必须具备重复提交保护。分片写操作通过 `IShardedRequest` 或显式 `IShardContext` 声明分片键。

## 工作单元与事务

`Tw.Data` 定义 UoW 抽象，`Tw.Data.SqlSugar` 提供 SqlSugar 实现。事务边界遵守以下规则：

- 单库写操作使用 SqlSugar 本地事务。
- 单租户单分片写操作使用 SqlSugar 本地事务。
- CAP 发布的集成事件写入 Outbox，与业务数据写入同一事务边界。
- 消费端使用 Inbox、幂等键和去重表处理重复消息。
- 跨服务写一致性使用 CAP 最终一致性。
- 跨分片写一致性使用 CAP 编排或 DTM 扩展能力。
- 读操作不默认开启事务。

## 事件总线与最终一致性

默认事件底座为 CAP。框架提供 `Tw.EventBus` 抽象和 `Tw.EventBus.Cap` 实现。

事件发布流程：

```text
业务写入
 -> SqlSugar UoW
 -> 业务表写入
 -> CAP Outbox 写入
 -> 本地事务提交
 -> CAP 投递消息
 -> 消费端 Inbox 幂等处理
 -> 缓存失效或业务补偿
```

CAP 与 SqlSugar 的绑定由 `Tw.Data.SqlSugar.Cap` 负责。CAP 官方提供分布式事务和 EventBus 能力。SqlSugar 与 CAP 的事务绑定通过框架适配层完成，事实依据为 SqlSugar issue #1207 中给出的第三方 ORM 扩展方式。来源：https://github.com/DotNetNext/SqlSugar/issues/1207

MassTransit 不作为默认消息底座。MassTransit v9 已进入商业授权模式，官方授权文档说明 license key 需要在运行 MassTransit 代码的机器或容器中可访问，NuGet 包页也声明 MassTransit 是需要授权的商业产品。默认底座选择 CAP 能保持开源可商用和成本边界清晰。需要 MassTransit 的项目通过独立适配包接入，并由项目自行完成商业授权确认。来源：https://masstransit.massient.com/configuration/license 与 https://www.nuget.org/packages/MassTransit/

## 缓存、锁与一致性

缓存默认采用 FusionCache。Redis 客户端采用 StackExchange.Redis。分布式锁采用 DistributedLock / Medallion.Threading。

缓存结构：

```text
L1 Memory
 -> L2 Redis
 -> Redis Backplane
 -> Tag/Key Invalidation
 -> OpenTelemetry Metrics
```

缓存键必须包含稳定命名空间：

```text
{system}:{service}:{tenantId}:{shardStrategy}:{shardKey}:{resource}:{id}
```

无分片服务使用固定 `shardStrategy = none`、`shardKey = default`。非 SaaS 模式使用固定 `tenantId = default`。

缓存一致性流程：

```text
业务写入
 -> UoW 提交
 -> CAP 发布缓存失效事件
 -> 消费端按 tag/key/version 失效缓存
 -> 读取端通过 FusionCache 回源
 -> 分布式锁或 FusionCache 防击穿保护
```

分布式锁键必须包含租户和分片维度。锁必须设置超时、租约和失败处理。不得使用无限等待。

## 认证授权

身份服务采用 OpenIddict。框架提供自建统一身份服务，同时支持外部身份源接入。认证确认主体，授权确认主体能否在当前租户、组织、资源和操作范围内执行动作。

租户解析不得直接信任客户端提交的租户标识。Token Claim、Header、域名或路径解析出的 `TenantId` 必须经过服务端校验：

- 租户存在且启用。
- 当前用户或客户端属于该租户。
- 当前服务对该租户开放。
- 当前操作满足角色、权限、资源所有权和数据边界。

服务间调用必须传播主体、租户、关联标识和授权范围。内部服务调用不得被视为天然可信。

## API、gRPC 与网关

API 支持 Controller、Minimal API 和 gRPC。网关优先采用 YARP 封装。OpenAPI 文档由 `Tw.OpenApi` 统一注册认证方案、错误响应、租户 Header 描述和版本信息。

Controller 保持协议适配边界，只处理参数绑定、基础校验、认证授权上下文读取和响应转换。业务规则进入 application service、domain service 或 CQRS Handler。

gRPC 通过 Metadata 传播 `trace_id`、`correlation_id`、`tenant_id` 和授权上下文。服务端拦截器负责错误映射、追踪传播和租户上下文写入。

## 日志、追踪与指标

结构化日志采用 Serilog。Trace 和 Metrics 采用 OpenTelemetry。所有服务默认写入以下上下文字段：

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

日志不得输出连接串、Token、密码、证书私钥、完整手机号、完整证件号和原始敏感载荷。分片路由失败、多租户解析失败、UoW 事务失败、CAP 投递失败、缓存回源失败必须有可查询事件名和指标。

## 韧性策略

`Tw.Resilience` 基于 Polly 封装以下策略：

- Timeout：所有数据库、缓存、消息、HTTP、gRPC 和文件存储调用必须有超时。
- Retry：只对明确可重试且满足幂等前提的操作启用。
- Circuit Breaker：关键下游依赖启用熔断。
- Rate Limiter：入口、昂贵操作和下游依赖调用启用限流或并发隔离。
- Fallback：降级路径必须声明数据新鲜度、功能边界和恢复条件。

非幂等写操作、输入错误、权限错误和契约错误不得自动重试。

## 本地开发与运行

本地开发采用 .NET Aspire。`Tw.ServiceDefaults` 统一注册服务发现、健康检查、OpenTelemetry、Serilog、Resilience 和基础配置校验。Aspire AppHost 负责本地编排数据库、Redis、消息中间件、身份服务、网关和示例业务服务。

生产运行采用 Kubernetes。Kubernetes 资源命名、标签、Secret、ConfigMap、健康检查和镜像追溯遵守工程规范。Aspire 不作为生产调度器。

## 功能开关

框架使用显式服务画像配置能力开关：

```yaml
Tw:
  Service:
    Name: FeeService
  MultiTenancy:
    Enabled: true
  Data:
    SqlSugar:
      DefaultConnectionName: business
    Sharding:
      Enabled: true
      Strategies:
        - Name: Community
          Required: true
  EventBus:
    Provider: CAP
  Caching:
    Provider: FusionCache
    RedisBackplane: true
```

开关语义：

- `MultiTenancy.Enabled = false`：租户固定为 `default`，连接串来自当前服务配置或当前服务主库。
- `MultiTenancy.Enabled = true`：租户来自当前上下文，连接串来自多租户主库的数据源目录。
- `Sharding.Enabled = false`：当前服务不执行分片路由。
- `Sharding.Enabled = true`：业务必须提供分片策略和分片键。
- `Strategies[].Required = true`：进入对应业务能力时缺少分片键直接失败。

## 典型服务画像

### 组织机构服务

组织机构服务不启用分片。

非 SaaS 模式：

```text
MultiTenancy.Enabled = false
Sharding.Enabled = false
appsettings -> 组织机构业务库
```

SaaS 模式：

```text
MultiTenancy.Enabled = true
Sharding.Enabled = false
TenantId -> 多租户主库 -> 租户组织机构业务库
```

业务代码无连接串分支。

### 收费业务服务

收费业务服务按小区分片。

非 SaaS 模式：

```text
MultiTenancy.Enabled = false
Sharding.Enabled = true
appsettings -> 收费服务主库 -> CommunityId -> 分片库
```

SaaS 模式：

```text
MultiTenancy.Enabled = true
Sharding.Enabled = true
TenantId -> 多租户主库 -> 租户收费服务数据源目录 -> CommunityId -> 分片库
```

业务契约必须携带 `CommunityId` 或等价分片键。

## 开源框架事实依据

| 能力 | 默认底座 | 许可证与事实依据 |
| --- | --- | --- |
| ORM、多库、多租户、分表 | SqlSugar | GitHub 显示 MIT license，并说明支持 SaaS、租户分库、租户分表、多数据库和 UnitOfWork。来源：https://github.com/DotNetNext/SqlSugar |
| EventBus、Outbox、最终一致性 | CAP | 官方站点说明 CAP 是 EventBus + Outbox 的分布式事务方案，并采用 MIT license。来源：https://cap.dotnetcore.xyz/ |
| 多级缓存 | FusionCache | GitHub 说明支持 L1/L2、Backplane、stampede protection、fail-safe、OpenTelemetry 和 tagging，采用 MIT license。来源：https://github.com/ZiggyCreatures/FusionCache |
| Redis 客户端 | StackExchange.Redis | NuGet 说明是高性能 RESP 客户端并采用 MIT license。来源：https://www.nuget.org/packages/StackExchange.Redis/ |
| 分布式锁 | DistributedLock / Medallion.Threading | GitHub 说明支持 distributed mutex、reader-writer lock、semaphore 和多种后端，采用 MIT license。来源：https://github.com/madelson/DistributedLock |
| 复杂分布式事务 | DTM | GitHub 说明支持 Saga、TCC、XA、2-phase message、Outbox、Workflow 和多语言；核心项目为 BSD-3-Clause。来源：https://github.com/dtm-labs/dtm |
| 认证授权 | OpenIddict | 官方站点说明 Apache 2.0，开源且可商用，支持 OIDC/OAuth2。来源：https://openiddict.com/ |
| 韧性策略 | Polly | 官方文档说明支持 Retry、Circuit Breaker、Hedging、Timeout、Rate Limiter、Fallback，属于 .NET Foundation。来源：https://www.pollydocs.org/ |
| 观测 | OpenTelemetry | 官方站点说明是云原生开放观测框架，提供 traces、metrics、logs 的 API、库、agent 和 collector。来源：https://opentelemetry.io/ |
| 结构化日志 | Serilog | 官方站点说明支持结构化事件日志并适用于 .NET。来源：https://serilog.net/ |
| 网关 | YARP | Microsoft 文档说明 YARP 是可定制、高性能的 .NET 反向代理库。来源：https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/yarp-overview |
| 本地开发编排 | .NET Aspire | 官方站点说明 Aspire 是本地开发期编排和观测工具链，并声明 free and open source。来源：https://aspire.dev/ |
| OpenAPI/Swagger UI | Swashbuckle.AspNetCore + Microsoft.AspNetCore.OpenApi | Swashbuckle GitHub 显示 MIT license；ASP.NET Core 已提供内置 OpenAPI 能力。来源：https://github.com/domaindrivendev/Swashbuckle.AspNetCore |
| 定时任务 | Quartz.NET | 官方站点说明 Quartz.NET 是开源任务调度系统，Apache 2.0 license。来源：https://www.quartz-scheduler.net/ |
| 参数验证 | FluentValidation | GitHub 显示 Apache-2.0 license，并提供强类型验证规则。来源：https://github.com/FluentValidation/FluentValidation |

## 测试策略

测试覆盖以下路径：

- 多租户关闭、分片关闭：配置连接串解析到默认业务库。
- 多租户开启、分片关闭：HTTP/gRPC/消息/后台任务上下文解析到租户业务库。
- 多租户关闭、分片开启：服务主库分片目录解析到业务分片库。
- 多租户开启、分片开启：租户数据源目录 + 业务分片键解析到业务分片库。
- UoW 开启后切换分片连接对象必须失败。
- CAP Outbox 与 SqlSugar 本地事务同提交。
- 消费端 Inbox 幂等处理重复消息。
- 缓存键包含租户和分片维度。
- 分布式锁键包含租户和分片维度。
- 认证授权拒绝非法租户、非法服务、非法资源访问。
- 日志和错误响应不泄露连接串、Token 和敏感载荷。

单元测试覆盖解析器、路由器、Pipeline、缓存键和错误分类。集成测试覆盖 SqlSugar、Redis、CAP、OpenIddict、YARP、Aspire AppHost 和 Kubernetes 配置模板。

## 失败模式

- 无法解析租户：返回认证或授权错误，记录安全事件。
- 租户未启用当前服务：返回授权错误，记录租户服务拒绝事件。
- 缺少必需分片键：返回稳定业务错误，阻止数据库访问。
- 分片规则缺失：返回配置错误，触发运行告警。
- UoW 中切换分片：抛出框架错误，标记为开发契约违规。
- CAP 投递失败：按 CAP 重试和死信机制处理，保留 Outbox 状态。
- 缓存回源失败：按缓存策略返回失败、降级或过期数据，必须记录数据新鲜度。
- 分布式锁获取失败：按业务策略快速失败或返回并发冲突。

## 实施边界

本设计固定最终技术方向、包边界、数据流、失败模式和验证策略。实现计划单独进入 `docs/superpowers/plans`，并按共享包 charter、使用文档、测试和发布治理要求拆分。
