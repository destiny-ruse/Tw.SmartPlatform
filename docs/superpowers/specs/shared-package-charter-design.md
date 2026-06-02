# 共享包 charter 设计

## 背景

本工程演进为多语言、前后端分离的微服务大仓。`backend/dotnet/BuildingBlocks` 已划分 `Tw.Core`、`Tw.AspNetCore` 等公共构建块，`frontend/packages` 预留前端共享包，未来还会出现更多跨服务、跨应用复用的包。

研发与评审需要回答三类问题：某个能力该放进哪个包、新功能要不要独立成包、AI 智能体写代码时怎么知道每个包负责什么。当前缺乏确定的事实来源：`docs/engineering-standards/03-project-and-code/project-structure.md` 的「文档约定」要求公共组件说明适用范围、不适用范围、兼容性、升级方式，但未定义形式、位置和强制力；`docs/superpowers/specs/ai-memory-design.md` 的 package card 把「职责」槽来源记为 `[manual] 或 README 派生`，是一处未确定的接缝；`BuildingBlocks/src` 下的包目前没有任何职责声明。

本设计补齐这处接缝：为每个共享包建立一份手写、结构化、带来源标签的 charter，作为该包职责与边界的唯一事实来源，并接入既有 `.tw-memory` 记忆层与 `tw-memory check` 硬闸门。

## 目标

- 为每个共享包提供单一事实来源，声明职责、适用范围、不适用范围、公开能力、依赖边界、兼容性指针。
- 让 AI 智能体经记忆层 package card 获得每个包做什么、不做什么，避免重复造轮子或把功能放错包。
- 把「新增包必须声明边界」「依赖边界不被破坏」「公开能力不重叠」做成确定性硬闸门，接入 pre-commit 与 CI。
- 保持人与 AI 的受众物理隔离：人只读 `docs/engineering-standards` 与各包 charter，AI 读记忆层派生卡片，互不索引。
- 复用既有 `.tw-memory` 的 source-index、provenance、hash、secret-scan、提交边界机制，不另起第二套管线。

## 非目标

- 不在记忆层生成任何供人阅读的跨包索引或矩阵。
- 不引入第五种 provenance 来源；charter 归入既有 `[manual]`。
- 不把 charter 做成包的使用教程或 API 文档；charter 只承载治理事实。
- 不让工具机械裁决语义级别的职责重叠；语义重叠归代码评审。
- 不改变记忆层提交文件数量预算；charter 是包根源文件，不是记忆层提交文件。
- 不要求提交层生成或校验依赖 CodeGraph。

## 受众分离与权威边界

人与 AI 两类受众物理隔离，互不索引。

| 受众 | 只读 | 不读 |
| --- | --- | --- |
| 人（员工、评审、包 owner） | `docs/engineering-standards`（规则）、各包根目录 `package-charter.yaml`（该包事实） | `.tw-memory` 记忆层 |
| AI 工具 | `.tw-memory` 的 routes 与 cards（由 charter 派生） | —— |

三处权威各管一件事，不重叠、不竞争：

| 问题 | 唯一权威 | 性质 | 维护方 |
| --- | --- | --- | --- |
| 文件或包放哪个目录、必须带什么 | `project-structure.md` 落点矩阵 | 规则 | 人（规范） |
| 某个具体包做什么、不做什么、公开什么、依赖边界 | 该包根目录 `package-charter.yaml` | 实例事实 | 人（包 owner） |
| 跨包检索（人不需要）、package card（AI 用） | 由 charter 生成的派生卡片 | 视图，非事实来源 | 生成器，禁手工编辑 |

唯一事实如何保住：每条信息只有一个产地。放哪个目录由落点矩阵管；包做什么由该包 charter 管；给 AI 的卡片机械派生、禁编辑、与 charter 不一致时以 charter 为准并重新生成。记忆层不生成任何供人阅读的跨包视图，撞名与漂移的根被拔掉。

「员工看哪里」的答案：先看 `engineering-standards`（规则），再打开对应包的 `package-charter.yaml`（该包职责）。记忆层员工不看。

## charter 字段 schema

charter 文件落在每个包的根目录，文件名 `package-charter.yaml`。dotnet 包根是 `.csproj` 所在目录，frontend 包根是 `package.json` 所在目录。charter 是与 `.csproj`、`package.json` 同级的普通受跟踪源文件，不属于记忆层，不受 `.tw-memory` 提交边界约束。

