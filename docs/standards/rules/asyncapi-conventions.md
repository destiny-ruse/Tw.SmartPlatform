---
id: rules.asyncapi-conventions
title: AsyncAPI 约定规范
doc_type: rule
status: active
version: 1.1.0
owners: [architecture-team]
roles: [architect, backend, frontend, qa, ai]
stacks: [dotnet, java, python, vue-ts, uniapp]
tags: [asyncapi, messaging, contract]
summary: 规定异步消息接口、事件主题和消息结构的设计和验证要求。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# AsyncAPI 约定规范

<!-- anchor: goal -->
## 目标

AsyncAPI 约定规范用于降低异步接口缺少契约、主题命名不一致、消息载荷演进破坏消费者和测试无法覆盖生产者行为的风险。异步消息必须像同步 API 一样拥有可审查、可生成、可测试的契约。这样生产者、消费者、QA 和 AI 助手可以在变更前识别兼容性影响。

<!-- anchor: scope -->
## 适用范围

本规范适用于 `apps/`、`services/`、`src/`、`backend/` 中事件发布、命令消息、集成事件、消息通道、AsyncAPI 文档、Schema、契约测试和消费者 Mock。它也适用于前端或移动端订阅消息时的主题和载荷约定。它不适用于进程内事件、日志事件、数据库变更日志的内部存储格式或没有跨服务契约的本地队列。

<!-- anchor: rules -->
## 规则

1. 每个跨服务消息通道必须有 AsyncAPI 契约，声明通道名称、消息方向、载荷 Schema、示例、错误处理和版本信息。
2. 主题或通道名称必须表达领域、资源和事件语义，使用稳定小写分段命名；不得使用临时环境名、内部类名或消费者名称作为长期主题。
3. Schema 所有权必须明确归属生产者或共享契约目录；消费者不得私自扩展生产者载荷并假定其他消费者可见。
4. 消息载荷必须包含稳定消息 ID、事件时间、事件类型或名称、载荷版本和追踪上下文；不得只依赖 broker 元数据表达业务契约。
5. 新增可选字段必须保持向后兼容；删除字段、改名、改变类型、改变枚举含义或改变必填性必须引入新版本或迁移计划。
6. 示例必须覆盖正例、最小载荷和至少一个校验失败或不兼容反例；时间、金额、枚举和 ID 必须符合数据格式规范。
7. 契约测试必须验证生产者输出和消费者解析；不得只验证应用内部 DTO。
8. 消息头和载荷必须传播 `traceparent` 或等价追踪上下文；不得在消息中写入密钥、令牌或不必要的 PII。

<!-- anchor: examples -->
## 示例

正例：

```yaml
channels:
  orders.events.v1:
    publish:
      message:
        name: OrderPaid
        headers:
          type: object
          properties:
            traceparent:
              type: string
        payload:
          type: object
          required: [messageId, eventType, occurredAt, data]
```

反例：

```yaml
channels:
  tmp.order-service.debug:
    publish:
      message:
        payload:
          type: object
          additionalProperties: true
```

<!-- anchor: checklist -->
## 检查清单

- 每个跨服务主题是否有 AsyncAPI 契约、Schema、示例和版本信息。
- 主题命名是否稳定表达领域和事件语义，没有环境名、类名或消费者名。
- 载荷是否包含消息 ID、事件时间、事件类型、版本和追踪上下文。
- Schema 变更是否说明兼容性，破坏性变更是否有新版本或迁移计划。
- 生产者和消费者契约测试是否覆盖最小载荷、扩展字段和不兼容载荷。

<!-- anchor: relations -->
## 相关规范

- rules.messaging-patterns
- rules.data-formats
- rules.contract-testing
- rules.tracing

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
