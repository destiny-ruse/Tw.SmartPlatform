# Knowledge Compiler 与仓库记忆图谱设计

- 日期：2026-04-27
- 状态：已确认设计，待实施计划
- 范围：为全仓建立可持续演进的知识图谱、确定性编译工具、漂移检测和 AI Skill 适配层
- 受众：架构、后端、前端、测试、运维、AI 编程工具使用者和代码评审者
- 基础：复用现有 `docs/standards/` 索引化规范体系、`tools/standards/standards.py` 的生成与校验模式、`.agents/skills` 技能目录和仓库目录职责文档

## 背景

当前仓库已经具备工程规范、目录职责、后端公共构件、微服务目录、契约目录和仓库技能目录。随着微服务、前端包、公共构件和契约逐步落地，开发者和 AI 编程工具都需要稳定知道已有能力在哪里、如何复用、哪些能力不得重复实现。

仅依赖自然语言规范无法解决复用发现问题。用户通常只描述业务目标，不会提前说明是否需要跨服务调用、是否应复用底层工具、是否触及认证授权或契约边界。AI 如果缺少可查询的仓库记忆，容易在业务服务中重复实现已有能力，例如重新实现用户查询、加密解密、远程服务调用或权限判断。

本设计建立一套仓库内的 Knowledge Compiler 与 Memory Graph。它是工程资产，不是 AI 专属资产。非 AI 团队可以通过确定性工具和 CI 使用它；使用 AI 编程工具的团队可以通过 Skill 消费和维护它。

## 目标

1. 建立完整、稳定、可演进的仓库记忆元模型，覆盖能力、模块、契约、集成和架构决策。
2. 通过确定性工具从代码、契约、README、项目文件、规范和 git diff 编译生成索引与漂移诊断。
3. 让 AI 和人类开发者都能按需查询已有能力，降低重复实现和跨服务调用方式漂移。
4. 支持 AI Skill 在开发者显式触发后直接维护正式图谱，并通过非 AI 工具校验结果。
5. 使用 L0/L1/L2 分片和字段级读取，保证 AI 最小上下文引用。
6. 为 Claude Code 创建相对路径符号链接适配，使多 AI 工具共享同一份仓库 Skill。
7. 面向开发者的诊断和 Skill 交互默认使用简体中文，机器稳定字段保持英文。

## 非目标

1. 不引入外部 RAG、向量库或图数据库作为第一阶段依赖。
2. 不要求所有团队必须使用 AI 编程工具。
3. 不让无人决策的 AI 流程静默修改正式图谱。
4. 不把生成索引作为人工维护对象。
5. 不要求第一阶段实现完整语义理解；语义判断由人工声明或显式 AI Skill 维护。

## 设计依据

本设计沿用现有规范体系的检索与生成原则：

1. `standards.authoring#retrieval`，版本 `2.0.0`，路径 `docs/standards/processes/standard-authoring.md`：AI 和自动化工具应先读取生成索引，再按需读取分片、section index 和目标行范围，不应一次性加载全部正文。
2. `standards.authoring#machine-rules`，版本 `2.0.0`，路径 `docs/standards/processes/standard-authoring.md`：存在可执行形态的规范应通过机器规则绑定，并由工具校验。
3. `rules.repo-layout#rules`，版本 `1.1.0`，路径 `docs/standards/rules/repo-layout.md`：工程文档、共享工具、生成产物和源码目录必须边界清晰，生成索引不得手工编辑。
4. `rules.auth-oauth-oidc#rules`，版本 `1.1.0`，路径 `docs/standards/rules/auth-oauth-oidc.md`：服务间调用、Scope、Audience 和授权边界必须由服务端可信路径保证。
5. `processes.change-governance#flow` 和 `processes.change-governance#rules`，版本 `1.0.0`，路径 `docs/standards/processes/change-governance.md`：公共能力、契约和规范变更需要版本、影响范围和验证记录。

