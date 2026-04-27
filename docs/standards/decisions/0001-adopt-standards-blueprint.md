---
id: decisions.0001-adopt-standards-blueprint
title: 采用工程规范蓝图
doc_type: decision
status: active
version: 1.0.0
owners: [architecture-team]
roles: [architect, ai]
stacks: []
tags: [decision, standards, governance]
summary: 决定采用 v2 工程规范蓝图作为标准体系的基础。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# 采用工程规范蓝图

<!-- anchor: context -->
## 背景

决定采用 v2 工程规范蓝图作为标准体系的基础。

<!-- anchor: decision -->
## 决策

采用分层索引、显式 anchor 和提交生成产物的方式组织工程规范。

<!-- anchor: consequences -->
## 影响

规范检索可按需进行，CI 能统一检查元数据、索引和引用。

<!-- anchor: alternatives -->
## 备选方案

仅维护人工 README；会导致 AI 检索粒度不稳定。

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.0.0 | 2026-04-27 | 记录初始决策。 |
