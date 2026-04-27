# Knowledge Compiler 方案 A 全量调整设计

- 日期：2026-04-28
- 状态：已确认设计，待实施计划
- 范围：基于对原始设计（2026-04-27）的对齐分析，补全所有遗漏功能、修复已知设计问题、建立跨栈目录约定和新栈持续记忆协议
- 受众：架构、后端、前端、测试、运维、AI 编程工具使用者
- 前置文档：`docs/superpowers/specs/2026-04-27-knowledge-compiler-memory-graph-design.md`

## 背景

对原始设计与当前实现进行对齐分析后，发现以下主要问题：

1. **确定性主链路不完整**：设计承诺的 `scan` 命令（无需 git、扫描当前整个仓库）未实现，主链路实际只有 `generate + check + check-drift`，缺少自动从现有代码全量发现图谱缺口的能力。
2. **跨栈覆盖缺失**：`taxonomy.yaml` 只有 dotnet 和部分前端规则，Java、Python 无路径规则，前端 5 个已存在的 app 完全不在记忆中。
3. **contract 节点系缺失**：`graph/contracts/` 目录不存在，无 contract 图谱节点，`integration.contract` 字段也未做引用校验。
4. **check-drift 对已有 contract 变更有盲区**：已有节点的 contract 文件发生变更时静默跳过，不发出任何诊断。
5. **目录和模板不完整**：`changes/`、`proposals/`、`rules/` 目录及 contract/integration/decision 模板均未创建。
6. **持续记忆无操作规范**：新栈（如 Go）接入时，没有标准的步骤说明，taxonomy 可能在代码已落地后才更新，导致 check-drift 漏检首次提交。

## 目标

1. 建立全仓跨栈的目录约定，覆盖 dotnet、java、python、vue-ts、uniapp，并为未来 go 等栈预留扩展路径。
2. 补全 `taxonomy.yaml` 的全栈路径规则，使 `scan` 和 `check-drift` 对所有栈生效。
3. 实现 `scan` 命令，使开发者无需 git 引用即可发现全量图谱缺口。
4. 实现 `init` 命令，简化新图谱节点的创建。
5. 实现 `diff` 命令，提供结构化的变更概览，与 `check-drift` 形成互补。
6. 修复 `check-drift` 对已有 contract 变更的盲区。
7. 补全 `integration.contract` 字段的引用校验。
8. 创建所有缺失目录并补全初始图谱种子（前端 app 节点、proto contract 节点）。
9. 明确 `changes/` 和 `proposals/` 的写入机制。
10. 建立新栈接入协议，确保持续记忆的操作规范。
11. 统一 `memory.schema.json` 与 Python 校验的权威关系。

## 非目标

1. 解析 .csproj / pom.xml / pyproject.toml 等项目文件内部结构（只按目录规则做路径匹配）。
2. 引入外部 JSON Schema 验证库（保持 Python 标准库约束）。
3. 实现完整的 semantic diff（理解代码语义变化）。
4. 自动向 `changes/` 写入历史记录（只在携带 `--save` 参数时写入）。

## 跨栈目录约定

本约定是本次设计的基础，所有 taxonomy 路径规则均以此为准。

### 后端

```
backend/
  dotnet/
    Services/{Name}/              微服务（PascalCase，保持现有约定）
    BuildingBlocks/src/{Name}/    框架级包（PascalCase，保持现有约定）
  java/
    services/{name}/              微服务（Spring Boot，小写）
    packages/{name}/              框架级包（starter / commons，小写）
  python/
    services/{name}/              微服务（小写）
    packages/{name}/              框架级包（小写）
  go/（预留，暂无代码）
    services/{name}/
    packages/{name}/
```

**说明**：dotnet 保持 PascalCase 不变以避免改动成本。Java、Python、Go 及后续新栈统一使用小写加复数（`services/`、`packages/`），框架级包统一称 `packages`，屏蔽各栈生态的命名差异（Spring Boot starter、Python library、Go pkg 均映射到 `packages`）。

### 前端

```
frontend/
  apps/{name}/      前端应用（vue-ts 或 uniapp，已有 5 个）
  packages/{name}/  共享前端包（已有 taxonomy 规则，目录空）
```

