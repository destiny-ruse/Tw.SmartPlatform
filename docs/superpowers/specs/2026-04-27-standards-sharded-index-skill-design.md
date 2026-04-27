# 工程规范分片索引与 Skill 体系设计

- 日期：2026-04-27
- 状态：已确认设计，待实现
- 范围：现有规范平台底座 v2 重构、分片索引、薄 Skill、完整多语言工程规范内容落地
- 基础：保留当前仓库已实现的 Python 标准库工具链、`docs/standards/`、RFC / ADR、机器规则绑定和 GitHub Actions 校验
- 参考：`D:\tw-engineering-standards\docs\superpowers\specs\2026-04-26-tw-engineering-standards-blueprint-design.md`

## 背景

当前仓库已经实现首版规范平台底座，包括：

1. `docs/standards/` 规范目录。
2. `docs/standards/index.generated.json` 全量索引。
3. `docs/standards/meta.schema.json` 元数据说明。
4. `tools/standards/standards.py` Python 标准库工具。
5. `docs/rfcs/`、`docs/adrs/` 治理目录。
6. `.github/workflows/standards.yml` CI 校验。

首版底座可以跑通规范生成、索引生成、元数据校验、链接校验和机器规则绑定校验，但索引仍是单一全量结构。随着完整多语言规范内容补齐，单一索引会逐渐变大，AI 或自动化工具在开发某个具体功能时可能加载过多无关索引内容。

本设计在保留现有底座的基础上升级为 v2：通过轻量总目录、分片索引、单篇章节索引和薄 Skill，让 AI 先根据开发任务、目标路径、技术栈和主题标签定位候选规范，再读取命中的章节或片段。

## 目标

1. 保留当前已实现的规范平台底座，不迁移到独立仓库，不切换 Node / TypeScript 工具链。
2. 将单一全量索引重构为多级分片索引，避免开发具体功能时加载全部规范索引。
3. 新增工程规范薄 Skill，使 AI 能按任务和路径推断应读取的规范。
4. 直接升级现有规范文档到 v2 契约，不保留旧字段兼容层。
5. 补齐完整多语言、多端、多微服务工程规范内容，覆盖蓝图中的完整规范清单。
6. 保持所有规范正文使用简体中文，示例代码、命令、配置项可保留英文。
7. 保持 `python tools/standards/standards.py check` 作为本地和 CI 的统一验收入口。

## 非目标

1. 不迁移到独立 `tw-engineering-standards` 仓库。
2. 不引入 Node / TypeScript 工具链。
3. 不在本阶段部署文档站点。
4. 不实现 lint-presets 发布体系。
5. 不将现有 `machine_rules` 字段替换为蓝图中的 `machineForm`。
6. 不把规范正文写入 Skill。

## 关键决策

| 编号 | 决策 | 结果 |
| --- | --- | --- |
| D1 | 总体方案 | 选择“现有 Python 底座 + 蓝图取用契约 + 分片索引 + 薄 Skill”。 |
| D2 | 兼容策略 | 不做旧字段兼容，直接迁移现有文档到 v2 front matter。 |
| D3 | 索引入口 | `docs/standards/index.generated.json` 改为轻量路由总目录。 |
| D4 | 取用粒度 | 默认章节级，支持 `region` 片段级。 |
| D5 | Skill 行为 | Skill 面向开发任务，不只面向“查规范”问题。 |
| D6 | 工具链 | 继续使用 Python 标准库工具，命令入口保持不变。 |
| D7 | 内容范围 | 补齐完整规范清单：66 篇规则 / 参考 / 流程规范，加起步 ADR。 |

## 目录结构

目标结构如下：

```text
docs/
  standards/
    README.md
    index.generated.json
    meta.schema.json
    _meta/
      retrieval-contract.md
      query-vocabulary.md
      index.schema.json
    _index/
      by-role/
        backend.generated.json
        frontend.generated.json
      by-stack/
        dotnet.generated.json
        java.generated.json
        python.generated.json
      by-doc-type/
        rule.generated.json
        reference.generated.json
        process.generated.json
        decision.generated.json
      by-tag/
        framework.generated.json
        api.generated.json
      sections/
        standards.authoring.generated.json
    rules/
      git-commit.md
      api-response-shape.md
      naming-dotnet.md
    references/
      error-codes.md
      http-status.md
    processes/
      rfc-flow.md
      change-governance.md
    decisions/
      0001-adopt-standards-blueprint.md
      0002-rest-response-shape-no-null.md
.agents/
  skills/
    engineering-standards/
      SKILL.md
      retrieval-flow.md
      query-vocabulary.md
      examples.md
tools/
  standards/
    standards.py
    templates/
      standard.md
      rfc.md
      adr.md
    rules/
      standard-authoring.rules.json
.github/
  workflows/
    standards.yml
```

