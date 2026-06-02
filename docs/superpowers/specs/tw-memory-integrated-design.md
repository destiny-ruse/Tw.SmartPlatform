# tw-memory 集成记忆层设计

## 背景

本工程会演进为多语言、前后端分离的微服务大仓。AI Agent 需要快速理解工程规则、包职责、服务边界、契约关系和代码入口，但不能在每次任务开始时加载全部工程规范、全部源码或全部历史记忆。

现有两个设计分别描述了 CodeGraph 集成式记忆层与共享包 charter。两者的边界需要合并：工程规范是给企业内部员工阅读和执行的正式文档，记忆层是 AI 使用的自动生成索引与摘要，CodeGraph 是可用即用的结构索引和 token 优化工具。三者不能互相替代，也不能让同一内容在 `.rules`、`.tw-memory` 和正式规范中重复加载。

## 目标

- 建立一个由 `tw-memory generate` 全量生成、由 `tw-memory check` 校验的 `.tw-memory` 提交层记忆。
- 让 AI Agent 通过分段索引读取最小必要工程规范，而不是加载整套规范或读取重复摘要。
- 让共享包通过包根 `package-charter.yaml` 声明职责、边界、公开能力和依赖约束，再由记忆层生成 package/public-api 卡片。
- 让 CodeGraph 与记忆层配合定位符号、调用关系和影响范围，但不成为记忆生成、记忆校验或 Skill 执行的必需前提。
- 保持工程规范面向员工可读，不为兼容 AI 记忆层写入机器字段、重复摘要或额外索引信息。
- 整理 `.rules`、`AGENTS.md`、`CLAUDE.md` 的加载边界，禁止规则入口和记忆层重复加载相同工程规范内容。

## 非目标

- 不把 `.tw-memory` 做成员工文档目录。
- 不在 `.tw-memory` 保留任何人工编辑内容。
- 不把 CodeGraph 查询结果写入 `.tw-memory`。
- 不生成源码文件、类、方法、组件或测试用例级别的长期记忆文件。
- 不引入 FTS、向量库、检索后端配置等非当前需求必需的设计。
- 不要求员工阅读 `.tw-memory`、`.rules` 或 Agent Skill 才能理解工程规范。

## 权威边界

| 问题 | 唯一权威 | 面向对象 | 记忆层处理 |
| --- | --- | --- | --- |
| 工程规则、检查项、例外流程 | `docs/engineering-standards` | 员工、评审者、Agent | 生成分段索引，只指向原文段落，不复制规则正文 |
| Agent 启动入口与加载顺序 | `AGENTS.md`、`CLAUDE.md`、`.rules/common-agent-instructions.md` | Agent | 只定义加载边界，不承载工程规则正文 |
| AI 规则路由 | `.rules/ai-coding-rules` | Agent | 作为启动路由，记忆层存在时使用生成索引收窄原文段落 |
| 包职责、边界、公开能力、依赖约束 | 包根 `package-charter.yaml` | 包 owner、评审者、Agent | 生成 package card 与 public-api card |
| API、消息、前后端协作契约 | `contracts/` | 开发、测试、Agent | 生成 api/integration route 与 card |
| 真实行为和配置 | 源码、构建文件、配置模板 | 开发、测试、Agent | 生成结构摘要和 source hash，修改前仍回读源文件 |
| 符号、调用关系、影响线索 | CodeGraph | Agent | 会话内查询，不进入提交层 |

## 规则与记忆内容分工

`.rules` 承载 Agent 启动和加载路由，`.tw-memory` 承载自动生成的索引和实体记忆。两者都不得承载正式工程规范正文。

适合保留在 `.rules` 的内容：

| 内容 | 保留原因 |
| --- | --- |
| `AGENTS.md` 与 `CLAUDE.md` 指向 `.rules/common-agent-instructions.md` | 入口规则必须在记忆层缺失时仍可执行 |
| `.rules/common-agent-instructions.md` 的多 Agent 公共行为、加载顺序和维护边界 | 属于 Agent bootstrap，不是项目实体事实 |
| `.rules/ai-coding-rules/00-always-load.md` | 定义基线规范加载入口和无记忆层时的 fallback |
| `.rules/ai-coding-rules/01-task-router.md` | 定义语言、任务类别到索引文件的一级路由 |
| `languages/*.md` 与 `tasks/*.md` 的触发条件 | 用于判断加载哪些正式规范 |
| `Required Formal Standards` 中的正式规范路径 | 用于定位 `docs/engineering-standards` 原文 |
| 必要交叉加载提示 | 例如 API 变更触及认证授权时加载安全任务索引 |

