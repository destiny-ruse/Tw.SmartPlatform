---
id: rules.api-error-response
title: API 错误响应规范
doc_type: rule
status: active
version: 1.0.0
owners: [architecture-team]
roles: [backend, frontend, ai]
stacks: [dotnet, java, python, vue-ts, uniapp]
tags: [api, error-handling, contract]
summary: 规定错误码、错误消息、追踪标识和客户端处理契约。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# API 错误响应规范

<!-- anchor: goal -->
## 目标

API 错误响应规范的目标是让团队使用一致、可审查、可自动化辅助的工程约定。

<!-- anchor: scope -->
## 适用范围

适用于 REST API、网关错误、领域校验失败和跨端错误展示。

<!-- anchor: rules -->
## 规则

1. 错误响应必须包含稳定错误码、用户可理解消息和追踪标识。
2. 不得把异常堆栈、数据库错误或内部服务名暴露给客户端。
3. 同一错误语义必须复用同一错误码。
4. 校验错误应包含字段级明细，便于前端定位输入项。

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
