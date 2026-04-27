---
id: rules.data-formats
title: 数据格式规范
doc_type: rule
status: active
version: 1.0.0
owners: [architecture-team]
roles: [architect, backend, frontend, qa, ai]
stacks: [dotnet, java, python, vue-ts, uniapp]
tags: [api, data-format, compatibility]
summary: 规定JSON、枚举、日期、金额和标识符格式的设计和验证要求。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# 数据格式规范

<!-- anchor: goal -->
## 目标

数据格式规范用于统一JSON、枚举、日期、金额和标识符格式的设计、演进和验证方式。

<!-- anchor: scope -->
## 适用范围

适用于JSON、枚举、日期、金额和标识符格式相关的服务接口、客户端集成、测试和文档。

<!-- anchor: rules -->
## 规则

1. 契约必须先描述兼容性边界，再描述实现细节。
2. 破坏性变更必须有版本策略、迁移说明和验收方式。
3. 示例必须能被实现方和调用方共同理解。
4. 不得把内部临时字段暴露为长期公共契约。

<!-- anchor: examples -->
## 示例

正例：在契约变更中说明新增字段的默认行为和客户端兼容策略。

反例：直接删除字段并要求所有客户端同步发布。

<!-- anchor: checklist -->
## 检查清单

- 是否说明兼容性。
- 是否包含调用方视角示例。
- 是否有测试或验证方式。

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
