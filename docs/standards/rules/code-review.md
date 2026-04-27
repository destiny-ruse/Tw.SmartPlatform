---
id: rules.code-review
title: 代码评审规范
doc_type: rule
status: active
version: 1.0.0
owners: [architecture-team]
roles: [architect, backend, frontend, qa, devops, ai]
stacks: []
tags: [review, governance, quality]
summary: 规定代码评审关注点、反馈方式和合并前质量门禁。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# 代码评审规范

<!-- anchor: goal -->
## 目标

代码评审规范的目标是让团队使用一致、可审查、可自动化辅助的工程约定。

<!-- anchor: scope -->
## 适用范围

适用于所有代码、测试、文档、CI 配置和工程规范变更。

<!-- anchor: rules -->
## 规则

1. 评审必须优先关注正确性、回归风险、安全风险和缺失测试。
2. 评论应指出具体文件、行号、可复现现象和建议的修正方向。
3. 合并前必须处理阻断性意见，并记录有争议决策的结论。
4. 不得用偏好性意见阻塞合并，风格争议应沉淀为自动化规则或团队规范。

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
