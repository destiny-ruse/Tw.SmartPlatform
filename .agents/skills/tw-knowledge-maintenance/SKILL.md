---
name: tw-knowledge-maintenance
description: Use when Knowledge Compiler diagnostics report graph drift, or when a developer explicitly asks to align formal memory graph files after code, contract, or documentation changes.
---

# Knowledge Maintenance

Use this only when the developer explicitly asks to align or repair the formal knowledge graph.

## Flow

1. Run `python tools/knowledge/knowledge.py check-drift --from <base> --to <head>` if diagnostics are not already available.
2. Read diagnostics and affected file paths.
3. Read L0, relevant L1 shards, relevant L2 section indexes, and the specific graph files to update.
4. Explain the planned graph changes in Chinese.
5. Modify `docs/knowledge/graph/**` only after explicit developer approval or an explicit instruction to apply.
6. Record `provenance.updated_by: ai-assisted:tw-knowledge-maintenance`.
7. Run `python tools/knowledge/knowledge.py generate`.
8. Run `python tools/knowledge/knowledge.py check`.

## Rules

Do not silently update the graph.
Do not edit generated files manually.
Do not read all graph files unless the diagnostics require a full consistency investigation.
