---
name: tw-knowledge-discovery
description: Use before adding code that may reuse existing platform, backend, frontend, contract, or tool capabilities in Tw.SmartPlatform.
---

# Knowledge Discovery

Use this to find existing capabilities before writing new code.

## Required Retrieval Flow

1. Read `docs/knowledge/generated/index.generated.json`.
2. Read only relevant L1 shards under `docs/knowledge/generated/_index/`.
3. Read relevant L2 section indexes under `docs/knowledge/generated/_index/sections/`.
4. Read only the required field ranges from `docs/knowledge/graph/**`.

Do not read all files under `docs/knowledge/graph/**`.

## Output

Respond in Chinese with:

- 应复用的 capability
- provider module
- entrypoints or evidence files
- `reuse.use_when`
- `reuse.do_not_reimplement`
- related standards
