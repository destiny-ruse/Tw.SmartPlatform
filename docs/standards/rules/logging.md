---
id: rules.logging
title: 日志规范
doc_type: rule
status: active
version: 1.1.0
owners: [architecture-team]
roles: [backend, qa, devops, ai]
stacks: [dotnet, java, python]
tags: [observability, operations]
summary: 规定日志级别、字段、脱敏、采样和查询要求。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# 日志规范

<!-- anchor: goal -->
## 目标

日志规范用于降低故障排查依赖自由文本、日志级别滥用、敏感数据泄露和无法跨信号关联的风险。服务必须输出结构化、可查询、可脱敏且能关联请求链路的日志。这样提交者、评审者和 AI 助手可以用同一字段集合判断变更是否具备生产排障能力。

<!-- anchor: scope -->
## 适用范围

本规范适用于 `apps/`、`services/`、`src/`、`backend/` 中 HTTP API、gRPC 服务、消息消费者、批处理任务、后台作业、网关适配器、框架中间件、日志配置和运维排障查询。它覆盖日志字段、日志级别、异常记录、采样、PII 脱敏、trace 关联和评审时的查询可用性。它不适用于仅在本地开发机临时打印且不会提交到仓库的调试输出，也不替代安全审计日志的专用保留策略。

<!-- anchor: rules -->
## 规则

1. 生产日志必须使用结构化格式，并包含 `service`、`environment`、`version`、`traceId` 或等价关联字段；不得只输出不可解析的自然语言文本。
2. 错误日志必须包含稳定错误码、异常分类、调用边界和脱敏后的关键上下文；不得直接记录堆栈外的 SQL、令牌、密钥、完整证件号、完整手机号或原始请求体。
3. 日志级别必须表达影响：`ERROR` 用于请求失败或需要处置的系统错误，`WARN` 用于降级、重试耗尽或可恢复异常，`INFO` 用于关键状态变化，`DEBUG` 用于非生产诊断；不得用 `ERROR` 表示正常业务拒绝。
4. 请求入口、外部依赖调用、消息消费和后台任务必须记录开始、结束或失败的可关联事件；高频成功路径应当通过采样或聚合避免噪声。
5. 日志字段名必须稳定，业务标识应当使用可查询的键值字段；不得把多个语义拼接到单个 `message` 后要求人工拆解。
6. 跨服务调用必须保留上游 `traceId`、`spanId` 或等价上下文，并在创建新请求、消息或任务时继续传递；不得在边界处丢失关联上下文。
7. 新增日志字段、采样策略或脱敏逻辑时必须提供可执行或可复用的查询示例；不得让评审者只能通过阅读代码推断排障方式。
8. 日志不得承担指标或审计的唯一来源；需要告警、趋势统计或合规留痕的信号必须同步映射到指标、追踪或审计记录。

<!-- anchor: examples -->
## 示例

正例：

```json
{
  "level": "ERROR",
  "service": "order-api",
  "environment": "prod",
  "version": "2026.04.27",
  "traceId": "4f9c2d8e7a6b4c10",
  "errorCode": "PAYMENT_TIMEOUT",
  "operation": "CreateOrder",
  "customerId": "usr_9f2a1c",
  "message": "dependency call timed out"
}
```

反例：

```java
log.error("create failed: " + request.getPhone() + " " + request.getPassword(), ex);
```

<!-- anchor: checklist -->
## 检查清单

- 日志是否为结构化字段，并包含服务、环境、版本和 trace 关联信息。
- 错误日志是否包含稳定错误码、异常分类和脱敏上下文。
- 日志级别是否与影响一致，且正常业务拒绝不会被记录为系统错误。
- PII、密钥、令牌、原始载荷和 SQL 是否已屏蔽或脱敏。
- 新增日志是否能通过字段查询定位请求、依赖调用、消息或任务。
- 高频路径是否有采样、聚合或降噪说明，避免生产日志不可用。

<!-- anchor: relations -->
## 相关规范

- rules.tracing
- rules.metrics
- rules.pii-handling
- rules.error-handling

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
