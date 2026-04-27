---
id: rules.env-strategy
title: 环境策略规范
doc_type: rule
status: active
version: 1.1.0
owners: [architecture-team]
roles: [backend, frontend, qa, devops, ai]
stacks: []
tags: [cicd, deployment, operations]
summary: 规定开发、测试、预发、生产环境的职责和隔离。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# 环境策略规范

<!-- anchor: goal -->
## 目标

环境策略规范用于降低环境混用、配置漂移、未验证制品晋级和生产数据误用的风险。它要求开发、测试、预发和生产环境在职责、隔离、晋级和漂移复核上保持一致，让发布与回滚具备清晰边界。

<!-- anchor: scope -->
## 适用范围

适用于源码构建、制品晋级、部署配置、环境变量、密钥引用、数据库和缓存实例、消息主题、外部依赖端点、CI/CD 作业和运行环境评审。个人本地实验环境可以使用简化配置，但不得连接生产资源、复用生产密钥或绕过提交前验证。

<!-- anchor: rules -->
## 规则

1. 环境必须按职责隔离，开发、测试、预发和生产不得共享可写数据库、缓存、消息主题或密钥。
2. 同一提交构建出的制品应当在环境间晋级，不得为生产单独重新构建无法追溯的制品。
3. 环境差异必须通过配置和密钥引用表达，不得通过代码分支、硬编码环境名或手工修改制品表达。
4. 生产环境必须使用生产专属密钥和外部端点；非生产环境不得默认访问生产数据或生产第三方服务。
5. 晋级到更高环境前必须满足对应质量门禁和启动配置校验，失败时不得通过手工覆盖长期绕过。
6. 环境配置、部署清单和关键依赖端点应当定期或在发布前进行漂移复核，发现差异必须归因、修复或记录例外。
7. 数据库迁移、缓存失效、消息订阅和后台任务在各环境中的启用边界必须明确，避免非生产任务影响生产资源。
8. 回滚必须优先回滚制品和配置到已知版本，环境级热修后必须补齐代码、配置和审计记录。

<!-- anchor: examples -->
## 示例

正例：

```yaml
artifact:
  image: registry.example.com/smart/catalog:1.8.3-4f2a91c
promotion:
  from: staging
  to: production
config:
  DATABASE_URL: ${SECRET_REF_PROD_DATABASE_URL}
```

正例说明：生产使用已验证制品晋级，差异通过环境配置和密钥引用表达。

反例：

```shell
ssh prod-web-01
vim appsettings.Production.json
docker restart catalog
```

反例说明：直接修改生产环境配置缺少版本、评审、漂移记录和回滚路径。

<!-- anchor: checklist -->
## 检查清单

- 各环境是否隔离数据库、缓存、消息主题、密钥和外部端点。
- 制品是否从同一提交构建并按环境晋级，标签是否可追溯。
- 环境差异是否仅通过配置和密钥引用表达。
- 晋级前是否通过质量门禁和启动配置校验。
- 是否完成或安排配置漂移复核，并记录例外原因和到期时间。
- 回滚路径是否覆盖制品、配置、迁移和环境级热修记录。

<!-- anchor: relations -->
## 相关规范

- rules.configuration
- rules.secrets-management
- rules.deploy-rollback
- rules.ci-pipeline

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