## 总体架构

系统分为两条通道。

第一条是确定性主链路。它不依赖 AI，适合本地开发、CI 和代码评审：

```text
repo files / contracts / project metadata / standards / git diff
  -> tools/knowledge/knowledge.py scan
  -> tools/knowledge/knowledge.py generate
  -> tools/knowledge/knowledge.py check
  -> docs/knowledge/generated/**
```

第二条是 AI 可选维护链路。它由开发者显式调用，用于降低正式图谱维护成本：

```text
generated index + diagnostics + git diff
  -> tw-knowledge-maintenance Skill
  -> 修改 docs/knowledge/graph/**
  -> generate/check
```

AI Skill 可以维护正式图谱，但必须满足三个条件：

1. 由开发者显式触发。
2. 修改基于可追溯证据，例如 commit、文件路径、契约和 README。
3. 修改后运行非 AI 的 `generate` 与 `check`。

## 目录结构

```text
docs/knowledge/
  README.md
  memory.schema.json
  taxonomy.yaml
  graph/
    capabilities/
    modules/
    contracts/
    decisions/
    integrations/
  changes/
    2026/
  proposals/
  generated/
    index.generated.json
    memory.generated.json
    edges.generated.json
    diagnostics.generated.json
    _index/
      by-kind/
      by-domain/
      by-stack/
      by-tag/
      by-owner/
      sections/

tools/knowledge/
  knowledge.py
  templates/
  rules/

.agents/skills/
  tw-requirement-router/
  tw-knowledge-discovery/
  tw-service-integration/
  tw-knowledge-maintenance/
  tw-skill-linker/

.claude/skills/
  tw-requirement-router -> ../../.agents/skills/tw-requirement-router
  tw-knowledge-discovery -> ../../.agents/skills/tw-knowledge-discovery
  tw-service-integration -> ../../.agents/skills/tw-service-integration
  tw-knowledge-maintenance -> ../../.agents/skills/tw-knowledge-maintenance
  tw-skill-linker -> ../../.agents/skills/tw-skill-linker
```

`docs/knowledge/graph/**` 是正式图谱事实源，可以由人工或显式 AI Skill 维护。`docs/knowledge/generated/**` 是编译产物，只能由工具生成。`docs/knowledge/changes/**` 记录 git diff 影响分析。`docs/knowledge/proposals/**` 作为可选候选区，供团队需要先审再合入时使用。

## Memory Graph 模型

正式图谱使用 YAML 维护，生成索引用 JSON 输出。所有节点共享基础字段：

```yaml
schema_version: 1.0.0
id: backend.dotnet.services.authentication
kind: module
name: 认证服务
status: active
summary: 承载用户身份认证、登录流程、令牌签发和认证测试相关服务代码。
owners:
  - platform
tags:
  - backend
  - dotnet
  - auth
source:
  declared_in: docs/knowledge/graph/modules/backend.dotnet.services.authentication.yaml
  evidence:
    - backend/dotnet/Services/Authentication/README.md
provenance:
  created_by: human
  created_at: 2026-04-27
  updated_by: human
  updated_at: 2026-04-27
```

图谱包含五类核心节点。

`capability` 记录可复用能力，例如认证、授权、远程调用、缓存、加密、事件总线、前端共享组件和部署模板：

```yaml
kind: capability
domain: security
provided_by:
  modules:
    - backend.dotnet.services.authentication
entrypoints:
  docs:
    - backend/dotnet/Services/Authentication/README.md
  contracts:
    - contracts.openapi.authentication
reuse:
  use_when:
    - 需要用户登录或令牌签发
  do_not_reimplement:
    - 不要在业务服务中自行签发访问令牌
aliases:
  - 登录
  - token
  - 当前用户
standards:
  - rules.auth-oauth-oidc#rules
```

`module` 记录代码模块，例如微服务、BuildingBlock、前端 app、前端 package 和工具目录：

