# TW Memory Risk Remediation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Harden the TW memory layer so AI tools own generated indexes while company source assets remain untouched facts.

**Architecture:** Keep `tools/tw-memory` as the only implementation surface. `SourceScanner` discovers fact sources, `MemoryGenerator` writes generated metadata, `MemoryChecker` proves freshness and contract safety, and `SearchIndex` plus `PreflightRunner` provide recall without copying source bodies into `.tw-memory`.

**Tech Stack:** Python 3 standard library, `unittest`, `argparse`, JSON/YAML text artifacts, SQLite FTS5 runtime cache, PowerShell verification commands

---

## Source Inputs

- Design: `docs/superpowers/specs/2026-04-30-tw-memory-risk-remediation-design.md`
- Existing engine: `tools/tw-memory/tw_memory_engine/*.py`
- Existing tests: `tests/tw_memory/*.py`
- Memory skill contract: `.agents/skills/tw-memory/SKILL.md`
- Current memory artifacts: `.tw-memory/**`

## Scope Boundary

This plan fixes the risks identified after commit `5b812c62dcc3afc0de1453feb49f42a9668200da`.

It must not require authors to add AI-only metadata to `docs/`, README files, or source files. All AI-specific metadata belongs in generated `.tw-memory` artifacts.

## File Structure

- Modify: `tools/tw-memory/tw_memory_engine/scanner.py`
  - Owns source discovery, source type classification, language/framework/service detection, and controlled source-file inclusion.
- Modify: `tools/tw-memory/tw_memory_engine/checker.py`
  - Owns freshness checks, source-index integrity checks, route-index integrity checks, and forbidden memory file checks.
- Modify: `tools/tw-memory/tw_memory_engine/semantic.py`
  - Owns deterministic summaries, bounded keyword extraction, and source relations.
- Modify: `tools/tw-memory/tw_memory_engine/generator.py`
  - Owns static memory files, generated source indexes, chunk metadata, graph metadata, route indexes, and route root contract fields.
- Modify: `tools/tw-memory/tw_memory_engine/preflight.py`
  - Owns task preflight, two-lane recall, and candidate deduplication.
- Modify: `tools/tw-memory/tw_memory_engine/search.py`
  - Owns FTS and route-index querying. Only change if preflight needs filter support that does not belong in preflight.
- Modify: `tests/tw_memory/test_checker.py`
- Modify: `tests/tw_memory/test_scanner.py`
- Modify: `tests/tw_memory/test_generator.py`
- Modify: `tests/tw_memory/test_query_read.py`
- Modify: `tests/tw_memory/test_preflight_postflight.py`
- Modify: `tests/tw_memory/test_cli_contract.py`
- Modify: `deploy/ci-cd/tw-memory-check.ps1`
- Modify: `deploy/ci-cd/README.md`
- Modify: `docs/standards/rules/comments-python.md`
- Generated after implementation: `.tw-memory/**`

---

## Task 1: Make Check Detect Source-Index Drift

**Files:**
- Modify: `tests/tw_memory/test_checker.py`
- Modify: `tools/tw-memory/tw_memory_engine/checker.py`

- [ ] **Step 1: Add failing checker tests for added, removed, and duplicate sources**

Add these methods to `CheckerTests` in `tests/tw_memory/test_checker.py`:

```python
    def test_check_reports_new_unindexed_source(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            docs = root / "docs"
            docs.mkdir()
            (docs / "README.md").write_text("# Docs\n", encoding="utf-8")
            MemoryGenerator(root).generate()

            (docs / "new-standard.md").write_text("# New Standard\n", encoding="utf-8")
            diagnostics = MemoryChecker(root).check()

            self.assertTrue(
                any(
                    item.level == "error"
                    and item.code == "source-index-stale"
                    and item.path == "docs/new-standard.md"
                    for item in diagnostics
                ),
                [item.to_json() for item in diagnostics],
            )

    def test_check_reports_removed_indexed_source(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            docs = root / "docs"
            docs.mkdir()
            source = docs / "README.md"
            source.write_text("# Docs\n", encoding="utf-8")
            MemoryGenerator(root).generate()

            source.unlink()
            diagnostics = MemoryChecker(root).check()

            self.assertTrue(
                any(
                    item.level == "error"
                    and item.code == "source-missing"
                    and item.path == "docs/README.md"
                    for item in diagnostics
                ),
                [item.to_json() for item in diagnostics],
            )

    def test_check_reports_duplicate_source_index_entries(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            docs = root / "docs"
            docs.mkdir()
            (docs / "README.md").write_text("# Docs\n", encoding="utf-8")
            MemoryGenerator(root).generate()

            index = root / ".tw-memory" / "source-index" / "docs.generated.json"
            payload = json.loads(index.read_text(encoding="utf-8"))
            payload["sources"].append(dict(payload["sources"][0]))
            index.write_text(json.dumps(payload), encoding="utf-8")

            diagnostics = MemoryChecker(root).check()

            self.assertTrue(
                any(
                    item.level == "error"
                    and item.code == "source-index-duplicate"
                    and item.path == "docs/README.md"
                    for item in diagnostics
                ),
                [item.to_json() for item in diagnostics],
            )
```

- [ ] **Step 2: Run the focused tests and verify they fail**

Run:

```powershell
python -m unittest tests.tw_memory.test_checker.CheckerTests.test_check_reports_new_unindexed_source tests.tw_memory.test_checker.CheckerTests.test_check_reports_removed_indexed_source tests.tw_memory.test_checker.CheckerTests.test_check_reports_duplicate_source_index_entries -v
```

Expected: at least `test_check_reports_new_unindexed_source` and `test_check_reports_duplicate_source_index_entries` fail because `MemoryChecker` does not compare scanned sources or duplicates yet.

- [ ] **Step 3: Import `SourceScanner` into checker**

In `tools/tw-memory/tw_memory_engine/checker.py`, add:

```python
from .scanner import SourceScanner
```

- [ ] **Step 4: Wire source-set drift checks into `check`**

In `MemoryChecker.check`, insert the new check after source freshness:

```python
    def check(self) -> list[Diagnostic]:
        diagnostics: list[Diagnostic] = []
        diagnostics.extend(self._check_required_artifacts())
        source_records = self._source_records(diagnostics)
        diagnostics.extend(self._check_source_freshness(source_records))
        diagnostics.extend(self._check_source_index_matches_scan(source_records))
        diagnostics.extend(self._check_chunk_ranges())
        diagnostics.extend(self._check_manual_freshness(source_records))
        diagnostics.extend(self._check_route_index())
        diagnostics.extend(self._check_memory_safety())
        return diagnostics
```

- [ ] **Step 5: Add source-index comparison helpers**

Add these methods to `MemoryChecker`:

```python
    def _check_source_index_matches_scan(self, source_records: list[tuple[Path, dict[str, Any]]]) -> list[Diagnostic]:
        diagnostics: list[Diagnostic] = []
        indexed_paths: dict[str, int] = {}
        for _index_path, record in source_records:
            source_path = record.get("source_path")
            if isinstance(source_path, str):
                indexed_paths[source_path] = indexed_paths.get(source_path, 0) + 1

        for source_path, count in sorted(indexed_paths.items()):
            if count > 1:
                diagnostics.append(
                    Diagnostic(
                        level="error",
                        code="source-index-duplicate",
                        path=source_path,
                        message="source-index contains duplicate entries for the same source file",
                    )
                )

        try:
            scanned_paths = {record.source_path for record in SourceScanner(self.root).scan()}
        except OSError as exc:
            diagnostics.append(
                Diagnostic(
                    level="error",
                    code="source-scan-failed",
                    path=None,
                    message=f"current source scan failed: {exc}",
                )
            )
            return diagnostics

        for source_path in sorted(scanned_paths - set(indexed_paths)):
            diagnostics.append(
                Diagnostic(
                    level="error",
                    code="source-index-stale",
                    path=source_path,
                    message="source file is not present in generated source-index",
                )
            )

        return diagnostics
```

Do not add a second removed-source check here. `_check_source_freshness` already emits `source-missing` for removed indexed files.

