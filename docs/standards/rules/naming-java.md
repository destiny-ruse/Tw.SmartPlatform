# Java 命名规范

## 目标

Java 命名规范用于降低包结构混乱、类职责模糊、方法语义不清和常量滥用的风险。命名必须符合 Java 生态约定，并稳定表达领域概念、模块边界和调用行为。统一命名后，代码导航、依赖扫描、Javadoc 和测试报告可以围绕一致词汇工作。

## 适用范围

本规范适用于 Java 项目的包、模块、类、接口、枚举、方法、字段、局部变量、参数、常量、测试类、配置类和文件命名。它不适用于工具生成目录、外部 vendored 源码、必须保持的序列化字段名或由协议生成器维护的客户端代码。

## 规则

1. 包名必须使用全小写反向域名或项目约定根包，并按领域或层次分段；不得使用大写字母、下划线或含义不明的 `misc` 包。
2. 类、接口和枚举必须使用 PascalCase，并表达领域对象、能力或职责；不得使用 `CommonHelper`、`Manager2` 等宽泛名称。
3. 方法、字段、局部变量和参数必须使用 camelCase，并表达动作、状态或数据含义；不得使用 `data`、`obj`、`tmp` 作为长期名称。
4. 常量必须使用 UPPER_SNAKE_CASE，并只用于真正不可变的值；不得把环境配置、可变阈值或数据库数据写成常量。
5. 布尔方法和变量应当使用 `is`、`has`、`can`、`should` 等判断语义；集合名称应当表达元素类型。
6. 接口名称不得强制使用 `I` 前缀，应当直接表达能力；实现类名称必须说明实现差异，例如 `JdbcOrderRepository`。
7. 测试类和测试方法名称必须表达被测对象、场景和期望；不得使用 `test1`、`shouldWork`。
8. 生成代码和外部协议字段必须隔离在边界层；进入领域模型后应当转换为 Java 命名风格。

| 构件 | 命名方式 | 示例 |
| --- | --- | --- |
| 包 | lower.dot | `com.tw.ordering.application` |
| 类/接口 | PascalCase | `OrderService` |
| 方法/变量 | camelCase | `calculateTotal` |
| 布尔值 | is/has/can/should + camelCase | `canCancel` |
| 常量 | UPPER_SNAKE_CASE | `MAX_RETRY_COUNT` |

## 示例

正例：

```java
package com.tw.ordering.application;

class OrderCancellationService {
    boolean canCancel(Order order) {
        return order.isPendingSettlement();
    }
}
```

反例：

```java
package Misc;

class CommonHelper2 {
    boolean flag;
}
```

## 检查清单

- 包名是否小写、稳定，并表达模块或领域边界。
- 类、接口、枚举、方法、字段和变量是否符合 Java 命名形态。
- 布尔值、集合和常量是否从名称可判断含义。
- 测试名称是否表达场景和期望结果。
- 是否避免万能 Helper、Manager、编号和无意义缩写。
- generated 或外部协议名称是否被隔离或转换。

## 相关规范

- rules.naming-common
- rules.comments-java

## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
