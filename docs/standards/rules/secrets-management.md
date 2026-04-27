---
id: rules.secrets-management
title: 密钥管理规范
doc_type: rule
status: active
version: 1.1.0
owners: [architecture-team]
roles: [architect, backend, frontend, qa, devops, ai]
stacks: [dotnet, java, python, vue-ts, uniapp]
tags: [security, compliance]
summary: 规定密钥、令牌、证书和连接串的存储与轮换。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# 密钥管理规范

<!-- anchor: goal -->
## 目标

密钥管理规范用于降低密钥、令牌、证书和连接串被硬编码、误记录、过度共享或泄露后无法快速处置的风险。系统必须用一致的存储、注入、轮换、日志屏蔽和泄露响应规则管理所有秘密值。这样开发、测试、部署和排障变更可以被直接审查，不会把凭据扩散到仓库、制品或普通日志中。

<!-- anchor: scope -->
## 适用范围

本规范适用于 `apps/`、`services/`、`src/`、`backend/`、`frontend/`、`mobile/`、部署清单、CI/CD 配置、容器镜像、配置文件、环境变量、数据库连接串、第三方 API token、OAuth 客户端密钥、证书、私钥、Webhook secret、测试夹具和运维脚本。它覆盖密钥生成、存储、引用、运行时注入、轮换、撤销、日志屏蔽和泄露处置。它不适用于公开标识符、非敏感配置或公开证书链中的公钥，但任何可授予访问、签名、解密或连接能力的值都属于本规范范围。

<!-- anchor: rules -->
## 规则

1. 密钥、令牌、私钥、证书口令和连接串必须存储在受控密钥存储、环境注入或等价受保护机制中；不得提交到源码、示例配置、镜像层、锁文件、测试快照或文档。
2. 应用代码和配置必须通过引用、环境变量或运行时注入读取秘密值；不得在业务代码中硬编码密钥，也不得把真实密钥作为默认值。
3. 连接串必须按环境隔离，并优先使用独立账号和最小权限；不得在非生产环境复用生产连接串、生产 token 或生产证书。
4. 日志、错误响应、启动配置摘要、CI 输出和监控标签必须屏蔽秘密值；不得输出完整密钥、连接串、Authorization 头或可恢复的片段。
5. 密钥必须具备轮换和撤销方式；新增密钥时应当说明所有者、使用范围、到期或复核时间以及受影响的服务。
6. 需要共享给本地开发或测试的凭据必须使用低权限、可撤销、非生产专用值；不得通过聊天记录、截图、工单正文或仓库附件传递真实生产密钥。
7. 自动化扫描、评审或运行时告警发现疑似密钥泄露时，必须按泄露边界立即撤销或轮换受影响密钥，并移除仓库、日志、制品和缓存中的可访问副本。
8. 测试代码可以使用明显无效的占位密钥，例如 `dummy-secret-for-test`；不得使用格式上可被外部系统接受的真实凭据样式作为测试数据。

<!-- anchor: examples -->
## 示例

正例：

```yaml
database:
  url: ${SECRET_REF_ORDERS_DATABASE_URL}
oauth:
  clientSecret: ${SECRET_REF_OAUTH_CLIENT_SECRET}
```

正例说明：配置只保存秘密值引用，真实值由受保护机制在运行时注入。

反例：

```shell
export DATABASE_URL="Server=prod-db;User Id=orders;Password=PlainTextPassword123"
echo "DATABASE_URL=$DATABASE_URL"
```

反例说明：生产连接串不得出现在脚本、终端输出、CI 日志或可复制的命令记录中。

<!-- anchor: checklist -->
## 检查清单

- 代码、配置、测试快照、文档、镜像和 CI 输出中是否没有明文密钥、令牌、私钥或连接串。
- 秘密值是否通过引用、环境变量或运行时注入获得，且没有真实默认值。
- 连接串和凭据是否按环境隔离，并使用最小权限账号。
- 日志、错误响应、启动摘要和监控标签是否屏蔽 `Authorization`、token、证书、私钥和连接串。
- 新增或变更密钥是否说明所有者、使用范围、轮换方式和复核时间。
- 疑似泄露时是否撤销或轮换受影响密钥，并清理仓库、日志、制品和缓存中的副本。

<!-- anchor: relations -->
## 相关规范

- rules.configuration
- rules.env-strategy
- rules.dependency-policy
- rules.pii-handling

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
