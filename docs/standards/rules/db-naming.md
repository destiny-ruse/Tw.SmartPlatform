---
id: rules.db-naming
title: 数据库命名规范
doc_type: rule
status: active
version: 1.0.0
owners: [architecture-team]
roles: [backend, qa, ai]
stacks: [dotnet, java, python]
tags: [data, storage]
summary: 规定库、表、列、索引和约束命名。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# 数据库命名规范

<!-- anchor: goal -->
## 目标

数据库命名规范用于让工程实践在安全性、可维护性和可验证性上保持一致。

<!-- anchor: scope -->
## 适用范围

适用于持久化存储、缓存、迁移脚本和数据访问层。

<!-- anchor: rules -->
## 规则

1. 数据结构必须有明确所有者和生命周期。
2. 变更必须考虑兼容、回滚和审计。
3. 缓存和数据库不得使用隐式魔法键或未记录 TTL。
4. 数据修复必须可追踪、可复核。

<!-- anchor: examples -->
## 示例

正例：缓存键包含业务域、实体 ID 和版本前缀。

反例：多个服务共享未命名空间隔离的缓存键。

<!-- anchor: checklist -->
## 检查清单

- 是否说明生命周期。
- 是否有迁移或失效策略。
- 是否覆盖回滚和审计。

<!-- anchor: relations -->
## 相关规范

关联 API 契约、错误处理、可观测和测试数据规范。

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