`docs/standards/_index/` 和 `docs/standards/index.generated.json` 均为生成产物并入仓。这样 Skill 在任意环境中无需运行工具即可读取索引。

## v2 文档契约

所有规范 Markdown 必须使用 v2 front matter。旧字段 `applies_to` 不再允许出现。

最小必填字段：

```yaml
---
id: standards.authoring
title: 规范撰写规范
doc_type: process
status: active
version: 1.0.0
owners: [architecture-team]
roles: [architect, ai]
stacks: []
tags: [standards, governance, ai]
summary: 规定工程规范文档的元数据、章节结构、索引生成、AI 按需引用和机器规则绑定方式。
machine_rules:
  - id: standard-authoring.rules
    path: tools/standards/rules/standard-authoring.rules.json
    type: schema
supersedes: []
superseded_by:
review_after: 2026-10-27
---
```

字段规则：

1. `id` 使用点分命名，继续沿用当前工具的形式，例如 `rules.git-commit`、`references.error-codes`。
2. `doc_type` 只能是 `rule`、`reference`、`process`、`decision`。
3. `status` 只能是 `draft`、`active`、`deprecated`、`superseded`。
4. `roles` 可取 `architect`、`backend`、`frontend`、`qa`、`devops`、`ai`。
5. `stacks` 可取 `dotnet`、`java`、`python`、`vue-ts`、`uniapp`，可为空数组。
6. `tags` 是主题标签，必须至少 1 个。
7. `summary` 是 AI 路由摘要，必须准确描述规范作用。
8. `review_after` 使用 `YYYY-MM-DD`。
9. `machine_rules` 沿用当前机制，可为空数组。

## 章节与片段取用

每个可被 AI 稳定引用的章节必须使用显式 anchor：

```markdown
<!-- anchor: rules -->
## 规则
```

章节内需要更细粒度引用时使用 region：

```markdown
<!-- region: no-null -->
### 禁止返回 null

REST API 响应字段不得返回 `null`，空对象返回 `{}`，空列表返回 `[]`。
<!-- endregion: no-null -->
```

引用格式：

```text
<standard-id>#<anchor>
<standard-id>#<anchor>:<region-id>
```

例如：

```text
rules.api-response-shape#rules:no-null
```

## 索引模型

索引分三层。

### L0 轻量总目录

路径：`docs/standards/index.generated.json`

职责：只做路由，不包含章节详情和 region 详情。

示例：

```json
{
  "schema_version": "2.0.0",
  "generated_at": "2026-04-27T00:00:00Z",
  "generator": {
    "name": "tools/standards/standards.py",
    "version": "2.0.0"
  },
  "standards": [
    {
      "id": "standards.authoring",
      "title": "规范撰写规范",
      "doc_type": "process",
      "status": "active",
      "version": "1.0.0",
      "path": "docs/standards/processes/standard-authoring.md",
      "roles": ["architect", "ai"],
      "stacks": [],
      "tags": ["standards", "governance", "ai"],
      "summary": "规定工程规范文档的元数据、章节结构、索引生成、AI 按需引用和机器规则绑定方式。",
      "sections_index": "docs/standards/_index/sections/standards.authoring.generated.json",
      "shards": [
        "docs/standards/_index/by-role/architect.generated.json",
        "docs/standards/_index/by-role/ai.generated.json",
        "docs/standards/_index/by-doc-type/process.generated.json",
        "docs/standards/_index/by-tag/standards.generated.json"
      ]
    }
  ]
}
```

### L1 分片索引

路径：

```text
docs/standards/_index/by-role/*.generated.json
docs/standards/_index/by-stack/*.generated.json
docs/standards/_index/by-doc-type/*.generated.json
docs/standards/_index/by-tag/*.generated.json
```

职责：列出某个角色、技术栈、规范类型或主题标签下的候选规范。

分片索引可包含 `summary` 用于排序和判断，但不得包含章节详情和正文。

### L2 单篇章节索引

路径：

```text
docs/standards/_index/sections/<standard-id>.generated.json
```

职责：保存单篇规范的章节与 region 行号。

示例：

