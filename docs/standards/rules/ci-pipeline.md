---
id: rules.ci-pipeline
title: CI 流水线规范
doc_type: rule
status: active
version: 1.1.0
owners: [architecture-team]
roles: [backend, frontend, qa, devops, ai]
stacks: []
tags: [cicd, deployment, operations]
summary: 规定流水线阶段、缓存、并发和质量门禁。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# CI 流水线规范

<!-- anchor: goal -->
## 目标

CI 流水线规范用于降低构建不可重复、质量门禁被绕过、制品来源不清和部署失败难以定位的风险。所有进入主干或发布候选的变更必须通过一致的验证路径，让提交、测试、构建、制品和部署结果可以被审查和追溯。流水线应当把自动化检查结果作为合并和发布判断的共同语言。

<!-- anchor: scope -->
## 适用范围

本规范适用于仓库内 CI 配置、构建脚本、测试命令、质量扫描、缓存配置、制品发布、镜像构建触发和部署流水线编排。它覆盖应用代码、文档、配置、容器和 Kubernetes 清单的自动化验证；不适用于个人本地临时脚本、一次性排障命令或不进入仓库历史的实验性验证。

<!-- anchor: rules -->
## 规则

1. 流水线必须至少区分依赖安装、静态检查、测试、构建、制品生成和发布前验证阶段；不得把所有动作合并为无法定位失败原因的单一步骤。
2. 主干、发布分支和 PR 的质量门禁必须包含与变更相关的格式、lint、测试或契约检查；门禁失败不得通过修改脚本返回码、跳过命令或提交空结果绕过。
3. 缓存必须以锁文件、运行时版本、操作系统和构建工具版本作为边界；不得跨语言、跨平台或跨安全上下文复用可能污染结果的缓存。
4. 生成的制品必须记录提交 SHA、分支或 tag、构建时间、流水线运行标识和构建命令；不得发布无法追溯到源码版本的包、镜像或压缩文件。
5. 流水线中的密钥必须来自受控运行环境变量或密钥存储；不得写入日志、缓存、构建上下文、制品或示例配置。
6. 与部署相关的流水线必须在发布前验证镜像标签、配置来源和回滚候选；不得只凭人工说明认定发布制品可回滚。
7. 生成索引、契约、客户端或文档产物的流水线必须校验源文件和生成文件一致；不得让生成产物长期落后于源定义。
8. 流水线变更应当与受影响的构建、测试或发布样例一起提交；不得在没有验证证据的情况下修改共享 CI 模板。

<!-- anchor: examples -->
## 示例

正例：

```yaml
steps:
  - name: test
    run: pytest
  - name: build
    run: docker build --label org.opencontainers.image.revision=$GIT_SHA -t app:1.4.0-$SHORT_SHA .
  - name: verify-standards
    run: python tools/standards/standards.py check
```

反例：

```yaml
steps:
  - name: all
    run: npm install && npm test || true
  - name: publish
    run: docker push app:latest
```

<!-- anchor: checklist -->
## 检查清单

- 是否能从流水线阶段定位失败发生在安装、检查、测试、构建、制品还是部署验证。
- 质量门禁是否覆盖本次变更影响的代码、配置、契约、文档或生成产物。
- 缓存键是否包含锁文件、运行时和平台边界，且不会复用不可信产物。
- 制品是否能追溯到提交 SHA、tag、构建命令和流水线运行。
- 日志、缓存、构建上下文和制品中是否排除了密钥。
- 生成产物是否由流水线验证为最新。

<!-- anchor: relations -->
## 相关规范

- rules.test-strategy
- rules.container-image
- rules.deploy-rollback
- rules.git-commit

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
