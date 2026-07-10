# backend/dotnet BuildingBlocks 包合并与扩展设计

## 文档定位

- 文档类型：Explanation
- 目标读者：内部架构师、Tech Lead、.NET 公共包维护者和业务服务开发人员
- 目标：确定 `backend/dotnet/BuildingBlocks` 的目标包边界、稳定性承诺、第三方依赖治理、入口管道模型和新能力扩包规则
- 实施边界：本文只定义设计和验收条件，不直接执行项目移动、API 修改、包发布或业务服务迁移

## 背景

`backend/dotnet/BuildingBlocks/src` 当前包含 73 个可打包项目。现有结构同时存在三类问题：

- 同一能力被机械拆成 `Abstractions`、无第三方依赖的薄运行时和 provider 三层，增加项目引用、测试项目、charter、文档和发布维护成本
- 部分包名表达了可替换能力，但实现实际上已经与持久化格式、协议、第三方运行时或公司标准技术栈绑定，容易形成错误的可替换预期
- 部分共享包仍是占位实现，尚未具备生产所需的真实 SDK 调用、失败语义、可观测性、集成测试和运行手册

本仓库尚处于框架采纳前整改阶段。用户已确认现有代码允许破坏性变更，NuGet 包进入稳定阶段后公共契约基本不再破坏。源码中大部分 charter 标记为 `experimental`，但 `Tw.Core` 当前标记为 `stable`。实施破坏性整改前必须同时核查内部 NuGet 源和具体应用引用；若与用户确认的采纳前状态一致，应先修正 `Tw.Core` charter 与实际状态的冲突。

## 目标

- 将 73 个现有项目收敛为 57 个职责清晰的目标项目，其中包含 53 个运行时项目和 4 个测试基础项目
- 以业务能力为 NuGet 选择粒度，不提供默认安装全部能力的总聚合包
- 所有包采用统一版本发布列车、中央依赖版本和整仓验证
- 只在真实依赖隔离成立时保留 `.Abstractions`、`.Contracts` 或 provider 包
- 让业务代码依赖公司拥有的能力契约，第三方类型停留在 provider、宿主或基础设施边界
- 将入口协议横切逻辑放回各入口原生管道，将跨入口业务横切逻辑收敛到应用用例管道
- 为新功能和新第三方库建立可重复执行的单包、抽象包、provider 包决策流程
- 生产项目、测试项目、解决方案文件夹、charter、文档、模板、锁文件和生成工具作为同一个迁移单元同步调整
- 让 `BuildingBlocks/src` 与 `BuildingBlocks/tests` 的物理能力目录和 `.slnx` 解决方案文件夹严格一致，并用架构测试持续约束
- 清理会误导职责或暴露公司品牌前缀的代码标识符；除 `TwException` 外，公开和内部代码标识符不再使用独立的 `Tw` 品牌词段

## 非目标

- 不以项目数量最少为唯一目标
- 不承诺数据库、消息、身份、ID、序列化和调度 provider 可以在已上线系统中无成本替换
- 不为未知的第二实现预建空 provider 包
- 不通过一个通用动态代理管道统一 HTTP、gRPC、CAP、Quartz 和 Worker 的异常、重试或上下文语义
- 不把业务系统的领域模型、领域共享枚举或 DTO 放入全公司通用的 `Tw.Domain.Shared`
- 不在一次结构迁移中把所有尚未完成的 provider 占位实现同时宣称为生产可用；每个 provider 需要独立设计、真实依赖集成验证和发布决策

## 总体决策

### 包与版本模型

采用“能力级 NuGet + 统一版本发布列车”：

- 服务只引用实际使用的能力包和 provider 包
- 所有包使用同一发布版本，来源于同一提交并通过同一整仓质量门禁
- 统一版本不表示所有包可以同步破坏兼容性；兼容性仍按每个 PackageId、公开 API 和外部契约独立判断
- 不新增 `Tw.All`、`Tw.Framework` 或隐式拉入全部 provider 的总聚合包
- provider 包未完成真实集成与生产验证时保留 `experimental`，不进入首个稳定包清单

### 依赖方向

```text
业务服务 / 宿主组合根
├─ Web、gRPC、CAP、Quartz、Worker 原生入口适配
│  └─ Tw.Application 应用用例管道
│     └─ Domain / Application 业务逻辑
│        └─ 公司拥有的能力契约
└─ provider 包
   └─ 第三方 SDK、数据库、消息、缓存和运行时
```

依赖必须满足：

- Domain 不依赖 Web、数据库、消息、调度和 provider 包
- Application 不引用 Autofac、Castle、SqlSugar、CAP、Quartz、YARP 等第三方实现类型
- provider 依赖能力包，能力包不得反向依赖 provider
- 宿主组合根显式选择 provider，不由 `Tw.AspNetCore` 隐式替所有服务选择容器或基础设施
- provider 包可以公开第三方集成入口，但不得让第三方对象进入业务契约

## `.Abstractions` 与 `.Contracts` 保留门槛

单独保留契约包必须同时满足以下条件：

1. 存在真实依赖倒置边界，消费者不引用运行时或 provider 也必须能够编译
2. 契约词汇由公司拥有，不复制第三方 API，不公开第三方配置、异常和结果类型
3. 运行时包包含消费者不应传递依赖的框架、宿主、扫描或 provider 能力，或者契约需要被外部实现者引用
4. 契约具备独立兼容性、owner、文档和契约测试责任
5. 该边界由真实生产模型支持，而不是因为当前项目文件少或未来存在理论上的替换可能

以下边界达到保留门槛：

- `Tw.DependencyInjection.Abstractions`：业务模块使用服务生命周期、暴露和 Options 元数据，宿主才需要扫描、拓扑和注册执行
- `Tw.Application.Contracts`：应用命令、查询和分页契约不得传递 MediatR、FluentValidation 或宿主依赖
- `Tw.Auditing.Contracts`：审计事件与存储端口由多层和基础设施实现共同使用，运行时包还依赖安全与可观测性
- `Tw.BackgroundJobs.Abstractions`：作业定义和控制契约不得强制业务模块引用 MediatR 或 Quartz
- `Tw.Json.Abstractions`：只服务于进程内、缓存或持久化等非协议 JSON 能力，并隔离 Newtonsoft 等 provider；它不用于承诺 HTTP、CAP 或数据库 JSON 可以透明换库

