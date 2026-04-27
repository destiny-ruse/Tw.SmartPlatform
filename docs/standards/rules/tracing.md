---
id: rules.tracing
title: 链路追踪规范
doc_type: rule
status: active
version: 1.1.0
owners: [architecture-team]
roles: [backend, qa, devops, ai]
stacks: [dotnet, java, python]
tags: [observability, operations]
summary: 规定 trace/span 命名、上下文传播和采样策略。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# 链路追踪规范

<!-- anchor: goal -->
## 目标

链路追踪规范用于降低跨服务调用无法串联、异步任务丢失上下文和性能瓶颈只能靠猜测定位的风险。服务必须以稳定 trace、span 和属性表达请求经过的边界、依赖和关键业务操作。这样日志、指标和错误响应可以通过同一上下文关联到完整调用路径。

<!-- anchor: scope -->
## 适用范围

本规范适用于 `apps/`、`services/`、`src/`、`backend/` 中 HTTP API、gRPC 服务、消息生产者、消息消费者、后台任务、批处理作业、网关适配器、外部依赖客户端和追踪中间件配置。它覆盖 trace/span 命名、上下文传播、span 属性、错误标记、采样、异步边界和评审查询准备。它不适用于不会跨进程、不会进入生产观测系统的本地单元测试内部调用。

<!-- anchor: rules -->
## 规则

1. 请求入口、外部依赖调用、消息发布、消息消费和后台任务必须创建或延续 trace 上下文；不得在协议、队列或调度边界丢弃 `traceId`。
2. Span 名称必须稳定表达协议或操作，例如 `HTTP GET /orders/{id}`、`Message Process order.created` 或 `Dependency Call payment`; 不得把用户 ID、订单号、完整 URL 或错误消息写入 span 名称。
3. Span 属性必须使用低基数字段描述 `service`、`environment`、`version`、`operation`、`dependency`、`status` 和错误码；不得把 PII、密钥、令牌、原始请求体或高基数业务主键作为属性。
4. 失败的 span 必须标记错误状态并记录稳定错误码或异常分类；不得只依赖日志说明该 span 已失败。
5. 异步消息、延迟任务和批处理子任务必须传播父上下文或显式记录关联 ID；不得让同一业务流程在追踪系统中断裂成无法关联的多条链路。
6. 采样策略必须说明入口、关键路径、错误路径和高流量路径的处理方式；不得用会丢失所有错误样本的固定采样替代错误优先策略。
7. 追踪与日志、指标和错误响应必须共享可关联字段，例如 `traceId`、`spanId`、错误码和操作名；不得让每类信号使用互不兼容的标识。
8. 新增链路埋点必须提供查询或截图可替代的文字说明，能按服务、操作、错误码和依赖过滤；不得只证明代码中创建了 span。

<!-- anchor: examples -->
## 示例

正例：

```yaml
span:
  name: "HTTP POST /orders"
  attributes:
    service: "order-api"
    operation: "CreateOrder"
    status: "error"
    errorCode: "PAYMENT_TIMEOUT"
    dependency: "payment-service"
```

反例：

```typescript
tracer.startSpan(`create order ${user.phone} ${order.id}`);
```

<!-- anchor: checklist -->
## 检查清单

- 请求入口、依赖调用、消息和后台任务是否创建或延续 trace 上下文。
- Span 名称是否稳定，且不包含用户 ID、订单号、完整 URL 或错误消息。
- Span 属性是否低基数、已脱敏，并包含服务、操作、依赖、状态和错误码。
- 错误路径是否在 span 上标记失败，而不是只写日志。
- 异步边界是否能通过 trace 或关联 ID 串联回原始业务流程。
- 采样策略是否覆盖关键路径、错误路径和高流量路径的取舍。

<!-- anchor: relations -->
## 相关规范

- rules.logging
- rules.metrics
- rules.messaging-patterns
- rules.api-error-response

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
