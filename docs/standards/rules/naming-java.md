---
id: rules.naming-java
title: Java 命名规范
doc_type: rule
status: active
version: 1.0.0
owners: [architecture-team]
roles: [backend, ai]
stacks: [java]
tags: [naming, java, code-style]
summary: 规定 Java 项目的类型、方法、变量和包命名方式。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# Java 命名规范

<!-- anchor: goal -->
## 目标

统一 Java 命名方式，让代码在搜索、评审和跨团队协作中保持可读。

<!-- anchor: scope -->
## 适用范围

适用于 Java 项目的类型、方法、变量、文件、测试和配置命名。

<!-- anchor: rules -->
## 规则

1. 命名必须表达领域含义和职责边界。
2. 同一概念在同一模块内必须使用同一词汇。
3. 禁止使用无业务含义的临时名称，如 `data1`、`obj`、`tmpValue`。
4. 缩写必须来自团队词表或行业通用词。

| 构件 | 命名方式 | 示例 |
| --- | --- | --- |
| 包 | lower.dot | `com.tw.order` |
| 类/接口 | PascalCase | `OrderService` |
| 方法/变量 | camelCase | `calculateTotal` |
| 常量 | UPPER_SNAKE_CASE | `MAX_RETRY_COUNT` |

<!-- anchor: examples -->
## 示例

正例：名称直接表达业务动作或领域对象。

反例：使用 `Manager2`、`CommonHelper` 或无法说明用途的缩写。

<!-- anchor: checklist -->
## 检查清单

- 是否能从名称判断职责。
- 是否和同领域已有名称一致。
- 是否避免无意义缩写和编号。

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