```yaml
kind: module
module_type: microservice
stack: dotnet
path: backend/dotnet/Services/Authentication
provides:
  capabilities:
    - backend.capability.authentication
depends_on:
  modules:
    - backend.dotnet.building-blocks.remote-service
project_files:
  - backend/dotnet/Tw.SmartPlatform.slnx
```

`contract` 记录 OpenAPI、proto、AsyncAPI、事件和跨服务接口：

```yaml
kind: contract
contract_type: openapi
path: contracts/openapi/authentication.yaml
provider:
  module: backend.dotnet.services.authentication
exposes:
  capabilities:
    - backend.capability.authentication
versioning:
  strategy: semantic
  compatibility: backward-compatible
```

`integration` 记录服务间调用链：

```yaml
kind: integration
caller: backend.dotnet.services.notice
callee: backend.dotnet.services.authentication
contract: contracts.openapi.authentication
protocol: http
auth:
  mode: client-credentials
tooling:
  required_capabilities:
    - backend.capability.remote-service
standards:
  - rules.auth-oauth-oidc#rules
  - rules.resilience#rules
  - rules.tracing#rules
```

`decision` 记录能力归属、禁止重复实现和架构边界：

```yaml
kind: decision
applies_to:
  capabilities:
    - backend.capability.authentication
decision: 用户身份认证能力由 Authentication 服务统一提供。
consequences:
  - 业务服务不得自行签发用户访问令牌。
  - 需要身份信息时必须通过契约或统一上下文能力获取。
```

## 最小上下文检索

图谱会随仓库增长，因此必须采用分片索引和按需读取。

`docs/knowledge/generated/index.generated.json` 是 L0 入口，只包含轻量字段：

1. `id`
2. `kind`
3. `name`
4. `summary`
5. `tags`
6. `path`
7. `sections_index`
8. `shards`

L1 分片按 kind、domain、stack、tag 和 owner 生成：

```text
docs/knowledge/generated/_index/by-kind/capability.generated.json
docs/knowledge/generated/_index/by-domain/security.generated.json
docs/knowledge/generated/_index/by-stack/dotnet.generated.json
docs/knowledge/generated/_index/by-tag/auth.generated.json
docs/knowledge/generated/_index/by-owner/platform.generated.json
```

L2 section index 记录节点字段位置：

```json
{
  "id": "backend.capability.authentication",
  "path": "docs/knowledge/graph/capabilities/backend.capability.authentication.yaml",
  "sections": [
    { "key": "summary", "start_line": 8, "end_line": 9 },
    { "key": "reuse", "start_line": 22, "end_line": 31 },
    { "key": "entrypoints", "start_line": 15, "end_line": 21 },
    { "key": "standards", "start_line": 32, "end_line": 35 }
  ]
}
```

AI Skill 必须遵循以下读取顺序：

1. 读取 L0 入口。
2. 读取相关 L1 分片。
3. 读取候选节点 L2 section index。
4. 只读取目标 YAML 的必要字段行范围。

禁止一次性读取 `docs/knowledge/graph/**` 全量正文。

## Knowledge Compiler

核心工具位于 `tools/knowledge/knowledge.py`，使用 Python 标准库实现，保持和现有 standards 工具链相似的本地可运行体验。

主要命令：

```powershell
python tools/knowledge/knowledge.py init
python tools/knowledge/knowledge.py scan
python tools/knowledge/knowledge.py diff --from main --to HEAD
python tools/knowledge/knowledge.py check-drift --from main --to HEAD
python tools/knowledge/knowledge.py generate
python tools/knowledge/knowledge.py check
python tools/knowledge/knowledge.py query --text "获取当前用户信息" --limit 5
python tools/knowledge/knowledge.py sync-skills --target claude
```

