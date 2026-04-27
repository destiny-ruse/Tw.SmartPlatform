# 标准检索契约

本文定义工程规范 v2 的索引分层和读取顺序。`docs/standards/_meta/` 下的文档说明检索契约，不作为正式规范参与标准索引。

## L0 轻量索引

`docs/standards/index.generated.json` 是入口索引，只包含标准 ID、标题、类型、状态、版本、路径、角色、技术栈、标签、摘要、L2 section index 路径和所属 L1 shard 路径。

L0 不包含规范正文，也不包含完整 section 详情。

## L1 分片索引

`docs/standards/_index/` 下的 L1 分片按以下维度生成：

1. `by-role/<role>.generated.json`
2. `by-stack/<stack>.generated.json`
3. `by-doc-type/<doc_type>.generated.json`
4. `by-tag/<tag>.generated.json`

读者应根据任务路径和语义只读取匹配的 L1 分片，不得合并读取全部分片。

## L2 Section Index

`docs/standards/_index/sections/<standard-id>.generated.json` 保存单篇标准的 anchor、标题、层级、起止行号和 region 起止行号。

读者需要正文时，应先读取 L2，再按目标行范围读取 Markdown 文件。

## 生成与提交

所有索引都是提交到仓库的生成产物。修改标准正文、元数据或 anchor 后，必须运行：

```powershell
python tools/standards/standards.py generate-index
python tools/standards/standards.py check
```

## 读取限制

读者不得预加载 `docs/standards/**/*.md`。读者不得一次性读取 `docs/standards/_index/` 下的所有分片。

## 引用格式

规范引用使用 `standard-id#anchor` 或 `standard-id#anchor:region`：

1. `standards.authoring#metadata`
2. `rules.api-response-shape#rules:no-null`

标准结论必须同时给出标准 ID、anchor 或 region、版本和 Markdown 文件路径。
