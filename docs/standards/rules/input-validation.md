---
id: rules.input-validation
title: 输入校验规范
doc_type: rule
status: active
version: 1.0.0
owners: [architecture-team]
roles: [architect, backend, frontend, qa, devops, ai]
stacks: [dotnet, java, python, vue-ts, uniapp]
tags: [security, compliance]
summary: 规定外部输入、请求参数和文件上传的校验边界。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# 输入校验规范

<!-- anchor: goal -->
## 目标

输入校验规范用于让工程实践在安全性、可维护性和可验证性上保持一致。

<!-- anchor: scope -->
## 适用范围

适用于所有接收外部输入、处理敏感数据或引入第三方组件的系统。

<!-- anchor: rules -->
## 规则

1. 必须在信任边界入口进行校验。
2. 必须记录安全相关例外和审批。
3. 禁止把密钥、个人敏感信息或未校验输入写入日志。
4. 禁止绕过安全扫描或以临时白名单长期规避风险。

<!-- anchor: examples -->
## 示例

正例：服务端在 DTO 绑定后执行字段级校验并返回稳定错误码。

反例：仅依赖前端校验，后端直接信任请求体。

<!-- anchor: checklist -->
## 检查清单

- 是否识别信任边界。
- 是否有禁止实践检查。
- 是否有测试或扫描证据。

<!-- anchor: relations -->
## 相关规范

关联认证、错误响应、依赖引入和日志规范。

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
