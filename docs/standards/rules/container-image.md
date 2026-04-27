---
id: rules.container-image
title: 容器镜像规范
doc_type: rule
status: active
version: 1.1.0
owners: [architecture-team]
roles: [backend, frontend, qa, devops, ai]
stacks: []
tags: [cicd, deployment, operations]
summary: 规定镜像构建、标签、安全扫描和基础镜像。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# 容器镜像规范

<!-- anchor: goal -->
## 目标

容器镜像规范用于降低运行环境漂移、镜像不可追溯、密钥泄漏和漏洞进入部署环境的风险。镜像必须以可重复构建、可扫描、可定位源码和可回滚为目标，让 CI、部署和运维排障使用同一份制品事实。镜像约定应当让运行时只包含应用需要的最小内容。

<!-- anchor: scope -->
## 适用范围

本规范适用于 Dockerfile、容器构建脚本、镜像标签、基础镜像选择、构建上下文、镜像扫描、镜像元数据和推送到镜像仓库的应用制品。它不适用于开发者本地一次性调试容器、第三方镜像的上游发布流程或不进入部署流水线的教学示例。

<!-- anchor: rules -->
## 规则

1. 镜像必须使用明确版本的基础镜像或内部受控基础镜像；不得在发布构建中使用漂移的 `latest` 基础镜像。
2. 应用镜像标签必须包含语义版本、构建版本或提交短 SHA 之一，并保留可用于回滚的不可变标签；不得只发布 `latest`。
3. 镜像必须写入可追溯标签或 label，至少包含源码提交、仓库路径、构建时间和版本；不得发布无法映射到提交的镜像。
4. Dockerfile 必须通过多阶段构建或等效方式排除编译缓存、测试数据、源码临时目录和包管理器凭据；不得把 `.git`、密钥、证书私钥或本地配置复制进镜像。
5. 镜像应当以非 root 用户运行，并只暴露应用必需端口、入口命令和运行目录；确需 root 的场景必须在部署说明中说明边界。
6. 镜像构建必须在 CI 中执行安全扫描或依赖扫描，并记录结果；发现高风险问题时必须修复、升级基础镜像或记录可复查的风险处置说明。
7. 构建上下文必须由 `.dockerignore` 控制，并与仓库忽略规则保持一致；不得依赖人工选择文件避免敏感内容进入上下文。
8. 生成的镜像清单、SBOM 或扫描报告属于发布证据，应当与流水线运行和镜像标签关联；不得手工改写已发布镜像的来源信息。

<!-- anchor: examples -->
## 示例

正例：

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
LABEL org.opencontainers.image.revision="abc1234"
WORKDIR /app
USER 10001
COPY --from=build /out/ ./
ENTRYPOINT ["dotnet", "Order.Api.dll"]
```

反例：

```dockerfile
FROM node:latest
COPY . /app
ENV DATABASE_PASSWORD=example
CMD ["npm", "start"]
```

<!-- anchor: checklist -->
## 检查清单

- 基础镜像是否固定版本或来自受控来源。
- 镜像标签和 label 是否能追溯到源码提交、版本和流水线运行。
- 构建上下文、镜像层和日志中是否排除了密钥、本地配置和 `.git`。
- 镜像是否经过安全扫描，扫描结果是否与发布证据关联。
- 运行用户、入口命令、端口和工作目录是否清晰且最小化。
- 回滚需要的不可变镜像标签是否保留。

<!-- anchor: relations -->
## 相关规范

- rules.ci-pipeline
- rules.secrets-management
- rules.k8s-resource-naming

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
