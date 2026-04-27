---
id: rules.git-branching
title: 分支与发布管理规范
doc_type: rule
status: active
version: 1.1.0
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

分支与发布管理规范用于降低长期分支漂移、合并边界不清、发布来源不可追溯和回滚难以执行的风险。统一分支命名、合并策略和发布标记后，主干可以保持可发布状态，短期变更也能被审查、验证和清理。发布分支和 hotfix 分支必须服务于可追溯交付，而不是成为长期并行主干。

<!-- anchor: scope -->
## 适用范围

本规范适用于所有进入仓库历史的功能、修复、文档、配置、CI、规范和发布相关变更，覆盖应用代码仓库、规范仓库、工具脚本和基础设施仓库。它不适用于开发者本地未推送的临时试验分支、一次性 spike 记录或外部上游仓库的分支模型。

<!-- anchor: rules -->
## 规则

1. 长期分支只能是仓库约定的主干分支，例如 `main` 或 `master`；不得维护长期漂移的 `dev`、`test`、`release-current` 作为并行主干。
2. 短期分支必须从最新主干或明确发布基线拉出，并使用 `<type>/<scope>-<short-desc>` 命名，例如 `feat/auth-oidc-login`、`fix/api-response-null`、`docs/standards-rules-executable-content`。
3. 分支类型必须与主要变更意图一致，可以使用 `feat`、`fix`、`docs`、`test`、`refactor`、`chore`、`hotfix`；不得用 `misc`、`temp`、`work` 表达正式变更。
4. 单个分支应当承载一个可审查主题；不得把无关功能、格式化、工具升级和发布修复混入同一分支。
5. 合并到主干前必须完成对应 CI 验证，并通过 PR 或等效评审记录说明变更意图、影响范围和回滚方式。
6. 发布分支可以用于稳定发布候选、补充发布验证和生成发布标记，但不得接收与该发布无关的新功能。
7. hotfix 分支必须从受影响发布 tag 或明确基线拉出，修复完成后应当把修复带回主干，避免下一次发布回归。
8. 主干和发布 tag 不得被 force push 或移动；确需修正发布历史时必须创建新的提交或新的 tag。
9. 已合并或放弃的短期远端分支应当清理，保留分支必须能从名称或 PR 说明判断当前用途。

<!-- anchor: examples -->
## 示例

正例：

```shell
git switch main
git pull --ff-only
git switch -c docs/standards-rules-executable-content
git commit -m "docs: expand general engineering standards rules"
```

反例：

```shell
git switch -c work
git push --force origin main
```

<!-- anchor: checklist -->
## 检查清单

- 分支名称是否符合 `<type>/<scope>-<short-desc>`，并能表达主要变更意图。
- 分支是否基于最新主干、发布基线或明确 hotfix 基线。
- 分支内容是否保持一个可审查主题，未混入无关变更。
- 合并前是否有 CI 结果和变更影响说明。
- 发布分支是否只接收本次发布相关修复和验证。
- hotfix 是否有回到主干的路径。

<!-- anchor: relations -->
## 相关规范

- rules.git-commit
- rules.deploy-rollback
- processes.change-governance

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
