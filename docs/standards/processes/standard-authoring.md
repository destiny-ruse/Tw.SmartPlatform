---
id: standards.authoring
title: 规范撰写规范
doc_type: process
status: active
version: 2.0.0
owners: [architecture-team]
roles: [architect, ai]
stacks: []
tags: [standards, governance, ai]
summary: 规定工程规范文档的元数据、章节结构、索引生成、AI 按需引用和机器规则绑定方式。
machine_rules:
  - id: standard-authoring.rules
    path: tools/standards/rules/standard-authoring.rules.json
    type: schema
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# 规范撰写规范

<!-- anchor: goal -->
## 目标

本规范定义工程规范文档的最小结构、路由元数据、章节粒度、索引生成、AI 引用方式和机器规则绑定方式。所有进入 `docs/standards/` 的正式规范都必须遵守本规范。

<!-- anchor: scope -->
## 适用范围

本规范适用于以下内容：

1. 工程实践、编码规范、交付约定和质量标准。
2. 面向 AI 或自动化工具检索的规范文档。
3. 与 lint、schema、模板或配置文件存在绑定关系的双形态规范。

`docs/standards/_meta/` 用于存放检索契约、词表和索引结构说明，不作为正式规范文档参与标准索引。

<!-- anchor: structure -->
## 文档结构

规范文档必须使用 Markdown，并包含以下结构：

1. front matter 元数据。
2. 一个一级标题，标题应与 `title` 保持一致。
3. 显式 anchor 注释和紧随其后的二级或三级标题。
4. 可独立引用的章节，每个章节只表达一个主题。
5. 变更记录。

章节 anchor 必须使用 `<!-- anchor: token -->`，token 使用小写字母、数字和连字符。需要更细粒度引用时，可在章节内使用 `<!-- region: token -->` 与 `<!-- endregion: token -->` 标记稳定行范围。

<!-- anchor: metadata -->
## 元数据要求

每篇规范必须声明以下字段：

1. `id`：全仓唯一，使用点分命名，例如 `standards.authoring`。
2. `title`：规范标题。
3. `doc_type`：只能是 `rule`、`reference`、`process` 或 `decision`。
4. `status`：只能是 `draft`、`active`、`deprecated` 或 `superseded`。
5. `version`：单篇规范版本，使用 `MAJOR.MINOR.PATCH`。
6. `owners`：责任团队或角色。
7. `roles`：用于检索路由的角色词表。
8. `stacks`：用于检索路由的技术栈词表，可为空数组。
9. `tags`：用于检索路由的主题标签。
10. `summary`：一句话说明规范的适用场景。
11. `machine_rules`：与规范绑定的机器可执行规则，可为空数组。
12. `supersedes`：当前规范替代的旧规范 ID，可为空数组。
13. `superseded_by`：替代当前规范的新规范 ID，可为空。
14. `review_after`：下次复核日期，格式为 `YYYY-MM-DD`。

v2 元数据禁止使用 `applies_to`。检索路由必须拆分到 `roles`、`stacks`、`doc_type` 和 `tags`。

<!-- anchor: lifecycle -->
## 状态管理

规范状态含义如下：

1. `draft`：草稿，可讨论，不强制执行。
2. `active`：已生效，可被工具、CI 和 AI 引用。
3. `deprecated`：不推荐新增使用，但保留历史追踪。
4. `superseded`：已被新规范替代，必须填写 `superseded_by`。

规范从 `draft` 变更为 `active` 前，应完成必要评审，并确认机器规则、索引和链接校验通过。

<!-- anchor: retrieval -->
## AI 引用要求

AI 和自动化工具必须先读取 `docs/standards/index.generated.json` 定位候选规范，再按需读取 L1 分片、L2 section index 和目标 Markdown 行范围。

引用规范结论时，至少说明：

1. 规范 ID。
2. anchor 或 region。
3. 规范版本。
4. Markdown 文件路径。

AI 不应一次性加载全部规范正文，也不应把规范正文复制到固定 Skill 中。规范变更后，只需要重新生成索引。

<!-- anchor: machine-rules -->
## 机器规则绑定

当规范存在可执行形态时，必须在 `machine_rules` 中声明绑定关系。

每条机器规则必须包含：

1. `id`：规则 ID。
2. `path`：规则文件路径，使用仓库根目录相对路径。
3. `type`：规则类型，例如 `schema`、`lint-preset` 或 `template`。

规则文件必须反向声明 `standard_id`，且该值必须等于规范文档的 `id`。

<!-- anchor: governance -->
## 变更流程

新增或重大修改规范时，应先创建 RFC。长期有效的工程决策应补充 ADR。小型修订可以直接修改规范，但必须更新版本号和变更记录。

修改规范后必须运行：

```powershell
python tools/standards/standards.py generate-index
python tools/standards/standards.py check
```

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 2.0.0 | 2026-04-27 | 升级为 v2 路由元数据、显式 anchor、分片索引和按需检索契约。 |
| 1.0.0 | 2026-04-27 | 建立规范撰写、索引和机器规则绑定的基础要求。 |