- [ ] **Step 6: Run the focused tests and verify they pass**

Run:

```powershell
python -m unittest tests.tw_memory.test_checker.CheckerTests.test_check_reports_new_unindexed_source tests.tw_memory.test_checker.CheckerTests.test_check_reports_removed_indexed_source tests.tw_memory.test_checker.CheckerTests.test_check_reports_duplicate_source_index_entries -v
```

Expected: all 3 tests pass.

- [ ] **Step 7: Commit Task 1**

Run:

```powershell
git add tests/tw_memory/test_checker.py tools/tw-memory/tw_memory_engine/checker.py
git commit -m "fix: detect tw memory source index drift"
```

---

## Task 2: Classify Standards and Include Controlled Source Files

**Files:**
- Modify: `tests/tw_memory/test_scanner.py`
- Modify: `tools/tw-memory/tw_memory_engine/scanner.py`
- Modify: `tools/tw-memory/tw_memory_engine/generator.py`

- [ ] **Step 1: Add failing scanner tests**

Add these methods to `ScannerTests` in `tests/tw_memory/test_scanner.py`:

```python
    def test_scan_classifies_docs_standards_by_path(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            files = {
                "docs/standards/rules/api-response-shape.md": "standard",
                "docs/standards/processes/rfc-flow.md": "process",
                "docs/standards/decisions/0001-example.md": "decision",
                "docs/standards/references/http-status.md": "reference",
            }
            for source_path in files:
                path = root / source_path
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text("# Title\n", encoding="utf-8")

            records = {record.source_path: record.source_type for record in SourceScanner(root).scan()}

            self.assertEqual(records, files)

    def test_scan_includes_controlled_source_files(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            included = [
                "backend/dotnet/BuildingBlocks/src/Tw.Caching/CacheClient.cs",
                "backend/dotnet/Services/Notice/NoticeService.cs",
                "backend/java/orders/src/main/java/Orders.java",
                "backend/python/orders/app.py",
                "frontend/packages/ui/src/Button.tsx",
                "frontend/apps/tw.web.ops/src/App.vue",
            ]
            excluded = [
                "scripts/local.py",
                "tools/tw-memory/tw_memory.py",
            ]
            for source_path in included + excluded:
                path = root / source_path
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text("public token\n", encoding="utf-8")

            records = {record.source_path: record for record in SourceScanner(root).scan()}

            for source_path in included:
                self.assertIn(source_path, records)
                self.assertEqual(records[source_path].source_type, "source")
            for source_path in excluded:
                self.assertNotIn(source_path, records)
```

- [ ] **Step 2: Run the focused tests and verify they fail**

Run:

```powershell
python -m unittest tests.tw_memory.test_scanner.ScannerTests.test_scan_classifies_docs_standards_by_path tests.tw_memory.test_scanner.ScannerTests.test_scan_includes_controlled_source_files -v
```

Expected: both tests fail because standards are classified as `manual` and controlled source files are not included.

- [ ] **Step 3: Add source-file constants to scanner**

In `tools/tw-memory/tw_memory_engine/scanner.py`, add:

```python
SOURCE_SUFFIXES = {
    ".cs",
    ".fs",
    ".vb",
    ".java",
    ".py",
    ".ts",
    ".tsx",
    ".js",
    ".jsx",
    ".vue",
}
```

- [ ] **Step 4: Include controlled source files**

Replace `_is_included` with:

```python
    def _is_included(self, path: Path, parts: tuple[str, ...]) -> bool:
        name = path.name
        if self._is_skill_file(name, parts):
            return True
        if len(parts) == 1 and name == "README.md":
            return True
        if name == "README.md" and parts[0] in MARKDOWN_ROOTS:
            return True
        if path.suffix.lower() == ".md" and parts[0] in MARKDOWN_ROOTS:
            return True
        if self._is_package_file(name):
            return True
        if self._is_controlled_source_file(path, parts):
            return True
        return False
```

Add:

```python
    def _is_controlled_source_file(self, path: Path, parts: tuple[str, ...]) -> bool:
        if path.suffix.lower() not in SOURCE_SUFFIXES:
            return False
        if len(parts) >= 5 and parts[:4] == ("backend", "dotnet", "BuildingBlocks", "src"):
            return True
        if len(parts) >= 5 and parts[:3] == ("backend", "dotnet", "Services"):
            return True
        if len(parts) >= 3 and parts[:2] in {("backend", "java"), ("backend", "python")}:
            return True
        if len(parts) >= 4 and parts[:2] in {("frontend", "packages"), ("frontend", "apps")}:
            return True
        return False
```

- [ ] **Step 5: Classify docs standards by path**

In `_source_type`, insert this block before `if parts[0] == "contracts":`:

```python
        if len(parts) >= 3 and parts[:2] == ("docs", "standards"):
            if parts[2] == "rules":
                return "standard"
            if parts[2] == "processes":
                return "process"
            if parts[2] == "decisions":
                return "decision"
            if parts[2] == "references":
                return "reference"
```

- [ ] **Step 6: Add new source types to generated taxonomy**

In `tools/tw-memory/tw_memory_engine/generator.py`, update `TAXONOMY_TEXT` so `source_types` contains:

```yaml
source_types:
  - spec
  - standard
  - process
  - decision
  - reference
  - manual
  - readme
  - source
  - package
  - service-directory
  - skill
```

- [ ] **Step 7: Run scanner tests**

Run:

```powershell
python -m unittest tests.tw_memory.test_scanner -v
```

Expected: all scanner tests pass.

- [ ] **Step 8: Commit Task 2**

Run:

```powershell
git add tests/tw_memory/test_scanner.py tools/tw-memory/tw_memory_engine/scanner.py tools/tw-memory/tw_memory_engine/generator.py
git commit -m "feat: index controlled tw memory source facts"
```

---

## Task 3: Derive Bounded Keywords From Chunk Bodies

**Files:**
- Modify: `tests/tw_memory/test_generator.py`
- Modify: `tests/tw_memory/test_query_read.py`
- Modify: `tools/tw-memory/tw_memory_engine/semantic.py`

- [ ] **Step 1: Add failing generator and query tests**

Add this method to `GeneratorTests` in `tests/tw_memory/test_generator.py`:

```python
    def test_generate_extracts_bounded_keywords_from_body_without_storing_body(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            docs = root / "docs"
            docs.mkdir()
            source = docs / "general.md"
            source.write_text(
                "# General\n\nRedis distributed cache wrapper handles cache invalidation.\n",
                encoding="utf-8",
            )

            MemoryGenerator(root).generate()

            chunk_file = root / ".tw-memory" / "generated" / "chunks" / "docs" / "general.md.generated.json"
            payload = json.loads(chunk_file.read_text(encoding="utf-8"))
            chunk = payload["chunks"][0]
            serialized = json.dumps(payload, ensure_ascii=False)
            self.assertIn("redis", chunk["keywords"])
            self.assertIn("cache", chunk["keywords"])
            self.assertLessEqual(len(chunk["keywords"]), 20)
            self.assertNotIn("Redis distributed cache wrapper handles cache invalidation.", serialized)
```

Add this method to `QueryReadTests` in `tests/tw_memory/test_query_read.py`:

```python
    def test_query_matches_body_derived_keywords_without_fts(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            docs = root / "docs"
            docs.mkdir()
            (docs / "general.md").write_text(
                "# General\n\nRedis distributed cache wrapper.\n",
                encoding="utf-8",
            )
            MemoryGenerator(root).generate()

            results = SearchIndex(root).query("redis", stack=None, kind=None, limit=5)

            self.assertEqual([result.chunk_id for result in results], ["docs.general#chunk-001"])
```

- [ ] **Step 2: Run the focused tests and verify they fail**

Run:

```powershell
python -m unittest tests.tw_memory.test_generator.GeneratorTests.test_generate_extracts_bounded_keywords_from_body_without_storing_body tests.tw_memory.test_query_read.QueryReadTests.test_query_matches_body_derived_keywords_without_fts -v
```

Expected: both tests fail because `extract_keywords` ignores body lines.

- [ ] **Step 3: Add deterministic body token extraction**