`scan` 读取文件系统、项目文件、README、契约和规范索引，生成派生事实。`generate` 编译正式图谱和派生事实，生成 L0/L1/L2 索引。`check` 校验 schema、路径、引用、生成产物一致性和图谱边关系。`query` 默认只返回摘要和建议读取位置，不返回完整正文。

`sync-skills --target claude` 为 Claude Code 创建相对路径符号链接。`.agents/skills` 是唯一事实源，`.claude/skills` 不复制技能正文。

## Git Diff 漂移检测

漂移检测通过确定性规则判断代码、契约、目录和依赖变更后，图谱是否需要同步。

输入包括：

1. `git diff --name-status <from> <to>`
2. 新增、删除和重命名文件。
3. `.csproj`、`.slnx`、`package.json`、`pyproject.toml`。
4. `contracts/openapi/**`、`contracts/protos/**`、`contracts/asyncapi/**`。
5. `README.md`。
6. `docs/standards/index.generated.json`。
7. `docs/knowledge/graph/**`。

路径映射规则由 `docs/knowledge/taxonomy.yaml` 维护：

```yaml
path_rules:
  - pattern: backend/dotnet/Services/*/**
    kind: module
    module_type: microservice
    stack: dotnet
  - pattern: backend/dotnet/BuildingBlocks/src/*/**
    kind: module
    module_type: building-block
    stack: dotnet
  - pattern: frontend/packages/*/**
    kind: module
    module_type: frontend-package
    stack: vue-ts
  - pattern: contracts/protos/**/*
    kind: contract
    contract_type: grpc
```

典型诊断包括：

```text
错误 [knowledge.missing-module]
位置: backend/dotnet/Services/User
说明: 新增服务目录尚未声明 module 图谱节点。
建议: 新增 docs/knowledge/graph/modules/backend.dotnet.services.user.yaml，或确认该目录不是长期服务模块。
```

```text
警告 [knowledge.missing-capability]
位置: backend/dotnet/BuildingBlocks/src/Tw.Cryptography
说明: 新增公共构件目录可能暴露可复用能力，但尚未声明 capability 图谱节点。
建议: 为该公共构件声明 capability，或在 taxonomy.yaml 中说明它不对外提供复用能力。
```

```text
错误 [knowledge.contract-drift]
位置: contracts/protos/authentication/v1/authentication.proto
说明: 公共契约发生变更，但对应 contract 图谱节点未更新。
建议: 更新 contract 节点的版本、兼容性说明或变更证据。
```

JSON 诊断保留英文稳定字段，并提供中文说明：

```json
{
  "severity": "error",
  "code": "knowledge.missing-module",
  "path": "backend/dotnet/Services/User",
  "message_zh": "新增服务目录尚未声明 module 图谱节点。",
  "suggestion_zh": "新增对应 module 图谱节点，或在 taxonomy.yaml 中声明忽略规则。"
}
```

面向开发者的 CLI 输出默认使用 `zh-CN`。标准 ID、anchor、路径、命令和 schema 字段保持英文。

## AI Skills

新增五个仓库 Skill。

`tw-requirement-router` 是前置路由 Skill。只要发生功能开发或行为修改，先运行它。它从用户业务描述中识别业务对象、动作、风险和交付位置，再决定是否需要进入能力发现、服务集成或标准检索。用户不需要显式说明是否跨服务、是否使用底层工具。

`tw-knowledge-discovery` 用于开发前查询已有能力。它按 L0/L1/L2 流程读取图谱，输出可复用能力、入口、禁止重复实现项和相关规范。

`tw-service-integration` 用于跨服务调用、契约变更和服务间客户端新增。它检查 contract、integration、授权方式、远程调用工具链、韧性、追踪和错误处理规范。

`tw-knowledge-maintenance` 用于修复记忆漂移。它读取 diagnostics、git diff 和相关图谱节点，提出中文修改计划。在开发者显式触发后，它可以直接修改 `docs/knowledge/graph/**`，并运行 `generate` 和 `check`。修改正式图谱时必须记录 provenance：

