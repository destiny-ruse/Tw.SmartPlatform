# CodeGraph 集成式按需记忆层设计

## 背景

当前工程预计演进为多语言、前后端分离、微服务大仓。仓库当前处于脚手架阶段：`backend/dotnet` 下已划分 `BuildingBlocks`、`Services`、`Aspire`、`Build`；`frontend` 下已划分 `apps`、`packages`；`contracts/protos`、`deploy`、`tools` 已就位；`backend/java`、`backend/python` 为预留占位。AI Agent 协助开发时需要快速理解服务边界、公共能力、跨服务通信、前后端协作和工程规则，但不得在每次开发开始时加载全量项目记忆。

旧版 `.tw-memory` 曾提供 `taxonomy.yaml`、`source-index`、`graph`、`route-index`、`generated/chunks`、`adapters` 等结构。该结构具备可追溯索引和路由基础，但 `generated/chunks` 会随源码文件数量增长，不适合作为长期提交的主要记忆载体；且其将易变结构线索与确定性聚合事实混在同一层提交，导致跨服务拓扑被错配给调用图、易变工具被绑成硬依赖、启发式影响面被固化进可提交文件。

本设计采用契约优先架构：把长期提交的记忆建立在确定性的契约、工程规范与目录结构之上，把 CodeGraph 作为 Skill 读取期必须可用的结构索引。提交层生成与校验不依赖 CodeGraph，Skill 开发流程依赖 CodeGraph。

## 目标

- 让不同开发 Skill 按任务读取最小必要记忆，而不是加载全量项目记忆。
- 把记忆层物理隔离为确定性的提交层与启发式的读取层，互不依赖。
- 提交层不依赖 CodeGraph 即可完整生成与校验，使 CI 不必索引整个大仓。
- 在 Skill 读取期必须结合 CodeGraph 获取符号、调用关系、影响范围和代码上下文，减少 Agent 盲目读取源码。
- 控制长期记忆文件数量，使其随服务、包、契约集合和 Skill 数量增长，而不是随类、方法和源码文件增长。
- 自动生成可从契约、工程规范和目录结构推导的记忆，且生成产物确定、无易变 diff 噪声。
- 明确契约、工程规范、源码、CodeGraph 和 `.tw-memory` 各自的权威边界。

## 非目标

- 不把 `.tw-memory` 做成公司文档目录。
- 不在 Skill 中内置全量项目知识。
- 不把 CodeGraph 查询结果作为修改代码的唯一依据。
- 不把 CodeGraph 的任何输出写入提交层。
- 不把每个源码文件、类或方法生成一个长期提交的记忆文件。
- 不提交 `.codegraph/`、FTS、向量库、SQLite 数据库和运行期缓存。

## 权威边界

| 层级 | 职责 | 权威程度 |
| --- | --- | --- |
| `docs/engineering-standards` | 工程规范、评审规则、测试安全要求、目录落点规则 | 工程规则权威来源 |
| 契约文件（`contracts/protos`、`contracts/apis/openapi`、`contracts/apis/asyncapi`、`contracts/apis/frontend-api-mapping`） | API 契约、消息契约、前后端调用约定、跨服务拓扑 | 接口与跨边界拓扑最终依据 |
| 源码与配置模板 | 真实实现、配置键、前端调用 | 行为最终依据 |
| CodeGraph | 本地结构索引、符号关系、调用链、影响范围、文件定位 | 读取期结构事实定位工具 |
| `.tw-memory`（提交层） | Skill 路由、聚合摘要、来源索引、读取策略 | Agent 记忆入口 |
| `.tw-memory/runtime`（读取层缓存） | FTS、向量、临时 chunk、CodeGraph 查询缓存、生成报告 | 本地加速缓存 |

跨服务通信与前后端协作的权威来源是契约文件，不是调用图。CodeGraph 查询结果只能用于读取期缩小范围。涉及行为变更、接口变更、权限、数据、配置、跨服务调用和公共能力复用时，Agent 必须读取命中的源码、契约或正式工程规范后再修改文件。

## 分层架构

