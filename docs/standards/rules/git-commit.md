---
id: rules.git-commit
title: Git 提交规范
doc_type: rule
status: active
version: 1.0.0
owners: [architecture-team]
roles: [architect, backend, frontend, qa, devops, ai]
stacks: []
tags: [git, governance]
summary: 规定提交信息格式、提交粒度和可审查历史要求。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# Git 提交规范

<!-- anchor: goal -->
## 目标

Git 提交规范的目标是让团队使用一致、可审查、可自动化辅助的工程约定。

<!-- anchor: scope -->
## 适用范围

适用于所有进入仓库历史的功能、修复、文档、配置和生成产物提交。

<!-- anchor: rules -->
## 规则

1. 提交信息必须使用清晰的类型前缀，如 `feat`、`fix`、`docs`、`test`、`refactor`、`chore`。
2. 单个提交应表达一个可回滚的变更意图。
3. 生成产物必须与源文件变更位于同一提交或紧邻提交，并在说明中标明生成命令。
4. 不得提交临时调试输出、个人环境文件或未解释的大规模格式化噪音。

<!-- anchor: examples -->
## 示例

正例：变更前说明意图，变更中保持单一职责，评审记录标出影响范围。

反例：一次提交混合重构、功能、格式化和临时调试代码，导致评审者无法判断真实风险。

<!-- anchor: checklist -->
## 检查清单

- 规则是否能被评审者独立检查。
- 例外是否有明确说明和责任人。
- 相关变更是否更新测试、文档或索引。

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
