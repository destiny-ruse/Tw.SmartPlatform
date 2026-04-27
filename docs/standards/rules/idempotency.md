---
id: rules.idempotency
title: 幂等性规范
doc_type: rule
status: active
version: 1.0.0
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

幂等性规范用于让工程实践在安全性、可维护性和可验证性上保持一致。

<!-- anchor: scope -->
## 适用范围

适用于后端服务、批处理任务、消息消费者和公共框架能力。

<!-- anchor: rules -->
## 规则

1. 框架能力必须有默认值和覆盖方式。
2. 失败路径必须可观测、可测试、可恢复。
3. 面向调用方的错误必须映射为稳定契约。
4. 不得把环境差异硬编码在业务逻辑中。

<!-- anchor: examples -->
## 示例

正例：为外部调用设置超时、重试上限和熔断指标。

反例：无限重试导致下游故障扩大。

<!-- anchor: checklist -->
## 检查清单

- 是否覆盖失败路径。
- 是否有默认值和覆盖说明。
- 是否包含观测指标。

<!-- anchor: relations -->
## 相关规范

关联日志、指标、健康检查、API 错误和部署回滚规范。

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
