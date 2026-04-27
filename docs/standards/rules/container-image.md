---
id: rules.container-image
title: 容器镜像规范
doc_type: rule
status: active
version: 1.0.0
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

容器镜像规范用于让工程实践在安全性、可维护性和可验证性上保持一致。

<!-- anchor: scope -->
## 适用范围

适用于构建、制品、部署、运行环境和发布治理。

<!-- anchor: rules -->
## 规则

1. 发布流程必须可重复、可审计、可回滚。
2. 质量门禁失败不得绕过，例外必须记录审批和到期时间。
3. 制品必须能追溯到提交、版本和构建环境。
4. 部署后必须执行健康检查和关键路径验证。

<!-- anchor: examples -->
## 示例

正例：镜像标签包含版本和提交短 SHA。

反例：生产热修后不补提交、不补构建记录。

<!-- anchor: checklist -->
## 检查清单

- 是否有构建和部署证据。
- 是否定义回滚触发条件。
- 是否记录制品来源。

<!-- anchor: relations -->
## 相关规范

关联 CI、容器镜像、健康检查、SLO 和变更治理规范。

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
