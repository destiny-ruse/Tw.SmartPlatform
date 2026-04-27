---
id: rules.test-data-mock
title: 测试数据与 Mock 规范
doc_type: rule
status: active
version: 1.0.0
owners: [architecture-team]
roles: [backend, frontend, qa, ai]
stacks: [dotnet, java, python, vue-ts, uniapp]
tags: [testing, quality]
summary: 规定测试数据构造、Mock 使用和隔离策略。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# 测试数据与 Mock 规范

<!-- anchor: goal -->
## 目标

测试数据与 Mock 规范用于让工程实践在安全性、可维护性和可验证性上保持一致。

<!-- anchor: scope -->
## 适用范围

适用于功能开发、缺陷修复、重构、契约变更和发布验证。

<!-- anchor: rules -->
## 规则

1. 测试必须覆盖正常路径、失败路径和边界条件。
2. Mock 只能替代不可控外部依赖，不得测试 Mock 自身行为。
3. 覆盖率目标必须服务风险，不得用无意义断言凑数。
4. 缺陷修复必须先补回归测试。

<!-- anchor: examples -->
## 示例

正例：先编写失败的回归测试，再修复缺陷。

反例：只验证 Mock 被调用而不验证真实行为。

验证命令示例：`python -m unittest discover -s tests -p "test_*.py" -v`

<!-- anchor: checklist -->
## 检查清单

- 是否覆盖失败路径。
- 是否能在本地重复执行。
- 是否给出验证命令。

<!-- anchor: relations -->
## 相关规范

关联契约测试、代码评审、CI 流水线和测试数据规范。

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
