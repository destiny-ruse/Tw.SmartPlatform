# 多智能体公共指令

## 适用范围

本文件是当前工程目录内多智能体协作的公共指令入口，适用于 Codex、Claude 以及其他读取本工程规则文件的 AI Agent。

各智能体专属入口文件只应保留对本文件的引用，不应复制本文件中的规则正文。公共规则发生变化时，应优先修改本文件，避免在多个智能体入口文件中重复维护。

## 回复语言

所有 Agent 面向用户的回复必须使用简体中文，包括叙述、解释、总结、提问和结论。

代码、标识符、命令、文件路径、引用的原文与外部专有名词保留原文，不强制翻译。

## 工程规范与记忆层加载边界

每轮对话开始时，凡涉及编码、评审、调试、重构、测试、构建、发布、文档、架构、配置或工程治理工作，Agent 必须先加载并应用 `.rules\ai-coding-rules` 下的 AI 编码规则索引。

必须按以下顺序加载：

1. `.rules\ai-coding-rules\00-always-load.md`
2. `.rules\ai-coding-rules\01-task-router.md`
3. 与当前技术栈匹配的 `.rules\ai-coding-rules\languages\*.md`
4. 与当前任务类型匹配的 `.rules\ai-coding-rules\tasks\*.md`
5. 上述索引文件引用的 `docs\engineering-standards` 下正式工程规范；当 `.tw-memory\manifest\source-index.generated.json` 与 `.tw-memory\routes\standards.generated.yaml` 存在且校验通过时，可以使用该分段索引收窄正式规范段落，再读取对应 `docs\engineering-standards` 原文。

`docs\engineering-standards` 是工程规范的唯一正式来源。`.rules\ai-coding-rules` 只作为 AI 加载路由索引，不得替代、复制或扩展正式工程规范。

`.tw-memory` 是 AI 使用的自动生成记忆层，只能作为分段索引、实体路由和派生卡片使用。Agent 不得把 `.tw-memory` 卡片当作工程规范正文，不得同时加载同一工程规范的全文和记忆层摘要。记忆层缺失或过期时，Agent 必须退回 `.rules\ai-coding-rules` 指向的正式规范文件。

即使任务看起来很小，Agent 也必须先检查索引并加载适用规范，再修改文件或给出实现指导。

如果无法读取 `.rules\ai-coding-rules` 或被索引引用的正式规范文件，Agent 必须明确说明缺失路径和影响范围，不得凭记忆假设规范内容。无法读取 `.tw-memory` 不得阻断工程任务，除非当前任务明确要求校验或生成记忆层。

## 正式规范编辑要求

当 Agent 编写或修改 `docs\engineering-standards` 下的正式工程规范时，必须将目标文件视为正式规范正文，而不是设计草稿、路线图或待办清单。

正式工程规范只能包含已经确定的规则、边界、例外流程和检查项。不得使用不确定、占位或未来承诺语义，例如“后续”“待定”“暂定”“视情况”“可能”“大概”“如有需要”“按需补充”“待补充”“TODO”“TBD”等表达。

描述受控术语、禁止词列表、代码注释标记或明确反例时，可以出现对应词语，但不得表达未完成规则。

修改 `docs\engineering-standards` 后，Agent 必须检查变更内容是否包含不确定、占位或未来承诺语义。命中结果只能来自禁止词列表本身、受控术语说明、代码注释标记说明或明确反例说明；正式规范正文不得保留实际未定规则。

## 维护要求

- 多智能体公共规则必须集中维护在 `.rules\common-agent-instructions.md`。
- `AGENTS.md`、`CLAUDE.md` 等智能体入口文件不得复制公共规则正文。
- 新增智能体入口文件时，必须指向本文件，并要求智能体先读取本文件。
- 修改工程规范正文时，应修改 `docs\engineering-standards`。
- 修改 AI 加载路由时，应修改 `.rules\ai-coding-rules`。
- 修改 AI 自动生成记忆层设计或工具时，应保持 `.tw-memory` 只承载生成索引、路由和卡片，不得复制工程规范正文。
- 修改多智能体公共行为时，应修改本文件。
