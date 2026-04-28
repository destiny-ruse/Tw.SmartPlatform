---
id: rules.comments-java
title: Java 注释规范
doc_type: rule
status: active
version: 1.2.0
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

Java 注释规范用于降低公共类缺少 Javadoc、异常和线程语义不清、复杂业务规则难以维护的风险。注释必须帮助调用方理解 API 契约，帮助维护者理解实现背后的领域约束。统一 Javadoc 和行内注释写法后，IDE、文档生成和代码评审可以获得一致信息。

<!-- anchor: scope -->
## 适用范围

本规范适用于 Java 源码、公共类、接口、枚举、控制器、服务、配置类、测试夹具和示例代码中的 Javadoc、块注释和行内注释。它不适用于工具生成目录、外部 vendored 源码、临时本地调试片段或由协议生成器覆盖维护的客户端代码。

<!-- anchor: rules -->
## 规则

1. 公共类、接口、枚举和跨模块公共方法必须提供 Javadoc，说明职责和调用契约；不得让调用方阅读实现才能理解用途。
2. Javadoc 必须在需要时使用 `@param`、`@return`、`@throws`、`@see` 说明输入、输出、异常和关联概念。
3. Javadoc、单行注释和块注释中的自然语言必须使用简体中文；代码标识符、协议名、英文缩写、标准格式和外部契约字段可以保留原文。
4. Javadoc 正文、`@param`、`@return`、`@throws` 和单行注释末尾不得使用中英文句号。
5. 字段、访问器和 record 组件注释必须直接描述成员含义；不得包含“获取”“设置”“获取或设置”等模板化字样。
6. 单行注释必须简洁说明意图，优先使用 `// ` 放在被说明代码上方并与代码对齐；注释与代码之间不留空行，注释上方保留空行，代码块第一行除外。
7. 除公共成员 Javadoc 外，方法内部的复杂算法、业务流程、规避已知缺陷的 Hack 和非显而易见分支必须添加注释，说明为什么这样处理。
8. 可能阻塞、重试、访问外部系统或要求事务边界的方法必须说明相关约束；不得隐藏线程、事务或 I/O 副作用。
9. 行内注释必须解释业务规则、兼容性、安全或性能取舍；不得逐行解释普通 Java 语句。
10. `@Deprecated` API 必须说明替代方案或迁移方向；不得只标记废弃而不给调用方路径。
11. 覆盖方法可以继承接口 Javadoc，但实现改变异常、性能、空值或副作用时必须补充说明。
12. 测试注释应当说明特殊夹具、时间控制、并发或边界场景；不得解释断言语法。
13. 生成代码目录不得添加人工维护注释；需要说明的内容必须写入生成源模板或相邻源定义。

<!-- anchor: examples -->
## 示例

正例：

```java
/**
 * 取消尚未结算的订单
 *
 * @param orderId 公共 API 中稳定的订单标识
 * @throws IllegalStateException 当订单已无法取消时抛出
 */
void cancelOrder(String orderId);
```

反例：

```java
// 循环订单
for (Order order : orders) {
    // 调用方法
    handle(order);
}
```

<!-- anchor: checklist -->
## 检查清单

- 公共类、接口、枚举和跨模块方法是否有必要 Javadoc。
- Javadoc 是否覆盖调用方需要知道的参数、返回、异常和副作用。
- 注释中的自然语言是否使用简体中文。
- Javadoc、参数描述和单行注释末尾是否避免中英文句号。
- 字段、访问器和 record 组件注释是否避免“获取”“设置”“获取或设置”。
- 方法内部复杂逻辑、兼容分支或 Hack 是否说明原因。
- 事务、线程、阻塞、重试或外部调用边界是否说明清楚。
- 废弃 API 是否说明替代方案或迁移方向。
- 行内注释是否解释原因和约束，而不是复述语法。
- generated 文件中是否避免人工维护注释。

<!-- anchor: relations -->
## 相关规范

- rules.comments-common
- rules.naming-java

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.2.0 | 2026-04-28 | 补充 Java 注释语言、句号、成员注释和方法内部复杂逻辑注释格式要求。 |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
