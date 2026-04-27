# Standards Tooling

本目录提供工程规范体系的本地工具、模板和机器规则示例。

## 常用命令

```powershell
python tools/standards/standards.py new-standard --id rules.git-commit --title Git提交规范 --doc-type rule --roles architect,backend,frontend,qa,devops,ai --stacks "" --tags git,governance --summary 规定提交信息格式和提交粒度。
python tools/standards/standards.py generate-index
python tools/standards/standards.py validate
python tools/standards/standards.py check-links
python tools/standards/standards.py check-machine-rules
python tools/standards/standards.py check
```

## v2 行为

1. 工具只依赖 Python 标准库。
2. `validate` 校验 v2 元数据、禁止的 `applies_to` 字段、显式 anchor 和 region。
3. `generate-index` 生成并提交 L0、L1 和 L2 JSON 产物。
4. `check-links` 同时校验 Markdown 链接和 `standard-id#anchor[:region]` 标准引用。
5. `check` 是本地和 CI 的单一入口。

## 生成产物

`docs/standards/index.generated.json` 是 L0 轻量入口。`docs/standards/_index/` 下的文件是按 role、stack、doc_type、tag 和 section 生成的分片索引。

生成产物不包含标准正文。正文仍保存在 `docs/standards/**/*.md`，读取方必须通过索引定位后按行范围读取。

## 机器规则

`tools/standards/rules/` 中的规则文件必须被规范文档通过 `machine_rules` 绑定。规则文件必须反向声明 `standard_id`，且该值必须等于规范文档的 `id`。
