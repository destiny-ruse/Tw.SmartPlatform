---
id: rules.comments-java
title: Java 注释规范
doc_type: rule
status: active
version: 1.0.0
owners: [architecture-team]
roles: [backend, ai]
stacks: [java]
tags: [comments, java, code-style]
summary: 规定 Java 注释、Javadoc 和复杂逻辑说明方式。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# Java 注释规范

<!-- anchor: goal -->
## 目标

让 Java 注释解释意图、约束和取舍，而不是重复代码表面行为。

<!-- anchor: scope -->
## 适用范围

适用于 Java 源码、测试、配置示例和公共 API 文档注释。

<!-- anchor: rules -->
## 规则

1. 注释应解释为什么这样做、边界条件、兼容性约束或安全注意事项。
2. 公共 API 的注释必须说明输入、输出、异常和副作用。
3. 删除过期注释，禁止让注释与代码产生冲突。
4. 不得用注释掩盖复杂代码；优先通过命名和结构降低复杂度。

<!-- anchor: examples -->
## 示例

正例：`// 保留旧字段名以兼容 2025 版移动端客户端。`

反例：`// i 加 1` 出现在 `i++` 上方，只是在复述代码。

<!-- anchor: checklist -->
## 检查清单

- 注释是否解释了代码看不出的原因。
- 注释是否仍然准确。
- 复杂逻辑是否优先通过重构变清晰。

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