以下现有 `.Abstractions` 不达到单独发包门槛，应合并回同能力包：

- `Tw.Authorization.Abstractions`
- `Tw.DistributedLocking.Abstractions`
- `Tw.EventBus.Abstractions`
- `Tw.Http.Abstractions`
- `Tw.MultiTenancy.Abstractions`
- `Tw.Sharding.Abstractions`
- `Tw.AspNetCore.Abstractions`

这些契约仍然可以保留接口形式，但接口与默认的 provider-neutral 实现位于同一个能力包，不再为薄边界维护独立 NuGet。

## 目标包结构

| 能力目录 | 现有项目数 | 目标项目数 |
| --- | ---: | ---: |
| Application | 9 | 7 |
| Auditing | 2 | 2 |
| BackgroundJobs | 3 | 3 |
| Caching | 2 | 2 |
| Configuration | 3 | 2 |
| Data | 2 | 2 |
| DistributedLocking | 3 | 2 |
| EventBus | 3 | 2 |
| Excel | 2 | 2 |
| Foundation | 13 | 7 |
| Gateway | 2 | 2 |
| Grpc | 1 | 1 |
| Http | 3 | 1 |
| Idempotency | 1 | 1 |
| IdGeneration | 2 | 2 |
| Localization | 1 | 1 |
| MultiTenancy | 2 | 1 |
| Observability | 3 | 3 |
| Resilience | 1 | 1 |
| Sharding | 2 | 1 |
| TestBase | 4 | 4 |
| TextTemplating | 2 | 2 |
| Web | 7 | 6 |
| **合计** | **73** | **57** |

### Application 与领域

| 现有项目 | 目标与动作 | 生产边界说明 |
| --- | --- | --- |
| `Tw.Application.Contracts` | 保留；移除对 `Tw.Domain.Shared` 的引用 | 承载命令、查询、分页和应用契约，不依赖 MediatR |
| `Tw.Application` | 保留 | 承载应用用例管道、验证适配和完成钩子 |
| `Tw.Authorization.Abstractions` + `Tw.Authorization` | 合并为 `Tw.Authorization` | 权限端口和默认检查器属于同一无第三方能力 |
| `Tw.Domain.Shared` | 删除 | 业务共享枚举、DTO 和领域契约必须属于具体限界上下文，不属于全局框架包 |
| `Tw.Domain` | 保留并补齐真实领域原语 | 从 `Tw.Data` 迁入软删除、审计字段、并发标记等 provider-neutral 领域契约；不得承载 ORM 能力 |
| `Tw.Features` | 保留 | Feature 定义、作用域、store/cache 端口和刷新语义 |
| `Tw.Identity.OpenIddict` | 保留 provider 包；未补实前保持 `experimental` | OpenIddict 是身份基础设施选择，Token、Claim 和密钥语义上线后不得静默换 provider；当前不新增 `Tw.Identity.Abstractions` |
| `Tw.Settings` | 保留 | Setting 定义、作用域、store/cache 端口和刷新语义 |

`Tw.Identity.Abstractions` 只有在身份契约出现两个独立生产消费者、第二个 provider 或外部实现者时才建立。单一身份服务专用的 token issuer 端口应优先放在该服务的消费方边界，不能仅为隐藏 OpenIddict 名称增加公共包。

### Auditing、作业、缓存和配置

| 现有项目 | 目标与动作 | 生产边界说明 |
| --- | --- | --- |
| `Tw.Auditing.Contracts` | 保留 | 审计事件和存储端口是稳定跨层契约 |
| `Tw.Auditing` | 保留 | 审计采集、作用域和脱敏运行时 |
| `Tw.BackgroundJobs.Abstractions` | 保留 | 业务作业定义不依赖调度器 |
| `Tw.BackgroundJobs` | 保留 | 统一把作业工作项派发到应用用例管道 |
| `Tw.BackgroundJobs.Quartz` | 保留 provider 边界；真实调度、失败和持久化验证完成前不发布 stable | Quartz JobKey、状态、misfire、并发和持久化语义是上线契约 |
| `Tw.Caching` | 保留 | 定义缓存键、失效和公司认可的缓存语义 |
| `Tw.Caching.FusionCache` | 保留 provider 边界；删除占位实现并补实后才能 stable | provider 可替换性只限缓存数据可清空或重建的场景，不覆盖业务持久状态 |
| `Tw.Configuration` + `Tw.Configuration.Json` | 合并为 `Tw.Configuration` | JSON 清单和路径校验属于内置配置治理，不需要独立 provider 包 |
| `Tw.Configuration.Nacos` | 保留配置 provider 边界 | 只承载 Nacos 配置源；服务发现能力不进入此包，真实采用时建立 `Tw.ServiceDiscovery.Nacos` |

### Data、锁、事件和 Excel

| 现有项目 | 目标与动作 | 生产边界说明 |
| --- | --- | --- |
| `Tw.Data` + `Tw.Uow` | 合并为 `Tw.Data` | 仓储、工作单元、事务和 Outbox 事务边界属于数据能力；领域实体标记迁入 `Tw.Domain` |
| `Tw.Data.SqlSugar` | 保留 provider 边界；真实 client、事务和连接路由完成前不发布 stable | ORM、事务和数据库行为上线后只能通过迁移项目替换 |
| `Tw.DistributedLocking.Abstractions` + `Tw.DistributedLocking` | 合并为 `Tw.DistributedLocking` | 锁端口和锁键构造属于一个 provider-neutral 能力 |
| `Tw.DistributedLocking.Redis` | 保留 provider 边界；真实 Redis 锁、租约和失锁处理完成前不发布 stable | 锁键格式、租约、续租和失锁语义必须稳定 |
| `Tw.EventBus.Abstractions` + `Tw.EventBus` | 合并为 `Tw.EventBus` | 集成事件、发布器和 transport 端口属于一个基础能力 |
| `Tw.EventBus.Cap` | 保留公司标准 CAP/RabbitMQ/SqlSugar 集成包；补实前不发布 stable | 当前只有一套批准栈，不预建 `Cap.SqlSugar`；出现第二个真实存储或 transport 后再按 provider 扩展规则拆分 |
| `Tw.Excel` | 保留 | Excel 导入导出契约、模板、校验和公式注入防护 |
| `Tw.Excel.MiniExcel` | 保留 provider 边界 | MiniExcel/OpenXML 只在组合根选择，第三方类型不得进入 `Tw.Excel` 公共契约 |

