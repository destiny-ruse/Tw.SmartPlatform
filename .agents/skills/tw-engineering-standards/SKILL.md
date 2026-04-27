---
name: tw-engineering-standards
description: Use when adding or modifying code, docs, contracts, configuration, CI, reviewing implementation compliance, or answering questions about repository engineering standards such as naming, comments, APIs, errors, auth, gRPC, AsyncAPI, Git commits, testing, deployment, and observability.
---

# Engineering Standards

Use this Skill to retrieve the smallest relevant set of engineering standards for the current task.

## Hard Rules

1. Do not load `docs/standards/**/*.md` in bulk.
2. Do not load every file under `docs/standards/_index/`.
3. Do not copy standards body text into this Skill.
4. Do not merge all shard indexes into one response.
5. Every standards-based conclusion must cite standard ID, anchor, version, and file path.

## Short Retrieval Flow

1. Infer `roles`, `stacks`, `tags`, and `doc_type` from the user task and target paths.
2. Read `docs/standards/index.generated.json`.
3. Read only the matching L1 shard files.
4. Rank candidate standards by path, status, tags, stack, and summary.
5. Read only candidate L2 section indexes.
6. Read only the matching Markdown line ranges.
7. If a matched standard is `deprecated` or `superseded`, identify its replacement before using it.

For detailed flow, read `retrieval-flow.md`.
For vocabulary mapping, read `query-vocabulary.md`.
For examples, read `examples.md`.
