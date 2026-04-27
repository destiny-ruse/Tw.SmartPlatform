---
id: rules.api-rest-design
title: REST API 设计规范
doc_type: rule
status: active
version: 1.0.0
owners: [architecture-team]
roles: [backend, frontend, ai]
stacks: [dotnet, java, python, vue-ts, uniapp]
tags: [api, rest, contract]
summary: 规定 REST 资源、方法、版本和兼容性约定。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# REST API 设计规范

<!-- anchor: goal -->
## 目标

让 REST API 在资源建模、方法语义、版本演进和客户端集成上保持一致。

<!-- anchor: scope -->
## 适用范围

适用于 HTTP JSON API、网关 API、前后端契约和自动化客户端生成。

<!-- anchor: resources -->
## 资源

资源路径使用名词复数，表达业务资源而不是内部表名或方法名。嵌套资源不得超过两级，复杂查询应使用查询参数。

<!-- anchor: methods -->
## 方法

GET 只读，POST 创建或触发非幂等动作，PUT 替换整体资源，PATCH 修改部分字段，DELETE 删除或关闭资源。

<!-- anchor: versioning -->
## 版本

破坏性变更必须引入新版本路径或协商机制；兼容新增字段不得改变既有字段含义。

<!-- anchor: examples -->
## 示例

正例：`GET /api/v1/orders/{id}` 查询订单。

反例：`POST /api/v1/getOrder` 用动词模拟 RPC。

<!-- anchor: checklist -->
## 检查清单

- 路径是否表达资源。
- 方法是否符合 HTTP 语义。
- 版本策略是否说明兼容性。

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.0.0 | 2026-04-27 | 建立 REST API 设计规范。 |