### Foundation

| 现有项目 | 目标与动作 | 生产边界说明 |
| --- | --- | --- |
| `Tw.Castle.Core` | 删除 | 不创建 `Tw.Interception` 替代包；入口使用原生管道，应用服务使用应用用例管道或显式 Decorator |
| `Tw.Core` + `Tw.Threading` | 删除 `Tw.Threading` 项目；仅将通用异步释放辅助类型并入 `Tw.Core` | 删除 ambient `ICancellationTokenProvider` 和 AsyncLocal 取消覆盖；入口与业务方法显式传播 `CancellationToken` |
| `Tw.Core` 中的密码学实现 + `Tw.Security` | 密码学实现迁入 `Tw.Security`，两个目标包均保留 | `Tw.Core` 不继续成为安全、Web、DI 和 provider 的汇总包 |
| `Tw.Timing` | 删除，统一使用 .NET `TimeProvider` | 不为 BCL 已提供且可测试的时间抽象维护自有 NuGet；测试使用受控 `TimeProvider` |
| `Tw.DependencyInjection.Abstractions` | 保留 | 模块声明服务和 Options 元数据的编译期契约 |
| `Tw.DependencyInjection` | 保留并作为 Microsoft DI 默认路径 | 扫描、拓扑、优先级、keyed/open generic、Options 和诊断只实现一次 |
| `Tw.DependencyInjection.Autofac` | 删除 | 当前没有属性注入、层级容器、自定义 lifetime 等真实需求；未来出现已验证需求时只能作为显式可选 provider 重新建立 |
| `Tw.ExceptionHandling` + `Tw.Validation.Abstractions` | 合并为 `Tw.ExceptionHandling` | 统一稳定错误描述、验证错误和异常分类；协议映射仍由各入口完成 |
| `Tw.Json.Abstractions` | 保留并在 stable 前完成契约审查 | 只定义公司拥有的非协议 JSON 语义，不公开 provider 类型 |
| `Tw.Json.Newtonsoft` | 保留 provider 边界 | 不得在该 PackageId 下静默改用 System.Text.Json |

### Gateway、gRPC、HTTP 和通用运行能力

| 现有项目 | 目标与动作 | 生产边界说明 |
| --- | --- | --- |
| `Tw.Gateway` | 保留 | 路由、可信请求头和限流策略等 provider-neutral 规则 |
| `Tw.Gateway.Yarp` | 保留 provider 边界；补实 wiring 前不发布 stable | YARP route/cluster/transform 适配只存在于网关宿主 |
| `Tw.Grpc` | 保留 | gRPC 客户端元数据、deadline 和协议治理辅助，不依赖 ASP.NET Core 服务端 |
| `Tw.Http.Abstractions` + `Tw.Http` + `Tw.Http.Client` | 合并为 `Tw.Http` | HTTP 常量、header propagation 和 client 注册属于一个出站 HTTP 能力 |
| `Tw.Idempotency` | 保留 | 稳定幂等键、预留、冲突和提交语义；入口只负责提取协议标识 |
| `Tw.IdGeneration` | 保留 | 公司拥有的 ID 生成消费端口 |
| `Tw.IdGeneration.Yitter` | 保留 provider 边界 | ID 位布局、节点号和时钟回拨规则进入生产后属于持久契约 |
| `Tw.Localization` | 保留 | 资源、回退链、动态文本和实体翻译属于完整独立能力 |
| `Tw.MultiTenancy.Abstractions` + `Tw.MultiTenancy` | 合并为 `Tw.MultiTenancy` | 当前租户契约和 provider-neutral 一致性规则不需要两个 NuGet |
| `Tw.Observability` | 保留 | 关联上下文、标签规范、健康模型和公司可观测语义 |
| `Tw.Observability.OpenTelemetry` | 保留 provider 边界；完成真实注册与插桩前不发布 stable | exporter 可以调整，但 span、metric、tag 名称和采样语义必须稳定 |
| `Tw.Observability.Serilog` | 保留 provider 边界 | Serilog 与脱敏、结构化字段和 OTel sink 的集成独立演进 |
| `Tw.Resilience` | 保留 provider-neutral 策略语义，并移除 Polly/HTTP provider 依赖 | Polly 和 HTTP resilience 的具体注册移到对应出站适配；禁止对未知写操作提供全局重试 |
| `Tw.Sharding.Abstractions` + `Tw.Sharding` | 合并为 `Tw.Sharding` | 分片描述、上下文和路由语义属于同一基础能力 |
| `Tw.TextTemplating` | 保留 | 模板请求、结果、诊断和渲染端口 |
| `Tw.TextTemplating.Scriban` | 保留 provider 边界 | 模板语法一旦持久化即形成数据契约，不能透明换引擎 |

### Web 与测试基础包

| 现有项目 | 目标与动作 | 生产边界说明 |
| --- | --- | --- |
| `Tw.AspNetCore.Abstractions` + `Tw.AspNetCore` | 合并为 `Tw.AspNetCore` | 协议错误、Correlation、认证方案、中间件、健康和限流属于同一 ASP.NET Core 基础能力 |
| `Tw.AspNetCore.Mvc` | 保留；删除 Castle 依赖和通用动态代理适配 | MVC、版本、响应、模型绑定和原生 Filter 能力 |
| `Tw.AspNetCore.Localization` | 保留 | 请求语言、`IStringLocalizer` 和资源导出属于 Web 本地化适配 |
| `Tw.AspNetCore.Grpc` | 保留服务端宿主边界；只有 `AddGrpc()` 转发时不发布 stable | 需要承载真实 interceptor、错误映射、metadata 和 streaming 约束后才能稳定 |
| `Tw.AspNetCore.Mvc.NewtonsoftJson` | 保留显式兼容包 | 仅选择 Newtonsoft MVC 的宿主引用；long ID JSON 表达属于 API 契约 |
| `Tw.AspNetCore.Swashbuckle` | 保留 provider 边界 | OpenAPI 文档生成工具可换，但已发布 OpenAPI 契约不能随生成器变化而破坏 |
| `Tw.TestBase` | 保留 | 轻量公共测试替身和契约测试选项 |
| `Tw.AspNetCore.TestBase` | 保留专项测试边界；补实前不发布 stable | 隔离 `Microsoft.AspNetCore.Mvc.Testing` 依赖 |
| `Tw.Data.SqlSugar.TestBase` | 保留专项测试边界；补实前不发布 stable | 隔离 Testcontainers、Respawn 和 SqlSugar fixture |
| `Tw.EventBus.Cap.TestBase` | 保留专项测试边界；补实前不发布 stable | 隔离 CAP/RabbitMQ 集成 fixture 和 Inbox/Outbox 断言 |

