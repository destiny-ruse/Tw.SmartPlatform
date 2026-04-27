---
id: rules.naming-common
title: 通用命名规范
doc_type: rule
status: active
version: 1.0.0
owners: [architecture-team]
roles: [backend, frontend, qa, devops, ai]
stacks: []
tags: [naming, code-style]
summary: 规定跨语言命名的可读性、一致性和领域表达原则。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# 通用命名规范

<!-- anchor: goal -->
## 目标

通用命名规范的目标是让团队使用一致、可审查、可自动化辅助的工程约定。

<!-- anchor: scope -->
## 适用范围

适用于代码、配置、测试、目录、接口字段和文档中的工程命名。

<!-- anchor: rules -->
## 规则

1. 命名必须表达业务含义或工程职责，避免 `data`、`info`、`temp` 等含糊词。
2. 同一概念在同一边界内只能使用一个名称。
3. 缩写必须来自团队已接受词表，禁止自造缩写。
4. 布尔变量应表达判断语义，如 `isEnabled`、`hasPermission`。

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