```json
{
  "id": "rules.api-response-shape",
  "path": "docs/standards/rules/api-response-shape.md",
  "version": "1.0.0",
  "sections": [
    {
      "anchor": "rules",
      "title": "规则",
      "level": 2,
      "start_line": 42,
      "end_line": 88,
      "regions": [
        {
          "id": "no-null",
          "start_line": 51,
          "end_line": 64
        }
      ]
    }
  ]
}
```

## Skill 设计

新增 Skill：`.agents/skills/engineering-standards/`。

Skill 本体只描述取用流程，不包含规范正文，不复制索引内容。

文件职责：

| 文件 | 职责 |
| --- | --- |
| `SKILL.md` | 触发条件、硬约束、最短取用流程。 |
| `retrieval-flow.md` | 详细任务推断、路径推断、分片读取流程。 |
| `query-vocabulary.md` | 用户语言到 `roles`、`stacks`、`tags`、`doc_type` 的映射。 |
| `examples.md` | 典型开发任务的按需取用示例。 |

Skill 触发条件：

1. 用户要新增或修改代码、文档、契约、配置、CI。
2. 用户要评审代码或判断实现是否合规。
3. 用户显式询问规范、约定、命名、注释、API、错误码、认证、gRPC、AsyncAPI、Git 提交等内容。

Skill 读取优先级：

1. 从目标路径推断角色、技术栈和主题。
2. 从任务语义推断变更类型。
3. 从显式关键词补充标签。
4. 必要时读取 `query-vocabulary.md`。
5. 只读取 L0 总目录、命中的 L1 分片、候选文档的 L2 章节索引和最终命中行段。

示例任务：

```text
在 backend/dotnet/BuildingBlocks/src/Tw.Caching 里实现缓存失效功能
```

推断结果：

```text
roles: [backend]
stacks: [dotnet]
tags: [framework, caching, configuration, logging, error-handling]
doc_type: [rule]
```

允许读取：

```text
docs/standards/index.generated.json
docs/standards/_index/by-stack/dotnet.generated.json
docs/standards/_index/by-role/backend.generated.json
docs/standards/_index/by-tag/caching.generated.json
docs/standards/_index/by-tag/framework.generated.json
docs/standards/_index/sections/<candidate>.generated.json
docs/standards/<candidate>.md 的命中行段
```

禁止：

1. 一次性读取 `docs/standards/**/*.md`。
2. 一次性读取所有 `_index/**/*.json`。
3. 把规范正文写进 `SKILL.md`。
4. 把所有分片索引合并后返回。
5. 用无来源的“根据规范”表述。

Skill 输出规范：

1. 每个规范性结论必须引用规范 ID。
2. 引用必须包含 anchor，可选 region。
3. 必须说明规范版本和文件路径。
4. 若命中 `deprecated` 或 `superseded` 规范，必须提示状态并优先查找替代规范。

## 工具链改造

继续使用 `tools/standards/standards.py`，命令保持不变。

### `validate`

校验：

1. v2 front matter 必填字段。
2. `doc_type`、`status`、`roles`、`stacks` 的取值。
3. `summary`、`owners`、`tags` 不为空。
4. `review_after` 日期格式。
5. `superseded` 状态必须填写 `superseded_by`。
6. 旧字段 `applies_to` 出现即失败。
7. anchor 与 region 命名合法且 region 成对。

### `generate-index`

生成：

1. L0 轻量总目录。
2. L1 角色分片。
3. L1 技术栈分片。
4. L1 规范类型分片。
5. L1 标签分片。
6. L2 单篇章节索引。

### `check`

执行所有校验，并重新计算索引与仓库文件比较。任一索引缺失、过期、手工改坏或漂移都失败。

### `check-links`

继续校验 Markdown 相对链接，并新增规范引用校验：

```text
standards.authoring#rules
rules.api-response-shape#rules:no-null
```

### `check-machine-rules`

继续使用当前 `machine_rules` 校验模型：

1. 文档声明的规则文件必须存在。
2. 规则文件必须声明 `id`、`standard_id`、`type`、`version`。
3. `standard_id` 必须等于规范 ID。
4. active 规范不得绑定 deprecated 规则。

## CI 设计

GitHub Actions 继续执行：

```yaml
run: python tools/standards/standards.py check
```

CI 不需要理解 v2 索引细节。所有复杂度集中在工具脚本内。

失败场景包括：

1. 规范仍使用旧字段。
2. v2 必填字段缺失。
3. anchor 或 region 不合法。
4. 分片索引缺失。
5. 单篇章节索引缺失。
6. 修改规范后未重新生成索引。
7. 文档引用了不存在的规范、anchor 或 region。
8. 机器规则绑定不一致。