## 测试项目、物理目录与解决方案结构

### 目录和 `.slnx` 约束

生产项目和测试项目采用同一能力目录，不允许只在物理目录或只在解决方案视图中分类：

```text
backend/dotnet/BuildingBlocks/src/<Capability>/<Package>/<Package>.csproj
backend/dotnet/BuildingBlocks/tests/<Capability>/<TestProject>/<TestProject>.csproj

/BuildingBlocks/src/<Capability>/
/BuildingBlocks/tests/<Capability>/
```

- `.slnx` 中每个 BuildingBlocks 项目必须且只能出现一次，路径必须指向真实 `.csproj`
- `<Folder Name>` 必须与项目物理路径中的 `src|tests/<Capability>` 精确对应；`/BuildingBlocks/src/` 和 `/BuildingBlocks/tests/` 只能作为空父节点，不能直接承载项目
- 普通测试项目与被测运行时项目使用同一能力目录；`Tw.Architecture.Tests` 作为整仓结构门禁保留在 `tests/Architecture`
- `Tw.DependencyInjection.Tests.Fixtures` 归属 `Tw.DependencyInjection`，保留在 `tests/Foundation`；四个 TestBase 源码包保留在 `src/TestBase`，现有 `Tw.TestBase.Tests` 保留在 `tests/TestBase`，未来专项 TestBase 测试也必须位于该能力目录
- 测试项目不得因为运行时项目已删除而成为孤儿；架构门禁必须把“找不到对应运行时或 fixture owner”判为失败，不能静默跳过

当前 57 个测试项目在合并后收敛为 50 个。生产包和测试项目必须在同一个迁移提交中处理：

| 生产项目动作 | 测试项目动作 |
| --- | --- |
| 删除 `Tw.Domain.Shared` | 删除空的 `Tw.Domain.Shared.Tests`；领域原语测试并入 `Tw.Domain.Tests`，业务共享 DTO/枚举不迁入 BuildingBlocks |
| `Tw.Configuration.Json` 合并到 `Tw.Configuration` | 将 `JsonConfigurationPathValidatorTests` 并入 `Tw.Configuration.Tests`，删除 `Tw.Configuration.Json.Tests` |
| `Tw.Uow` 合并到 `Tw.Data` | 将工作单元契约与选项测试并入 `Tw.Data.Tests`，删除 `Tw.Uow.Tests` |
| 三个 HTTP 项目合并为 `Tw.Http` | 将 `Tw.Http.Client.Tests` 重命名为 `Tw.Http.Tests`，保留 header propagation 与出站 HTTP 行为测试 |
| 删除 Castle 和 Autofac 路径 | 删除 `Tw.Castle.Core.Tests`；`Tw.DependencyInjection.Autofac.Tests` 仅把容器中立语义迁入 `Tw.DependencyInjection.Tests`，删除 provider 专属断言 |
| 删除 `Tw.Threading` | 删除 ambient cancellation 测试；为迁入 `Tw.Core` 的异步释放辅助类型在 `Tw.Core.Tests` 补测试 |
| 删除 `Tw.Timing` | 删除自有 clock/DI wrapper 测试；消费方用受控 `TimeProvider` 验证自身行为 |
| 合并薄 `.Abstractions` 和 Validation 契约 | 扩展同能力测试项目，不建立或保留 `*.Abstractions.Tests` |

每次项目删除、合并或重命名还必须同步 `.csproj`、`ProjectReference`、命名空间、测试命名空间、`.slnx`、`packages.lock.json`、charter、文档与生成索引。只移动生产代码而留下旧测试项目、旧 lock 或旧 solution entry 视为未完成。

合并后不得继续保留会暗示旧独立边界的 `Tw.Authorization.Abstractions`、`Tw.EventBus.Abstractions`、`Tw.Http.Client` 等 retired namespace。`Tw.Configuration.Json` 作为 `Tw.Configuration` 内部清晰的功能子命名空间可以保留；仍独立发包的 `Tw.BackgroundJobs.Abstractions`、`Tw.DependencyInjection.Abstractions` 和 `Tw.Json.Abstractions` 也不属于 retired namespace。

`Tw.Cli`、`Tw.Analyzers` 和 `Tw.Templates` 是跨团队使用并可独立打包的 .NET 工具，同样纳入 charter、文档和生成索引治理，但不计入 BuildingBlocks 的 57 个目标项目。迁移完成后的受治理 .NET 项目总数为 60：57 个 BuildingBlocks 项目加 3 个工具项目。

## DependencyInjection、Autofac 与 Castle 决策

默认宿主只使用 Microsoft DI：

- `Tw.AspNetCore` 调用 `builder.Services.AddServiceRegistration(builder.Configuration)`
- `Tw.DependencyInjection` 承载唯一注册规划和 Microsoft DI 执行器
- 删除 `UseAutofac()`、Autofac 重复注册执行器和 Castle proxy planner
- 业务包不得引用 `ILifetimeScope`、Autofac Module、`IIndex<T>` 或 Castle interceptor 类型

Autofac 不是常规可插拔 provider。只有出现以下已验证需求之一，才允许以独立 `Tw.DependencyInjection.Autofac` 重新引入：

- 大量无法整改的属性注入
- 层级子容器或特殊嵌套 scope
- Microsoft DI 无法表达的自定义 lifetime 或解析管道
- 已经通过基准、集成测试和架构评审证明必须采用的服务方法动态代理

重新引入也不得让 `Tw.AspNetCore` 默认依赖 Autofac，provider 选择必须留在具体宿主组合根。

## 语义命名与品牌词段治理

`Tw.*` 继续作为 PackageId、程序集名和根命名空间；`Tw:` 配置根、`TWGOV000` 至 `TWGOV006` 诊断号以及迁移资料中的旧包名也不属于代码标识符重命名范围。代码中的品牌词段按大小写不敏感规则治理：

