---
id: rules.api-response-shape
title: API 统一响应结构
doc_type: rule
status: active
version: 1.0.0
owners: [architecture-team]
roles: [backend, frontend, ai]
stacks: [dotnet, java, python, vue-ts, uniapp]
tags: [api, response, contract]
summary: 规定 REST API 响应结构、空值表达和错误响应关系。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# API 统一响应结构

<!-- anchor: goal -->
## 目标

让客户端能够稳定解析成功响应、空集合、分页结果和错误响应。

<!-- anchor: scope -->
## 适用范围

适用于对外和内部 REST API 的 JSON 响应。

<!-- anchor: rules -->
## 规则

1. 成功响应必须保持稳定对象结构，不得随数据状态改变顶层类型。
2. 空集合使用空数组，空对象使用省略字段或明确的空对象，不使用 `null` 表示集合。
<!-- region: no-null -->
3. REST API 响应字段不得返回 `null`；无法提供值时应使用缺省值、空集合、字段省略或明确错误。
<!-- endregion: no-null -->
4. 分页响应必须包含数据列表和分页元数据。

<!-- anchor: examples -->
## 示例

正例：`items: []` 表示无数据。

反例：`items: null` 让客户端必须额外分支处理。

<!-- anchor: relations -->
## 相关规范

错误响应应与 `rules.api-error-response` 和 `references.error-codes` 保持一致。

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.0.0 | 2026-04-27 | 建立统一响应结构和 no-null 区域。 |