### 契约

```
contracts/
  protos/**/*.proto     gRPC 契约（已有 taxonomy 规则）
  openapi/**/*.yaml     OpenAPI 契约（补齐规则）
```

## taxonomy.yaml 全栈规则设计

在现有规则基础上，补充以下内容：

### path_rules 补充

```yaml
# Java 微服务
- pattern: backend/java/services/*/**
  kind: module
  module_type: microservice
  stack: java

# Java 框架包
- pattern: backend/java/packages/*/**
  kind: module
  module_type: framework-package
  stack: java

# Python 微服务
- pattern: backend/python/services/*/**
  kind: module
  module_type: microservice
  stack: python

# Python 框架包
- pattern: backend/python/packages/*/**
  kind: module
  module_type: framework-package
  stack: python

# 前端应用（补齐）
- pattern: frontend/apps/*/**
  kind: module
  module_type: frontend-app
  stack: vue-ts

# OpenAPI 契约（补齐）
- pattern: contracts/openapi/**/*.yaml
  kind: contract
  contract_type: openapi
```

### valid_stacks 补充

```yaml
valid_stacks:
  - dotnet
  - java
  - python
  - vue-ts
  - uniapp
  - go        # 预留
```

### valid_module_types 新增字段

```yaml
valid_module_types:
  - microservice
  - building-block
  - framework-package
  - frontend-app
  - frontend-package
```

**knowledge.py 同步变更**：需在 `collect_validation_messages` 中读取 `taxonomy.get("valid_module_types")`，对 module 节点的 `module_type` 字段做枚举校验，报错码 `knowledge.invalid-module-type`。

同时，`path_rule_diagnostic` 当前只对 `module_type == "building-block"` 发 WARN，对其他类型发 ERROR。需扩展为：`building-block` 和 `framework-package` 两种类型都发 WARN（因为框架包的能力声明是可选的），其余 module 类型发 ERROR。

### query_aliases 补充

```yaml
query_aliases:
  Spring Boot:
    - java
    - spring
    - packages
  前端:
    - vue
    - frontend
    - apps
```

### diagnostics 补充

```yaml
diagnostics:
  knowledge.contract-outdated:
    severity: warning
    message_zh: 契约文件发生变更，对应 contract 图谱节点可能未同步更新。
    suggestion_zh: 检查 contract 节点版本、兼容性说明和变更证据是否已更新。
```

## knowledge.py 新命令设计

### `scan` 命令

```powershell
python tools/knowledge/knowledge.py scan
```

**行为**：遍历当前仓库文件系统，对所有匹配 `taxonomy.yaml path_rules` 的路径做分类，与现有图谱比对，以中文诊断输出全量缺口。

**与 `check-drift` 的区别**：

| | `scan` | `check-drift` |
|---|---|---|
| 路径来源 | 当前文件系统全量遍历 | `git diff --name-status` |
| 需要 git | 否 | 是 |
| 典型用途 | 新栈接入后全面审查、首次图谱建立 | CI 增量检测、PR 前检查 |

**实现策略**：复用现有 `detect_drift_from_paths()`，只替换路径来源——文件系统遍历收集所有文件路径，传入现有函数即可。不需要新增核心逻辑。

**输出示例**：

```
错误 [knowledge.missing-module]
位置: frontend/apps/tw.web.client
说明: 前端应用目录尚未声明 module 图谱节点。
建议: 新增对应 module 图谱节点，或在 taxonomy.yaml 中声明忽略规则。
```

### `init` 命令

```powershell
python tools/knowledge/knowledge.py init --kind capability --id backend.capability.encryption
python tools/knowledge/knowledge.py init --kind module --id backend.java.services.user
python tools/knowledge/knowledge.py init --kind contract --id contracts.openapi.authentication
```

**行为**：

1. 从 `tools/knowledge/templates/{kind}.yaml` 读取模板。
2. 填充 `id`、`declared_in`（基于 kind 和 id 计算目标路径）、`created_at` / `updated_at`（今日日期）。
3. 写入 `docs/knowledge/graph/{kind}s/{id}.yaml`。
4. 若目标文件已存在，报错退出，不覆盖。

**新增模板**（`tools/knowledge/templates/`）：

