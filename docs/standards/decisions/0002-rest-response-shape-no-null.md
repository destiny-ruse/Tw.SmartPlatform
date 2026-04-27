---
id: decisions.0002-rest-response-shape-no-null
title: REST 响应字段禁止 null
doc_type: decision
status: active
version: 1.0.0
owners: [architecture-team]
roles: [architect, ai]
stacks: []
tags: [decision, standards, governance]
summary: API 响应需要减少客户端空值分支，并与 rules.api-response-shape#rules:no-null 保持一致。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# REST 响应字段禁止 null

<!-- anchor: context -->
## 背景

API 响应需要减少客户端空值分支，并与 rules.api-response-shape#rules:no-null 保持一致。

<!-- anchor: decision -->
## 决策

REST API 响应字段不返回 `null`，使用空集合、缺省值、字段省略或明确错误表达状态。

<!-- anchor: consequences -->
## 影响

客户端处理更稳定，但服务端必须在序列化前明确空值策略。

<!-- anchor: alternatives -->
## 备选方案

允许 null；会把兼容成本转嫁给所有客户端。

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.0.0 | 2026-04-27 | 记录初始决策。 |