- `TwConfigurationException`、`AddTwYarpGateway`、`twOrder`、`TW_ORDER` 等包含独立 `Tw` 词段的类型、方法、属性、字段、事件或参数名均禁止新增
- `Tw.Core` 程序集中的 `Tw.Exceptions.TwException` 异常类型是唯一批准保留的品牌前缀代码标识符；同名方法、变量、其他程序集或其他命名空间中的同名类型不继承该豁免
- `Between`、`Write`、`Twice`、`Twin` 等正常英文单词不包含独立品牌词段，不得因简单子串匹配被误报或机械改名
- analyzer 的受控负例源码可以保留一个违规标识符以验证诊断，但不得编译进入生产制品
- 名称清理必须依据真实职责选择语义名，不允许只删除两个字符后形成新的含混名称；正式规范已禁止宽泛 `Manager`、`Helper`、`Util` 等角色名

本次迁移采用以下已审计映射：

| 旧标识符 | 目标标识符或动作 | 理由 |
| --- | --- | --- |
| `TwConfigurationException` | 移入 `Tw.Localization` 并改为 `LocalizationConfigurationException` | 当前真实调用全部属于本地化配置，不应继续暴露为 Core 的宽泛异常 |
| `TwAssemblyPriorityAttribute` | `AssemblyRegistrationPriorityAttribute` | 与类型级 `ServicePriorityAttribute` 区分，准确表达程序集注册排序 |
| `TwStringLocalizer`、`TwStringLocalizer<T>` | `StaticSnapshotStringLocalizer`、`StaticSnapshotStringLocalizer<T>` | 名称表达基于静态快照的实现语义，而非公司前缀 |
| `TwStringLocalizerFactory` | `StaticSnapshotStringLocalizerFactory` | 与对应实现保持一致 |
| `MapTwHealthEndpoints` | `MapHealthEndpoint` | 当前只映射一个 `/health` 端点 |
| `AddTwYarpGateway` | 真实 wiring 完成后使用 `AddYarpGateway`；当前 no-op API 删除或内部化 | 不发布无行为的稳定扩展入口 |
| `AddTwHttpResilience` | 从 `Tw.Resilience` 删除；真实 HTTP 集成迁入 `Tw.Http` 后使用 `AddHttpResilience` | provider-neutral resilience 不承担 HTTP 注册 |
| `EnrichWithTwRedaction` | `EnrichWithSensitiveDataRedaction` | 直接表达脱敏行为 |
| `AddTwOpenTelemetry` | 真实 wiring 完成后使用 `AddOpenTelemetryIntegration`；当前 no-op API 删除或内部化 | 未完成 provider 不应公开占位入口 |
| `TwActionInterceptionFilter`、`TwPageInterceptionFilter` | 随 Castle/MVC 动态代理路径删除 | Web 横切逻辑改用原生 Filter/Middleware |
| `IUnitOfWorkManager` | `IUnitOfWorkCoordinator` | 该角色创建并暴露当前工作单元，且 `Manager` 不符合正式命名规范 |
| `SqlSugarUnitOfWorkManager` 及测试替身 | 对应改为 `*UnitOfWorkCoordinator` | 与契约和真实协调职责一致 |

`Tw.Analyzers` 必须把该规则实现为符号级、大小写不敏感且词段边界明确的诊断，并在存量清理完成后作为 analyzer 接入全部 .NET 项目；不能用对源码文本执行不区分语义的全局替换代替。

## 入口与横切逻辑模型

### 支持的入口

| 入口 | 原生拦截点 | 适合处理的关注点 |
| --- | --- | --- |
| Web API / MVC | Middleware、Authorization Policy、MVC Filter | HTTP 异常映射、认证授权、Correlation、限流、模型绑定后审计 |
| Minimal API | Middleware、Endpoint Filter | Endpoint 参数和结果处理 |
| gRPC 服务端 | gRPC Server Interceptor | Metadata、deadline、streaming、`RpcException` 与 Status 映射 |
| CAP 消费 | CAP Subscribe Filter | 消费上下文、观测和异常记录；异常必须保留 CAP 重试语义 |
| Quartz | Job Runner、Job/Trigger Listener | 调度观测、misfire、控制和任务失败语义 |
| `BackgroundService` / 内存队列 / 轮询 | Scoped Worker Executor | 创建 scope、取消、工作项观测并派发应用命令 |
| SignalR | `IHubFilter` | Hub 调用、连接上下文、参数和安全错误 |
| 启动初始化、数据修复、批处理、CLI | Command Runner | 明确身份、审计、停止条件并派发应用命令 |
| 健康检查和运维端点 | Health Check 原生机制 | 健康状态与超时，不进入业务事务管道 |

Webhook 仍属于 HTTP 入口。HTTP/gRPC 客户端属于出站边界，分别使用 `DelegatingHandler`、resilience handler 和 gRPC client interceptor，不纳入入口 AOP。

### 横切关注点归属

- 协议认证、状态码、metadata、streaming、请求大小、限流和协议错误转换归入口原生管道
- 业务验证、应用权限、工作单元、审计、用例性能和业务幂等归 `Tw.Application` 管道
- CAP、Quartz 的异常不得被通用拦截器吞掉，必须让原框架决定重试、misfire 或失败状态
- 只属于一个应用服务的组合逻辑使用显式 Decorator，不建立全局动态代理
- `INotificationHandler` 不经过 MediatR request behavior；领域通知的观测与失败策略必须单独设计，不能假设应用请求管道已经覆盖

原生管道通常减少动态代理、方法选择和异步返回值包装，但本文不承诺固定性能比例。性能验收以代表性入口的吞吐、分配、启动时间和 p95/p99 为准。选择原生管道的首要原因是上下文完整、异常语义正确、调用链可调试和长期维护成本更低。

## 稳定性与第三方替换规则

### 稳定性等级

| 等级 | 范围 | 变更规则 |
| --- | --- | --- |
| 采纳前 / `experimental` | 无具体服务引用且从未发布为稳定制品 | 允许删除、改名、合并和重写，但必须同步 charter、文档和测试 |
| `stable` Package/API | PackageId、程序集、命名空间、公开类型、DI 入口、Options、默认值、错误和可观察行为 | 只允许向后兼容增加、安全修复和不改变契约的内部重构 |
| 持久或跨系统契约 | HTTP/OpenAPI、gRPC、事件 Schema/topic、数据库、ID、Token、持久 JSON、模板语法、缓存/锁/任务键 | 必须版本化、双轨或迁移，不得原地替换 |
| `deprecated` | 已有消费者的旧包、API 或 provider | 禁止新引用，保留兼容和安全维护直至消费者、数据与协议迁移清零 |