- `contract.yaml`
- `integration.yaml`
- `decision.yaml`

### `diff` 命令

```powershell
python tools/knowledge/knowledge.py diff --from main --to HEAD
```

**行为**：调用 `git diff --name-status`，按 taxonomy kind 分类变更路径，以中文分组输出"什么变了"，不做漂移诊断判断。

**与 `check-drift` 的区别**：`diff` 只描述变化（描述层），`check-drift` 做漂移判断和诊断（诊断层）。

**输出示例**：

```
变更概览（main → HEAD）

module [dotnet microservice]
  新增: backend/dotnet/Services/User

contract [grpc]
  修改: contracts/protos/authentication/v1/authentication.proto

其他文件（不在 taxonomy 规则内）
  修改: docs/knowledge/graph/capabilities/backend.capability.authentication.yaml
```

## Bug 修复

### B1：check-drift 契约变更盲区

**问题**：已有 contract 图谱节点的文件在 git diff 中发生变更时，当前代码直接 `continue` 跳过，不发出任何诊断。

**修复**：当变更路径对应一个已有 contract 节点时，发出 `knowledge.contract-outdated`（WARN 级别），提示开发者检查节点是否需要更新。仅当路径既无 contract 节点又在 path_rules 内时，才发出原有的 `knowledge.contract-drift`（ERROR 级别）。

**新旧行为对比**：

| 情况 | 修复前 | 修复后 |
|---|---|---|
| contract 文件变更，已有对应节点 | 静默跳过 | WARN contract-outdated |
| contract 文件新增，无对应节点 | ERROR contract-drift | ERROR contract-drift（不变） |

### B2：integration.contract 引用校验

**问题**：`validate_graph_references` 未校验 `integration.contract` 字段，无效引用静默通过。

**修复**：在 `validate_graph_references` 中增加：若 integration 节点有 `contract` 字段，则其值必须对应已声明的 contract 节点 id，否则报 `knowledge.dangling-reference` 错误。

## 目录结构扩充

新增以下目录和文件：

```
docs/knowledge/
  graph/
    contracts/            新建目录，存放 contract 图谱节点
  changes/
    2026/                 新建目录，存放 check-drift --save 的历史产物
  proposals/              新建目录，供 tw-knowledge-maintenance skill-propose 模式使用

tools/knowledge/
  templates/
    contract.yaml         新增
    integration.yaml      新增
    decision.yaml         新增
  rules/
    README.md             新增（诊断规则文档目录）
    knowledge-diagnostics.md  新增（各诊断码的含义、触发条件和处理建议）
```

## 初始图谱种子补全

### 前端 app 节点（立即补全，5 个）

| id | name | path |
|---|---|---|
| frontend.apps.tw-app-owner | 业主端 App | frontend/apps/tw.app.owner |
| frontend.apps.tw-app-staff | 员工端 App | frontend/apps/tw.app.staff |
| frontend.apps.tw-web-client | 客户 Web 端 | frontend/apps/tw.web.client |
| frontend.apps.tw-web-ops | 运营 Web 端 | frontend/apps/tw.web.ops |
| frontend.apps.tw-web-portal | 门户 Web 端 | frontend/apps/tw.web.portal |

所有节点：`status: draft`，`owners: [frontend]`，`stack: vue-ts`（具体是否为 uniapp 在节点 tags 中区分）。

### Proto contract 节点

运行 `scan` 后确认 `contracts/protos/` 下实际文件数量，再逐一用 `init` 命令创建 contract 节点。

### OpenAPI contract 节点

`contracts/openapi/` 目录当前为空，暂不创建节点。首个 OpenAPI 契约落地时，由 `check-drift` 自动发现并提示。

### Java / Python 节点

两个目录当前只有 README，无实际服务。暂不创建节点，等代码落地后由 `scan` 或 `check-drift` 自动发现。

## changes/ 和 proposals/ 机制

### changes/

由 `check-drift` 携带 `--save` 参数时写入，记录历史漂移分析产物，方便复盘。

```powershell
python tools/knowledge/knowledge.py check-drift --from main --to HEAD --save
# 写入 docs/knowledge/changes/2026/2026-04-28-main-HEAD.json
```

