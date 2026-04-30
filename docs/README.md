# 文档目录

本目录用于集中存放项目架构、工程标准、目录结构和协作规范等文档资料。

## 索引

- [工程规范](standards/README.md)：公司正式规范资产，包含规则、流程、决策和参考资料。
- [Superpowers 设计与计划](superpowers/)：面向 AI 协作流程的设计稿、实施计划和任务记录。

## 维护约定

规范正文和公司治理资料统一维护在 `docs/standards`。修改 `docs` 内 Markdown 后，应运行 `python tools\tw-memory\tw_memory.py generate --format brief` 和 `python tools\tw-memory\tw_memory.py check --format brief` 更新并校验 AI 记忆索引。