`stable` 冻结消费者可见契约，不冻结第三方版本。安全漏洞和兼容性缺陷仍必须升级；升级不得改变序列化、重试、异常、排序、键格式、ID 或协议行为。

### 基本不允许直接切换的技术选择

| 技术边界 | 不能直接切换的原因 | 正确变更方式 |
| --- | --- | --- |
| SqlSugar / 数据访问行为 | 数据库 Schema、事务、并发和查询语义已经形成 | 新 provider、迁移脚本、双轨验证、回滚制品 |
| CAP / RabbitMQ / Inbox-Outbox | Topic、消息 Schema、投递与重试、存储表已经形成 | 版本化消息、双写/双消费、数据迁移 |
| OpenIddict | Token、Claim、密钥、客户端和授权流程已经形成安全边界 | 并行 issuer/validator、密钥和客户端迁移 |
| Yitter ID | ID 位布局、节点和时钟规则已经进入持久数据 | 新 ID 版本或新字段，不得原地更换生成规则 |
| Newtonsoft 或其他持久 JSON | 字段名、long 表达、枚举、日期和多态格式已经持久化或对外发布 | 新 codec/版本字段、兼容读、数据重写 |
| Quartz 持久任务 | JobKey、Trigger、misfire、并发和 JobData 已持久化 | 调度迁移、并行观察和明确回滚 |
| Redis 分布式锁 | 锁键、租约、续租和 fencing 语义影响并发安全 | 新键空间、灰度切换和并发验证 |
| Scriban 模板 | 模板文本和语法已经存储 | 模板批量转换、双引擎验证和回滚 |

### 可以替换但仍需保持行为的内部机制

- DI 容器
- Polly 或 HTTP resilience 的内部实现
- OpenTelemetry exporter 和后端
- Serilog sink
- Swagger/OpenAPI 文档生成器
- 缓存 provider，前提是缓存内容可以安全失效或重建
- Excel 生成库，前提是输出兼容性和公式安全测试通过

这些内部机制不需要为“可替换”单独建立一层模糊接口；应稳定公司认可的行为、配置和可观测结果。

### 不建立自有替代抽象的 .NET 原语

- `IServiceCollection`、`IServiceProvider`
- `ILogger<T>`
- `IConfiguration`、`IOptions<T>`
- `HttpClient`、`IHttpClientFactory`
- `CancellationToken`
- `TimeProvider`
- `Activity`、`ActivitySource`、`Meter`

框架可以提供配置和组合扩展，但不得复制这些原语的全部 API 再包装成公司接口。

## 新能力与第三方包扩展流程

### 决策顺序

1. 只有一个具体服务使用：在服务内部建立 adapter，不创建共享 NuGet
2. 已命中现有包 charter 的 `in_scope`：进入现有包
3. 命中现有包 `out_of_scope`：不得强行放入该包
4. 至少两个服务复用，且单一职责、依赖和公开能力可以独立：建立一个能力包
5. 只有一个公司选定第三方实现，第三方类型可以完全隐藏：仍然只建立一个能力包，在包内使用 internal adapter
6. 消费者确实必须与 provider 传递依赖隔离，或者已有第二个真实 provider/外部实现者：建立 `Abstractions + Provider`
7. 只有真实使用 ASP.NET Core、gRPC、CAP、Quartz 等宿主类型时才增加宿主集成包；单纯 `IServiceCollection` 注册不构成宿主包理由

### Provider 包建立门槛

provider 包进入仓库必须同时具备：

- 跨服务复用或批准的平台级采用计划
- 清晰的第三方依赖隔离、运行时、许可证、安全或部署价值
- 真实 SDK 调用和完整失败语义，不得只返回 provider 名、`object` 或 `CompletedTask`
- 外部调用具备明确 timeout/deadline、错误分类、重试幂等前提和资源释放边界
- 真实依赖集成测试、公共契约测试、存活/就绪/依赖健康检查、可观测性、运行手册和回滚说明
- 精确中央版本、锁文件、许可证与漏洞检查、charter、owner 和使用文档

第二个 provider 不满足这些条件时，不创建空项目占位。

## 拼音转换扩展示例

### 默认决策

若公司选择一个进程内、托管、无网络和原生运行时依赖的拼音 NuGet，并由多个服务复用，则建立单包：

```text
backend/dotnet/BuildingBlocks/src/Pinyin/Tw.Pinyin/
├─ Tw.Pinyin.csproj
├─ package-charter.yaml
├─ PinyinConverter.cs
├─ PinyinConversionOptions.cs
├─ PinyinToneStyle.cs
├─ PinyinLetterCase.cs
├─ UnknownCharacterHandling.cs
└─ Internal/
   └─ ChosenLibraryAdapter.cs

backend/dotnet/BuildingBlocks/tests/Pinyin/Tw.Pinyin.Tests/
docs/shared-packages/dotnet/Tw.Pinyin/
```

默认不创建：

- `Tw.Pinyin.Abstractions`
- `Tw.Pinyin.<Provider>`
- `Tw.AspNetCore.Pinyin`

接口是否存在与是否拆成独立 NuGet 是两件事。只有消费者确实通过 DI 依赖该能力时，才在同一个 `Tw.Pinyin` 包中增加 `IPinyinConverter`；单元测试需求本身不足以拆包。

### API 语义

“汉字转拼音”和“拼音转汉字”不是同一个确定性操作：

- 汉字转拼音可以提供同步 `Romanize`，明确声调、大小写、分隔符、未知字符和多音字默认策略
- 拼音转汉字具有歧义，应提供候选集合及排序依据，不能用一个 `Convert` 方法假装返回唯一正确文本
- 只有调用远程词典、模型或 I/O provider 时才使用异步 API；纯 CPU NuGet 不提供虚假 `ConvertAsync`

公开 API 必须使用公司自有类型，不公开第三方枚举、配置、异常或结果。禁止增加隐藏依赖和行为的 `string.ToPinyin()` 全局扩展。

### 错误、线程安全和数据影响

