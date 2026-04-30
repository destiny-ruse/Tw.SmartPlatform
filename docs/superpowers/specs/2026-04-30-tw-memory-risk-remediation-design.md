# TW Memory Risk Remediation Design

**Date:** 2026-04-30
**Status:** Approved design boundary, ready for implementation planning
**Document type:** Explanation
**Audience:** TW memory maintainers, platform maintainers, and AI implementation agents

---

## 1. Core Decision

AI tools may take over the index-building stage for internal asset documents.

They must not take over authorship of the internal assets themselves.

In practical terms:

1. `docs/` remains the company-owned source of truth for standards, manuals, decisions, processes, and references.
2. `tools/tw-memory` may scan `docs/` and generate line-level indexes, hashes, summaries, keywords, relations, route shards, and evidence pointers.
3. `.tw-memory` may store generated metadata that helps AI tools find and read relevant evidence.
4. `.tw-memory` must not store manual bodies, standards bodies, source copies, third-party docs, source archives, chat logs, secrets, SQLite files, vector caches, or web captures.
5. AI tools must not require `docs/` files to add front matter, hidden anchors, duplicate summaries, AI-only tags, or other content whose only purpose is search convenience.

The remediation principle is:

```text
AI owns index production, not source-of-truth writing.
Tools fix recall. Documents preserve facts.
```

---

## 2. Problem Summary

The first TW memory phase provides a working local memory engine, but several risks make the memory layer too optimistic:

1. `check` can miss newly added or removed source files.
2. `source-index/code.generated.json` is empty in the current generated memory.
3. Search recall depends mostly on path, heading, summary, and generated keywords, so body-only concepts can be missed.
4. `preflight --stack <language>` can filter out global company standards.
5. `docs/standards/rules/*.md` is currently classified as `manual`, which makes `--kind standard` ineffective.
6. `generate` rewrites vector backend configuration and can discard local operator choices.
7. `route-index/index.generated.json` does not fully match the documented root route contract.
8. Repository text quality checks are not represented in the TW memory verification path.

These are tool and generated-index risks. They are not reasons to force new content into `docs/`.

---

## 3. Boundary Model

### 3.1 Source Assets

`docs/`, README files, package files, and selected source files are fact sources.

The remediation must not require source assets to carry AI-specific metadata. If an asset already has a human-readable title, structure, or prose, the indexer should consume it as-is.

When source assets are genuinely outdated because the product, framework, or public API changed, postflight may ask for human review. That review is about company asset correctness, not about AI search quality.

### 3.2 Generated Memory

`.tw-memory` is generated memory. It may contain:

1. source indexes
2. route indexes
3. graph metadata
4. generated chunk metadata
5. summaries
6. keywords
7. relations
8. hashes
9. line ranges
10. evidence pointers

Generated memory may be rebuilt by AI tools or scripts at any time.

### 3.3 Runtime Caches

FTS databases, vector caches, embeddings, and search sidecars are runtime artifacts.

They must stay outside committed `.tw-memory` content and remain ignored by Git.

---

## 4. Remediation Design

### 4.1 Freshness Detection

`MemoryChecker` should rebuild the current source list through `SourceScanner` during `check`.

It should compare the scanned source set with committed source-index records:

1. scanned source exists but is absent from source-index: error `source-index-stale`
2. source-index record points to a removed file: error `source-missing`
3. source-index record hash differs from current file: error `source-stale`
4. source-index contains duplicate source paths: error `source-index-duplicate`

This keeps freshness enforcement in tooling. It does not require authors to edit `docs/`.

### 4.2 Controlled Source Coverage

The scanner should support controlled source-file indexing for code facts that affect AI development decisions.

The first implementation should include source files under known internal roots, such as:

1. `backend/dotnet/BuildingBlocks/src/**`
2. `backend/dotnet/Services/**`
3. `backend/java/**`
4. `backend/python/**`
5. `frontend/packages/**`
6. `frontend/apps/**`

Generated metadata may include path, hash, language, service, framework, line range, summary, keywords, and relations.

It must not store full source bodies in `.tw-memory`.

### 4.3 Body-Derived Keywords

The generator may read source lines while generating chunk metadata.

