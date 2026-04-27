# Retrieval Flow

Use the smallest cache-safe read set that can answer the task.

## Order

1. Infer path signals from target files and directories.
2. Infer semantic signals from the user request.
3. Map keywords to `roles`, `stacks`, `tags`, and `doc_type`.
4. Read `docs/standards/index.generated.json`.
5. Read only matching L1 shards under `docs/standards/_index/by-*`.
6. Rank candidates by path relevance, active status, tag overlap, stack overlap, and summary.
7. Read only candidate L2 section indexes from `docs/standards/_index/sections/`.
8. Read only selected Markdown line ranges from candidate standards.
9. Cite every standards-based conclusion.

## Ranking Hints

Prefer standards that are:

1. `active`.
2. Matched by both path and semantic tags.
3. Matched by stack when a stack is clear.
4. More specific to the task than broad governance standards.

If a candidate is `deprecated` or `superseded`, locate and use its replacement before citing it.

## Citation Format

Use:

```text
<standard-id>#<anchor>[:<region>] v<version> <path>
```

When citing a section without a region, omit the region suffix.
