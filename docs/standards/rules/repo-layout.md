---
id: rules.repo-layout
title: 仓库目录布局规范
doc_type: rule
status: active
version: 1.1.0
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

仓库目录布局规范用于降低目录职责不清、工具入口分散、测试与源码失配和生成产物被误改的风险。统一目录职责后，开发者、评审者和 AI 助手可以快速定位源码、测试、文档、工具和发布证据。布局规则还保证标准索引和生成目录由工具负责维护。

<!-- anchor: scope -->
## 适用范围

本规范适用于仓库根目录、源码目录、测试目录、文档目录、工具目录、配置目录、部署目录、生成目录和标准索引目录的职责划分。它不适用于第三方依赖内部目录、包管理器缓存、构建输出目录或开发者本地临时实验目录。

<!-- anchor: rules -->
## 规则

1. 源码、测试、文档、工具、部署资产、配置和生成产物必须有清晰目录边界；不得把一次性脚本、实验文件或构建输出散落在正式源码目录。
2. 业务源码应当放入项目约定的 `src/`、`apps/`、`services/`、`backend/`、`frontend/` 或模块目录；不得把可运行代码隐藏在 `docs/` 或 `tools/` 中。
3. 测试目录必须能从路径或命名关联到被测源码，例如 `tests/`、`*.Tests/`、`src/**/__tests__/`；不得把长期测试样例混入生产代码目录且无命名区分。
4. 工程文档必须放入 `docs/`，标准规范必须位于 `docs/standards/` 的对应 `rules/`、`processes/`、`references/` 或 `decisions/` 子目录。
5. 共享工具、生成器和校验脚本必须放入 `tools/` 或项目约定工具目录，并提供可从仓库根目录运行的入口；不得只依赖个人本地路径。
6. 部署资产和运行配置应当集中在明确目录，例如 `deploy/`、`k8s/`、`helm/`、`infra/` 或服务内约定位置；不得与源码文件混放到无法识别发布边界。
7. 生成产物必须放入可识别目录或使用可识别后缀，并说明生成来源；不得手工维护工具声明为 generated 的文件。
8. `docs/standards/index.generated.json` 和 `docs/standards/_index/**` 只能由标准工具生成和更新；不得手工编辑生成索引内容。
9. 新增顶层目录必须有明确职责，并在 README、目录文档或相邻说明中提供入口；不得新增含义不明的 `misc/`、`new/`、`temp/` 顶层目录。

<!-- anchor: examples -->
## 示例

正例：

```text
docs/standards/rules/repo-layout.md
tools/standards/standards.py
docs/standards/_index/sections/rules.repo-layout.generated.json
```

反例：

```text
src/tmp-check.ps1
docs/run-production-job.py
misc/new2/final-config.yaml
```

<!-- anchor: checklist -->
## 检查清单

- 新增或移动目录是否有明确职责和可发现入口。
- 源码、测试、文档、工具、配置、部署资产和生成产物是否边界清晰。
- 测试路径是否能关联到被测源码或模块。
- 工具脚本是否位于共享工具目录，并能从仓库根目录执行。
- 生成文件是否可追溯到源文件和生成命令，且未被手工编辑。
- 标准索引是否只由 `tools/standards/standards.py generate-index` 更新。

<!-- anchor: relations -->
## 相关规范

- rules.editorconfig
- rules.ci-pipeline
- rules.test-strategy
- standards.authoring

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
