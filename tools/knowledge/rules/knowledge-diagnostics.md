# Knowledge Diagnostics

| Code | Severity | Trigger | Related field |
|---|---|---|---|
| `knowledge.yaml-parse` | error | 图谱 YAML 文件无法解析 | `docs/knowledge/graph/**` |
| `knowledge.taxonomy-parse` | error | `taxonomy.yaml` 无法解析 | `docs/knowledge/taxonomy.yaml` |
| `knowledge.required-field` | error | 图谱节点缺少必填字段 | root required fields |
| `knowledge.schema-version` | error | `schema_version` 不符合 `x.y.z` | `schema_version` |
| `knowledge.id-format` | error | `id` 不是小写点分层级格式 | `id` |
| `knowledge.duplicate-id` | error | 多个图谱节点声明同一 id | `id` |
| `knowledge.invalid-kind` | error | `kind` 不在 `taxonomy.yaml valid_kinds` | `kind` |
| `knowledge.invalid-status` | error | `status` 不在 `taxonomy.yaml valid_statuses` | `status` |
| `knowledge.invalid-module-type` | error | `module_type` 不在 `taxonomy.yaml valid_module_types` | `module_type` |
| `knowledge.declared-in` | error | `source.declared_in` 缺失或不等于实际路径 | `source.declared_in` |
| `knowledge.source-format` | error | `source` 或 `source.evidence` 结构错误 | `source` |
| `knowledge.provenance-format` | error | `provenance` 结构错误 | `provenance` |
| `knowledge.missing-evidence` | warning | `source.evidence` 指向不存在的文件 | `source.evidence` |
| `knowledge.dangling-reference` | error | module/capability/decision/integration 引用不存在的图谱节点 | reference fields |
| `knowledge.missing-module` | error | taxonomy 命中的服务或应用路径没有 module 节点 | `path_rules` |
| `knowledge.missing-capability` | warning | taxonomy 命中的公共构件可能缺少 capability 或 module 节点 | `path_rules` |
| `knowledge.contract-drift` | error | 新增契约文件没有 contract 节点 | `path_rules` |
| `knowledge.contract-outdated` | warning | 已有 contract 节点对应的契约文件发生变更 | `path` |
| `knowledge.index-missing` | error | 生成索引缺失 | `generated` |
| `knowledge.index-invalid-json` | error | 生成索引不是合法 JSON | `generated` |
| `knowledge.index-stale` | error | 生成索引不是最新 | `generated` |
| `knowledge.index-obsolete` | error | 存在过期生成索引 | `generated` |
| `knowledge.skill-missing` | error | Claude Skill 同步计划引用的源 Skill 不存在 | `.agents/skills` |
| `knowledge.skill-link-conflict` | error | Claude Skill 目标已存在且不是预期相对符号链接 | `.claude/skills` |
| `knowledge.symlink-failed` | error | 创建 Claude Skill 相对符号链接失败 | `.claude/skills` |
| `knowledge.git-diff` | error | `git diff` 执行失败或 ref 参数非法 | `diff/check-drift` |
| `knowledge.unsupported-kind` | error | `init` 收到不支持的图谱节点类型 | `init --kind` |
| `knowledge.init-exists` | error | `init` 目标图谱节点已存在 | `init --id` |
| `knowledge.template-missing` | error | `init` 找不到对应节点模板 | `docs/knowledge/templates` |
| `knowledge.unsupported-target` | error | `sync-skills` 收到不支持的同步目标 | `sync-skills --target` |