## 完整规范内容范围

本次范围包含蓝图中的完整规范清单：66 篇规则 / 参考 / 流程规范，加起步 ADR。

所有规范必须是可评审的完整正文，不接受只有标题和占位语的骨架。每篇规范至少包含：

1. v2 front matter。
2. 稳定 anchor。
3. 明确适用范围。
4. 可执行规则或参考条目。
5. 正例、反例或检查清单，按 doc_type 适用。
6. 与相关规范的关系。
7. 变更记录或 ADR 状态记录。

### 规则类

工程基础：

1. `rules.git-commit` Git 提交规范。
2. `rules.git-branching` 分支与发布管理。
3. `rules.code-review` PR 与 Code Review。
4. `rules.naming-common` 命名规范-通用。
5. `rules.naming-java` Java 命名规范。
6. `rules.naming-dotnet` .NET Core 命名规范。
7. `rules.naming-python` Python 命名规范。
8. `rules.naming-ts-vue` TypeScript & Vue 命名规范。
9. `rules.naming-uniapp` uni-app 命名规范。
10. `rules.comments-common` 注释规范-通用。
11. `rules.comments-java` Java 注释模板。
12. `rules.comments-dotnet` .NET XML 文档注释。
13. `rules.comments-python` Python docstring 模板。
14. `rules.comments-ts` TS / JSDoc 注释模板。
15. `rules.editorconfig` 编码、换行与缩进。
16. `rules.repo-layout` 仓库与目录结构。

通信契约：

1. `rules.api-rest-design` REST API 设计规范。
2. `rules.api-response-shape` API 统一响应结构。
3. `rules.api-error-response` API 错误响应。
4. `rules.grpc-proto-style` gRPC proto 风格。
5. `rules.grpc-versioning` gRPC 版本化与兼容。
6. `rules.asyncapi-conventions` AsyncAPI 消息契约。
7. `rules.messaging-patterns` 消息可靠性。
8. `rules.data-formats` 数据格式规范。

安全与合规：

1. `rules.auth-oauth-oidc` OAuth + OIDC 认证授权。
2. `rules.input-validation` 输入校验与防注入。
3. `rules.pii-handling` PII 与敏感信息。
4. `rules.secrets-management` 密钥与凭据。
5. `rules.dependency-policy` 依赖与第三方组件。

可观测性与运维：

1. `rules.logging` 日志规范。
2. `rules.metrics` 指标规范。
3. `rules.tracing` 链路追踪。
4. `rules.health-checks` 健康检查与探针。

数据与存储：

1. `rules.db-naming` 数据库命名规范。
2. `rules.db-migration` 数据库迁移规范。
3. `rules.db-audit-fields` 通用审计字段。
4. `rules.cache-conventions` 缓存规范。

后端工程通用：

1. `rules.configuration` 配置与环境管理。
2. `rules.error-handling` 错误处理与异常分类。
3. `rules.idempotency` 幂等性与并发控制。
4. `rules.resilience` 限流、熔断与降级。
5. `rules.slo` 性能与 SLO。

前端专项：

1. `rules.fe-vue-ts-project` Vue + TS 工程规范。
2. `rules.fe-typescript` TypeScript 类型规范。
3. `rules.fe-uniapp-cross` uni-app 跨端规范。
4. `rules.fe-i18n` 前端国际化与本地化。
5. `rules.fe-styles` 样式与设计令牌。
6. `rules.fe-compat` 浏览器与设备兼容矩阵。
7. `rules.fe-a11y` 可访问性 A11y。

测试：

1. `rules.test-strategy` 测试分层策略。
2. `rules.test-coverage` 覆盖率目标。
3. `rules.test-data-mock` 测试数据与 Mock。
4. `rules.contract-testing` 契约测试。

CI/CD 与部署：

1. `rules.ci-pipeline` CI 流水线规范。
2. `rules.container-image` 容器与镜像规范。
3. `rules.k8s-resource-naming` K8s 资源命名与 labels。
4. `rules.env-strategy` 环境策略。
5. `rules.deploy-rollback` 部署与回滚。

### 参考类

1. `references.error-codes` 错误码目录。
2. `references.http-status` HTTP 状态码语义对照。
3. `references.glossary` 术语词典。
4. `references.datetime` 时区与日期时间格式参考。

### 流程类

1. `processes.rfc-flow` RFC 流程与模板。
2. `processes.change-governance` 变更治理与版本策略。
3. `processes.doc-authoring` 规范文档撰写与协作流程。
4. `processes.dependency-onboarding` 第三方组件准入流程。

