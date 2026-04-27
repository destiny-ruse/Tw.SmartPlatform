---
id: rules.editorconfig
title: EditorConfig 规范
doc_type: rule
status: active
version: 1.0.0
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

保持不同编辑器生成一致的文本格式。

<!-- anchor: scope -->
## 适用范围

适用于仓库内所有文本源文件、配置、脚本和文档。

<!-- anchor: rules -->
## 规则

1. 所有文本文件必须使用 UTF-8。
2. 文件末尾保留单个换行。
3. 缩进宽度由语言规范决定，不得在同类文件中混用。

<!-- anchor: examples -->
## 示例

正例：新增语言目录时同步更新 `.editorconfig`。

反例：仅依赖个人 IDE 设置控制格式。

<!-- anchor: checklist -->
## 检查清单

- 是否覆盖新文件类型。
- 是否避免和格式化工具冲突。

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
