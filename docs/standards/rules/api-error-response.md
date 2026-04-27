---
id: rules.api-error-response
title: API 错误响应规范
doc_type: rule
status: active
version: 1.1.0
owners: [architecture-team]
roles: [backend, frontend, ai]
stacks: [dotnet, java, python, vue-ts, uniapp]
tags: [api, error-handling, contract]
summary: 规定错误码、错误消息、追踪标识和客户端处理契约。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# API 错误响应规范

<!-- anchor: goal -->
## 目标

API 错误响应规范用于降低客户端依赖异常文本、服务端泄露内部实现和跨端错误处理分叉的风险。所有 API 必须用稳定错误码、可展示消息和可追踪标识表达失败，使前端、移动端、自动化测试和运维排查得到一致结果。错误响应也必须与统一响应结构和错误处理映射保持一致。

<!-- anchor: scope -->
## 适用范围

本规范适用于 `apps/`、`services/`、`src/`、`backend/`、`frontend/`、`mobile/` 中 REST JSON API、网关错误、表单校验失败、领域规则失败和客户端错误展示逻辑，也适用于 OpenAPI、Mock、集成测试断言和错误码目录审查。它不适用于 gRPC 状态细节、异步消息失败重试语义或仅写入服务端日志且不返回给调用方的内部异常对象。

<!-- anchor: rules -->
## 规则

1. 错误响应必须使用稳定 JSON 对象，至少包含 `code`、`message` 和 `traceId`；字段名和类型不得随业务场景变化。
2. `code` 必须来自已登记的错误码分类，且同一错误语义必须复用同一错误码；不得把异常类名、数据库错误或第三方原始错误作为公开错误码。
3. `message` 必须面向调用方可理解且安全；不得包含堆栈、SQL、连接串、密钥、访问令牌、PII 或内部服务拓扑。
4. HTTP 状态码必须表达协议层结果，错误码必须表达业务或领域原因；不得用 `200 OK` 包装业务失败，也不得用单一 `500` 覆盖可分类错误。
5. 输入校验失败必须返回字段级明细，例如 `details[].field`、`details[].code`、`details[].message`；字段路径必须与请求契约一致。
6. `traceId` 必须与服务端日志、链路追踪和网关记录可关联；跨服务转发错误时应当保留上游追踪上下文。
7. 幂等冲突、限流和重试后仍失败的场景必须映射到稳定错误码和适当 HTTP 状态；不得要求客户端解析自然语言判断是否可重试。

<!-- anchor: examples -->
## 示例

正例：

```json
{
  "code": "VALIDATION_FAILED",
  "message": "请求参数校验失败。",
  "traceId": "0af7651916cd43dd8448eb211c80319c",
  "details": [
    {
      "field": "items[0].quantity",
      "code": "MIN_VALUE",
      "message": "数量必须大于 0。"
    }
  ]
}
```

反例：

```json
{
  "error": "System.NullReferenceException at OrderService.Load",
  "sql": "select * from orders where id = '...'",
  "server": "order-db-01"
}
```

<!-- anchor: checklist -->
## 检查清单

- 错误响应是否包含稳定的 `code`、`message`、`traceId`，且字段类型固定。
- HTTP 状态码、错误码和错误语义是否与错误码目录、HTTP 状态参考一致。
- 校验失败是否提供字段级明细，并覆盖数组、嵌套对象和文件上传字段。
- 响应、日志和链路追踪是否能通过同一个 `traceId` 关联。
- 错误消息是否已排除堆栈、SQL、密钥、令牌、PII 和内部拓扑。

<!-- anchor: relations -->
## 相关规范

- rules.api-response-shape
- rules.error-handling
- references.error-codes
- references.http-status
- rules.tracing

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
