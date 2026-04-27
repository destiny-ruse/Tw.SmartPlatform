---
id: rules.contract-testing
title: 契约测试规范
doc_type: rule
status: active
version: 1.1.0
owners: [architecture-team]
roles: [architect, backend, frontend, qa, ai]
stacks: [dotnet, java, python, vue-ts, uniapp]
tags: [testing, contract, api]
summary: 规定提供方和消费方契约测试的设计和验证要求。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# 契约测试规范

<!-- anchor: goal -->
## 目标

契约测试规范用于降低 REST、AsyncAPI、gRPC 和客户端集成在独立演进时发生不兼容变更的风险。
提供方和消费方必须用可执行契约确认字段、状态码、消息、错误和兼容性边界，而不是依赖口头约定。
一致的契约测试可以让破坏性变更在合并前暴露，并让兼容演进具备可审查证据。

<!-- anchor: scope -->
## 适用范围

适用于 OpenAPI/REST schema、AsyncAPI 消息定义、gRPC proto、客户端 SDK、消费方适配层、契约 fixture、契约测试配置和 CI 契约验证步骤。
本规范覆盖提供方验证、消费方期望、schema 兼容性检查、示例数据和破坏性变更处理。
它不要求所有内部私有函数编写契约测试；纯实现细节应当由单元测试或集成测试覆盖。

<!-- anchor: rules -->
## 规则

1. 公共 API、跨服务消息和生成客户端必须有可执行契约测试，覆盖成功响应、错误响应、必填字段、可选字段和版本字段。
2. 消费方应当声明其实际依赖的字段、状态码、错误码、消息主题或 RPC 方法，不得用超出业务需要的断言锁死提供方内部实现。
3. 提供方必须在合并前验证现有消费方契约，确认新增字段、默认值、枚举值、错误结构和 schema 变更保持向后兼容。
4. 破坏性变更必须有新版本、迁移说明和过渡验收方式；不得直接删除字段、重命名字段、收紧类型或改变语义后要求所有消费方同步发布。
5. REST 契约必须验证请求/响应 schema、HTTP status、错误响应和空值表达；AsyncAPI 契约必须验证 channel、message、payload 和 header；gRPC 契约必须验证 proto 字段编号、服务方法和兼容演进。
6. 契约示例必须使用确定性、PII-safe 的测试数据，并与 schema 中的类型、格式、枚举和必填性一致。
7. 契约测试失败时必须先判断是提供方破坏、消费方期望过窄还是契约已过期，并在修复中同步更新测试、文档和生成客户端。
8. 不得把日志字段、调试字段、数据库列名或临时内部字段暴露为长期公共契约，除非它们已进入正式 API 或消息 schema。

<!-- anchor: examples -->
## 示例

正例：

```yaml
consumer: order-web
provider: order-api
request:
  method: GET
  path: /orders/ord_1001
response:
  status: 200
  body:
    id: ord_1001
    status: PAID
    items: []
```

反例：

```yaml
change: remove response.body.status
reason: backend no longer needs it
migration: none
```

<!-- anchor: checklist -->
## 检查清单

- 是否覆盖提供方和消费方各自负责验证的契约。
- 是否检查 schema、字段类型、必填性、枚举、错误结构和兼容性。
- 是否为破坏性变更提供版本策略、迁移说明和验收方式。
- 是否使用确定性且不含 PII 的契约示例。
- 是否在 CI 或合并前流程中运行契约验证。
- 是否同步更新 API 文档、消息文档、proto 或生成客户端。

<!-- anchor: relations -->
## 相关规范

- rules.api-rest-design
- rules.asyncapi-conventions
- rules.grpc-proto-style
- rules.test-strategy

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
