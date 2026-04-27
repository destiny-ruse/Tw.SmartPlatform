---
id: rules.data-formats
title: 数据格式规范
doc_type: rule
status: active
version: 1.1.0
owners: [architecture-team]
roles: [architect, backend, frontend, qa, ai]
stacks: [dotnet, java, python, vue-ts, uniapp]
tags: [api, data-format, compatibility]
summary: 规定JSON、枚举、日期、金额和标识符格式的设计和验证要求。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# 数据格式规范

<!-- anchor: goal -->
## 目标

数据格式规范用于降低跨语言序列化不一致、时间和金额精度丢失、枚举演进破坏客户端和 ID 表达混乱的风险。所有公开契约必须用稳定、可验证、跨平台一致的格式表达数据。这样 REST、gRPC、AsyncAPI、数据库边界和客户端类型可以共享同一解释。

<!-- anchor: scope -->
## 适用范围

本规范适用于 `apps/`、`services/`、`src/`、`backend/`、`frontend/`、`mobile/` 中 JSON、OpenAPI、AsyncAPI、Proto、数据库导入导出、配置示例、测试样例和客户端类型。它覆盖日期时间、时区、枚举、金额、数量、布尔值、ID、分页游标和可选字段表达。它不适用于仅供人工阅读的 UI 文案、日志自由文本或第三方系统不可控的原始载荷。

<!-- anchor: rules -->
## 规则

1. JSON 字段名必须使用稳定 `camelCase`，Proto 字段使用 `snake_case`；不得在同一契约中混用多种字段命名风格。
2. 日期时间必须使用带时区语义的 ISO 8601 / RFC 3339 字符串或引用日期时间参考；不得使用本地化字符串、模糊时区或 Unix 时间戳作为默认公开格式。
3. 金额必须使用十进制字符串或明确精度的整数最小单位，并同时声明币种；不得使用浮点数表达金额。
4. 枚举必须使用稳定英文标识值，新增值必须保持消费者兼容；不得改写已有枚举值含义或依赖本地化展示文本作为协议值。
5. ID 必须作为不透明字符串传输；不得要求客户端解析 ID 结构、数据库自增序号含义或分片信息。
6. 布尔值必须使用 JSON boolean 或协议原生 boolean；不得使用 `"Y"`、`"N"`、`"0"`、`"1"` 字符串表达布尔语义。
7. 可选字段不得返回 `null`；缺省、空集合、字段省略和显式错误必须在契约中说明。
8. 分页游标、Token 和外部引用值必须作为不透明字符串处理，并明确有效范围和排序语义；不得让客户端拼接或解码内部状态。

<!-- anchor: examples -->
## 示例

正例：

```json
{
  "id": "ord_01HZY8K7R6",
  "createdAt": "2026-04-27T08:30:00Z",
  "status": "paid",
  "amount": "129.90",
  "currency": "CNY",
  "active": true
}
```

反例：

```json
{
  "id": 12345,
  "createdAt": "2026/04/27 16:30",
  "status": "已支付",
  "amount": 129.9,
  "active": "Y"
}
```

<!-- anchor: checklist -->
## 检查清单

- 字段命名是否符合目标协议风格，且同一契约内一致。
- 日期时间是否包含明确时区语义，并符合日期时间参考。
- 金额是否避免浮点数，并声明币种和单位。
- 枚举值是否稳定、英文、可扩展，且客户端处理未知值。
- ID、游标和 Token 是否被当作不透明字符串处理。

<!-- anchor: relations -->
## 相关规范

- references.datetime
- rules.api-response-shape
- rules.grpc-proto-style
- rules.asyncapi-conventions

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
