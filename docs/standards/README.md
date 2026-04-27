# 工程规范目录

本目录是项目工程规范的统一入口，用于存放业务无关的研发标准、协作约定、质量要求和 AI 可检索索引。

## 使用方式

新增规范时，优先使用 v2 模板命令：

```powershell
python tools/standards/standards.py new-standard --id rules.git-commit --title Git提交规范 --doc-type rule --roles architect,backend,frontend,qa,devops,ai --stacks "" --tags git,governance --summary 规定提交信息格式和提交粒度。
python tools/standards/standards.py generate-index
python tools/standards/standards.py check
```

每篇规范必须包含 front matter 元数据，字段契约见 [meta.schema.json](meta.schema.json)。修改规范后必须重新生成索引并运行检查。

## 检索契约

v2 索引分三层：

1. L0: [index.generated.json](index.generated.json)，只包含轻量路由元数据。
2. L1: `_index/by-*/*.generated.json`，按 role、stack、doc_type 和 tag 分片。
3. L2: `_index/sections/*.generated.json`，记录单篇标准的 anchor、region 和行号。

详细契约见 [_meta/retrieval-contract.md](_meta/retrieval-contract.md)，词表见 [_meta/query-vocabulary.md](_meta/query-vocabulary.md)。

AI 和自动化工具不得一次性加载全部规范正文，也不得读取所有分片后再筛选。引用规范结论时必须包含标准 ID、anchor 或 region、版本和 Markdown 路径。

## 当前规范

- [规范撰写规范](processes/standard-authoring.md)

## 治理入口

- [RFC 流程](../rfcs/README.md)
- [ADR 流程](../adrs/README.md)
