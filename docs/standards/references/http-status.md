---
id: references.http-status
title: HTTP 状态码参考
doc_type: reference
status: active
version: 1.0.0
owners: [architecture-team]
roles: [backend, frontend, qa, ai]
stacks: []
tags: [api, http, reference]
summary: 定义常用 HTTP 状态码的工程语义和使用边界。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# HTTP 状态码参考

<!-- anchor: goal -->
## 目标

统一 API 设计、测试断言和客户端处理中的 HTTP 状态码含义。

<!-- anchor: scope -->
## 适用范围

适用于 API、文档、测试、监控和跨团队沟通中的统一引用。

<!-- anchor: catalog -->
## 目录

| 状态码 | 场景 | 说明 |
| --- | --- | --- |
| 200 | 成功查询 | 返回资源或结果。 |
| 201 | 创建成功 | 返回新资源标识或位置。 |
| 400 | 请求无效 | 输入格式或校验失败。 |
| 401 | 未认证 | 缺少有效身份。 |
| 403 | 无权限 | 已认证但无权访问。 |
| 404 | 未找到 | 资源不存在或不可见。 |
| 409 | 冲突 | 版本、状态或幂等冲突。 |
| 500 | 服务错误 | 未预期系统错误。 |

<!-- anchor: examples -->
## 示例

正例：在设计文档中引用统一术语或状态码含义。

反例：不同团队为同一概念使用不同名称。

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.0.0 | 2026-04-27 | 建立参考目录。 |