不适合保留在 `.rules` 的内容：

| 内容 | 目标位置 | 原因 |
| --- | --- | --- |
| 包、服务、API、前端应用事实 | `.tw-memory/routes` 与 `.tw-memory/cards` | 项目实体事实随代码和契约变化 |
| 包职责、公开能力、依赖边界摘要 | 源头在 `package-charter.yaml`，派生到 `.tw-memory` | `.rules` 不维护包事实 |
| 工程规范段落索引 | `.tw-memory/routes/standards.generated.yaml` | 这是生成定位索引 |
| CodeGraph 查询意图 | `.tw-memory/routes/codegraph-queries.generated.yaml` | 属于工具加速路径 |
| 正式规范要求摘要 | `docs/engineering-standards` 原文 | 避免规则重复和漂移 |

适合放入 `.tw-memory` 的内容：

| 内容 | 来源 | 生成物 |
| --- | --- | --- |
| 工程规范分段索引 | `.rules/ai-coding-rules` 引用的正式规范 | `routes/standards.generated.yaml` |
| Skill 到规范段落、实体路由的映射 | Skill 元数据、`.rules`、项目结构 | `routes/skills.generated.yaml` |
| 包和公开能力卡片 | `package-charter.yaml`、构建文件、契约 | `cards/packages/*.generated.md`、`cards/public-apis/*.generated.md` |
| 服务、API、前端和集成卡片 | 源码结构、契约、构建文件 | `cards/services`、`cards/apis`、`cards/frontend`、`cards/integrations` |
| CodeGraph 查询意图 | 生成器内置查询名称和参数映射 | `routes/codegraph-queries.generated.yaml` |
| 来源 hash 和 extractor | 所有被索引源文件 | `manifest/source-index.generated.json` |

不适合放入 `.tw-memory` 的内容：

| 内容 | 原因 |
| --- | --- |
| 工程规范正文 | 员工阅读和工程判断以 `docs/engineering-standards` 原文为准 |
| Agent 公共行为规则 | 记忆层缺失时也必须生效 |
| `.rules` 启动顺序 | bootstrap 不能依赖生成物 |
| CodeGraph 查询结果 | 本地、易变、启发式，不是提交层事实 |
| 手写决策卡片 | 记忆层只保留生成内容 |
| 源码片段、类/方法级长期摘要 | 文件量和漂移风险高，读取期用 CodeGraph 或源码定位 |

## 记忆层原则

`.tw-memory` 是 AI 提交层记忆，所有文件都由工具生成并带 `.generated.*` 后缀或位于生成目录。人工不得编辑 `.tw-memory` 内容。

记忆层只保存三类内容：

- **来源索引**：源文件路径、hash、extractor、source type。
- **读取路由**：任务、Skill、实体、工程规范段落到源文件或卡片的映射。
- **聚合卡片**：服务、包、公开能力、契约关系等由源文件确定性派生的摘要。

记忆层不保存工程规范正文、不保存 CodeGraph 查询结果、不保存源码片段、不保存聊天记录、不保存临时分析结果。

## 目录结构

