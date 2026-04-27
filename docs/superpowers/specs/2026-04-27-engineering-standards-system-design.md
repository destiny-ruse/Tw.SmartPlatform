# 工程规范体系建设设计

- 日期：2026-04-27
- 状态：已确认设计，待实现
- 范围：业务无关的工程规范文档体系、AI 检索契约、治理流程、机器规则绑定、工具链与 CI 校验
- 方案：Python 标准库脚本 + GitHub Actions

## 背景

公司即将启动一个全新的多语言、多微服务、前后端分离 B/S 项目。当前仓库已经具备 `backend/`、`frontend/`、`contracts/`、`deploy/`、`docs/`、`tools/` 等基础目录，`docs/standards/` 和 `tools/` 可作为规范体系与工具链的承载位置，但尚未形成统一的规范元数据、索引、治理流程、机器规则绑定和 CI 校验。

本设计先建设一套业务无关的统一工程规范底座，让团队在业务开发前获得可读、可查、可执行、可校验的研发治理基础。

## 目标

1. 建立统一的规范文档结构、目录体系、元数据和索引方式。
2. 支持 AI 和自动化工具按需读取指定规范、章节或片段，避免一次性加载全部规范。
3. 对可工具化执行的规范提供文档和机器规则双形态绑定。
4. 建立规范生命周期管理，包括草稿、生效、弃用、被替代、版本、复核、RFC 和 ADR。
5. 通过本地工具和 CI 校验规范质量，防止文档结构、索引、引用和机器规则漂移。

## 非目标

1. 首版不制定所有语言和框架的完整编码细则。
2. 首版不强制引入 Node、.NET 或第三方 Python 包作为规范工具链依赖。
3. 首版不实现复杂全文搜索服务，仅提供静态索引和可定位锚点。
4. 首版不绑定具体业务域模型、数据库设计或微服务拆分策略。

## 推荐方案

采用 Python 标准库脚本实现规范生成、索引生成和校验，使用 GitHub Actions 执行 CI。这个方案对多语言仓库侵入小，依赖少，能在新项目早期稳定落地。

备选方案包括：

1. Node/TypeScript 工具链：生态丰富，适合前端团队扩展，但会过早引入运行时依赖。
2. 仅文档模板不接 CI：启动成本最低，但无法满足“可校验”和“故意破坏时 CI 拦截”的验收要求。

## 目录设计

```text
docs/
  standards/
    README.md
    index.generated.json
    meta.schema.json
    engineering/
      standard-authoring.md
    examples/
      standard-authoring.example.md
  rfcs/
    README.md
    template.md
  adrs/
    README.md
    template.md
tools/
  standards/
    README.md
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

说明：

1. `docs/standards/` 是人类可读规范的唯一入口。
2. `index.generated.json` 是 AI 和自动化工具的主索引，由脚本生成，不手工维护。
3. `meta.schema.json` 描述规范元数据契约，供人和脚本共同参考。
4. `tools/standards/standards.py` 使用 Python 标准库提供生成、索引、校验命令。
5. `tools/standards/rules/` 存放与规范绑定的机器可读规则示例。
6. `docs/rfcs/` 和 `docs/adrs/` 承载规范演进和技术决策流程。

## 规范文档契约

每篇规范使用 Markdown，并在文件头部声明 YAML-like front matter。为避免引入第三方 YAML 解析依赖，首版只支持简单键值、字符串数组和对象数组三类结构，由工具脚本做受限解析。

示例字段：

```yaml
---
id: standards.authoring
title: 规范撰写规范
status: active
version: 1.0.0
owners: [architecture-team]
applies_to: [docs, standards, ai]
machine_rules:
  - id: standard-authoring.rules
    path: tools/standards/rules/standard-authoring.rules.json
    type: schema
