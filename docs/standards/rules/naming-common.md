---
id: rules.naming-common
title: 通用命名规范
doc_type: rule
status: active
version: 1.1.0
owners: [architecture-team]
roles: [backend, frontend, qa, devops, ai]
stacks: []
tags: [naming, code-style]
summary: 规定跨语言命名的可读性、一致性和领域表达原则。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# 通用命名规范

<!-- anchor: goal -->
## 目标

通用命名规范用于降低同一概念多名、缩写不可理解、领域语言漂移和生成代码边界混乱的风险。命名必须让读者从名称判断业务对象、职责、生命周期或工程用途。统一命名后，代码搜索、评审、文档和自动化生成可以围绕同一词汇工作。

<!-- anchor: scope -->
## 适用范围

本规范适用于代码、配置、测试、目录、文件、接口字段、数据库相邻模型、事件名称、文档示例和部署资源中的工程命名。它不适用于必须保留的第三方协议字段、外部 API 原始字段、数据库遗留列名或生成代码中由工具固定的名称；这些名称进入本项目领域模型时应当转换或隔离。

<!-- anchor: rules -->
## 规则

1. 命名必须表达领域含义、职责或工程用途；不得使用 `data`、`info`、`obj`、`tmp`、`common`、`manager` 等无法独立判断职责的名称。
2. 同一概念在同一边界内必须使用同一词汇；不得混用 `order`、`trade`、`bill` 表达同一业务对象。
3. 缩写必须来自行业通用词或团队已接受词表；不得创造只有当前作者理解的缩写。
4. 布尔名称必须表达判断语义，应当使用 `is`、`has`、`can`、`should`、`enabled` 等稳定结构；不得使用含义不明的 `flag`。
5. 集合、映射和计数名称必须表达元素类型或键值含义，例如 `orders`、`ordersById`、`retryCount`；不得使用 `list1`、`mapData`。
6. 异步、批处理、流式、缓存和临时对象名称必须表达行为边界；不得让调用方从实现中推断是否有 I/O、延迟或副作用。
7. 文件、目录、配置键和部署资源名称应当与代码中的领域词一致；不得让同一组件在不同资产中使用不同名称。
8. 生成代码边界必须保留工具生成的名称，并在进入手写代码或领域模型时做必要转换；不得直接把外部怪异命名扩散到核心代码。

<!-- anchor: examples -->
## 示例

正例：

```typescript
const ordersByCustomerId = new Map<string, Order[]>();
const canRetryPayment = failure.reason === "timeout";
```

反例：

```typescript
const data = {};
const flag = true;
const mgr2 = createThing();
```

<!-- anchor: checklist -->
## 检查清单

- 名称是否能表达领域对象、职责、生命周期或工程用途。
- 同一概念是否在代码、测试、配置、文档和部署资产中使用同一词汇。
- 是否避免无意义缩写、编号、万能后缀和临时名称。
- 布尔值、集合、映射和计数是否从名称可判断含义。
- 外部协议或 generated 名称是否被隔离，未扩散到核心领域模型。
- 新命名是否与语言专项命名规范一致。

<!-- anchor: relations -->
## 相关规范

- rules.naming-dotnet
- rules.naming-java
- rules.naming-python
- rules.naming-ts-vue
- rules.naming-uniapp

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