记忆层物理隔离为两层，互不依赖。

| 层 | 内容 | 数据来源 | 是否提交 | 是否依赖 CodeGraph |
| --- | --- | --- | --- | --- |
| 提交层 | `manifest` + `routes` + `cards` | 契约 / 工程规范 / 目录与构建文件 | 是 | 否（确定性生成与校验） |
| 读取层 | 会话内符号、调用、影响定位 | CodeGraph；诊断场景可使用 `rg` 与源码辅助定位 | 否 | 是（Skill 场景必须可用） |

核心不变量：

- CodeGraph 的任何输出都不得写入提交层。
- 提交层在没有 CodeGraph 的机器上也能完整生成和校验。
- 易变数据（时间戳、CodeGraph 版本、查询记录）只写入不提交的 `runtime/generation-report.json`。

总体流程：

```text
用户任务
  -> 触发具体 tw- Skill
  -> Skill 读取 routes/skill-routes.yaml 中该 Skill 的路由
  -> 根据任务中的 service/package/api/frontend-app/symbol 定位记忆路由
  -> 读取命中的 cards 聚合卡片（不读未命中卡片）
  -> 读取层：用 CodeGraph 定位符号、调用方、被调用方和影响范围
  -> 回读源码、契约或工程规范
  -> 执行开发、验证
```

## 目录结构