- `null`、空串、非法选项和未知字符策略必须有明确且可测试的行为
- 不在异常和日志中写入完整姓名、地址等原始敏感文本
- 转换器与默认选项保持不可变，通过并发测试后才允许 Singleton 注册
- 第三方库存在进程级可变静态状态时必须隔离初始化和并发访问；无法安全隔离时不得采纳
- 不预设无界缓存，只有基准证明收益并具备容量限制时才增加缓存
- 拼音结果进入数据库、搜索索引、URL、排序键或缓存键后即成为数据契约；第三方升级改变输出时必须重建索引或执行数据迁移

### 何时升级为 provider 结构

只有出现以下事实之一，才调整为：

```text
Tw.Pinyin.Abstractions
Tw.Pinyin.<ProviderName>
```

- 本地库和远程服务需要同时运行
- provider 存在原生运行时、许可证、目标框架或独立安全升级隔离要求
- 至少两个 provider 各有真实消费者、owner、契约测试和发布计划
- 业务程序集必须在编译期完全排除 provider 传递依赖

拆分后，业务只依赖 `Tw.Pinyin.Abstractions`，组合根引用具体 provider。所有 provider 执行同一套 golden corpus 和非公开 provider contract tests。

### 测试门槛

- 简体、繁体和明确不支持的字符范围
- 中文、ASCII、数字、标点、emoji、代理项和扩展 CJK 混合输入
- 声调、大小写、分隔符、未知字符和多音字策略
- 空值、空串、长文本和非法选项
- 并行调用的确定性与线程安全
- 真实第三方库集成测试，不能只 Mock internal adapter
- 公开 API 扫描，证明第三方类型没有泄漏
- 固定 golden corpus，第三方升级必须显式审查输出差异
- `dotnet pack` 后安装到最小消费者项目验证，而不只使用 `ProjectReference`

## 迁移顺序

### 第一阶段：采纳状态与兼容基线

- 核查内部 NuGet 源、具体服务引用和 `Tw.Core` 的 stable 标记
- 固化当前公开 API、配置键、错误码、序列化、数据库和消息契约清单
- 确认所有允许破坏性变更的包满足“无稳定消费者且无稳定制品”条件
- 先增加目标项目集合、测试项目集合、solution parity、retired reference 和品牌标识符的失败门禁，证明迁移前基线确实不满足目标

### 第二阶段：目录与治理工具基线

- 先把现有生产和测试项目在 `.slnx` 中移动到与物理能力目录一致的 solution folder
- 在破坏性删除前先用架构测试锁定“57 个保留项目 + 尚未迁移的 retired 子集”、真实 ProjectReference 与 solution parity；`tw_memory` 的三层发现、fixture 和完整仓库门禁必须在发布阶段前修复并接管 charter/docs/generated-memory 事实校验
- 修复 charter `public_capabilities` 的前缀重叠判定：规范化后完全相同的能力 ID 仍然冲突，`Tw.Data` 与 `Tw.Data.SqlSugar` 等不同的层级 ID 不因点号前缀自动冲突；语义重叠继续由 charter 的职责、范围和架构评审裁决
- 扩展架构测试、CLI 禁包目录、模板 smoke test 和 analyzer；门禁在对应存量清理完成前可以按任务逐步启用，但最终不得保留永久 suppression

### 第三阶段：删除错误主路径

- 删除 `Tw.DependencyInjection.Autofac`、`Tw.Castle.Core` 及专用测试
- `Tw.AspNetCore` 回归 Microsoft DI
- MVC 删除通用 Castle 管道，保留和补齐原生 Filter
- 应用横切逻辑统一进入 `Tw.Application`

### 第四阶段：同能力合并

- 合并 Authorization、Configuration.Json、DistributedLocking、EventBus、Http、MultiTenancy、Sharding 和 AspNetCore 的薄分层
- `Tw.Uow` 合并到 `Tw.Data`
- `Tw.Validation.Abstractions` 合并到 `Tw.ExceptionHandling`
- `Tw.Threading` 仅保留通用异步释放辅助类型并迁入 `Tw.Core`，删除 ambient 取消服务，调用链改为显式传播 `CancellationToken`
- `Tw.Timing` 迁移到 `TimeProvider`
- 密码学实现从 `Tw.Core` 迁入 `Tw.Security`
- 删除 `Tw.Domain.Shared`，补齐 `Tw.Domain` 的 provider-neutral 领域原语
- 每个生产项目动作同步完成测试合并/删除、solution entry、charter、文档和 lock 文件，不把这些工作推迟到最后集中补账

### 第五阶段：provider 补实

- Quartz、FusionCache、Nacos、SqlSugar、Redis、CAP、YARP、OpenTelemetry、gRPC host 和专项 TestBase 逐包完成真实集成
- 删除 `object`、no-op handle、空注册、空 scheduler 和 `CompletedTask` 占位行为
- 每个 provider 增加真实依赖集成测试、失败语义、健康与可观测性验证
- 未补实的 provider 保持 `experimental`，不进入稳定包目录
- Quartz、FusionCache、Nacos、SqlSugar、Redis、CAP、YARP、OpenTelemetry、gRPC host 和专项 TestBase 分别建立专项 spec 与实施计划；结构合并计划只负责边界、引用和 stable gate，不以一次大迁移替代各 provider 的生产设计

### 第六阶段：工具、发布与迁移资料

- 更新 `.slnx`、项目引用、命名空间、中央版本、锁文件和测试目录
- 更新全部受影响的 `package-charter.yaml`
- 更新 `docs/shared-packages/dotnet` 总索引、包索引和能力 How-to
- 为被删除和改名的包提供迁移映射与禁止新引用门禁
- 更新 `Tw.Cli` 禁包检查、`Tw.Analyzers`、`Tw.Templates`、Python `tw_memory`、`.tw-memory` 生成结果、pre-commit 与正式治理命令
- 从中央版本文件移除 Autofac/Castle 和不再属于目标包的依赖；对全部保留项目和模板重新生成 lock 文件
- 从同一提交构建统一版本的 prerelease 包，并使用真实服务完成安装验证

## 验证设计

### 架构与包验证