supersedes: []
superseded_by:
review_after: 2026-10-27
---
```

必填字段：

1. `id`：全仓唯一，使用点分命名。
2. `title`：规范标题。
3. `status`：`draft`、`active`、`deprecated`、`superseded` 之一。
4. `version`：单篇规范版本，使用语义化版本。
5. `owners`：责任团队或角色。
6. `applies_to`：适用范围标签。
7. `review_after`：下次复核日期。

可选字段：

1. `machine_rules`：绑定机器规则。
2. `supersedes`：被当前规范替代的规范 ID。
3. `superseded_by`：替代当前规范的规范 ID。

## AI 检索契约

AI 和自动化工具只依赖生成索引，不把规范内容写入 Skill 或提示词。索引包含：

1. 规范 ID、标题、状态、版本、路径、责任方和适用范围。
2. 每个二级及三级标题的锚点、标题、起止行号和片段摘要。
3. 机器规则绑定列表。
4. 替代关系和复核日期。
5. 索引生成时间和生成器版本。

AI 使用方式：

1. 先读取 `docs/standards/index.generated.json`。
2. 根据 `id`、`applies_to`、`status` 或章节标题定位候选规范。
3. 只读取命中的 Markdown 文件和具体章节行段。
4. 回答或执行时引用规范 ID、章节标题和文件路径。

这样可以满足“改文档不需要改 Skill”“不一次性加载全部规范”“引用内容可定位、可追溯、可校验”。

## 机器规则绑定

每篇规范可以绑定零个或多个机器规则。首版提供一种通用 JSON 规则描述格式，用于证明双形态能力可跑通；后续可以扩展为 ESLint preset、StyleCop 配置、OpenAPI lint、Protobuf lint、Markdown lint 等具体工具配置。

绑定校验规则：

1. 文档中声明的 `machine_rules[].path` 必须存在。
2. 规则文件必须声明 `id`、`standard_id`、`type`、`version`。
3. `standard_id` 必须等于规范文档的 `id`。
4. 生效状态规范绑定的规则不得处于弃用状态。

## 治理流程

规范生命周期：

1. `draft`：草稿，可讨论，不强制执行。
2. `active`：生效，工具和 CI 可以引用。
3. `deprecated`：不推荐新增使用，但历史内容仍可追溯。
4. `superseded`：已被明确规范替代，必须填写 `superseded_by`。

RFC 流程：

1. 新增或重大修改规范时，在 `docs/rfcs/` 新建 RFC。
2. RFC 说明问题、目标、备选方案、推荐方案、影响范围和迁移方式。
3. RFC 通过后再更新规范文档和机器规则。

ADR 流程：

1. 对长期有效的工程决策，在 `docs/adrs/` 新建 ADR。
2. ADR 记录上下文、决策、结果和替代方案。
3. ADR 与规范通过链接相互引用。

复核机制：

1. 每篇规范必须有 `review_after`。
2. 校验脚本对过期复核日期输出警告。
3. CI 首版对复核过期不失败，避免因日期导致紧急阻塞；后续可切换为失败。

## 工具设计

`tools/standards/standards.py` 提供以下命令：

```text
python tools/standards/standards.py new-standard --id standards.authoring --title 规范撰写规范
python tools/standards/standards.py generate-index
python tools/standards/standards.py validate
python tools/standards/standards.py check-links
python tools/standards/standards.py check-machine-rules
```

首版也可以提供聚合命令：

```text
python tools/standards/standards.py check
```

职责划分：

1. `new-standard`：根据模板创建规范骨架。
2. `generate-index`：扫描规范文档并生成 `index.generated.json`。
3. `validate`：校验 front matter、必填字段、状态、版本、ID 唯一性和复核日期格式。
4. `check-links`：校验规范、RFC、ADR 中的相对链接和锚点引用。
5. `check-machine-rules`：校验文档和规则文件的双向绑定。
6. `check`：执行全部校验，并确认索引没有漂移。

## 数据流

1. 作者运行 `new-standard` 创建规范骨架。
2. 作者补全文档内容、元数据和机器规则绑定。
3. 作者运行 `generate-index` 更新索引。
4. 作者运行 `check` 本地验证。
5. PR 触发 GitHub Actions。
6. CI 重新生成临时索引并与仓库中的 `index.generated.json` 比较。
7. CI 执行元数据、链接和机器规则绑定校验。
8. 校验通过后，规范可被团队和 AI 作为可信来源使用。

## 错误处理

脚本错误输出遵循一致格式：

```text
ERROR [standard-metadata] docs/standards/engineering/standard-authoring.md: missing required field "owners"
WARN  [standard-review] docs/standards/engineering/standard-authoring.md: review_after is in the past
```

约定：

1. `ERROR` 会导致命令和 CI 失败。
2. `WARN` 不导致首版 CI 失败。
3. 每条错误必须包含类别、文件路径和可执行修复线索。
4. 索引漂移错误提示开发者运行 `generate-index`。

## CI 设计

新增 `.github/workflows/standards.yml`：

1. 在 push 和 pull request 时运行。
2. 使用 `actions/setup-python` 安装 Python 3.x。
3. 执行 `python tools/standards/standards.py check`。
4. 当元数据缺失、索引漂移、引用失效或机器规则绑定不一致时失败。

由于当前仓库尚未存在 `.github/`，实现时会新增该目录。

## 示例闭环

首版至少提供一篇示例规范：`docs/standards/engineering/standard-authoring.md`。

该规范说明如何撰写规范文档，包括标题结构、元数据填写、章节粒度、AI 引用要求、机器规则绑定和变更记录。它绑定 `tools/standards/rules/standard-authoring.rules.json`，用于跑通双形态校验。

验收演示：

1. 正常运行 `python tools/standards/standards.py check` 通过。
2. 删除示例规范的必填元数据后，`check` 失败。
3. 手工改动规范标题但不更新索引后，`check` 失败。
4. 删除绑定规则文件后，`check-machine-rules` 失败。
5. RFC 和 ADR 模板可以被复制并填写。

## 测试策略

首版测试以脚本自校验和可复现实例为主：

1. 用示例规范验证元数据解析、索引生成和机器规则绑定。
2. 用工具命令验证模板生成路径和 ID 命名。
3. 用 CI 验证索引漂移和引用错误能被拦截。
4. 在实现中优先保持脚本函数小而明确，便于后续补充单元测试。

## 验收映射

1. 仓库、工具链、CI 可正常运行：通过 Python 脚本和 GitHub Actions 实现。
2. AI 可精准读取指定规范章节或片段：通过 `index.generated.json` 的章节路径和行号实现。
3. 至少 1 篇示例规范跑通全流程：通过 `standard-authoring.md` 和绑定规则实现。
4. 故意破坏结构或索引时 CI 能拦截：通过 `check` 聚合校验实现。
5. RFC / ADR 流程可实际使用：通过目录、README 和模板实现。

## 实现顺序

1. 新增规范目录结构、README、模板、示例规范和 RFC / ADR 模板。
2. 新增 `standards.py`，实现解析、生成、校验和聚合命令。
3. 生成首版 `index.generated.json`。
4. 新增 GitHub Actions workflow。
5. 本地运行 `check` 验证通过。
6. 手动破坏一个副本或临时场景，确认关键错误能被脚本识别。

## 风险与约束

1. 受限 front matter 解析能力必须在文档中说明，避免作者使用复杂 YAML 语法。
2. 首版 JSON 规则格式只是通用绑定契约，不替代各语言真实 lint 工具。
3. 复核过期首版只警告，后续是否改为 CI 失败需要团队治理决策。
4. 目录结构应保持轻量，避免在业务开发前引入过多流程负担。