文件格式与 `diagnostics.generated.json` 相同，增加 `from_ref` 和 `to_ref` 字段。

不携带 `--save` 时行为不变（只输出到 stdout）。

### proposals/

由 `tw-knowledge-maintenance` Skill 在 **skill-propose 模式**下写入候选图谱 YAML，供人工审核后手工复制到 `graph/`。

```
docs/knowledge/proposals/
  2026-04-28-missing-frontend-apps.yaml   候选节点文件
  2026-04-28-missing-frontend-apps.md     中文说明和审核要点
```

正式图谱不自动从 proposals 合入，需开发者显式操作。

## schema / 校验统一方案

**结论**：`memory.schema.json` 降为文档参考，Python 校验逻辑为唯一权威。

**理由**：引入 JSON Schema 验证库会打破"Python 标准库"约束，且双轨维护容易漂移。

**操作**：

1. 在 `memory.schema.json` 文件顶部注释中写明："本文件仅作文档参考，实际校验由 `tools/knowledge/knowledge.py` 执行，两者以 Python 实现为准。"
2. 在 `tools/knowledge/rules/knowledge-diagnostics.md` 中列出所有诊断码及其对应的 schema 字段，保持文档同步。

## 新栈接入协议（持续记忆的操作规范）

本协议是"后续有 Go 怎么办"的设计答案，写入 `docs/knowledge/README.md`。

### 核心原则

**taxonomy.yaml 的变更必须先于新栈代码提交。** 若代码先落地，`check-drift` 在首次 PR 时将无规则可匹配，导致首次提交漏检。

### 标准步骤

**第零步：约定目录结构**（在第一行代码之前）

确认新栈的服务目录和框架包目录，遵循 `backend/{stack}/services/{name}/` 和 `backend/{stack}/packages/{name}/` 约定，记录到团队 ADR 或 README。

**第一步：更新 taxonomy.yaml**

```yaml
# valid_stacks 添加新栈名
valid_stacks:
  - go

# path_rules 添加路径规则
path_rules:
  - pattern: backend/go/services/*/**
    kind: module
    module_type: microservice
    stack: go
  - pattern: backend/go/packages/*/**
    kind: module
    module_type: framework-package
    stack: go
```

**第二步：验证 taxonomy 变更**

```powershell
python tools/knowledge/knowledge.py scan
# 此时目录为空，应无诊断输出
python tools/knowledge/knowledge.py check
```

**第三步：提交 taxonomy 变更**

taxonomy 变更单独提交，提交信息说明引入新栈的原因。

**第四步：新栈代码落地后，补全图谱节点**

```powershell
# 发现全量缺口
python tools/knowledge/knowledge.py scan

# 逐一初始化节点
python tools/knowledge/knowledge.py init --kind module --id backend.go.services.xxx

# 编辑节点内容（填写 summary、provides、depends_on 等）

# 验证并生成索引
python tools/knowledge/knowledge.py generate
python tools/knowledge/knowledge.py check
```

**第五步：后续由 CI 自动兜底**

CI 在每次 PR 运行 `check-drift`，新增服务目录自动触发 `knowledge.missing-module` 诊断，无需人工记忆规则。

### 何时需要更新 taxonomy

| 场景 | 操作 |
|---|---|
| 新增语言 / 运行时 | 先更新 taxonomy，再提交代码 |
| 新增目录层级约定 | 先更新 taxonomy path_rules |
| 服务目录重命名 | 更新 taxonomy + 更新 module 图谱节点 path 字段 |
| 废弃某类目录 | 将对应 path_rules 移入注释或标注 deprecated |

## 验证方案

实施完成后应支持以下全量验证：

```powershell
# 全量扫描（无 git 依赖）
python tools/knowledge/knowledge.py scan

# 增量漂移检测
python tools/knowledge/knowledge.py check-drift --from main --to HEAD

# 变更概览
python tools/knowledge/knowledge.py diff --from main --to HEAD

# 生成索引
python tools/knowledge/knowledge.py generate

# 校验图谱和索引
python tools/knowledge/knowledge.py check

# 查询
python tools/knowledge/knowledge.py query --text "获取当前用户信息" --limit 5
python tools/knowledge/knowledge.py query --text "Spring Boot 框架包" --limit 5

# 初始化新节点
python tools/knowledge/knowledge.py init --kind module --id backend.java.services.user

# Skill 符号链接同步
python tools/knowledge/knowledge.py sync-skills --target claude
```

