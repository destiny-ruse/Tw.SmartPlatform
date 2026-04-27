---
id: rules.auth-oauth-oidc
title: OAuth 与 OIDC 认证规范
doc_type: rule
status: active
version: 1.1.0
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

OAuth 与 OIDC 认证规范用于降低登录流程选型错误、令牌误用、权限边界漂移和错误响应泄露身份信息的风险。系统必须用一致的 OAuth2/OIDC 流程、令牌校验和授权检查表达身份边界，使后端、前端、移动端和服务间调用可以被稳定审查。认证结果不得替代业务授权，授权决策必须在服务端可信边界内完成。

<!-- anchor: scope -->
## 适用范围

本规范适用于 `apps/`、`services/`、`src/`、`backend/`、`frontend/`、`mobile/` 中使用 OAuth2/OIDC 的 Web 登录、移动端登录、后端 API、网关、服务间调用、回调端点、Token 解析中间件、OpenAPI 安全定义、认证配置和集成测试。它覆盖授权码、PKCE、客户端凭据、重定向 URI、访问令牌、刷新令牌、Scope、Audience、用户声明和错误响应。它不适用于不跨系统边界的本地开发假身份、一次性手工排查脚本或纯离线批处理内部主体，但这些场景不得复用生产令牌或绕过生产授权边界。

<!-- anchor: rules -->
## 规则

1. 用户交互式登录必须使用授权码模式并启用 PKCE；浏览器、移动端和桌面客户端不得使用隐式模式，也不得保存客户端密钥。
2. 服务间调用必须使用适合非用户交互的授权方式，例如客户端凭据或等价的工作负载身份；不得使用个人用户刷新令牌驱动后台任务。
3. 重定向 URI 必须使用精确匹配的已登记 URI，并限制协议、主机、端口和路径；不得接受通配域名、开放跳转参数或运行时拼接的回调地址。
4. 访问令牌必须短期有效，后端必须校验签名、颁发方、过期时间、Audience、Scope 和关键声明；不得只在前端解析令牌后信任其结果。
5. 刷新令牌必须绑定客户端、可撤销并具备轮换或等价的重放防护；不得把刷新令牌写入普通日志、URL、前端可读持久存储或错误响应。
6. Scope 必须表达调用能力，Audience 必须表达目标资源；API 必须同时校验两者，不得用登录成功或存在用户 ID 替代资源授权。
7. 授权边界必须放在服务端、网关或资源服务的可信执行路径上；前端可以控制可见性，但不得决定最终访问权限。
8. 认证和授权失败必须返回稳定、安全的错误响应；不得暴露令牌内容、用户存在性判断、身份提供方内部错误、堆栈或配置细节。

<!-- anchor: examples -->
## 示例

正例：

```yaml
oauth:
  flow: authorization_code
  pkce: required
  redirectUris:
    - https://app.example.com/auth/callback
  tokenValidation:
    issuer: https://idp.example.com
    audience: smart-platform-api
    requiredScopes: [orders.read]
```

正例说明：交互式登录使用授权码和 PKCE，回调 URI 精确登记，资源服务校验签发方、受众和权限范围。

反例：

```typescript
const claims = JSON.parse(atob(token.split(".")[1]));
if (claims.role === "admin") {
  showAdminActions();
  await fetch("/api/admin/users");
}
```

反例说明：前端解析声明只能用于体验展示，不能替代服务端对令牌签名、Audience、Scope 和业务权限的校验。

<!-- anchor: checklist -->
## 检查清单

- 登录流程是否按客户端类型选择授权码加 PKCE、客户端凭据或等价安全流程。
- 重定向 URI 是否精确登记，并排除通配、开放跳转和运行时拼接风险。
- 后端是否校验令牌签名、颁发方、过期时间、Audience、Scope 和关键声明。
- 刷新令牌是否绑定客户端、可撤销并避免进入日志、URL 和前端可读存储。
- 授权判断是否位于服务端可信边界，前端可见性控制是否未被当作权限控制。
- 认证和授权失败响应是否不泄露令牌、用户枚举线索、堆栈或身份提供方内部细节。

<!-- anchor: relations -->
## 相关规范

- rules.secrets-management
- rules.pii-handling
- rules.api-error-response

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