```text
.tw-memory/
|-- README.md
|-- manifest/
|   |-- taxonomy.yaml
|   |-- source-index.generated.json      # 来源 hash，确定性，唯一权威
|   |-- codegraph-adapter.yaml            # 读取期适配器契约：命令、版本区间、json 期望、降级策略
|   `-- retrieval-backends.yaml           # 可选检索后端配置
|-- routes/
|   |-- skill-routes.yaml
|   |-- codegraph-queries.yaml            # 读取期查询模板，可选
|   |-- services/*.generated.yaml
|   |-- packages/*.generated.yaml
|   |-- apis/*.generated.yaml
|   `-- frontend/*.generated.yaml
|-- cards/
|   |-- services/*.generated.md           # service-summary
|   |-- packages/*.generated.md           # package-summary
|   |-- public-apis/*.generated.md        # public-api-summary
|   |-- frontend/*.generated.md           # frontend-summary
|   |-- integrations/*.generated.md       # service-communication 与 frontend-backend-contract，契约派生
|   `-- decisions/*.md                     # 人工确认的架构决策
`-- runtime/                               # 全部不提交
    |-- chunks/
    |-- fts/
    |-- vector/
    |-- codegraph-cache/
    `-- generation-report.json            # 时间戳、CodeGraph 版本、查询记录
```

`.tw-memory/README.md` 说明目录职责、生成命令、校验命令、提交边界和禁止内容。

`.tw-memory/manifest/taxonomy.yaml` 记录语言、来源类型、记忆类型、查找键和生成器版本。

`.tw-memory/manifest/source-index.generated.json` 记录提交层卡片所依据的源文件、契约和规范路径及其 hash。

`.tw-memory/manifest/codegraph-adapter.yaml` 是读取期适配器契约：CodeGraph 命令名、允许版本区间、期望的 `--json` 字段、版本不符或输出漂移时的失败语义、Skill 场景阻断策略、非 Skill 生成校验场景的降级策略。该文件不记录任何易变状态。

`.tw-memory/routes/skill-routes.yaml` 是 Skill 到记忆类型、查找键和 CodeGraph 查询意图的集中路由。

`.tw-memory/routes/codegraph-queries.yaml` 保存读取期标准查询模板，例如按符号查调用方、按服务查入口文件、按包查公共导出、按 API 查处理函数。

`.tw-memory/cards` 只保存聚合事实卡片。`*.generated.md` 卡片由生成器维护，禁止人工编辑。

`.tw-memory/runtime`、`.codegraph/` 不提交。

## Provenance 事实来源模型

提交层每条事实都带来源标签，且只允许四种来源：

| 标签 | 来源 | 用途 |
| --- | --- | --- |
| `[contract:<id>]` | `contracts/protos`、`contracts/apis/openapi`、`contracts/apis/asyncapi`、`contracts/apis/frontend-api-mapping` | 接口与跨服务、前后端拓扑的权威 |
| `[standard:<doc>]` | `docs/engineering-standards` 规范 | 工程规则约束 |
| `[structure]` | 目录落点、构建引用（`.csproj` ProjectReference、`package.json` deps） | 依赖与落点事实 |
| `[manual]` | 人工 decision card | 已确认架构决策 |

提交层不存在 `[codegraph]` 来源。调用图、调用方、被调用方和影响面属于读取层，按需即时计算，不进卡片。`tw-memory check` 校验每条事实的 provenance 标签合法且来源文件存在。

## Skill 集合与驱动读取

项目专属开发 Skill 统一使用 `tw-` 前缀，映射到已成形的功能域。当前集合为五个核心 Skill：

| Skill | 对应域 | 主要 lookup key | 必读记忆类型 |
| --- | --- | --- | --- |
| `tw-contract` | `contracts/protos` 契约定义与演进 | api, service | engineering-rules, api-contract |
| `tw-dotnet-buildingblock` | `backend/dotnet/BuildingBlocks` | package, framework, symbol | engineering-rules, package-summary, public-api-summary |
| `tw-dotnet-service` | `backend/dotnet/Services`，含 Aspire 与 Build 编排 | service, api, package, symbol | engineering-rules, service-summary, api-contract, service-communication |
| `tw-frontend-app` | `frontend/apps`，web 端 `tw.web.*` 与移动端 `tw.app.*` 用 lookup key 区分 | frontend-app, api, component | engineering-rules, frontend-summary, frontend-backend-contract |
| `tw-frontend-package` | `frontend/packages` 共享模块 | frontend-app, package, component, api | engineering-rules, package-summary, public-api-summary |

`backend/java`、`backend/python` 为空占位，对应 Skill 在相应栈出现真实代码后再建。`deploy` 暂不单设 Skill。`tw-memory generate` 与 `tw-memory check` 是 `tools/` 下的工具，不是 Skill。

每个开发 Skill 保持短小，只描述触发条件、开发流程、记忆读取规则、CodeGraph 查询意图和验证要求。项目记忆不写入 Skill 正文。

`skill-routes.yaml` 示例：

```yaml
schema_version: "2.0.0"
skills:
  tw-contract:
    required_memory:
      - engineering-rules
      - api-contract
    conditional_memory:
      - service-communication
      - frontend-backend-contract
    lookup_keys:
      - api
      - service
    codegraph_queries:
      - route_handlers
      - impact

  tw-dotnet-buildingblock:
    required_memory:
      - engineering-rules
      - package-summary
      - public-api-summary
    conditional_memory:
      - dependency-boundary
      - test-strategy
    lookup_keys:
      - package
      - framework
      - symbol
    codegraph_queries:
      - find_symbol
      - callers
      - callees
      - impact

  tw-dotnet-service:
    required_memory:
      - engineering-rules
      - service-summary
    conditional_memory:
      - api-contract
      - service-communication
      - config-boundary
    lookup_keys:
      - service
      - api
      - package
      - symbol
    codegraph_queries:
      - service_entrypoints
      - route_handlers
      - callers
      - impact

  tw-frontend-app:
    required_memory:
      - engineering-rules
      - frontend-summary
    conditional_memory:
      - frontend-backend-contract
      - consumer-map
    lookup_keys:
      - frontend-app
      - api
      - component
    codegraph_queries:
      - importers
      - impact

  tw-frontend-package:
    required_memory:
      - engineering-rules
      - package-summary
      - public-api-summary
    conditional_memory:
      - frontend-backend-contract
      - consumer-map
    lookup_keys:
      - frontend-app
      - package
      - component
      - api
    codegraph_queries:
      - package_exports
      - importers
      - impact
```

Agent 使用 Skill 时的读取规则：

1. 加载本项目公共规则和适用工程规范。
2. 根据任务触发具体 `tw-` Skill。
3. 读取 `skill-routes.yaml` 中该 Skill 的路由。
4. 根据任务中的 service、package、api、frontend-app、symbol 等查找键读取相关 routes。
5. 读取命中的 cards，不读取未命中卡片。
6. 进入读取层：用 CodeGraph 查询定位源文件、符号、调用链和影响范围；CodeGraph 不可用时停止 Skill 开发流程并提示安装、初始化或刷新索引。
7. 回读源码、契约或工程规范后执行开发。

## 全局唯一稳定键

为避免大仓内同名实体碰撞，查找键使用 canonical 形式：

- `service = <solution>/<service-name>`，例如 `dotnet/Authentication`。
- `package = <根命名空间>`。
- `frontend-app = <app 目录名>`，例如 `tw.web.portal`。
- `api = <契约 id>`。
- `symbol = 全限定名`。

路由文件以 canonical 键命名和索引。CodeGraph 查询命中多个候选时，Agent 按 canonical 键、命名空间、路由和文件落点收窄范围。

## Card 结构与文件数量预算

卡片使用固定槽位结构而非自由文本。以 service card 为例：

```text
标识：canonical-id / 路径 / Owner
职责：一段话                              [manual] 或 README 派生
公共面：暴露的 API 契约 id 列表            [contract:*]
入向依赖：消费的契约、引用的包              [contract:*][structure]
出向依赖：调用的下游契约、消息主题           [contract:*][structure]
配置边界：关键配置键                        [structure]
验证入口：测试工程位置                      [structure]
```

每张卡片受行数与体积上限约束。卡片超限时 `tw-memory check` 报警并提示对应服务或包过大、考虑拆分。

长期提交文件数量预算：

| 范围 | 文件预算 |
| --- | --- |
| 每个后端服务 | 1 个 service card，必要时 1 个 communication card（落 `integrations`） |
| 每个前端应用 | 1 个 frontend card，必要时 1 个 backend-contract card（落 `integrations`） |
| 每个共享包或构建块 | 1 个 package card，必要时 1 个 public-api card |
| 每个契约集合 | 1 个 api card |
| 每个 Skill | 只在 `skill-routes.yaml` 增加路由，不创建 Skill 专属大文档 |
| 架构决策 | 每个稳定决策 1 个 decision card |

源码文件、类、方法、组件和测试用例不生成长期提交卡片。对应事实由读取层的 CodeGraph 与运行缓存即时提供。

## 契约优先的跨边界记忆

跨服务通信和前后端协作的事实来自契约，不来自调用图。

- Protobuf 契约存放在 `contracts/protos`。
- OpenAPI 契约存放在 `contracts/apis/openapi`。
- AsyncAPI 契约存放在 `contracts/apis/asyncapi`。
- 前端 API mapping 存放在 `contracts/apis/frontend-api-mapping`。
- `integrations` 下的 service-communication card：由契约的生产方与消费方声明、消息主题绑定、HTTP 客户端配置派生，标注 `[contract:*]` 与 `[structure]`。
- `integrations` 下的 frontend-backend-contract card：由前端调用的契约 id 与对应后端契约定义派生。
- 当某条预期的跨边界关系缺少契约支撑时，生成器将其记为缺口并在生成摘要中提示，而不是凭调用图臆测。

CodeGraph 的 framework-aware 路由检测只在读取层用于定位处理函数，不作为提交层跨服务拓扑的依据。

## CodeGraph 集成

CodeGraph 是 Skill 读取期必须可用的结构索引，通过 `tools/` 下入口从仓库根目录调用，支持初始化、查询和健康检查。提交层生成与 `tw-memory check` 不依赖 CodeGraph。

`manifest/codegraph-adapter.yaml` 定义稳定适配器契约：

- 命令名、允许版本区间、期望的 `--json` 字段。
- Skill 读取期探测 CodeGraph 健康状态；不可用、版本不符或输出漂移时阻断 Skill 开发流程。
- 非 Skill 的生成与校验场景中，CodeGraph 不可用只发出 advisory，不阻断提交层生成或校验。

CodeGraph 读取期职责：

- 定位文件、符号、服务入口、包入口、导出组件和处理函数。
- 查询 callers、callees 和 impact，用于会话内的复用判断与影响判断。

`codegraph-queries.yaml` 示例：

```yaml
schema_version: "2.0.0"
queries:
  find_symbol:
    intent: "按名称定位符号定义"
    requires:
      - symbol
    verify_with_source: true

  callers:
    intent: "查找调用方"
    requires:
      - symbol
    verify_with_source: true

  callees:
    intent: "查找被调用方"
    requires:
      - symbol
    verify_with_source: true

  impact:
    intent: "分析修改影响范围"
    requires:
      - path
    verify_with_source: true

  route_handlers:
    intent: "定位 API 路由处理函数"
    requires:
      - api
      - service
    verify_with_source: true
```

CodeGraph 结果处理规则：

- 结果只存在于会话与 `runtime/codegraph-cache`，不得写入提交层卡片。
- 查询失败时必须先刷新或修复 CodeGraph 索引；不得在 Skill 开发流程中静默回退。
- `rg`、契约和源码读取只作为诊断与最终验证手段，不替代 CodeGraph 读取期定位。
- 查询结果为空时不得断言不存在，必须检查语言支持、索引状态和源码路径。
- 修改任何文件前必须回读命中的源码、契约或正式工程规范。
- impact 结果基于启发式调用图，可能漏报或过宽，只作为读取范围线索，不作为安全性结论。

## 生成管线

记忆更新分为三类：

| 类型 | 更新方式 | 提交边界 |
| --- | --- | --- |
| CodeGraph 索引 | CodeGraph 文件监听或显式初始化命令更新 `.codegraph/` | 不提交 |
| 运行缓存 | `tw-memory generate` 可选预热 FTS、向量、临时 chunks 和 CodeGraph cache | 不提交 |
| 提交层记忆 | `tw-memory generate` 从契约、规范、目录与构建文件生成 manifest、routes、cards | 可提交 |

`tw-memory generate` 顺序：

1. 扫描契约、工程规范、服务目录、前端目录、包目录和构建文件。
2. 计算来源 hash，写 `source-index`。
3. 提取聚合事实，逐条打 provenance 标签。
4. 通过 secret-scan 与脱敏闸门；命中密钥、令牌、生产连接串或未脱敏数据时阻断生成。
5. 确定性写入 `manifest`、`routes`、`cards`：键排序固定，不写入时间戳等易变字段。
6. 易变元数据写入 `runtime/generation-report.json`。
7. 可选预热 `runtime` 缓存与 CodeGraph cache。
8. 输出生成摘要、缺口提示和诊断。

提交层生成不依赖 CodeGraph。

## 校验

`tw-memory check` 校验项（不依赖 CodeGraph）：

- `.tw-memory` 必要目录存在。
- `source-index` 中的 hash 与当前源文件、契约、规范一致。
- routes 指向的 cards 存在。
- cards 指向的源码、契约和规范文件存在。
- 每条事实的 provenance 标签合法且来源文件存在。
- 可提交 cards 数量与单卡体积符合预算。
- secret-scan 通过。
- `.tw-memory/runtime`、`.codegraph/`、SQLite、向量文件和本地缓存未进入提交范围。

附加项（不阻断）：本地存在 CodeGraph 且版本匹配时，输出索引健康提示。CodeGraph 缺失不影响校验通过，CI 不必索引整个大仓。

## 旧 `.tw-memory` 迁移

| 旧结构 | 新结构 | 处理方式 |
| --- | --- | --- |
| `taxonomy.yaml` | `manifest/taxonomy.yaml` | 保留并扩展 Skill、lookup key、memory type |
| `source-index/*.generated.json` | `manifest/source-index.generated.json` | 统一为单一来源索引，保留 hash 和路径安全校验 |
| `graph/services/*.yaml` | `routes/services/*.generated.yaml` 与 `cards/services/*.generated.md` | 拆分路由和聚合摘要 |
| `graph/frameworks/*.yaml` | `routes/packages/*.generated.yaml` 与 `cards/packages/*.generated.md` | 聚合为包级和框架级记忆 |
| `route-index/*` | `routes/*` | 升级为 Skill 可用的路由体系 |
| `generated/chunks/*` | `runtime/chunks/*` | 降级为本地缓存，不提交 |
| `generated/fts/*` | `runtime/fts/*` | 保持本地缓存，不提交 |
| `generated/vector/*` | `runtime/vector/*` | 保持可选本地缓存，不提交 |
| `adapters/vector-backends.yaml` | `manifest/retrieval-backends.yaml` | 保留为可选检索后端配置 |

## 错误处理

- CodeGraph 不可用：不阻断提交层生成与校验；阻断 Skill 开发流程，并提示安装、初始化或刷新索引。
- CodeGraph 索引过期：阻断 Skill 开发流程并要求刷新索引；不阻断提交层校验。
- 记忆卡片过期：卡片依据的 source hash 与当前源文件不一致时，Agent 读取源码并触发重新生成。
- 路由缺失：Agent 不得猜测卡片路径，必须使用契约目录规则、`rg` 和工程目录规则定位事实来源。
- 契约冲突：源码、契约和记忆卡片不一致时，以契约、源码和正式工程规范为准，并将记忆卡片标记为需要重新生成。

## 安全与提交边界

不得提交以下内容：

- `.codegraph/`
- `.tw-memory/runtime/`
- SQLite、向量文件、嵌入文件、二进制索引和本地缓存
- 第三方文档原文、第三方源码副本、聊天记录、密钥、令牌、生产连接串和未脱敏数据

可提交内容仅限：

- `.tw-memory/README.md`
- `.tw-memory/manifest/*.yaml` 和必要的 `*.generated.json`
- `.tw-memory/routes/**/*.yaml`
- `.tw-memory/cards/**/*.md`

强制机制：`.gitignore` 或等效忽略规则排除 `.codegraph/` 与 `.tw-memory/runtime/`；`tw-memory check` 接入 pre-commit 钩子与 CI 闸门，作为提交边界、预算和 secret-scan 的硬强制。

## 测试与验证

实施完成后必须提供以下验证：

- `tw-memory generate` 能从干净工作区生成 manifest、routes、cards，且在无 CodeGraph 环境下同样成功。
- 提交层生成产物确定：两次连续生成的 routes 与 cards 字节一致，无时间戳类易变 diff。
- `tw-memory check` 能发现过期 source hash、缺失 card、provenance 来源缺失、提交边界违规、卡片数量或体积超预算，并在无 CodeGraph 时仍全绿。
- secret-scan 能拦截注入的假密钥与连接串。
- 至少覆盖五个 Skill 场景：`tw-contract`、`tw-dotnet-buildingblock`、`tw-dotnet-service`、`tw-frontend-app`、`tw-frontend-package`。
- 每个场景验证 Agent 只读取命中的路由和卡片。
- 每个场景验证 CodeGraph 只用于读取期定位，修改前会回读源码、契约或正式工程规范。

## 实施影响

需要新增或修改：

- `.tw-memory` 目录结构和提交层生成产物。
- `tools/` 下 `tw-memory` 生成器、校验器、secret-scan 与 CodeGraph 读取期适配器。
- `manifest/codegraph-adapter.yaml` 适配器契约。
- `.gitignore` 或等效忽略规则，排除 `.codegraph/` 与 `.tw-memory/runtime/`。
- pre-commit 钩子与 CI 闸门接入 `tw-memory check`。
- `.agents` 下五个 `tw-` 开发 Skill 的记忆读取说明。
- `docs/engineering-standards/03-project-and-code/project-structure.md` 中的文件落点规则，明确 `.tw-memory` 是 Agent 提交层记忆目录，`.codegraph/` 与 `.tw-memory/runtime/` 是不提交的本地索引与缓存。

当前工作区未检测到 `codegraph` 命令和 `.codegraph/` 目录。由于提交层不依赖 CodeGraph，实施可先交付提交层生成与校验；任何 `tw-` Skill 读取场景验收前必须完成 CodeGraph 安装、初始化和健康检查。
