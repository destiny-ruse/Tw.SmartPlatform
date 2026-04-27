---
id: decisions.0003-section-level-retrieval
title: 采用章节级检索
doc_type: decision
status: active
version: 1.0.0
owners: [architecture-team]
roles: [architect, ai]
stacks: []
tags: [decision, standards, governance]
summary: AI 需要按需读取标准内容，并遵守 standards.authoring#retrieval 的最小读取原则。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# 采用章节级检索

<!-- anchor: context -->
## 背景

AI 需要按需读取标准内容，并遵守 standards.authoring#retrieval 的最小读取原则。

<!-- anchor: decision -->
## 决策

标准体系采用 L2 section index 和 Markdown 行范围进行章节级检索。

<!-- anchor: consequences -->
## 影响

减少上下文消耗，提高引用精度，但要求文档维护稳定 anchor。

<!-- anchor: alternatives -->
## 备选方案

只使用全文检索；会增加无关上下文和引用漂移。

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.0.0 | 2026-04-27 | 记录初始决策。 |
