---
id: rules.editorconfig
title: EditorConfig 规范
doc_type: rule
status: active
version: 1.1.0
owners: [architecture-team]
roles: [architect, backend, frontend, qa, devops, ai]
stacks: []
tags: [editorconfig, governance]
summary: 规定跨编辑器缩进、换行、编码和文件末尾行为。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# EditorConfig 规范

<!-- anchor: goal -->
## 目标

EditorConfig 规范用于降低跨编辑器格式漂移、无意义 diff、编码不一致和换行符冲突的风险。统一缩进、编码、行尾和文件末尾行为后，代码评审可以聚焦真实工程变更。该规范还为格式化工具和语言规则提供基础边界。

<!-- anchor: scope -->
## 适用范围

本规范适用于仓库内源码、测试、脚本、配置、Markdown、YAML、JSON、SQL、生成源文件和文档等文本文件的编辑器格式约定。它不适用于二进制资源、第三方 vendored 文件、外部生成且不可格式化的快照文件，或由协议工具严格控制格式的产物。

<!-- anchor: rules -->
## 规则

1. 仓库根目录必须提供 `.editorconfig`，并设置 `root = true`；不得只依赖个人 IDE 或团队口头约定。
2. 所有普通文本文件必须使用 `charset = utf-8`，并在文件末尾保留单个换行；不得引入混合编码或无结尾换行的文本文件。
3. 行尾必须按仓库约定统一，跨平台项目应当优先使用 `end_of_line = lf`；不得在同一文件或同类文件中混用 CRLF 与 LF。
4. 缩进风格和宽度必须按语言或文件类型声明，例如 C#、Java、Python、TypeScript、YAML、JSON；不得在同类文件中混用 tab 和 space。
5. `.editorconfig` 必须覆盖新增的主要文件类型或目录；新增语言、脚本或配置格式时应当同步更新规则。
6. EditorConfig 与格式化工具、lint preset 或语言标准冲突时，必须调整为同一结果；不得让开发者在保存和 CI 格式化之间反复产生 diff。
7. 生成文件如果进入版本管理，应当明确是否服从 `.editorconfig`；不可格式化的生成文件必须通过路径规则或说明排除。
8. 大规模格式化应当与功能变更分开提交；不得把格式化噪音隐藏在业务提交中。

<!-- anchor: examples -->
## 示例

正例：

```ini
root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true

[*.{json,yml,yaml}]
indent_style = space
indent_size = 2
```

反例：

```text
仅在 README 中要求大家把 IDE 调成一样，但仓库没有 .editorconfig。
```

<!-- anchor: checklist -->
## 检查清单

- 仓库根目录是否有 `.editorconfig` 且声明 `root = true`。
- 新增文件类型是否有编码、行尾、结尾换行和缩进规则。
- EditorConfig 与格式化工具、lint 和语言规范是否一致。
- 生成文件是否明确服从或排除格式化规则。
- 本次提交是否避免把大规模格式化混入业务变更。

<!-- anchor: relations -->
## 相关规范

- rules.repo-layout
- rules.naming-common

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