```text
.tw-memory/
|-- manifest/
|   |-- taxonomy.generated.yaml
|   `-- source-index.generated.json
|-- routes/
|   |-- standards.generated.yaml
|   |-- skills.generated.yaml
|   |-- codegraph-queries.generated.yaml
|   |-- packages.generated.yaml
|   |-- services.generated.yaml
|   |-- apis.generated.yaml
|   `-- frontend.generated.yaml
`-- cards/
    |-- packages/*.generated.md
    |-- public-apis/*.generated.md
    |-- services/*.generated.md
    |-- apis/*.generated.md
    |-- frontend/*.generated.md
    `-- integrations/*.generated.md
```

`.tw-memory/runtime/` 不属于提交层，工具不得在默认生成和校验流程中创建它。CodeGraph 自身索引位于 `.codegraph/`，不提交。

## 来源模型

`source-index.generated.json` 是记忆层事实的来源登记簿。每个来源条目包含：

- `source_id`：稳定 id。
- `source_type`：`standard`、`charter`、`contract`、`structure`、`skill`。
- `path`：仓库相对路径，使用 `/`。
- `hash_algorithm`：固定为 `sha256`。
- `sha256`：对 UTF-8、LF 规范化内容计算。
- `extractor`：解析器名称与主版本，例如 `engineering-standard-segment:v1`、`package-charter:v1`、`csproj-reference:v1`。

提交层事实必须引用 `source-index.generated.json` 中存在的 `source_id`。`source_type` 不表达权威等级，权威等级由源文件所在边界决定。

## 工程规范分段索引

工程规范保持员工可读的 Diátaxis/规范正文形式，不增加机器 front matter，不为 AI 添加隐藏字段，也不复制记忆层信息。

`tw-memory generate` 通过以下方式建立规范映射：

1. 扫描 `.rules/ai-coding-rules` 引用的 `docs/engineering-standards` 文件。
2. 按 Markdown 标题树切分段落，生成稳定 `segment_id`。
3. 为每个段落记录 `path`、标题层级、起止行、内容 hash、任务标签和语言标签。
4. 写入 `.tw-memory/routes/standards.generated.yaml`。

Agent 读取工程规范时遵守以下规则：

- `.rules` 只决定本次任务需要哪些正式规范或规范段落。
- `.tw-memory/routes/standards.generated.yaml` 只用于定位段落，不提供规则正文替代品。
- 命中规范段落后，Agent 读取 `docs/engineering-standards` 原文。
- 同一工程规则不得同时读取正式规范原文和 `.tw-memory` 摘要；记忆层没有工程规则摘要。
- 当分段索引不存在或 hash 过期时，Agent 退回 `.rules/ai-coding-rules` 指向的正式规范文件。

## 共享包 charter

共享包 charter 是包职责和边界的源事实，不属于 `.tw-memory`。

charter 文件位于包根目录，文件名固定为 `package-charter.yaml`。`.NET` 包根是 `.csproj` 所在目录；前端共享包根是 `package.json` 所在目录。

必填字段：

| 字段 | 含义 |
| --- | --- |
| `schema_version` | charter 格式版本 |
| `package` | canonical key；`.NET` 使用 `.csproj` 文件名去扩展名，前端使用 `package.json` 的 `name` |
| `owner` | 负责人或团队 |
| `responsibility` | 本包职责 |
| `in_scope` | 本包承担的能力，非空 |
| `out_of_scope` | 本包不承担的能力，非空 |
| `public_capabilities` | 对外暴露的命名空间、模块或入口 |
| `dependency_rules` | `forbid` 与 `allow` 依赖约束 |

可选字段：

| 字段 | 含义 |
| --- | --- |
| `stability` | `experimental`、`stable`、`deprecated`，缺省 `stable` |
| `compatibility` | 兼容性承诺短文本 |
| `migration_ref` | 指向 CHANGELOG、迁移说明或契约版本的仓库相对路径 |

charter 的内容由员工和包 owner 维护。记忆层从 charter 生成 package card、public-api card 和依赖边界校验数据。

## 包卡片

每个共享包最多生成两张卡片：

- `cards/packages/<package>.generated.md`
- `cards/public-apis/<package>.generated.md`

package card 固定槽位：

```text
标识：package / path / owner
职责：来自 package-charter.yaml responsibility
适用范围：来自 in_scope
不适用范围：来自 out_of_scope
依赖边界：来自 dependency_rules 与实际构建依赖
稳定性：来自 stability
兼容性：来自 compatibility 与 migration_ref
来源：source_refs
```

public-api card 固定槽位：

```text
标识：package / path
公开能力：来自 public_capabilities
契约关联：来自 contracts 命中结果
消费提示：来自结构化依赖与契约
来源：source_refs
```

卡片是 AI 读取入口，不是员工文档。charter 与卡片不一致时，以 charter 为准，重新生成卡片。

## 契约与跨边界记忆

跨服务通信、消息、前后端协作和公开 API 的权威来源是 `contracts/`。调用图不作为跨边界拓扑权威。

`tw-memory generate` 从以下目录生成 api 与 integration 路由：

- `contracts/protos`
- `contracts/apis/openapi`
- `contracts/apis/asyncapi`
- `contracts/apis/frontend-api-mapping`

当源码、配置或前端 mapping 声明了具体跨边界调用但缺少契约时，生成器记录阻断错误，不从调用图推断契约关系。

## CodeGraph 集成

CodeGraph 不是记忆层必需品，也不是 Skill 必需品。它的职责是减少 Agent 查找源码、调用关系和影响范围时消耗的 token，并提高定位效率。

CodeGraph 与记忆层的关系：

| 场景 | CodeGraph 要求 | 处理 |
| --- | --- | --- |
| `tw-memory generate` | 不需要 | 只读取源文件、规范、契约、charter、构建文件 |
| `tw-memory check` | 不需要 | 校验 hash、provenance、路由、卡片、提交边界、secret-scan |
| Skill 执行 | 可用即用 | 优先用 CodeGraph 定位符号和影响范围；不可用时用记忆路由、契约和源码读取完成任务 |
| 代码修改前验证 | 不作为唯一依据 | 无论是否使用 CodeGraph，修改前必须回读源文件、契约或正式规范 |

`.tw-memory/routes/codegraph-queries.generated.yaml` 只保存查询意图和参数映射，例如 `find_symbol`、`callers`、`callees`、`impact`、`route_handlers`。这些查询是加速路径，不是阻断门槛。

CodeGraph 查询结果只存在于当前会话或本地工具缓存中，不进入 `.tw-memory`，不参与 source hash，不作为可提交事实。

## Skill 读取流程

Skill 文件保持短小，只描述触发条件、任务流程和验证要求，不内置项目知识。

Agent 执行任务时的读取顺序：

1. 读取 `AGENTS.md` 或 `CLAUDE.md`。
2. 读取 `.rules/common-agent-instructions.md`。
3. 读取 `.rules/ai-coding-rules/00-always-load.md` 与 `01-task-router.md`，确定语言与任务类别。
4. 若 `.tw-memory/routes/standards.generated.yaml` 存在且 hash 有效，按分段索引读取命中的正式规范段落；否则读取 `.rules` 指向的正式规范文件。
5. 若任务涉及包、服务、契约、前端应用或公开能力，读取 `.tw-memory/routes/*.generated.yaml` 中命中的路由和对应卡片。
6. 若 CodeGraph 可用，按 `codegraph-queries.generated.yaml` 查询符号、调用方、被调用方和影响范围。
7. 修改前回读源文件、契约、charter 或正式规范原文。

禁止重复加载规则：

- `.rules` 与 `.tw-memory/routes/standards.generated.yaml` 不得分别触发同一正式规范全文加载。
- `.tw-memory/cards` 不得承载工程规范正文。
- Skill 正文不得复制 `.tw-memory` 卡片内容。
- `AGENTS.md` 与 `CLAUDE.md` 只指向公共入口，不复制公共规则正文。

## `.rules` 与入口文件整理

`AGENTS.md` 和 `CLAUDE.md` 保持启动入口职责，只要求读取 `.rules/common-agent-instructions.md`。

`.rules/common-agent-instructions.md` 维护统一加载顺序：

- 无 `.tw-memory` 时，按 `.rules/ai-coding-rules` 直接加载正式规范文件。
- 有 `.tw-memory` 且 source-index 校验通过时，用 `standards.generated.yaml` 收窄正式规范段落。
- 规则正文只来自 `docs/engineering-standards`。
- 记忆层只用于路由、索引和实体摘要。

`.rules/ai-coding-rules` 保持 AI 加载索引职责，不复制规范正文，不复制记忆卡片，不维护包、服务、契约事实。

`.rules/ai-coding-rules` 的目标形态：

```text
.rules/ai-coding-rules/
|-- 00-always-load.md
|-- 01-task-router.md
|-- languages/*.md
`-- tasks/*.md
```

各索引文件只保留三类信息：

- `When To Load`：触发条件。
- `Required Formal Standards`：正式规范路径。
- `Execution Requirements`：加载流程、交叉加载和 fallback 规则。

`Execution Requirements` 不写正式规范要求摘要。任务风险、测试策略、安全边界、发布回滚、可观测性等工程判断必须读取 `docs/engineering-standards` 原文；当分段索引可用时，读取命中的原文段落。

## 生成管线

`tw-memory generate` 执行顺序：

1. 发现仓库根目录。
2. 扫描 `.rules/ai-coding-rules`、`docs/engineering-standards`、`package-charter.yaml`、`contracts/`、构建文件、Skill 元数据和项目目录。
3. 计算来源 hash，写入 `source-index.generated.json`。
4. 生成工程规范分段索引。
5. 生成 Skill、包、服务、契约、前端和 CodeGraph 查询路由。
6. 生成 package/public-api/service/api/frontend/integration 卡片。
7. 执行 secret-scan、provenance、source_refs、预算和提交边界预检查。
8. 确定性写入 `.tw-memory`，不写时间戳、不写绝对路径、不写本地状态。

## 校验

`tw-memory check` 必须覆盖：

- `.tw-memory` 只包含生成文件。
- `source-index.generated.json` 路径、hash、extractor 合法。
- `standards.generated.yaml` 指向的规范路径和行号存在。
- 规则索引与分段索引不存在同一规范全文重复加载配置。
- `.rules/ai-coding-rules` 不包含包、服务、契约、前端应用事实。
- `.rules/ai-coding-rules` 的 `Execution Requirements` 不复制正式规范正文。
- routes 指向的 cards 存在。
- cards 的 `source_refs` 都能回到 source-index。
- 每个共享包存在 `package-charter.yaml`。
- charter schema 完整，`in_scope`、`out_of_scope`、`public_capabilities` 非空。
- charter `package` 与 canonical key 一致。
- 实际依赖不违反 `dependency_rules`。
- 不同包 `public_capabilities` 不重叠。
- charter、卡片和生成路由不包含密钥、令牌、连接串或未脱敏敏感信息。
- `.codegraph/`、`.tw-memory/runtime/`、SQLite、向量文件、本地缓存未被 Git 跟踪或暂存。

## 错误处理

- 分段索引缺失：Agent 退回 `.rules/ai-coding-rules` 指向的正式规范文件。
- source hash 过期：`tw-memory check` 失败，要求重新生成。
- charter 缺失或 schema 不完整：`tw-memory check` 失败。
- 契约缺失且存在明确跨边界调用声明：`tw-memory generate` 失败。
- CodeGraph 不可用：Skill 使用记忆路由、契约和源码读取继续执行。
- CodeGraph 查询为空：不得断言目标不存在，必须回读路由、契约和源码验证。
- 记忆卡片与源文件冲突：以源文件、契约、charter 或正式规范为准，重新生成记忆。

## 实施影响

需要新增或修改：

- `tools/` 下 `tw-memory` 生成器与校验器。
- `.tw-memory` 生成目录和提交边界。
- 包根 `package-charter.yaml`。
- `docs/engineering-standards/03-project-and-code/shared-package-charter.md`，作为员工阅读的 charter 规范。
- `.rules/common-agent-instructions.md`，加入记忆层存在时的分段索引加载边界。
- `.rules/ai-coding-rules`，保持正式规范路由，不维护实体记忆。
- `AGENTS.md` 与 `CLAUDE.md`，保持只指向公共入口。

不需要修改工程规范来承载 AI 记忆层字段；工程规范只增加员工需要理解和执行的规则。

## 验证

实施完成后必须证明：

- 删除 CodeGraph 或清空 `.codegraph/` 后，`tw-memory generate` 与 `tw-memory check` 仍成功。
- 删除 CodeGraph 后，Skill 能通过 `.rules`、`.tw-memory` 路由、契约和源码读取完成任务。
- 连续两次 `tw-memory generate` 生成字节一致。
- `.tw-memory` 下不存在人工维护文件。
- 工程规范分段索引只指向 `docs/engineering-standards` 原文，不复制规范正文。
- `.rules` 与 `.tw-memory` 不会导致同一工程规范全文被重复加载。
- `.rules/ai-coding-rules` 只保留触发条件、正式规范路径、加载流程和交叉加载提示。
- `.tw-memory` 保存包、服务、API、前端应用、CodeGraph 查询意图等自动生成记忆。
- package card 与 public-api card 完全由 `package-charter.yaml`、契约和构建文件派生。
- 删除 charter、清空 `out_of_scope`、违反依赖边界、公开能力重叠、注入假密钥都会让 `tw-memory check` 失败。
- CodeGraph 可用时能减少源码定位读取量；CodeGraph 不可用时不阻断记忆层和 Skill 流程。