| 字段 | 必填 | 含义与用途 | 喂给 |
| --- | --- | --- | --- |
| `schema_version` | 是 | charter 格式版本 | 校验器 |
| `package` | 是 | canonical 唯一键。dotnet 为根命名空间（如 `Tw.Core`），frontend 为 `package.json` 的 `name`。校验器比对实际值 | 卡片标识、全局唯一键 |
| `owner` | 是 | 负责人或团队 | 卡片标识 |
| `responsibility` | 是 | 一段话描述本包负责什么 | package card `职责` 槽 `[manual]` |
| `in_scope` | 是，非空 | 明确属于本包的能力清单 | 卡片与评审依据 |
| `out_of_scope` | 是，非空 | 明确不属于本包的能力，用于防蔓延并告知 AI 不要往此包添加 | package card `不适用范围` 槽 `[manual]` |
| `public_capabilities` | 是 | 对外暴露的命名空间或模块清单 | public-api card、跨包互斥检查 |
| `dependency_rules` | 是 | `forbid` 与 `allow` 依赖清单 | 对照实际依赖校验 |
| `stability` | 否 | `experimental`、`stable`、`deprecated`，缺省 `stable` | 卡片 |
| `compatibility` | 否 | semver 级兼容承诺，短文本 | 卡片 |
| `migration_ref` | 否 | 指向 CHANGELOG 或契约版本的指针，不放迁移长文 | 卡片 |

强约束：`out_of_scope` 必须非空，空 `out_of_scope` 等于未声明边界，校验失败。`responsibility`、`in_scope`、`out_of_scope` 文本不得出现未来承诺语义（如「后续」「待定」「暂定」「视情况」「TODO」「TBD」）。

示例 `backend/dotnet/BuildingBlocks/src/Tw.Core/package-charter.yaml`：

```yaml
schema_version: "1.0.0"
package: Tw.Core
owner: platform-team
stability: stable
compatibility: "semver-minor 内向后兼容"
migration_ref: "backend/dotnet/BuildingBlocks/src/Tw.Core/CHANGELOG.md"
responsibility: >
  跨服务复用的基础原语：值对象、Result、领域异常、调用上下文、
  通用集合与反射工具。不绑定任何 Web、框架或数据访问技术。
in_scope:
  - 基础值对象与 Result 原语
  - 通用领域异常与错误码
  - 调用上下文与环境抽象
out_of_scope:
  - HTTP、中间件、过滤器（属于 Tw.AspNetCore）
  - 数据访问、EF、仓储实现
  - 任何具体业务领域模型
public_capabilities:
  - Tw.Core.Primitives
  - Tw.Core.Exceptions
  - Tw.Core.Context
dependency_rules:
  forbid:
    - "Microsoft.AspNetCore.*"
    - "Microsoft.EntityFrameworkCore*"
  allow: []
```

## 规范落地与记忆层接入

### docs/engineering-standards 改动（人读规则源）

- 新增 `03-project-and-code/shared-package-charter.md`「共享包 charter 规范」，定义 charter 必须存在、字段语义、`out_of_scope` 强约束、`dependency_rules` 边界、`stability`、`compatibility`、`migration_ref`、新增包流程与重叠处理。该文件是人学习 charter 是什么、怎么写的唯一权威。
- `project-structure.md` 落点矩阵增加一行：`包职责与边界声明 (charter) -> <package-root>/package-charter.yaml`。落点矩阵仍是唯一导航根。
- `project-structure.md`「文档约定」中关于公共组件说明适用范围、不适用范围、兼容性、升级方式的散文要求，收敛为「以 `package-charter.yaml` 声明」，消除另写 README 描述边界的竞争路径。
- `engineering-standards/README.md` 目录导航登记新规范。

### docs/superpowers/specs/ai-memory-design.md 改动（AI 派生层）

- package card 槽位：`职责` 槽来源由 `[manual] 或 README 派生` 改为 `[manual]` 且来源为 charter 的 `responsibility`；新增 `不适用范围` 槽，来源为 charter 的 `out_of_scope`，标签 `[manual]`；`公共面` 槽来源为 charter 的 `public_capabilities` 加命中的契约 id。
- provenance：charter 归入既有 `[manual]`，不引入第五种。扩写 `[manual]` 定义为「人工 decision card 与 package charter」。charter 以 `source_type: manual`、`extractor: package-charter:v1` 登记进 `source-index.generated.json` 并计 hash。
- 文件数量预算不变：仍是每包一个 package card 加一个 public-api card。charter 是包根源文件，不占记忆层提交预算。
- 生成管线增加扫描各包 `package-charter.yaml`，并将其 hash 写入 source-index。
- 实施影响补充 charter 规范、生成器解析 charter、校验器闸门三项。

## 硬闸门校验

`tw-memory check` 新增以下校验项，全部确定性，仅从 charter、`.csproj`、`package.json` 与目录结构推导，不依赖 CodeGraph。pre-commit 检查暂存范围，CI 检查仓库跟踪范围。

