---
id: rules.idempotency
title: 幂等性规范
doc_type: rule
status: active
version: 1.1.0
owners: [architecture-team]
roles: [architect, backend, qa, devops, ai]
stacks: [dotnet, java, python]
tags: [backend, framework]
summary: 规定请求去重、幂等键和重复消息处理。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# 幂等性规范

<!-- anchor: goal -->
## 目标

幂等性规范用于降低客户端重试、网络超时、重复提交和消息重复投递导致多次扣款、多次创建或状态错乱的风险。写操作和消息消费必须明确哪些路径可安全重试，以及重复请求如何返回稳定结果。这样调用方可以在失败不确定时重试，而不会制造重复副作用。

<!-- anchor: scope -->
## 适用范围

本规范适用于 `apps/`、`services/`、`src/`、`backend/` 中 REST 写接口、后台作业、消息消费者、支付或订单类业务操作、外部回调、批处理任务和集成测试。它覆盖 `Idempotency-Key`、业务唯一键、去重记录、重复响应、并发冲突和重试安全边界。它不适用于纯查询、无副作用计算、临时开发脚本或单次本地任务。

<!-- anchor: rules -->
## 规则

1. 所有可能被客户端或调度器重试的写操作必须声明幂等策略，使用 `Idempotency-Key`、业务唯一键或资源天然唯一标识。
2. `Idempotency-Key` 必须与调用方、目标操作和请求摘要关联；不得让不同用户或不同请求体复用同一键产生错误结果。
3. 首次成功处理后必须保存可重复返回的结果摘要或资源引用；重复请求应当返回与首次成功一致的状态和响应语义。
4. 正在处理中的重复请求必须返回明确冲突或处理中状态；不得并发执行相同副作用。
5. 幂等记录必须有明确生命周期和清理策略，并满足业务审计和重试窗口要求；不得无限增长且无人维护。
6. 消息消费者必须使用消息 ID 或业务键去重；不得依赖 broker 精确一次投递保证。
7. 外部回调和第三方通知必须按外部事件 ID 或业务单号去重，并在验签和输入校验通过后再执行业务副作用。
8. 幂等冲突、重复请求、请求摘要不一致和过期幂等键必须映射到稳定错误码或状态；不得要求调用方解析日志判断结果。

<!-- anchor: examples -->
## 示例

正例：

```http
POST /api/v1/orders
Idempotency-Key: 8b4f3ad0-6c2c-4b9f-a69e-4bfc4fbf7b21
Content-Type: application/json
```

反例：

```python
def create_order(request):
    charge_card(request.card)
    insert_order(request)
```

<!-- anchor: checklist -->
## 检查清单

- 写操作和消息消费是否声明幂等策略和唯一键来源。
- 幂等键是否绑定调用方、操作和请求摘要。
- 重复请求是否返回稳定结果，且不会重复执行副作用。
- 并发重复请求、处理中状态和请求摘要冲突是否有明确响应。
- 幂等记录是否有生命周期、清理策略和审计边界。

<!-- anchor: relations -->
## 相关规范

- rules.api-rest-design
- rules.messaging-patterns
- rules.resilience

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