- 架构测试验证目标项目数、目录、PackageId、命名空间和项目引用方向
- 架构测试验证精确的 57 个目标生产项目和 50 个目标测试项目，不只比较数量
- `backend/dotnet/BuildingBlocks/building-blocks-topology.json` 是 runtime/test 路径、retired PackageId/namespace、替代映射、批准契约包和工具项目的唯一机器可读清单；架构测试、CLI、模板测试、Python 治理和 pack/consume 脚本共同读取它，不分别硬编码清单
- `.slnx` 中的 BuildingBlocks 项目集合与磁盘 `.csproj` 集合完全一致、无重复，solution folder 与物理 capability folder 完全一致
- 每个普通测试项目必须映射到现存运行时项目或明确 fixture owner，不允许孤儿测试项目
- 禁止生产项目引用 `*TestBase`
- 禁止 `Tw.AspNetCore` 引用 Autofac、Castle 和基础设施 provider
- 禁止业务/Application 项目引用第三方 provider 类型
- 禁止 provider 反向向 `.Abstractions` 命名空间贡献类型
- 使用包验证或公开 API 基线检测 stable 包的破坏性变化
- 每个包执行 `dotnet pack` 与最小消费者安装测试
- 禁止项目、模板和文档新增 retired PackageId；禁止除 `TwException` 外的独立 `Tw` 品牌词段代码标识符
- charter 检查必须扫描真实三层目录并验证仓库中的 charter 事实，不能只运行 validator 自身的单元测试
- charter 与生成索引覆盖 57 个 BuildingBlocks 项目和 3 个 .NET 工具项目；BuildingBlocks 的目标集合断言仍保持独立，不能用 60 混淆包结构验收

### 行为与集成验证

- Microsoft DI 覆盖普通、keyed、开放泛型、Options、作用域和启动诊断
- HTTP、gRPC、CAP、Quartz 和 Worker 分别验证原生入口管道、异常与取消语义
- CAP 验证至少一次投递、Inbox 去重、Outbox 事务和失败重试
- CAP 同时验证重试上限、死信或补偿路径、消息积压指标和恢复方式
- Quartz 验证 misfire、并发、持久化、取消、超时和失败状态
- SqlSugar 验证真实连接、事务提交/回滚、并发冲突和 Outbox 边界
- Redis 锁验证竞争、超时、租约、失锁和恢复
- 所有网络、数据库、缓存、消息和文件调用验证 timeout/deadline，不允许无限等待
- 健康检查分别验证存活、就绪和关键依赖状态，不使用一个不可解释的总结果替代
- 关键入口验证 Correlation/Trace 传播、结构化日志、稳定指标名、低基数标签和 SLO 所需的延迟/错误率信号
- 序列化、ID、模板、缓存键、锁键和消息契约使用 golden/contract tests 固定行为
- 入口性能比较使用代表性负载记录启动时间、吞吐、分配和 p95/p99，不使用无上下文的微基准结论替代系统验证

### 推荐验证命令

```powershell
dotnet restore backend/dotnet/Tw.SmartPlatform.slnx --locked-mode
dotnet test backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj
dotnet test backend/dotnet/tools/tests/Tw.Analyzers.Tests/Tw.Analyzers.Tests.csproj
dotnet test backend/dotnet/tools/tests/Tw.Cli.Tests/Tw.Cli.Tests.csproj
dotnet test backend/dotnet/tools/tests/Tw.Templates.Tests/Tw.Templates.Tests.csproj
$env:PYTHONPATH = "tools/src"
python -m pytest tools/tests
python -m tw_memory check --root .
dotnet build backend/dotnet/Tw.SmartPlatform.slnx --no-restore
dotnet test backend/dotnet/Tw.SmartPlatform.slnx --no-build --no-restore
```

实现阶段还必须运行受影响 provider 的真实依赖集成测试和 pack/consume 验证；上述命令不是完整发布门禁的替代品。

## 兼容、弃用和回滚

- stable 包的 Patch 只用于兼容修复和安全修复，Minor 只允许向后兼容增加
- Major 版本不是绕过迁移责任的手段；破坏性变化仍需架构评审、迁移说明和回滚方案
- 弃用同步提供 `[Obsolete]`、替代 Package/API、CHANGELOG、charter `migration_ref` 和禁止新引用门禁
- 旧包删除前必须满足替代包 stable、已知消费者清零、数据与协议迁移完成、生产观察通过且回滚制品可用
- provider 替换使用新 PackageId 并行引入，不得在 `Tw.Json.Newtonsoft`、`Tw.Data.SqlSugar`、`Tw.EventBus.Cap` 等既有名称下静默更换引擎
- 涉及数据库、消息、ID、Token、模板、缓存键、锁键和任务键的迁移必须提供数据或协议级回滚，不得只依赖 NuGet 降级

## 验收标准

- 73 个现有项目全部有明确保留、合并、删除或迁移目标，目标结构为 57 个项目
- 测试项目从 57 个收敛为设计批准的 50 个；生产包合并、删除和重命名均有对应测试迁移，且不存在孤儿测试项目
- `BuildingBlocks/src`、`BuildingBlocks/tests` 的物理 capability folder 与 `.slnx` solution folder 完全一致，磁盘和解决方案项目集合一一对应
- `Tw.DependencyInjection.Abstractions` 与 `Tw.DependencyInjection` 独立保留，默认宿主不再引用 Autofac/Castle
- 不存在 `Tw.Interception` 或其他覆盖所有入口的通用动态代理包
- 保留的 `.Abstractions`、`.Contracts` 和 provider 包逐一满足本文门槛
- `Tw.Domain.Shared` 删除，框架领域原语归 `Tw.Domain`，业务共享契约归具体限界上下文
- 未补实 provider 不发布 stable 包
- 入口原生管道与应用用例管道职责清晰，异常、重试、取消和幂等语义经过入口级测试
- 新能力可以根据本文决策顺序确定留在服务内部、进入现有包、建立单包或拆分 provider
- 拼音转换示例能够以单包起步，并在真实依赖隔离出现时按规则升级，不预建空抽象
- 除 `TwException` 外，代码标识符不再包含独立的 `Tw` 品牌词段；普通英文单词、PackageId、程序集名、根命名空间、配置根和诊断号不被误伤
- CLI、analyzer、模板、Python 包发现、charter gate、`.tw-memory`、中央版本与 lock 文件均与目标结构一致，不再接受 retired 包或旧扁平目录
- `tw_memory` 的 .NET charter、文档和生成索引精确覆盖 57 个 BuildingBlocks 项目与 3 个工具项目，无漏项和孤儿项
- 统一版本发布、公开 API 基线、charter、使用文档、迁移说明和 pack/consume 验证完整
