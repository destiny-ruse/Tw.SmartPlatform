---
id: rules.auth-oauth-oidc
title: OAuth 与 OIDC 认证规范
doc_type: rule
status: active
version: 1.0.0
owners: [architecture-team]
roles: [architect, backend, frontend, ai]
stacks: [dotnet, java, python, vue-ts, uniapp]
tags: [security, auth, oauth, oidc]
summary: 规定 OAuth2/OIDC 登录、令牌、刷新和权限边界要求。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# OAuth 与 OIDC 认证规范

<!-- anchor: goal -->
## 目标

OAuth 与 OIDC 认证规范的目标是让团队使用一致、可审查、可自动化辅助的工程约定。

<!-- anchor: scope -->
## 适用范围

适用于统一身份认证、前后端登录、服务间调用和移动端授权。

<!-- anchor: rules -->
## 规则

1. 用户登录必须使用授权码模式和 PKCE，禁止在浏览器中保存长期密钥。
2. 访问令牌必须短期有效，刷新令牌必须可撤销并绑定客户端。
3. 后端必须在信任边界内校验签名、过期时间、受众和权限声明。
4. 前端不得根据未校验的本地声明决定服务端授权结果。

<!-- anchor: examples -->
## 示例

正例：变更前说明意图，变更中保持单一职责，评审记录标出影响范围。

反例：一次提交混合重构、功能、格式化和临时调试代码，导致评审者无法判断真实风险。

<!-- anchor: checklist -->
## 检查清单

- 规则是否能被评审者独立检查。
- 例外是否有明确说明和责任人。
- 相关变更是否更新测试、文档或索引。

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
