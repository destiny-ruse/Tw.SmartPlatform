---
id: rules.health-checks
title: 健康检查规范
doc_type: rule
status: active
version: 1.0.0
owners: [architecture-team]
roles: [backend, qa, devops, ai]
stacks: [dotnet, java, python]
tags: [observability, operations]
summary: 规定存活、就绪和依赖检查的语义。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# 健康检查规范

<!-- anchor: goal -->
## 目标

健康检查规范用于让工程实践在安全性、可维护性和可验证性上保持一致。

<!-- anchor: scope -->
## 适用范围

适用于服务、任务、网关、基础设施组件和运维告警。

<!-- anchor: rules -->
## 规则

1. 可观测信号必须可关联到服务、环境、版本和请求。
2. 必须避免记录敏感数据。
3. 就绪检查必须在 1 秒内返回，连续 3 次失败应触发摘流。
4. 告警必须有明确处理人和降噪策略。

<!-- anchor: examples -->
## 示例

正例：错误日志包含 traceId、错误码和业务键。

反例：只输出自然语言错误，无法定位请求链路。

<!-- anchor: checklist -->
## 检查清单

- 是否定义可度量信号。
- 是否可按环境和版本过滤。
- 是否避免敏感字段。

<!-- anchor: relations -->
## 相关规范

关联健康检查、SLO、错误处理和部署回滚规范。

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