**sync-skills 范围扩展**：当前 `KNOWLEDGE_SKILLS` 硬编码 5 个技能名，`.claude/skills/` 中其他仓库技能（如 brainstorming、tw-engineering-standards）的符号链接由人工创建。为保证一致性，`sync-skills` 改为扫描 `.agents/skills/` 下所有子目录，统一为每个 skill 在 `.claude/skills/` 创建相对符号链接。无需在代码中维护技能名列表，新增技能自动被纳入同步范围。

验证通过标准：

1. `scan` 对 5 个前端 app 无漏报。
2. `check-drift` 对变更的 contract 文件发出 `contract-outdated` 警告。
3. `init` 生成的节点能通过 `check` 校验。
4. `diff` 输出按 kind 分类的变更列表。
5. `generate` 后所有 L0/L1/L2 索引与图谱一致。
6. `check` 对 integration.contract 无效引用发出 `dangling-reference` 错误。
7. 新增 Java/Python/Go 规则后，`scan` 能识别对应目录下的新增服务。

## 风险与处理

| 风险 | 处理方式 |
|---|---|
| `scan` 遍历大仓库性能问题 | 只遍历 taxonomy path_rules 涉及的根目录（backend/、frontend/、contracts/），不全量 rglob |
| taxonomy 规则配置错误导致误报 | `init` 写入前先 dry-run 路径计算；`scan` 输出中包含匹配的规则 pattern，便于排查 |
| frontend app 的 stack 标注不准（vue-ts vs uniapp）| 在 module 节点的 tags 字段区分，stack 统一为 vue-ts；后续如需分 stack 可在 taxonomy 增加 uniapp pattern |
| 新栈代码先于 taxonomy 提交 | CI check-drift 无法兜底；通过新栈接入协议文档和 PR checklist 防范 |
| proposals 候选文件长期未审核 | proposals/ 目录中的文件不影响生成索引；可在 CI 加检测提示超过 N 天未处理的候选文件 |

## 实施优先级

| 优先级 | 内容 | 理由 |
|---|---|---|
| P0 | B1 check-drift 修复 + B2 integration.contract 校验 | Bug，应立即修 |
| P0 | 前端 5 个 app 节点 + taxonomy 前端规则 | 已有实体无记忆 |
| P1 | `scan` 命令 | 确定性主链路最高价值缺口 |
| P1 | 跨栈 taxonomy 规则（java/python/openapi）| 在首个 java/python 服务落地前必须就绪 |
| P1 | contract/integration/decision 模板 + `graph/contracts/` 目录 | 完善节点系 |
| P2 | `init` 命令 + `diff` 命令 | 开发体验改善 |
| P2 | `changes/` `proposals/` 目录及机制 | 审计和 skill-propose 模式 |
| P3 | `tools/knowledge/rules/` 文档目录 | 可读性，不影响功能 |
| P3 | schema 注释统一说明 | 防漂移，低风险 |

## 验收标准

1. `taxonomy.yaml` 覆盖所有已存在实体（dotnet / 前端 app / proto / openapi）和待落地实体（java / python）的路径规则。
2. 前端 5 个 app 有对应 module 图谱节点，通过 `check` 校验。
3. `scan` 命令能在无 git 依赖下输出全量漂移诊断，对现有已记录节点无误报。
4. `check-drift` 对变更的已有 contract 文件发出 `WARN contract-outdated`。
5. `integration.contract` 无效引用触发 `dangling-reference` 错误。
6. `init` 命令能为所有五种 kind 生成合法节点文件。
7. `diff` 命令输出按 taxonomy kind 分类的中文变更概览。
8. `changes/` 和 `proposals/` 机制有对应目录和说明文档。
9. `docs/knowledge/README.md` 包含新栈接入协议的完整步骤。
10. 新栈接入协议可在不修改 `knowledge.py` 的前提下，只通过 `taxonomy.yaml` 完成接入。