| 序号 | 校验项 | 失败条件 |
| --- | --- | --- |
| 1 | charter 存在性，即新增包硬闸门 | 任一被识别为包的目录缺 `package-charter.yaml`。dotnet 为 `BuildingBlocks/src` 下含 `.csproj` 的目录，frontend 为 `frontend/packages` 下含 `package.json` 的目录 |
| 2 | schema 完整 | 必填字段缺失或为空，尤其 `out_of_scope` 为空 |
| 3 | canonical key 一致 | `charter.package` 不等于从 `.csproj` 根命名空间或 `package.json` `name` 推导的实际值 |
| 4 | 依赖边界一致 | 实际依赖命中 `dependency_rules.forbid`；`allow` 非空时出现 allow 之外的依赖 |
| 5 | 跨包公开能力互斥 | 两个包的 `public_capabilities` 命名空间相交 |
| 6 | 占位词扫描 | charter 文本字段含未来承诺语义 |
| 7 | provenance 与 hash 一致 | charter 未登记 source-index；hash 过期；card 的 `[manual]` 事实 `source_refs` 未指向 charter source_id |
| 8 | secret-scan | charter 内容命中密钥、令牌或连接串 |

确定性与启发式的边界：命名空间级别的具体重叠由第 5 项机械判定为硬闸门。语义级别的职责重叠无法机械判定，不做硬闸门，归代码评审；charter 把双方边界摆在评审者面前提供判断依据，工具不裁决。

## 新增包流程与重叠处理

新增功能时，按以下流程界定归属，写入 `shared-package-charter.md` 供人遵循；AI 经 package card 的「不适用范围」槽获得同样约束。

1. 先查既有 charter。新能力落在某包 `in_scope` 则进该包，不新建；命中某包 `out_of_scope` 但属另一包 `in_scope` 则进另一包。
2. 建新包须同时满足四条判据：单一职责可一句话表述且不属任何现有包 `in_scope`；有真实跨服务或跨应用复用需求，即至少两个消费方或明确规划，否则留在使用方；依赖边界独立，不把重依赖泄漏给轻量包；不与现有包 `public_capabilities` 命名空间冲突。
3. 建包即提交 charter，由硬闸门第 1 项兜底；新 charter 的 `out_of_scope` 显式划清与相邻包的边界。
4. 重叠处理：命名空间重叠由硬闸门第 5 项挡下，必须重划；语义重叠由评审裁决，方式为合并、迁移能力或调整 in_scope 与 out_of_scope，稳定决策记一张 decision card，标签 `[manual]`。
5. 日常防蔓延：往某包加功能前，人读该包 charter 的 `out_of_scope`，AI 读 card 的「不适用范围」槽；命中即停，改放正确的包或新建包。

## 实施影响

需要新增或修改：

- 各共享包根目录新增 `package-charter.yaml`，当前覆盖 `Tw.Core` 与 `Tw.AspNetCore`，并随新包同步新增。
- `docs/engineering-standards/03-project-and-code/shared-package-charter.md` 新增规范。
- `docs/engineering-standards/03-project-and-code/project-structure.md` 落点矩阵与文档约定修改。
- `docs/engineering-standards/README.md` 目录导航补登。
- `docs/superpowers/specs/ai-memory-design.md` 的 package card 槽位、provenance 定义、生成管线、实施影响修改。
- `tools/` 下 `tw-memory` 生成器增加解析 charter、填充卡片槽位、登记 source-index 的能力。
- `tools/` 下 `tw-memory` 校验器增加第 1 至第 8 项校验。

## 验证

实施完成后必须提供以下验证：

- 为 `Tw.Core` 与 `Tw.AspNetCore` 编写 charter 后，`tw-memory generate` 生成的 package card 的「职责」与「不适用范围」槽内容与 charter 一致，且带 `[manual]` 标签与指向 charter 的 `source_refs`。
- 删除任一包的 charter，或将其 `out_of_scope` 置空，`tw-memory check` 失败。
- 在 `Tw.Core` 的 `.csproj` 中引入命中 `dependency_rules.forbid` 的依赖，`tw-memory check` 失败。
- 让两个包的 `public_capabilities` 声明相交，`tw-memory check` 失败。
- charter 文本注入未来承诺语义或假密钥，`tw-memory check` 失败。
- `charter.package` 与实际根命名空间或 `package.json` `name` 不符，`tw-memory check` 失败。
- 以上校验在无 CodeGraph 环境下同样可执行并得到一致结果。
- 两次连续 `tw-memory generate` 的卡片字节一致，charter 派生不引入易变 diff。
