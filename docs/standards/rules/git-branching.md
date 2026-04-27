---
id: rules.git-branching
title: 分支与发布管理规范
doc_type: rule
status: active
version: 1.0.0
owners: [architecture-team]
roles: [architect, backend, frontend, qa, devops, ai]
stacks: []
tags: [git, governance, release]
summary: 规定分支模型、命名约定、合并策略和发布流程，确保历史可审查、回滚可执行。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# 分支与发布管理规范

<!-- anchor: goal -->
## 目标

为团队提供统一的分支模型、命名约定、合并策略和发布流程，使主干始终可发布、变更可追溯、回滚可执行。

<!-- anchor: scope -->
## 适用范围

适用于所有进入仓库的功能、修复、文档、配置、CI 与发布相关变更，覆盖应用代码仓库、规范仓库与基础设施仓库。

<!-- anchor: rules -->
## 规则

1. 长期分支只有 `master`（或 `main`），主干必须随时可发布、可回滚。
2. 短期分支必须从最新 `master` 拉出，并使用 `<type>/<scope>-<short-desc>` 命名，例如 `feat/auth-oidc-login`、`fix/api-response-null`、`chore/standards-shard-index`。
3. 短期分支生命周期不超过两周；超期必须 rebase 主干或拆分为更小变更。
4. 合并到主干必须通过 PR，且 PR 必须满足 CI 通过、必要评审通过、规范引用合规。
5. 默认使用 squash merge 进入主干，保持主干历史每次提交对应一个可回滚意图；rebase merge 仅用于线性历史项目并保留单一作者签名。
6. 严禁直接向主干 push，严禁 `--force` 推送主干或受保护分支。
7. 发布通过带 `v<MAJOR>.<MINOR>.<PATCH>` 的 tag 标记，tag 必须从主干提交创建，tag 一经发布不得移动。
8. 紧急修复使用 `hotfix/<issue>-<short-desc>` 分支，从最近发布 tag 创建，修复完成后必须同时合并回主干。
9. 任何废弃分支必须在合并或关闭后 7 天内删除，远端不得保留陈旧分支。

<!-- anchor: examples -->
## 示例

正例：从最新主干拉出 `feat/cache-invalidation`，单一职责提交，PR 关联 issue 与受影响规范引用，CI 通过后 squash 合入主干，发布通过 tag `v1.4.0` 标记。

反例：长期分支 `dev` 与主干长期不同步，多人在同一分支强推 `--force`，发布无 tag 仅依赖时间戳，回滚需逐个 cherry-pick。

<!-- anchor: checklist -->
## 检查清单

- 分支名称是否符合 `<type>/<scope>-<short-desc>`。
- 分支是否基于最新主干并已通过 CI。
- PR 是否描述变更意图、影响范围和回滚方式。
- 合并策略是否为 squash 或受控 rebase，未引入合并气泡。
- 发布是否通过 tag 标记，tag 是否对应可重现构建。

<!-- anchor: relations -->
## 相关规范

- `rules.git-commit`：提交粒度与提交信息格式。
- `rules.code-review`：合并前评审要求。
- `processes.change-governance`：变更治理与版本策略。

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