### 起步 ADR

1. `decisions.0001-adopt-standards-blueprint` 采纳规范体系蓝图。
2. `decisions.0002-rest-response-shape-no-null` REST 响应禁止 null。
3. `decisions.0003-section-level-retrieval` 采用章节级与片段级取用。

## 分阶段落地

### M1：平台底座 v2

1. 迁移 `standard-authoring.md` 到 v2 front matter。
2. 改造 `standards.py` 的 v2 校验与分片索引生成。
3. 新增 `_meta/` 契约文档。
4. 新增薄 Skill。
5. 更新 README 和模板。
6. 保证 `check` 通过，并验证旧字段失败、索引漂移失败、删除分片失败。

### M2：冷启动必备规范

先完成以下规范正文：

1. `rules.git-commit`
2. `rules.code-review`
3. `rules.naming-common`
4. `rules.api-rest-design`
5. `rules.api-response-shape`
6. `rules.api-error-response`
7. `rules.auth-oauth-oidc`
8. `references.error-codes`
9. `processes.rfc-flow`
10. `processes.change-governance`

### M3：多语言与多端规范

补齐 Java、.NET Core、Python、TypeScript + Vue、uni-app 的命名、注释和工程规范。

### M4：通信契约与微服务规范

补齐 gRPC、AsyncAPI、消息可靠性、数据格式、契约测试。

### M5：质量、安全、运维、数据与底层框架规范

补齐日志、指标、链路追踪、健康检查、配置、错误处理、幂等、韧性、SLO、安全、依赖、数据库、缓存、CI/CD、容器、K8s、部署回滚等规范。

## 错误处理

工具错误继续使用统一格式：

```text
ERROR [standard-metadata] docs/standards/rules/api-response-shape.md: missing required field "summary"
ERROR [standard-index] docs/standards/_index/by-stack/dotnet.generated.json: shard is out of date; run generate-index
WARN  [standard-review] docs/standards/rules/logging.md: review_after is in the past
```

约定：

1. `ERROR` 导致命令和 CI 失败。
2. `WARN` 首阶段不导致 CI 失败。
3. 每条错误必须包含类别、路径和可执行修复线索。

## 测试策略

1. 正常运行 `python tools/standards/standards.py check` 应通过。
2. 删除 `summary` 后 `validate` 应失败。
3. 添加旧字段 `applies_to` 后 `validate` 应失败。
4. 删除任一 `_index` 分片后 `check` 应失败。
5. 修改规范正文但不重新生成索引后 `check` 应失败。
6. 引用不存在的 `standard-id#anchor` 后 `check-links` 应失败。
7. 删除绑定的机器规则文件后 `check-machine-rules` 应失败。
8. Skill 示例场景必须证明读取路径只经过 L0、必要 L1、必要 L2 和命中 Markdown 行段。

## 验收标准

整体完成时必须满足：

1. 当前首版底座能力被保留。
2. 所有规范文档均使用 v2 front matter。
3. `index.generated.json` 为轻量总目录，不包含章节详情。
4. L1 分片索引完整生成。
5. L2 单篇章节索引完整生成。
6. 薄 Skill 存在，且说明按任务和路径推断规范。
7. 完整规范清单全部落地为非占位正文。
8. `python tools/standards/standards.py check` 通过。
9. GitHub Actions 仍通过同一命令执行校验。
10. 至少一个开发任务示例能证明不加载全部规范或全部索引。

## 风险与约束

| 风险 | 影响 | 缓解 |
| --- | --- | --- |
| 一次补齐全部规范内容工作量大 | 实施周期长，容易混入质量不均的内容 | 按 M1-M5 分阶段，每批都可独立验收。 |
| 分片索引数量多 | 生成和校验逻辑复杂 | 统一由 `standards.py` 管理，CI 只运行 `check`。 |
| 直接不兼容升级 | 旧文档会立即失败 | 当前规范数量少，适合硬切换。 |
| Skill 推断任务不准 | 可能漏读相关规范 | 使用路径、任务语义、显式关键词三类信号，并允许必要时读取词典或询问目标位置。 |
| 完整规范内容可能与后续业务实践冲突 | 规范需要持续演进 | 使用 `status`、`version`、`review_after`、RFC / ADR 流程治理。 |

## 后续动作

设计文档确认后，进入 implementation plan。计划应按 M1-M5 拆分，优先实现 M1，再批量补齐规范内容，避免工具链和规范正文同时失控。