```yaml
provenance:
  updated_by: ai-assisted:tw-knowledge-maintenance
  based_on:
    commits:
      - <sha>
    files:
      - backend/dotnet/Services/Authentication/README.md
```

`tw-skill-linker` 用于同步 Claude Code 相对路径符号链接。它调用或指导使用 `knowledge.py sync-skills --target claude`，确保 `.claude/skills` 指向 `.agents/skills`。

## 更新模式

系统支持三种更新模式：

| 模式 | 触发方 | 是否依赖 AI | 是否写正式图谱 | 用途 |
| --- | --- | --- | --- | --- |
| `compiler` | CLI 或 CI | 否 | 否，只写 generated 和 changes | 持续生成索引、发现漂移 |
| `skill-propose` | AI Skill | 是 | 否，只写 proposals | 团队需要先审候选时使用 |
| `skill-apply` | 开发者显式调用 AI Skill | 是 | 是 | 快速修复正式图谱漂移 |

正式图谱可以由 AI Skill 修改，但不能由无人决策的自动流程静默修改。

## 验证方案

实施完成后应支持以下验证：

```powershell
python tools/knowledge/knowledge.py generate
python tools/knowledge/knowledge.py check
python tools/knowledge/knowledge.py check-drift --from main --to HEAD
python tools/knowledge/knowledge.py query --text "获取当前用户信息" --limit 5
python tools/knowledge/knowledge.py sync-skills --target claude
```

验证通过标准：

1. `docs/knowledge/graph/**` 通过 schema 校验。
2. 生成的 L0/L1/L2 索引与正式图谱一致。
3. 所有引用的路径、标准 ID、节点 ID 和契约路径存在。
4. 漂移诊断默认输出中文说明和建议。
5. 查询命令默认返回摘要和建议读取位置，不输出全量图谱正文。
6. Claude Code 相对符号链接指向 `.agents/skills`，且目标存在。

## 风险与处理

| 风险 | 处理方式 |
| --- | --- |
| 图谱文件随仓库增长导致上下文过大 | 使用 L0/L1/L2 分片、section index 和字段级读取；AI 禁止全量读取 graph 正文。 |
| 记忆变成 AI 专属资产 | Knowledge Compiler 不依赖 AI；CI 和 CLI 使用同一套工具与索引。 |
| AI Skill 误改正式图谱 | 只允许开发者显式触发；修改必须记录 provenance，并通过非 AI 工具校验。 |
| 开发者修改代码但未更新记忆 | `check-drift` 输出中文诊断；使用 AI 的团队可调用 `tw-knowledge-maintenance` 直接修复正式图谱。 |
| Claude Code 与仓库 Skill 内容漂移 | `.agents/skills` 是唯一源，`.claude/skills` 只放相对路径符号链接。 |
| 用户需求没有说明底层依赖 | `tw-requirement-router` 在实现前识别业务对象、动作、风险和交付位置，再路由到相关图谱查询。 |

## 验收标准

1. 形成 `docs/knowledge/`、`tools/knowledge/`、`.agents/skills/tw-*` 和 `.claude/skills` 符号链接适配设计。
2. 明确 Memory Graph 的节点类型、字段、来源、provenance 和生成索引结构。
3. 明确 Knowledge Compiler 的命令、输入、输出和中文诊断要求。
4. 明确 AI Skill 的消费、路由、维护和正式图谱修改边界。
5. 明确最小上下文读取流程，避免 AI 加载全量记忆。
6. 后续实施计划可以按工具、schema、初始图谱、Skill、Claude 适配和验证分阶段推进。

## 后续计划

设计确认后进入实施计划阶段。实施计划应列出 `docs/knowledge` schema 与 taxonomy、`tools/knowledge/knowledge.py` 命令、初始图谱种子、五个 Skill、Claude Code 符号链接同步和验证命令的分阶段任务。