In `tools/tw-memory/tw_memory_engine/semantic.py`, add:

```python
STOP_WORDS = {
    "and",
    "are",
    "but",
    "for",
    "from",
    "has",
    "have",
    "into",
    "not",
    "the",
    "this",
    "that",
    "with",
    "without",
}
MAX_KEYWORDS = 20
BODY_KEYWORD_RE = re.compile(r"[A-Za-z][A-Za-z0-9_]{2,}")
CAMEL_RE = re.compile(r"(?<=[a-z0-9])(?=[A-Z])")
```

Replace `extract_keywords` with:

```python
def extract_keywords(path: str, heading: str | None, lines: Sequence[str]) -> list[str]:
    keywords = {token for token in _tokens(path) if token}
    if heading:
        keywords.update(token for token in _tokens(heading) if len(token) > 2)
    keywords.update(token for token in LANGUAGE_TOKENS if token in path.lower().split("/"))
    keywords.update(_body_tokens(lines))
    return sorted(keywords)[:MAX_KEYWORDS]
```

Add:

```python
def _body_tokens(lines: Sequence[str]) -> set[str]:
    tokens: set[str] = set()
    for line in lines:
        for raw_token in BODY_KEYWORD_RE.findall(line):
            for token in CAMEL_RE.sub(" ", raw_token).replace("_", " ").split():
                normalized = token.lower()
                if len(normalized) < 3 or normalized in STOP_WORDS:
                    continue
                tokens.add(normalized)
                if len(tokens) >= MAX_KEYWORDS:
                    return tokens
    return tokens
```

- [ ] **Step 4: Run the focused tests and verify they pass**

Run:

```powershell
python -m unittest tests.tw_memory.test_generator.GeneratorTests.test_generate_extracts_bounded_keywords_from_body_without_storing_body tests.tw_memory.test_query_read.QueryReadTests.test_query_matches_body_derived_keywords_without_fts -v
```

Expected: both tests pass.

- [ ] **Step 5: Run generator and query test suites**

Run:

```powershell
python -m unittest tests.tw_memory.test_generator tests.tw_memory.test_query_read -v
```

Expected: all tests pass.

- [ ] **Step 6: Commit Task 3**

Run:

```powershell
git add tests/tw_memory/test_generator.py tests/tw_memory/test_query_read.py tools/tw-memory/tw_memory_engine/semantic.py
git commit -m "feat: derive tw memory keywords from evidence text"
```

---

## Task 4: Add Global Standards Recall to Preflight

**Files:**
- Modify: `tests/tw_memory/test_preflight_postflight.py`
- Modify: `tools/tw-memory/tw_memory_engine/preflight.py`

- [ ] **Step 1: Add failing preflight recall test**

Add this method to `PreflightPostflightTests` in `tests/tw_memory/test_preflight_postflight.py`:

```python
    def test_preflight_includes_global_standards_with_stack_filter(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            dotnet = root / "backend" / "dotnet" / "BuildingBlocks" / "src" / "Tw.Caching"
            standard = root / "docs" / "standards" / "rules"
            dotnet.mkdir(parents=True)
            standard.mkdir(parents=True)
            (dotnet / "README.md").write_text("# Dotnet Cache\n\nCaching building blocks.\n", encoding="utf-8")
            (standard / "api-response-shape.md").write_text(
                "# API Response Shape\n\nResponse envelope standard.\n",
                encoding="utf-8",
            )
            MemoryGenerator(root).generate()

            result = PreflightRunner(root).run(
                task="api response shape for dotnet cache",
                stack="dotnet",
                path="backend/dotnet/BuildingBlocks/src/Tw.Caching",
            )

            candidate_paths = [candidate["source_path"] for candidate in result["candidates"]]
            self.assertIn("backend/dotnet/BuildingBlocks/src/Tw.Caching/README.md", candidate_paths)
            self.assertIn("docs/standards/rules/api-response-shape.md", candidate_paths)
```

- [ ] **Step 2: Run the focused test and verify it fails**

Run:

```powershell
python -m unittest tests.tw_memory.test_preflight_postflight.PreflightPostflightTests.test_preflight_includes_global_standards_with_stack_filter -v
```

Expected: fails because `preflight` passes `stack="dotnet"` to the only query lane.

- [ ] **Step 3: Add two-lane recall in preflight**

In `tools/tw-memory/tw_memory_engine/preflight.py`, replace:

```python
        candidates = [result.to_json() for result in index.query(query, stack=stack, kind=None, limit=5)]
```

with:

```python
        candidates = self._candidate_payloads(index, query, stack)
```

Add this method to `PreflightRunner`:

```python
    def _candidate_payloads(self, index: SearchIndex, query: str, stack: str | None) -> list[dict[str, object]]:
        by_chunk_id: dict[str, dict[str, object]] = {}
        for result in index.query(query, stack=stack, kind=None, limit=5):
            by_chunk_id[result.chunk_id] = result.to_json()

        for kind in ("standard", "process", "decision", "reference", "skill"):
            for result in index.query(query, stack=None, kind=kind, limit=3):
                by_chunk_id.setdefault(result.chunk_id, result.to_json())

        return list(by_chunk_id.values())[:8]
```

This keeps stack-specific results first and then appends global results that were filtered out by stack.

- [ ] **Step 4: Run focused and preflight tests**

Run:

```powershell
python -m unittest tests.tw_memory.test_preflight_postflight -v
```

Expected: all preflight/postflight tests pass.

- [ ] **Step 5: Commit Task 4**

Run:

```powershell
git add tests/tw_memory/test_preflight_postflight.py tools/tw-memory/tw_memory_engine/preflight.py
git commit -m "feat: include global standards in tw memory preflight"
```

---

## Task 5: Preserve Vector Backend Configuration During Generation

**Files:**
- Modify: `tests/tw_memory/test_generator.py`
- Modify: `tools/tw-memory/tw_memory_engine/generator.py`

- [ ] **Step 1: Add failing config preservation test**

Add this method to `GeneratorTests` in `tests/tw_memory/test_generator.py`:

```python
    def test_generate_preserves_existing_vector_backend_config(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            docs = root / "docs"
            docs.mkdir()
            (docs / "README.md").write_text("# Docs\n", encoding="utf-8")
            config = root / ".tw-memory" / "adapters" / "vector-backends.yaml"
            config.parent.mkdir(parents=True)
            config.write_text(
                'schema_version: "1.0.0"\n'
                "default_backend: fts\n"
                "vector_backends:\n"
                "  aliyun:\n"
                "    enabled: true\n",
                encoding="utf-8",
            )

            MemoryGenerator(root).generate()

            self.assertIn("enabled: true", config.read_text(encoding="utf-8"))
```

- [ ] **Step 2: Run the focused test and verify it fails**

Run:

```powershell
python -m unittest tests.tw_memory.test_generator.GeneratorTests.test_generate_preserves_existing_vector_backend_config -v
```

Expected: fails because `_write_static_files` rewrites `adapters/vector-backends.yaml`.

- [ ] **Step 3: Add write-if-missing helper**

In `tools/tw-memory/tw_memory_engine/generator.py`, add:

```python
    def _write_text_if_missing(self, relative_path: str, text: str) -> str:
        path = self.memory_root / relative_path
        if path.exists():
            return path.relative_to(self.root).as_posix()
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding="utf-8")
        return path.relative_to(self.root).as_posix()
```

- [ ] **Step 4: Preserve vector backend config in static file generation**

Replace `_write_static_files` with:

```python
    def _write_static_files(self) -> list[str]:
        return [
            self._write_text("README.md", README_TEXT),
            self._write_text("taxonomy.yaml", TAXONOMY_TEXT),
            self._write_text_if_missing("adapters/vector-backends.yaml", VECTOR_BACKENDS_TEXT),
        ]
```

- [ ] **Step 5: Run generator tests**

Run:

```powershell
python -m unittest tests.tw_memory.test_generator -v
```

Expected: all generator tests pass.

- [ ] **Step 6: Commit Task 5**

Run:

```powershell
git add tests/tw_memory/test_generator.py tools/tw-memory/tw_memory_engine/generator.py
git commit -m "fix: preserve tw memory vector backend config"
```

---

## Task 6: Align Route Root Contract and Schema Checks

**Files:**
- Modify: `tests/tw_memory/test_generator.py`
- Modify: `tests/tw_memory/test_checker.py`
- Modify: `tools/tw-memory/tw_memory_engine/generator.py`
- Modify: `tools/tw-memory/tw_memory_engine/checker.py`

- [ ] **Step 1: Add failing route contract tests**

Add this method to `GeneratorTests` in `tests/tw_memory/test_generator.py`:

```python
    def test_route_index_root_contains_generated_at(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            docs = root / "docs"
            docs.mkdir()
            (docs / "README.md").write_text("# Docs\n", encoding="utf-8")

            MemoryGenerator(root).generate()

            route_index = json.loads(
                (root / ".tw-memory" / "route-index" / "index.generated.json").read_text(encoding="utf-8")
            )
            self.assertEqual(route_index["schema_version"], "1.0.0")
            self.assertIn("generated_at", route_index)
            self.assertRegex(route_index["generated_at"], r"^\d{4}-\d{2}-\d{2}T")
            self.assertIn("repo_hash", route_index)
            self.assertIsInstance(route_index["shards"], list)
```

Add this method to `CheckerTests` in `tests/tw_memory/test_checker.py`:

```python
    def test_check_reports_route_index_missing_required_fields(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            docs = root / "docs"
            docs.mkdir()
            (docs / "README.md").write_text("# Docs\n", encoding="utf-8")
            MemoryGenerator(root).generate()
            route_index = root / ".tw-memory" / "route-index" / "index.generated.json"
            payload = json.loads(route_index.read_text(encoding="utf-8"))
            del payload["repo_hash"]
            route_index.write_text(json.dumps(payload), encoding="utf-8")

            diagnostics = MemoryChecker(root).check()

            self.assertTrue(
                any(item.level == "error" and item.code == "route-index-schema-invalid" for item in diagnostics),
                [item.to_json() for item in diagnostics],
            )
```

- [ ] **Step 2: Run focused tests and verify they fail**

Run:

```powershell
python -m unittest tests.tw_memory.test_generator.GeneratorTests.test_route_index_root_contains_generated_at tests.tw_memory.test_checker.CheckerTests.test_check_reports_route_index_missing_required_fields -v
```

Expected: both tests fail.

- [ ] **Step 3: Add timestamp import and route root field**

In `tools/tw-memory/tw_memory_engine/generator.py`, add:

```python
from datetime import datetime, timezone
```

In `_write_route_indexes`, replace `root_payload` with:

```python
        root_payload = {
            "schema_version": SCHEMA_VERSION,
            "generated_at": datetime.now(timezone.utc).isoformat(),
            "repo_hash": repo_hash,
            "shards": sorted(shards, key=lambda shard: (str(shard["kind"]), str(shard["name"]))),
        }
```

- [ ] **Step 4: Add route root schema validation**

In `MemoryChecker._check_route_index`, after loading `payload`, insert:

```python
        diagnostics.extend(self._check_route_root_schema(payload, index_path))
```

Add:

```python
    def _check_route_root_schema(self, payload: dict[str, Any], index_path: Path) -> list[Diagnostic]:
        diagnostics: list[Diagnostic] = []
        required = {
            "schema_version": str,
            "generated_at": str,
            "repo_hash": str,
            "shards": list,
        }
        for field, expected_type in required.items():
            if not isinstance(payload.get(field), expected_type):
                diagnostics.append(
                    Diagnostic(
                        level="error",
                        code="route-index-schema-invalid",
                        path=relative_posix(self.root, index_path),
                        message=f"route-index root field {field} is missing or invalid",
                    )
                )
        return diagnostics
```

- [ ] **Step 5: Run generator and checker tests**

Run:

```powershell
python -m unittest tests.tw_memory.test_generator tests.tw_memory.test_checker -v
```

Expected: all generator and checker tests pass.

- [ ] **Step 6: Commit Task 6**

Run:

