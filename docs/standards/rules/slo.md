---
id: rules.slo
title: SLO 规范
doc_type: rule
status: active
version: 1.1.0
owners: [architecture-team]
roles: [architect, backend, qa, devops, ai]
stacks: [dotnet, java, python]
tags: [backend, framework]
summary: 规定服务目标、错误预算和告警口径。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# SLO 规范

<!-- anchor: goal -->
## 目标

SLO 规范用于降低服务可靠性目标口径不一致、告警只反映单点症状、错误预算无法指导取舍的风险。服务必须用可度量 SLI 和明确窗口描述用户可感知可靠性，并把发布、降级和事故复盘连接到同一目标。这样团队可以在功能交付、风险控制和可靠性改进之间做可审计决策。

<!-- anchor: scope -->
## 适用范围

本规范适用于 `apps/`、`services/`、`src/`、`backend/` 中面向用户或上下游系统提供能力的 HTTP API、gRPC 服务、消息消费者、批处理任务、后台作业、网关适配器、SLO 文档、仪表盘、告警规则和发布评审材料。它覆盖 SLI 定义、SLO 目标文字、统计窗口、排除边界、错误预算、告警口径和评审记录。它不适用于尚未承诺生产可靠性的实验性原型、一次性迁移脚本或仅供本地开发的工具。

<!-- anchor: rules -->
## 规则

1. 每个生产服务必须声明至少一个面向用户或调用方结果的 SLI，例如成功率、延迟、可用性、数据新鲜度或任务完成率；不得只使用 CPU、内存或进程存活作为可靠性目标。
2. SLO 必须以清晰文字说明服务对象、成功条件、统计窗口、目标值来源和排除边界；不得只写“高可用”“尽快”“稳定”这类无法评审的描述。
3. SLI 必须绑定到可查询指标、健康检查或作业结果，并说明过滤维度和聚合方式；不得要求人工阅读日志来计算 SLO。
4. 错误预算必须说明消耗来源、复核节奏和触发的工程讨论边界；不得把错误预算等同于自动冻结发布或固定审批流程。
5. 告警必须优先覆盖用户可感知的 SLO 消耗、快速恶化或持续不可用；不得仅因单个实例、单个依赖探针或瞬时噪声触发高优先级告警。
6. 发布、回滚、降级和韧性策略评审应当引用相关 SLI/SLO；不得在可靠性目标受影响时只用主观判断推进变更。
7. SLO 文档必须声明哪些错误、维护窗口、测试流量、调用方错误或外部不可控事件会被排除或单独标记；不得事后临时改口径掩盖事故影响。
8. 新增、修改或删除 SLO 必须更新仪表盘、告警、运行手册或评审材料中的引用；不得让目标、图表和告警口径分叉。

<!-- anchor: examples -->
## 示例

正例：

```yaml
slo:
  service: order-api
  sli: successful_order_create_requests
  successCondition: "HTTP 2xx or documented business conflict response"
  window: "service-defined rolling window"
  excludes:
    - caller validation errors
    - synthetic test traffic
  evidence:
    metric: http_server_requests_total
    labels: [service, operation, status]
```

反例：

```text
订单服务必须稳定，出现问题时及时处理。
```

<!-- anchor: checklist -->
## 检查清单

- SLI 是否表达用户或调用方可感知结果，而不是只表达资源利用率。
- SLO 是否包含服务对象、成功条件、统计窗口、目标值来源和排除边界。
- SLI 是否能通过指标、健康检查或作业结果直接查询和复现。
- 错误预算是否说明消耗来源、复核节奏和工程讨论边界。
- 告警是否围绕 SLO 消耗、快速恶化或持续不可用，而不是瞬时噪声。
- 发布、回滚、降级和事故复盘是否引用同一 SLO 口径。

<!-- anchor: relations -->
## 相关规范

- rules.metrics
- rules.health-checks
- rules.resilience
- rules.deploy-rollback

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