It should derive bounded keywords from chunk text and store only those keywords, not the full body. This improves recall for concepts that appear in prose but not in headings or paths.

Keyword extraction should be deterministic, bounded, and safe:

1. normalize English and common identifier tokens
2. keep Chinese terms only when extraction can be deterministic and useful
3. remove common stop words
4. cap keyword count per chunk
5. avoid storing long phrases that reconstruct source text

### 4.4 Global Standards Recall

`preflight` should combine two recall lanes:

1. stack-specific lane: language, framework, service, and package candidates
2. global lane: company standards, decisions, processes, references, and relevant skills

The global lane must remain available even when `--stack dotnet`, `--stack frontend`, `--stack java`, or `--stack python` is provided.

Results should be deduplicated by `chunk_id` and ranked with stack-specific results first when scores are otherwise comparable.

### 4.5 Standards Classification

`SourceScanner` should classify `docs/standards/**` by path:

1. `docs/standards/rules/**`: `standard`
2. `docs/standards/processes/**`: `process`
3. `docs/standards/decisions/**`: `decision`
4. `docs/standards/references/**`: `reference`

If the first phase keeps a smaller taxonomy, `rules/**` must at least become `standard` so `query --kind standard` works.

This classification must be generated from paths. It must not require Markdown front matter.

### 4.6 Config Preservation

`generate` should create `.tw-memory/adapters/vector-backends.yaml` only when it is missing.

If the file already exists, generation should preserve it and let `check` validate required structure. This avoids discarding local operator choices such as enabling or disabling a backend.

Static generated files and operator-owned configuration should have separate ownership rules.

### 4.7 Route Contract Alignment

`route-index/index.generated.json` should match the documented root route contract.

It should include:

1. `schema_version`
2. `generated_at`
3. `repo_hash`
4. `shards`

`generated_at` should be an ISO-8601 timestamp. Tests should verify presence and shape, not an exact wall-clock value.

### 4.8 Verification Quality Gate

The TW memory verification path should include:

1. `python tools\tw-memory\tw_memory.py generate`
2. `python tools\tw-memory\tw_memory.py check`
3. `python -m unittest discover tests\tw_memory -v`
4. `git diff --check`

Existing text quality issues should be fixed as repository hygiene. They should not be treated as AI memory content requirements.

---

## 5. Acceptance Criteria

The remediation is complete when:

1. Adding a new `docs/` source after generation makes `check` fail until `generate` is run.
2. Removing an indexed source makes `check` fail.
3. Modifying an indexed source hash makes `check` fail.
4. `source-index/code.generated.json` contains controlled source records when source files exist under configured internal roots.
5. Generated chunk metadata does not contain full source or document bodies.
6. A document whose heading is generic but whose body mentions a specific term can be found by querying that term.
7. `preflight --stack dotnet` can return both dotnet-specific evidence and global standards evidence.
8. `query --kind standard --text "api response shape"` returns relevant `docs/standards/rules/**` candidates.
9. Existing vector backend configuration survives `generate`.
10. `route-index/index.generated.json` includes `generated_at`.
11. `git diff --check` passes for the remediation diff.
12. The full TW memory verification command set passes.

---

## 6. Non-Goals

This remediation does not:

1. require authors to add AI-only metadata to `docs/`
2. copy manual bodies into `.tw-memory`
3. copy source bodies into `.tw-memory`
4. ingest third-party documentation
5. implement cloud vector synchronization
6. build an MCP server
7. change ordinary product code behavior

---

## 7. Implementation Order

The implementation should proceed in this order:

1. Add checker tests for new, removed, duplicate, and stale source records.
2. Implement current-source comparison in `MemoryChecker`.
3. Add scanner tests for controlled source files and standards classification.
4. Implement controlled source inclusion and path-based standards kinds.
5. Add generator/search tests for body-derived keywords without body storage.
6. Implement bounded keyword extraction from chunk lines.
7. Add preflight tests for global standards recall with a stack filter.
8. Implement two-lane preflight recall and deduplication.
9. Add generator tests for preserving vector backend config.
10. Implement config-preserving static file generation.
11. Add route-index contract tests for `generated_at`.
12. Implement route root contract alignment.
13. Fix text-quality issues in the remediation diff.
14. Run generation, check, unit tests, and `git diff --check`.

