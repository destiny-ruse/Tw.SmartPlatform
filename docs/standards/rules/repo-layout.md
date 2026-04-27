---
id: rules.repo-layout
title: 仓库目录布局规范
doc_type: rule
status: active
version: 1.0.0
owners: [architecture-team]
roles: [architect, backend, frontend, qa, devops, ai]
stacks: []
tags: [repo-layout, governance]
summary: 规定源码、测试、文档、工具和生成产物的目录职责。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# 仓库目录布局规范

<!-- anchor: goal -->
## 目标

让仓库目录职责清晰，降低查找和维护成本。

<!-- anchor: scope -->
## 适用范围

适用于项目根目录、服务目录、前端目录、工具目录和文档目录。

<!-- anchor: rules -->
## 规则

1. 源码、测试、文档、脚本和生成产物必须有清晰边界。
2. 公共工具放入 `tools/`，工程文档放入 `docs/`。
3. 不得把临时实验文件放入正式目录。

<!-- anchor: examples -->
## 示例

正例：规范工具位于 `tools/standards/`。

反例：在业务源码目录散落一次性脚本。

<!-- anchor: checklist -->
## 检查清单

- 新目录是否有明确职责。
- README 是否说明入口。
- 生成产物是否可追溯。

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
