---
id: references.error-codes
title: 错误码目录
doc_type: reference
status: active
version: 1.0.0
owners: [architecture-team]
roles: [backend, frontend, qa, ai]
stacks: []
tags: [api, error-handling, reference]
summary: 定义通用错误码分类、命名方式和使用场景。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# 错误码目录

<!-- anchor: goal -->
## 目标

为 API、测试和用户提示提供稳定的错误码引用目录。

<!-- anchor: scope -->
## 适用范围

适用于 REST API、前端错误处理、自动化测试断言和运维排障。

<!-- anchor: catalog -->
## 目录

| 前缀 | 场景 | 示例 |
| --- | --- | --- |
| `AUTH` | 认证与授权 | `AUTH.UNAUTHORIZED` |
| `VALIDATION` | 输入校验 | `VALIDATION.REQUIRED_FIELD` |
| `RESOURCE` | 资源状态 | `RESOURCE.NOT_FOUND` |
| `SYSTEM` | 系统错误 | `SYSTEM.UNAVAILABLE` |

<!-- anchor: examples -->
## 示例

正例：客户端根据 `VALIDATION.REQUIRED_FIELD` 定位表单项。

反例：客户端解析自然语言错误消息来判断业务分支。

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.0.0 | 2026-04-27 | 建立错误码目录。 |