```powershell
git add tests/tw_memory/test_generator.py tests/tw_memory/test_checker.py tools/tw-memory/tw_memory_engine/generator.py tools/tw-memory/tw_memory_engine/checker.py
git commit -m "fix: align tw memory route index contract"
```

---

## Task 7: Add Verification Quality Gate and Regenerate Memory

**Files:**
- Modify: `deploy/ci-cd/tw-memory-check.ps1`
- Modify: `deploy/ci-cd/README.md`
- Modify: `docs/standards/rules/comments-python.md`
- Generated: `.tw-memory/**`

- [ ] **Step 1: Update CI check script**

Replace `deploy/ci-cd/tw-memory-check.ps1` with:

```powershell
$ErrorActionPreference = "Stop"

python tools\tw-memory\tw_memory.py check --format brief
git diff --check
```

- [ ] **Step 2: Update CI README**

In `deploy/ci-cd/README.md`, update the TW Memory Check section to say:

```markdown
The script validates `.tw-memory` freshness, source hashes, route paths, chunk line ranges, forbidden runtime cache files, and repository whitespace issues. It does not generate files in CI.
```

- [ ] **Step 3: Normalize the existing Python comments standard file**

Run this formatting command to normalize line endings and remove trailing whitespace in `docs/standards/rules/comments-python.md`:

```powershell
$path = "docs/standards/rules/comments-python.md"
$text = Get-Content -LiteralPath $path -Raw -Encoding utf8
$lines = $text -split "`r?`n"
$normalized = ($lines | ForEach-Object { $_.TrimEnd() }) -join "`n"
Set-Content -LiteralPath $path -Value $normalized -Encoding utf8 -NoNewline
Add-Content -LiteralPath $path -Value "" -Encoding utf8
```

- [ ] **Step 4: Regenerate memory artifacts**

Run:

```powershell
python tools\tw-memory\tw_memory.py generate
```

Expected: exits `0` and prints `diagnostics: 0`.

- [ ] **Step 5: Run check**

Run:

```powershell
python tools\tw-memory\tw_memory.py check
```

Expected: exits `0` and prints no error diagnostics.

- [ ] **Step 6: Run unit tests**

Run:

```powershell
python -m unittest discover tests\tw_memory -v
```

Expected: all tests pass.

- [ ] **Step 7: Run whitespace quality gate**

Run:

```powershell
git diff --check
```

Expected: exits `0`.

- [ ] **Step 8: Run acceptance smoke commands**

Run:

```powershell
python tools\tw-memory\tw_memory.py query --kind standard --text "api response shape" --format json --limit 3
python tools\tw-memory\tw_memory.py preflight --task "api response shape cache standard" --stack dotnet --path backend/dotnet/BuildingBlocks/src/Tw.Caching --format json
```

Expected:

1. The query output contains at least one `docs/standards/rules/api-response-shape.md` result.
2. The preflight output contains at least one dotnet candidate and at least one `docs/standards/` candidate.

- [ ] **Step 9: Commit Task 7**

Run:

```powershell
git add deploy/ci-cd/tw-memory-check.ps1 deploy/ci-cd/README.md docs/standards/rules/comments-python.md .tw-memory
git commit -m "chore: verify tw memory remediation"
```

---

## Final Verification

After all tasks are complete, run:

```powershell
python tools\tw-memory\tw_memory.py generate
python tools\tw-memory\tw_memory.py check
python -m unittest discover tests\tw_memory -v
git diff --check
```

Expected:

1. `generate` exits `0`
2. `check` exits `0`
3. all `tests\tw_memory` tests pass
4. `git diff --check` exits `0`
5. `git status --short` shows only intentional committed changes or is clean after the final commit

## Self-Review Checklist

- Every acceptance criterion in `docs/superpowers/specs/2026-04-30-tw-memory-risk-remediation-design.md` is covered by at least one task.
- No task requires adding AI-only metadata to `docs/`.
- No generated `.tw-memory` artifact stores source or document bodies.
- Tests are written before implementation steps.
- Each task has a focused verification command.
- The final quality gate includes `generate`, `check`, unit tests, and `git diff --check`.

