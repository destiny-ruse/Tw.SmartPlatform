# TW Memory AI Development Layer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the first-phase local AI memory layer with `.tw-memory`, `tools/tw-memory`, local FTS, controlled chunk reads, preflight/postflight checks, and thin `tw-*` skills.

**Architecture:** `tools/tw-memory` is the only code path that scans, generates, checks, queries, and reads memory. `.tw-memory` stores source indexes, relationship indexes, route indexes, and generated chunk metadata, while original docs, READMEs, and source files remain the facts. Thin skills only run semantic preflight/postflight flows and never embed project memory.

**Tech Stack:** Python 3 standard library, `argparse`, `dataclasses`, `json`, `hashlib`, `sqlite3` FTS5, `unittest`, Markdown source files, Codex/Claude-style skill folders

---

## Source Inputs

- Design: `docs/superpowers/specs/2026-04-30-tw-memory-ai-development-design.md`
- Repository instruction: `CLAUDE.md`
- Existing tools convention: `tools/README.md`
- Existing documentation convention: `docs/README.md`
- Existing skills convention: `.agents/skills/*/SKILL.md`
- Existing standards source assets: `docs/standards/**/*.md`; standards are human-readable Markdown without front matter metadata or hidden anchor comments.
- Existing language/service roots: `frontend/`, `backend/dotnet/`, `backend/java/`, `backend/python/`, `contracts/`, `deploy/`

## Scope Boundary

This plan implements first-phase local memory only:

1. `.tw-memory` skeleton and generated JSON/YAML artifacts.
2. `tools/tw-memory` CLI commands: `scan`, `generate`, `check`, `query`, `read`, `preflight`, `postflight`, `build-search`, and `sync-vector`.
3. Local SQLite FTS5 cache under `.tw-memory/generated/fts/`, excluded from Git.
4. Automatic Markdown chunking with line ranges, hashes, summaries, keywords, and relations.
5. Thin skills: `tw-frontend`, `tw-dotnet`, `tw-java`, `tw-python`, and `tw-memory`.
6. CI-ready check command and repository ignore rules.

This plan does not build an MCP server, ingest third-party docs, copy source or manual bodies into `.tw-memory`, or implement cloud vector synchronization beyond a safe command boundary.

## File Structure

- Create: `tools/tw-memory/tw_memory.py` command entry point.
- Create: `tools/tw-memory/tw_memory_engine/__init__.py` package marker and version.
- Create: `tools/tw-memory/tw_memory_engine/models.py` dataclasses for sources, chunks, shards, query results, and diagnostics.
- Create: `tools/tw-memory/tw_memory_engine/paths.py` repository path and memory path helpers.
- Create: `tools/tw-memory/tw_memory_engine/hashing.py` normalized file and repository hashes.
- Create: `tools/tw-memory/tw_memory_engine/scanner.py` source discovery for docs, README, code, package files, language roots, services, and skills.
- Create: `tools/tw-memory/tw_memory_engine/chunking.py` Markdown chunking and synthetic chunk fallback.
- Create: `tools/tw-memory/tw_memory_engine/semantic.py` deterministic summary, keyword, language, kind, service, framework, and capability metadata.
- Create: `tools/tw-memory/tw_memory_engine/generator.py` `.tw-memory` artifact generation.
- Create: `tools/tw-memory/tw_memory_engine/search.py` local SQLite FTS5 build/query logic.
- Create: `tools/tw-memory/tw_memory_engine/reader.py` hash-checked chunk reads from fact files.
- Create: `tools/tw-memory/tw_memory_engine/checker.py` stale-index, broken-link, size, forbidden-file, and route-index checks.
- Create: `tools/tw-memory/tw_memory_engine/preflight.py` read-only task preflight.
- Create: `tools/tw-memory/tw_memory_engine/postflight.py` changed-file impact classification.
- Create: `tools/tw-memory/tw_memory_engine/vector.py` safe vector-backend command boundary.
- Create: `tools/tw-memory/tw_memory_engine/cli.py` argparse command wiring.
- Create: `tools/tw_memory_test_support.py` shared test import helper.
- Create: `tests/tw_memory/__init__.py` test package marker.
- Create: `tests/tw_memory/test_cli_contract.py`.
- Create: `tests/tw_memory/test_scanner.py`.
- Create: `tests/tw_memory/test_chunking.py`.
- Create: `tests/tw_memory/test_generator.py`.
- Create: `tests/tw_memory/test_checker.py`.
- Create: `tests/tw_memory/test_query_read.py`.
- Create: `tests/tw_memory/test_preflight_postflight.py`.
- Create: `tests/tw_memory/test_skills.py`.
- Create: `.tw-memory/README.md`.
- Create: `.tw-memory/taxonomy.yaml`.
- Create: `.tw-memory/adapters/vector-backends.yaml`.
- Create: `.agents/skills/tw-frontend/SKILL.md`.
- Create: `.agents/skills/tw-dotnet/SKILL.md`.
- Create: `.agents/skills/tw-java/SKILL.md`.
- Create: `.agents/skills/tw-python/SKILL.md`.
- Create: `.agents/skills/tw-memory/SKILL.md`.
- Create: `deploy/ci-cd/tw-memory-check.ps1`.
- Modify: `.gitignore` to ignore local memory runtime caches.
- Modify: `tools/README.md` to document the memory CLI.
- Modify: `deploy/ci-cd/README.md` to document the CI check entry point.

## Data Contracts

Use schema version `1.0.0` for first-phase artifacts.

`source-index/*.generated.json` entries must include:

```json
{
  "source_path": "docs/README.md",
  "source_hash": "sha256:<hex>",
  "source_type": "readme",
  "language": "docs",
  "framework": null,
  "service": null,
  "generator": {
    "name": "tw-memory",
    "version": "1.0.0"
  }
}
```

`generated/chunks/**/*.generated.json` entries must include:

```json
{
  "chunk_id": "docs.readme#chunk-001",
  "source_path": "docs/README.md",
  "source_hash": "sha256:<hex>",
  "start_line": 1,
  "end_line": 12,
  "heading": "Docs",
  "summary": "Short deterministic summary.",
  "keywords": ["docs"],
  "relations": {
    "language": "docs",
    "kind": "readme",
    "service": null,
    "framework": null,
    "capabilities": []
  }
}
```

`route-index/index.generated.json` must stay thin:

```json
{
  "schema_version": "1.0.0",
  "generated_at": "2026-04-30T00:00:00+08:00",
  "repo_hash": "sha256:<hex>",
  "shards": [
    {
      "id": "dotnet",
      "kind": "language",
      "path": ".tw-memory/route-index/by-language/dotnet.generated.json",
      "summary": ".NET memory entry"
    }
  ]
}
```

## Tasks

### Task 1: Create CLI Skeleton and Test Harness

**Files:**
- Create: `tools/tw-memory/tw_memory.py`
- Create: `tools/tw-memory/tw_memory_engine/__init__.py`
- Create: `tools/tw-memory/tw_memory_engine/cli.py`
- Create: `tests/tw_memory/__init__.py`
- Create: `tests/tw_memory/test_cli_contract.py`
- Modify: `tools/README.md`

- [ ] **Step 1: Write failing CLI contract tests**

Create `tests/tw_memory/__init__.py`:

```python
"""Tests for the TW memory engine."""
```

Create `tests/tw_memory/test_cli_contract.py`:

```python
import subprocess
import sys
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
CLI = REPO_ROOT / "tools" / "tw-memory" / "tw_memory.py"


class CliContractTests(unittest.TestCase):
    def test_help_lists_all_public_commands(self):
        result = subprocess.run(
            [sys.executable, str(CLI), "--help"],
            cwd=REPO_ROOT,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        for command in [
            "scan",
            "generate",
            "check",
            "query",
            "read",
            "preflight",
            "postflight",
            "build-search",
            "sync-vector",
        ]:
            self.assertIn(command, result.stdout)

    def test_scan_can_emit_json_without_writing_memory(self):
        memory_root = REPO_ROOT / ".tw-memory"
        before_exists = memory_root.exists()

        result = subprocess.run(
            [sys.executable, str(CLI), "scan", "--format", "json"],
            cwd=REPO_ROOT,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn('"sources"', result.stdout)
        self.assertEqual(before_exists, memory_root.exists())


if __name__ == "__main__":
    unittest.main()
```

- [ ] **Step 2: Run tests to verify they fail**

Run:

```powershell
python -m unittest tests.tw_memory.test_cli_contract -v
```

Expected: FAIL because `tools/tw-memory/tw_memory.py` does not exist.

- [ ] **Step 3: Create CLI package marker**

Create `tools/tw-memory/tw_memory_engine/__init__.py`:

```python
"""TW local AI memory engine."""

__version__ = "1.0.0"
GENERATOR_NAME = "tw-memory"
SCHEMA_VERSION = "1.0.0"
```

- [ ] **Step 4: Create command entry point**

Create `tools/tw-memory/tw_memory.py`:

```python
from tw_memory_engine.cli import main


if __name__ == "__main__":
    raise SystemExit(main())
```

- [ ] **Step 5: Create initial argparse wiring**

Create `tools/tw-memory/tw_memory_engine/cli.py` with command names and temporary read-only responses:

```python
from __future__ import annotations

import argparse
import json
from typing import Sequence


COMMANDS = (
    "scan",
    "generate",
    "check",
    "query",
    "read",
    "preflight",
    "postflight",
    "build-search",
    "sync-vector",
)


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(prog="tw_memory.py")
    subparsers = parser.add_subparsers(dest="command", required=True)

    scan = subparsers.add_parser("scan", help="Scan fact sources without writing memory files.")
    scan.add_argument("--format", choices=("brief", "json"), default="brief")

    generate = subparsers.add_parser("generate", help="Generate .tw-memory artifacts.")
    generate.add_argument("--format", choices=("brief", "json"), default="brief")

    check = subparsers.add_parser("check", help="Validate .tw-memory freshness and safety.")
    check.add_argument("--format", choices=("brief", "json"), default="brief")

    query = subparsers.add_parser("query", help="Query memory candidates.")
    query.add_argument("--text", required=True)
    query.add_argument("--stack")
    query.add_argument("--kind")
    query.add_argument("--format", choices=("brief", "json"), default="brief")
    query.add_argument("--limit", type=int, default=5)

    read = subparsers.add_parser("read", help="Read one checked evidence chunk.")
    read.add_argument("--chunk-id", required=True)
    read.add_argument("--format", choices=("evidence", "json", "full-section"), default="evidence")
    read.add_argument("--with-neighbors", type=int, default=0)

    preflight = subparsers.add_parser("preflight", help="Run read-only task memory preflight.")
    preflight.add_argument("--task", required=True)
    preflight.add_argument("--stack")
    preflight.add_argument("--path")
    preflight.add_argument("--format", choices=("brief", "json"), default="brief")

    postflight = subparsers.add_parser("postflight", help="Classify changed files after AI edits.")
    postflight.add_argument("--changed-files", required=True)
    postflight.add_argument("--format", choices=("brief", "json"), default="brief")

    build_search = subparsers.add_parser("build-search", help="Build local search cache.")
    build_search.add_argument("--backend", choices=("fts",), default="fts")
    build_search.add_argument("--format", choices=("brief", "json"), default="brief")

    sync_vector = subparsers.add_parser("sync-vector", help="Sync allowed internal memory to vector backend.")
    sync_vector.add_argument("--backend", required=True)
    sync_vector.add_argument("--format", choices=("brief", "json"), default="brief")

    return parser


def main(argv: Sequence[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)

    if args.command == "scan":
        payload = {"sources": [], "writes": []}
        if args.format == "json":
            print(json.dumps(payload, ensure_ascii=False, indent=2))
        else:
            print("sources: 0")
        return 0

    parser.error(f"{args.command} is wired but not implemented in this task")
    return 2
```

- [ ] **Step 6: Run CLI contract tests**

Run:

```powershell
python -m unittest tests.tw_memory.test_cli_contract -v
```

Expected: PASS.

- [ ] **Step 7: Document tool entry point**

Append this section to `tools/README.md`:

````markdown
## TW Memory CLI

`tools/tw-memory` contains the project-local AI memory engine. Use the command through the repository root:

```powershell
python tools\tw-memory\tw_memory.py <command>
```

The CLI owns scanning, generating, checking, querying, reading, preflight, postflight, and local search cache commands for `.tw-memory`.
````

- [ ] **Step 8: Commit**

```bash
git add tools/tw-memory tests/tw_memory/__init__.py tests/tw_memory/test_cli_contract.py tools/README.md
git commit -m "feat: add tw memory cli skeleton"
```

### Task 2: Discover Fact Sources Without Copying Bodies

**Files:**
- Create: `tools/tw-memory/tw_memory_engine/models.py`
- Create: `tools/tw-memory/tw_memory_engine/paths.py`
- Create: `tools/tw-memory/tw_memory_engine/hashing.py`
- Create: `tools/tw-memory/tw_memory_engine/scanner.py`
- Create: `tools/tw_memory_test_support.py`
- Modify: `tools/tw-memory/tw_memory_engine/cli.py`
- Create: `tests/tw_memory/test_scanner.py`

- [ ] **Step 1: Write failing scanner tests**

Create `tests/tw_memory/test_scanner.py`:

```python
import tempfile
import unittest
from pathlib import Path

from tools.tw_memory_test_support import import_engine


engine = import_engine()
SourceScanner = engine.scanner.SourceScanner


class ScannerTests(unittest.TestCase):
    def test_scan_finds_docs_readmes_package_files_and_language_roots(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            (root / "docs").mkdir()
            (root / "docs" / "README.md").write_text("# Docs\n", encoding="utf-8")
            (root / "backend" / "dotnet" / "ServiceA").mkdir(parents=True)
            (root / "backend" / "dotnet" / "ServiceA" / "README.md").write_text("# Service A\n", encoding="utf-8")
            (root / "frontend").mkdir()
            (root / "frontend" / "package.json").write_text('{"name":"frontend"}\n', encoding="utf-8")
            (root / ".tw-memory").mkdir()
            (root / ".tw-memory" / "old.generated.json").write_text("{}", encoding="utf-8")

            records = SourceScanner(root).scan()
            paths = {record.source_path for record in records}

            self.assertIn("docs/README.md", paths)
            self.assertIn("backend/dotnet/ServiceA/README.md", paths)
            self.assertIn("frontend/package.json", paths)
            self.assertNotIn(".tw-memory/old.generated.json", paths)

    def test_scan_records_hash_type_language_and_service(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            (root / "backend" / "dotnet" / "Services" / "Notice").mkdir(parents=True)
            path = root / "backend" / "dotnet" / "Services" / "Notice" / "README.md"
            path.write_text("# Notice Service\n", encoding="utf-8")

            [record] = SourceScanner(root).scan()

            self.assertEqual(record.source_type, "readme")
            self.assertEqual(record.language, "dotnet")
            self.assertEqual(record.service, "notice")
            self.assertTrue(record.source_hash.startswith("sha256:"))
```

Create `tools/tw_memory_test_support.py` for tests:

```python
import importlib
import sys
from pathlib import Path


def import_engine():
    repo_root = Path(__file__).resolve().parents[1]
    tool_root = repo_root / "tools" / "tw-memory"
    if str(tool_root) not in sys.path:
        sys.path.insert(0, str(tool_root))

    class Engine:
        def __getattr__(self, name):
            return importlib.import_module(f"tw_memory_engine.{name}")

    return Engine()
```

- [ ] **Step 2: Run scanner tests to verify they fail**

Run:

```powershell
python -m unittest tests.tw_memory.test_scanner -v
```

Expected: FAIL because scanner modules do not exist.

- [ ] **Step 3: Create models**

Create `tools/tw-memory/tw_memory_engine/models.py` with these dataclasses:

```python
from __future__ import annotations

from dataclasses import asdict, dataclass, field
from typing import Any


@dataclass(frozen=True)
class GeneratorInfo:
    name: str
    version: str


@dataclass(frozen=True)
class SourceRecord:
    source_path: str
    source_hash: str
    source_type: str
    language: str | None
    framework: str | None
    service: str | None
    generator: GeneratorInfo

    def to_json(self) -> dict[str, Any]:
        return asdict(self)


@dataclass(frozen=True)
class ChunkRecord:
    chunk_id: str
    source_path: str
    source_hash: str
    start_line: int
    end_line: int
    heading: str | None
    summary: str
    keywords: list[str]
    relations: dict[str, Any] = field(default_factory=dict)

    def to_json(self) -> dict[str, Any]:
        return asdict(self)


@dataclass(frozen=True)
class Diagnostic:
    level: str
    code: str
    message: str
    path: str | None = None

    def to_json(self) -> dict[str, Any]:
        return asdict(self)


@dataclass(frozen=True)
class QueryResult:
    chunk_id: str
    source_path: str
    start_line: int
    end_line: int
    summary: str
    score: float

    def to_json(self) -> dict[str, Any]:
        return asdict(self)
```

- [ ] **Step 4: Create path and hash helpers**

Implement:

```text
paths.py:
1. repo_root(start: Path | None = None) -> Path finds the first parent containing README.md and tools/.
2. memory_root(root: Path) -> Path returns root / ".tw-memory".
3. relative_posix(root: Path, path: Path) -> str returns slash-separated relative paths.

hashing.py:
1. file_sha256(path: Path) -> str returns "sha256:<hex>" over raw file bytes.
2. tree_hash(root: Path, paths: Iterable[str]) -> str hashes each path and source hash in sorted order.
3. normalize_text_for_hash(text: str) -> bytes normalizes CRLF to LF before hashing generated text.
```

- [ ] **Step 5: Create scanner**

Implement `SourceScanner(root: Path).scan()` with these exact rules:

```text
1. Include Markdown files under docs/, backend/, frontend/, contracts/, deploy/, and root README.md.
2. Include README.md files under source roots.
3. Include package files: package.json, pnpm-lock.yaml, yarn.lock, package-lock.json, *.csproj, *.sln, pom.xml, build.gradle, requirements*.txt, pyproject.toml.
4. Exclude .git/, .tw-memory/, .worktrees/, node_modules/, bin/, obj/, __pycache__/, generated/fts/, generated/vector/.
5. source_type values: spec, standard, manual, readme, source, package, service-directory, skill.
6. language values from path: frontend, dotnet, java, python, contracts, deploy, docs.
7. service names from backend/dotnet/Services/<Name>, backend/java/<Name>, backend/python/<Name>, frontend/apps/<Name>.
8. framework names from backend/dotnet/BuildingBlocks/src/<Name>, frontend/packages/<Name>, or null.
9. Return records sorted by source_path.
10. Never read or store complete body text in SourceRecord.
```

- [ ] **Step 6: Wire real scan command**

Update `cli.py` so `scan --format json` returns:

```json
{
  "sources": [
    {
      "source_path": "docs/README.md",
      "source_hash": "sha256:<hex>",
      "source_type": "readme",
      "language": "docs",
      "framework": null,
      "service": null,
      "generator": {
        "name": "tw-memory",
        "version": "1.0.0"
      }
    }
  ],
  "writes": []
}
```

Brief output must show `sources: <count>` and no file writes.

- [ ] **Step 7: Run scanner and CLI tests**

Run:

```powershell
python -m unittest tests.tw_memory.test_scanner tests.tw_memory.test_cli_contract -v
```

Expected: PASS.

- [ ] **Step 8: Commit**

```bash
git add tools/tw-memory/tw_memory_engine tools/tw-memory/tw_memory.py tools/tw_memory_test_support.py tests/tw_memory/test_scanner.py tests/tw_memory/test_cli_contract.py
git commit -m "feat: scan tw memory fact sources"
```

### Task 3: Chunk Markdown and Generate `.tw-memory`

**Files:**
- Create: `tools/tw-memory/tw_memory_engine/chunking.py`
- Create: `tools/tw-memory/tw_memory_engine/semantic.py`
- Create: `tools/tw-memory/tw_memory_engine/generator.py`
- Modify: `tools/tw-memory/tw_memory_engine/cli.py`
- Create: `tests/tw_memory/test_chunking.py`
- Create: `tests/tw_memory/test_generator.py`
- Create: `.tw-memory/README.md`
- Create: `.tw-memory/taxonomy.yaml`
- Create: `.tw-memory/adapters/vector-backends.yaml`

- [ ] **Step 1: Write failing chunking tests**

Create `tests/tw_memory/test_chunking.py`:

```python
import tempfile
import unittest
from pathlib import Path

from tools.tw_memory_test_support import import_engine


engine = import_engine()
MarkdownChunker = engine.chunking.MarkdownChunker


class ChunkingTests(unittest.TestCase):
    def test_markdown_headings_create_natural_chunks(self):
        text = "# Title\n\nintro\n\n## Usage\n\nrun this\n\n## API\n\ncall that\n"
        with tempfile.TemporaryDirectory() as work:
            path = Path(work) / "README.md"
            path.write_text(text, encoding="utf-8")

            chunks = MarkdownChunker(path, "docs.readme").chunk()

            self.assertEqual([chunk.heading for chunk in chunks], ["Title", "Usage", "API"])
            self.assertEqual(chunks[0].start_line, 1)
            self.assertEqual(chunks[0].end_line, 4)
            self.assertEqual(chunks[1].start_line, 5)
            self.assertEqual(chunks[1].end_line, 8)

    def test_code_fences_do_not_split_inside_fence(self):
        text = "# Title\n\n```text\n## not heading\n```\n\n## Real\n\nbody\n"
        with tempfile.TemporaryDirectory() as work:
            path = Path(work) / "README.md"
            path.write_text(text, encoding="utf-8")

            chunks = MarkdownChunker(path, "docs.readme").chunk()

            self.assertEqual([chunk.heading for chunk in chunks], ["Title", "Real"])
            self.assertEqual(chunks[0].end_line, 6)
```

- [ ] **Step 2: Write failing generator tests**

Create `tests/tw_memory/test_generator.py`:

```python
import json
import tempfile
import unittest
from pathlib import Path

from tools.tw_memory_test_support import import_engine


engine = import_engine()
MemoryGenerator = engine.generator.MemoryGenerator


class GeneratorTests(unittest.TestCase):
    def test_generate_writes_three_layer_memory_without_source_bodies(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            (root / "docs").mkdir()
            (root / "docs" / "README.md").write_text("# Docs\n\nHuman readable body.\n", encoding="utf-8")

            result = MemoryGenerator(root).generate()

            self.assertEqual(result.errors, [])
            source_index = json.loads((root / ".tw-memory" / "source-index" / "docs.generated.json").read_text(encoding="utf-8"))
            route_index = json.loads((root / ".tw-memory" / "route-index" / "index.generated.json").read_text(encoding="utf-8"))
            chunk_files = list((root / ".tw-memory" / "generated" / "chunks").rglob("*.generated.json"))

            self.assertGreaterEqual(len(source_index["sources"]), 1)
            self.assertIn("shards", route_index)
            self.assertGreaterEqual(len(chunk_files), 1)
            self.assertNotIn("Human readable body.", json.dumps(source_index, ensure_ascii=False))
            self.assertNotIn("Human readable body.", json.dumps(route_index, ensure_ascii=False))

    def test_generate_creates_language_graph_for_dotnet_frontend_java_python(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            for path in [
                "backend/dotnet/README.md",
                "backend/java/README.md",
                "backend/python/README.md",
                "frontend/README.md",
            ]:
                target = root / path
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_text(f"# {path}\n", encoding="utf-8")

            MemoryGenerator(root).generate()

            for language in ["dotnet", "java", "python", "frontend"]:
                graph = root / ".tw-memory" / "graph" / "languages" / f"{language}.yaml"
                self.assertTrue(graph.exists(), language)
```

- [ ] **Step 3: Run tests to verify they fail**

Run:

```powershell
python -m unittest tests.tw_memory.test_chunking tests.tw_memory.test_generator -v
```

Expected: FAIL because chunking and generation modules do not exist.

- [ ] **Step 4: Implement Markdown chunking**

Implement `MarkdownChunker` with these exact rules:

```text
1. Split on ATX headings (# through ######) only when outside fenced code blocks.
2. Keep heading line inside the chunk it starts.
3. Keep blank lines and body lines in line ranges, but do not store body text in ChunkRecord.
4. If a Markdown file has no heading, create synthetic chunks by 80-line windows with 8-line overlap.
5. chunk_id format is "<base_id>#chunk-001", "<base_id>#chunk-002", zero-padded to three digits.
6. start_line and end_line are 1-based inclusive.
7. heading is null for synthetic chunks without headings.
```

- [ ] **Step 5: Implement deterministic semantic metadata**

Implement `semantic.py` functions:

```text
summarize_chunk(path: str, heading: str | None, lines: list[str]) -> str:
  - If heading exists, return "<heading> in <path>".
  - Otherwise return "Lines <start>-<end> in <path>" from caller-provided metadata.

extract_keywords(path: str, heading: str | None, lines: list[str]) -> list[str]:
  - Include lowercase path tokens split on slash, dash, underscore, and dot.
  - Include lowercase heading tokens longer than 2 characters.
  - Include language tokens from path.
  - Return sorted unique tokens, max 20.

relations_for_source(record: SourceRecord) -> dict:
  - Include language, kind, service, framework, and capabilities.
  - Capabilities are deterministic keywords from path and heading, not model-generated claims.
```

- [ ] **Step 6: Implement generator**

Implement `MemoryGenerator(root).generate()` so it writes:

```text
.tw-memory/README.md
.tw-memory/taxonomy.yaml
.tw-memory/source-index/docs.generated.json
.tw-memory/source-index/code.generated.json
.tw-memory/source-index/packages.generated.json
.tw-memory/graph/languages/<language>.yaml
.tw-memory/graph/frameworks/<framework>.yaml
.tw-memory/graph/services/<service>.yaml
.tw-memory/route-index/index.generated.json
.tw-memory/route-index/by-language/<language>.generated.json
.tw-memory/route-index/by-kind/<kind>.generated.json
.tw-memory/route-index/by-service/<service>.generated.json
.tw-memory/route-index/by-framework/<framework>.generated.json
.tw-memory/generated/chunks/<source-path>.generated.json
.tw-memory/adapters/vector-backends.yaml
```

Generation rules:

```text
1. Create parent directories before writing.
2. Write JSON with ensure_ascii=False, indent=2, sorted keys where stable.
3. Write only metadata and chunk line ranges; never write complete source body.
4. Classify source indexes into docs, code, and packages by source_type.
5. Route shards contain chunk IDs, source path, line range, heading, summary, keywords, and relations.
6. Root route index contains only shard metadata and repo_hash.
7. YAML files contain simple key/value and list structures generated by deterministic string rendering.
8. generated/fts and generated/vector directories may be created empty but must not contain committed runtime data.
```

- [ ] **Step 7: Create static memory policy files**

Create `.tw-memory/README.md`:

````markdown
# TW Memory

`.tw-memory` is the AI memory layer for this repository. It stores generated source indexes, relationship graphs, route indexes, chunk metadata, and search synchronization metadata.

It is not a company documentation directory. Do not place manuals, standards bodies, source copies, third-party documentation, chat logs, secrets, SQLite files, vector caches, or web captures here.

Use the CLI:

```powershell
python tools\tw-memory\tw_memory.py generate
python tools\tw-memory\tw_memory.py check
```
````

Create `.tw-memory/taxonomy.yaml`:

```yaml
schema_version: "1.0.0"
languages:
  - frontend
  - dotnet
  - java
  - python
  - contracts
  - deploy
  - docs
source_types:
  - spec
  - standard
  - manual
  - readme
  - source
  - package
  - service-directory
  - skill
memory_layers:
  - source-index
  - graph
  - route-index
```

Create `.tw-memory/adapters/vector-backends.yaml`:

```yaml
schema_version: "1.0.0"
default_backend: fts
vector_backends:
  aliyun:
    enabled: false
  tencent:
    enabled: false
  volcengine:
    enabled: false
  self-hosted:
    enabled: false
```

- [ ] **Step 8: Wire generate command**

Update `cli.py`:

```text
1. `generate --format brief` prints generated file count and diagnostic count.
2. `generate --format json` prints generated paths and diagnostics.
3. Exit 0 when generation succeeds.
4. Exit 1 if generation has errors.
```

- [ ] **Step 9: Run generator tests and generate repository memory**

Run:

```powershell
python -m unittest tests.tw_memory.test_chunking tests.tw_memory.test_generator -v
python tools\tw-memory\tw_memory.py generate --format brief
```

Expected: tests PASS and `.tw-memory` generated artifacts exist.

- [ ] **Step 10: Commit**

```bash
git add .tw-memory tools/tw-memory tests/tw_memory/test_chunking.py tests/tw_memory/test_generator.py
git commit -m "feat: generate tw memory indexes"
```

### Task 4: Validate Freshness and Repository Safety

**Files:**
- Create: `tools/tw-memory/tw_memory_engine/checker.py`
- Modify: `tools/tw-memory/tw_memory_engine/cli.py`
- Create: `tests/tw_memory/test_checker.py`
- Modify: `.gitignore`

- [ ] **Step 1: Write failing checker tests**

Create `tests/tw_memory/test_checker.py`:

```python
import tempfile
import unittest
from pathlib import Path

from tools.tw_memory_test_support import import_engine


engine = import_engine()
MemoryGenerator = engine.generator.MemoryGenerator
MemoryChecker = engine.checker.MemoryChecker


class CheckerTests(unittest.TestCase):
    def test_check_reports_stale_source_hash(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            (root / "docs").mkdir()
            source = root / "docs" / "README.md"
            source.write_text("# Docs\n\nFirst\n", encoding="utf-8")
            MemoryGenerator(root).generate()

            source.write_text("# Docs\n\nChanged\n", encoding="utf-8")
            diagnostics = MemoryChecker(root).check()

            self.assertTrue(any(item.code == "source-stale" for item in diagnostics))

    def test_check_rejects_runtime_cache_files_inside_committable_memory(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            (root / "docs").mkdir()
            (root / "docs" / "README.md").write_text("# Docs\n", encoding="utf-8")
            MemoryGenerator(root).generate()
            forbidden = root / ".tw-memory" / "source-index" / "cache.sqlite"
            forbidden.write_bytes(b"sqlite")

            diagnostics = MemoryChecker(root).check()

            self.assertTrue(any(item.code == "forbidden-memory-file" for item in diagnostics))

    def test_check_warns_when_route_index_is_too_large(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            index = root / ".tw-memory" / "route-index"
            index.mkdir(parents=True)
            (index / "index.generated.json").write_text("x" * 210_000, encoding="utf-8")

            diagnostics = MemoryChecker(root).check()

            self.assertTrue(any(item.code == "large-route-index" and item.level == "warning" for item in diagnostics))
```

- [ ] **Step 2: Run checker tests to verify they fail**

Run:

```powershell
python -m unittest tests.tw_memory.test_checker -v
```

Expected: FAIL because `MemoryChecker` does not exist.

- [ ] **Step 3: Implement checker**

Implement `MemoryChecker(root).check()` with these diagnostics:

```text
source-stale:
  level error
  when a source-index source_hash differs from current source file hash

source-missing:
  level error
  when a source-index source_path no longer exists

chunk-range-invalid:
  level error
  when chunk start_line or end_line is outside current source file line count

manual-maybe-stale:
  level warning
  when a framework package README/manual source exists and related package source files are newer than the generated source-index file

broken-route-path:
  level error
  when route-index shard path points to a missing generated file

forbidden-memory-file:
  level error
  when .tw-memory contains .sqlite, .db, .parquet, .bin, .npy, generated/fts runtime files, generated/vector runtime files, third-party-docs, third-party-source, or files larger than 1 MB

large-memory-file:
  level warning at >200 KB
  level error at >1 MB
  applies to committable .tw-memory files

large-route-index:
  level warning at >200 KB
  level error at >1 MB
  applies to .tw-memory/route-index/index.generated.json
```

- [ ] **Step 4: Wire check command**

Update `cli.py`:

```text
1. `check --format brief` prints one line per diagnostic as "<level> <code> <path> <message>".
2. `check --format json` prints {"diagnostics": [...]}.
3. Exit 0 if no error diagnostics exist.
4. Exit 1 if any error diagnostic exists.
```

- [ ] **Step 5: Update Git ignore rules**

Append to `.gitignore`:

```gitignore
.tw-memory/generated/fts/
.tw-memory/generated/vector/
.tw-memory/**/*.sqlite
.tw-memory/**/*.db
```

- [ ] **Step 6: Run checker tests and current repo check**

Run:

```powershell
python -m unittest tests.tw_memory.test_checker -v
python tools\tw-memory\tw_memory.py check --format brief
```

Expected: tests PASS and current repository check exits 0 after generated artifacts are fresh.

- [ ] **Step 7: Commit**

```bash
git add tools/tw-memory tests/tw_memory/test_checker.py .gitignore .tw-memory
git commit -m "feat: validate tw memory freshness"
```

### Task 5: Implement Query, Hash-Checked Read, and Local FTS

**Files:**
- Create: `tools/tw-memory/tw_memory_engine/search.py`
- Create: `tools/tw-memory/tw_memory_engine/reader.py`
- Modify: `tools/tw-memory/tw_memory_engine/cli.py`
- Create: `tests/tw_memory/test_query_read.py`

- [ ] **Step 1: Write failing query/read tests**

Create `tests/tw_memory/test_query_read.py`:

```python
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

from tools.tw_memory_test_support import import_engine


engine = import_engine()
MemoryGenerator = engine.generator.MemoryGenerator
SearchIndex = engine.search.SearchIndex
ChunkReader = engine.reader.ChunkReader


class QueryReadTests(unittest.TestCase):
    def test_query_returns_limited_brief_results(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            docs = root / "docs"
            docs.mkdir()
            (docs / "cache.md").write_text("# Cache\n\nRedis distributed cache wrapper.\n", encoding="utf-8")
            MemoryGenerator(root).generate()
            SearchIndex(root).build_fts()

            results = SearchIndex(root).query("redis cache", stack=None, kind=None, limit=1)

            self.assertEqual(len(results), 1)
            self.assertIn("cache", results[0].summary.lower())

    def test_read_returns_evidence_and_verifies_hash(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            docs = root / "docs"
            docs.mkdir()
            source = docs / "cache.md"
            source.write_text("# Cache\n\nRedis distributed cache wrapper.\n", encoding="utf-8")
            MemoryGenerator(root).generate()
            chunk_id = "docs.cache#chunk-001"

            evidence = ChunkReader(root).read(chunk_id, with_neighbors=0)

            self.assertEqual(evidence["chunk_id"], chunk_id)
            self.assertIn("Redis distributed cache wrapper.", evidence["text"])
            self.assertEqual(evidence["stale"], False)

            source.write_text("# Cache\n\nChanged body.\n", encoding="utf-8")
            stale = ChunkReader(root).read(chunk_id, with_neighbors=0)
            self.assertEqual(stale["stale"], True)
            self.assertNotIn("Changed body.", stale.get("text", ""))
```

- [ ] **Step 2: Run tests to verify they fail**

Run:

```powershell
python -m unittest tests.tw_memory.test_query_read -v
```

Expected: FAIL because search and reader modules do not exist.

- [ ] **Step 3: Implement local FTS**

Implement `SearchIndex`:

```text
build_fts():
  1. Create .tw-memory/generated/fts/tw-memory.sqlite.
  2. Use sqlite3.
  3. Create table chunks(chunk_id primary key, source_path, start_line, end_line, summary, keywords_json, relations_json).
  4. Create virtual table chunks_fts using fts5(chunk_id, summary, keywords, source_path, content="").
  5. Insert only generated chunk metadata, not source body text.
  6. Return count of indexed chunks.

query(text, stack, kind, limit):
  1. Prefer FTS table when database exists.
  2. Fall back to route-index JSON scan when FTS database is missing.
  3. Filter by relations.language == stack when stack is provided.
  4. Filter by relations.kind == kind when kind is provided.
  5. Return QueryResult objects sorted by score descending then chunk_id.
  6. Never return source body text.
```

- [ ] **Step 4: Implement hash-checked reader**

Implement `ChunkReader.read(chunk_id: str, with_neighbors: int = 0)`:

```text
1. Locate chunk metadata by scanning .tw-memory/generated/chunks/**/*.generated.json.
2. If chunk_id is unknown, return {"chunk_id": chunk_id, "stale": true, "error": "chunk-not-found"}.
3. Recompute current source hash before reading line text.
4. If hash differs, return {"chunk_id": chunk_id, "stale": true, "error": "source-stale", "action": "run generate/check"} and do not read old line ranges.
5. Apply neighbor range by expanding to adjacent chunk metadata from the same source when --with-neighbors is greater than 0.
6. Return evidence JSON with chunk_id, source_path, source_hash, start_line, end_line, text, and stale=false.
7. `--format evidence` prints source path, line range, and text.
8. `--format full-section` is allowed only when the selected chunk heading has child lines in the same source; otherwise it behaves like evidence.
```

- [ ] **Step 5: Wire query, read, and build-search commands**

Update `cli.py`:

```text
query:
  - Calls SearchIndex(root).query(args.text, args.stack, args.kind, args.limit).
  - brief output lists chunk_id, source path, line range, and summary.
  - json output returns {"results": [...]}.

read:
  - Calls ChunkReader(root).read(args.chunk_id, args.with_neighbors).
  - exits 2 when stale is true.
  - does not continue reading stale line ranges.

build-search:
  - Calls SearchIndex(root).build_fts().
  - prints indexed chunk count.
```

- [ ] **Step 6: Run query/read tests and smoke commands**

Run:

```powershell
python -m unittest tests.tw_memory.test_query_read -v
python tools\tw-memory\tw_memory.py build-search --backend fts
python tools\tw-memory\tw_memory.py query --text "dotnet service readme" --format brief --limit 3
```

Expected: tests PASS, FTS build exits 0, query exits 0 and prints at most 3 results.

- [ ] **Step 7: Commit**

```bash
git add tools/tw-memory tests/tw_memory/test_query_read.py .tw-memory
git commit -m "feat: query and read tw memory chunks"
```

### Task 6: Implement Preflight and Postflight

**Files:**
- Create: `tools/tw-memory/tw_memory_engine/preflight.py`
- Create: `tools/tw-memory/tw_memory_engine/postflight.py`
- Modify: `tools/tw-memory/tw_memory_engine/cli.py`
- Create: `tests/tw_memory/test_preflight_postflight.py`

- [ ] **Step 1: Write failing preflight/postflight tests**

Create `tests/tw_memory/test_preflight_postflight.py`:

```python
import tempfile
import unittest
from pathlib import Path

from tools.tw_memory_test_support import import_engine


engine = import_engine()
MemoryGenerator = engine.generator.MemoryGenerator
PreflightRunner = engine.preflight.PreflightRunner
PostflightRunner = engine.postflight.PostflightRunner


class PreflightPostflightTests(unittest.TestCase):
    def test_preflight_is_read_only_and_reports_missing_memory(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            (root / "docs").mkdir()
            (root / "docs" / "README.md").write_text("# Docs\n", encoding="utf-8")

            result = PreflightRunner(root).run(task="cache wrapper", stack="dotnet", path="backend/dotnet")

            self.assertIn("generate", result["actions"])
            self.assertFalse((root / ".tw-memory").exists())

    def test_preflight_returns_candidates_when_memory_exists(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            docs = root / "docs"
            docs.mkdir()
            (docs / "cache.md").write_text("# Cache\n\nRedis cache wrapper.\n", encoding="utf-8")
            MemoryGenerator(root).generate()

            result = PreflightRunner(root).run(task="redis cache wrapper", stack=None, path="docs/cache.md")

            self.assertEqual(result["status"], "ok")
            self.assertGreaterEqual(len(result["candidates"]), 1)

    def test_postflight_classifies_memory_impact(self):
        result = PostflightRunner(Path(".")).run([
            "docs/standards/rules/cache.md",
            "backend/dotnet/BuildingBlocks/src/Tw.Caching/README.md",
            "backend/dotnet/BuildingBlocks/src/Tw.Caching/CacheClient.cs",
            "frontend/apps/tw.web.ops/src/view.vue",
        ])

        self.assertIn("docs/standards/rules/cache.md", result["memory_affecting_files"])
        self.assertIn("backend/dotnet/BuildingBlocks/src/Tw.Caching/README.md", result["memory_affecting_files"])
        self.assertIn("backend/dotnet/BuildingBlocks/src/Tw.Caching/CacheClient.cs", result["review_required_files"])
        self.assertIn("generate", result["actions"])
        self.assertIn("check", result["actions"])
```

- [ ] **Step 2: Run tests to verify they fail**

Run:

```powershell
python -m unittest tests.tw_memory.test_preflight_postflight -v
```

Expected: FAIL because preflight and postflight modules do not exist.

- [ ] **Step 3: Implement preflight**

Implement `PreflightRunner(root).run(task, stack, path)`:

```text
1. Never write files.
2. If .tw-memory/route-index/index.generated.json is missing, return status "missing-memory" and actions ["generate", "check"].
3. Run MemoryChecker and include diagnostics.
4. If error diagnostics exist, return status "stale-or-invalid" and actions ["generate", "check"].
5. Build a semantic query from task, stack, path basename, path language, and path service.
6. Query memory with limit 5.
7. Return status "ok", candidates, diagnostics, and actions [] unless FTS is missing.
8. If FTS is missing, include action "build-search" but still return route-index fallback candidates.
```

- [ ] **Step 4: Implement postflight**

Implement `PostflightRunner(root).run(changed_files: list[str])`:

```text
memory_affecting_files:
  - docs/**
  - **/README.md
  - package files
  - .tw-memory/taxonomy.yaml
  - .tw-memory/graph/**

review_required_files:
  - backend/dotnet/BuildingBlocks/src/**/*.cs
  - frontend/packages/**/*
  - backend/java/**/src/**/*
  - backend/python/**/*

actions:
  - ["generate", "check"] when memory_affecting_files is non-empty
  - ["review-manual-sync", "postflight-again"] when only review_required_files are present
  - [] for ordinary business code changes
```

Return JSON fields: `memory_affecting_files`, `review_required_files`, `ordinary_files`, `actions`, and `reason`.

- [ ] **Step 5: Wire preflight and postflight commands**

Update `cli.py`:

```text
preflight:
  - Parses --task, --stack, --path, --format.
  - Exits 0 for status ok.
  - Exits 2 for missing-memory or stale-or-invalid.

postflight:
  - Parses --changed-files as a semicolon-separated or comma-separated list.
  - Prints brief action lines.
  - Exits 0.
```

- [ ] **Step 6: Run tests and smoke commands**

Run:

```powershell
python -m unittest tests.tw_memory.test_preflight_postflight -v
python tools\tw-memory\tw_memory.py preflight --task "cache wrapper" --stack dotnet --path backend/dotnet --format brief
python tools\tw-memory\tw_memory.py postflight --changed-files "docs/README.md;frontend/apps/tw.web.ops/README.md" --format brief
```

Expected: tests PASS, preflight exits 0 after memory generation, postflight suggests `generate` and `check`.

- [ ] **Step 7: Commit**

```bash
git add tools/tw-memory tests/tw_memory/test_preflight_postflight.py
git commit -m "feat: add tw memory preflight postflight"
```

### Task 7: Add Vector Boundary and CI Check Entry

**Files:**
- Create: `tools/tw-memory/tw_memory_engine/vector.py`
- Modify: `tools/tw-memory/tw_memory_engine/cli.py`
- Modify: `.tw-memory/adapters/vector-backends.yaml`
- Create: `deploy/ci-cd/tw-memory-check.ps1`
- Modify: `deploy/ci-cd/README.md`

- [ ] **Step 1: Write vector command behavior test**

Add this test to `tests/tw_memory/test_cli_contract.py`:

```python
    def test_sync_vector_is_safe_when_backend_is_disabled(self):
        result = subprocess.run(
            [sys.executable, str(CLI), "sync-vector", "--backend", "aliyun", "--format", "json"],
            cwd=REPO_ROOT,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
        )

        self.assertEqual(result.returncode, 2)
        self.assertIn('"status": "disabled"', result.stdout)
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
python -m unittest tests.tw_memory.test_cli_contract.CliContractTests.test_sync_vector_is_safe_when_backend_is_disabled -v
```

Expected: FAIL because `sync-vector` is not implemented.

- [ ] **Step 3: Implement vector boundary**

Implement `VectorSyncRunner(root).run(backend: str)`:

```text
1. Read .tw-memory/adapters/vector-backends.yaml with a small line-based parser for backend names and enabled flags.
2. Supported backend names: aliyun, tencent, volcengine, self-hosted.
3. If backend is unknown, return status "unknown-backend" and exit code 2.
4. If backend is disabled, return status "disabled" and exit code 2.
5. If backend is enabled, return status "not-configured" and exit code 2 unless a future adapter command is explicitly configured.
6. Never read third-party docs.
7. Never upload data in first-phase implementation.
```

- [ ] **Step 4: Wire sync-vector command**

Update `cli.py`:

```text
1. `sync-vector --backend aliyun --format brief` prints "aliyun: disabled".
2. JSON output prints {"backend": "aliyun", "status": "disabled", "uploaded": 0}.
3. Disabled, unknown, and not-configured statuses exit 2.
```

- [ ] **Step 5: Create CI check script**

Create `deploy/ci-cd/tw-memory-check.ps1`:

```powershell
$ErrorActionPreference = "Stop"

python tools\tw-memory\tw_memory.py check --format brief
```

Append to `deploy/ci-cd/README.md`:

````markdown
## TW Memory Check

CI should run this command from the repository root:

```powershell
.\deploy\ci-cd\tw-memory-check.ps1
```

The script validates `.tw-memory` freshness, source hashes, route paths, chunk line ranges, and forbidden runtime cache files. It does not generate files in CI.
````

- [ ] **Step 6: Run vector and CI checks**

Run:

```powershell
python -m unittest tests.tw_memory.test_cli_contract -v
powershell -ExecutionPolicy Bypass -File deploy\ci-cd\tw-memory-check.ps1
```

Expected: tests PASS and CI script exits 0.

- [ ] **Step 7: Commit**

```bash
git add tools/tw-memory tests/tw_memory/test_cli_contract.py .tw-memory/adapters/vector-backends.yaml deploy/ci-cd/tw-memory-check.ps1 deploy/ci-cd/README.md
git commit -m "feat: add tw memory vector boundary and ci check"
```

### Task 8: Add Thin Skills

**Files:**
- Create: `.agents/skills/tw-frontend/SKILL.md`
- Create: `.agents/skills/tw-dotnet/SKILL.md`
- Create: `.agents/skills/tw-java/SKILL.md`
- Create: `.agents/skills/tw-python/SKILL.md`
- Create: `.agents/skills/tw-memory/SKILL.md`
- Create: `tests/tw_memory/test_skills.py`

- [ ] **Step 1: Write failing skill tests**

Create `tests/tw_memory/test_skills.py`:

```python
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]


class ThinSkillTests(unittest.TestCase):
    def test_language_skills_are_thin_and_call_cli(self):
        for name, stack in [
            ("tw-frontend", "frontend"),
            ("tw-dotnet", "dotnet"),
            ("tw-java", "java"),
            ("tw-python", "python"),
        ]:
            path = REPO_ROOT / ".agents" / "skills" / name / "SKILL.md"
            text = path.read_text(encoding="utf-8")

            self.assertIn(f"name: {name}", text)
            self.assertIn("python tools\\tw-memory\\tw_memory.py preflight", text)
            self.assertIn("python tools\\tw-memory\\tw_memory.py postflight", text)
            self.assertIn(f"--stack {stack}", text)
            self.assertNotIn(".tw-memory/source-index/", text)
            self.assertNotIn(".tw-memory/route-index/", text)

    def test_tw_memory_skill_is_for_memory_system_only(self):
        text = (REPO_ROOT / ".agents" / "skills" / "tw-memory" / "SKILL.md").read_text(encoding="utf-8")

        self.assertIn("Use only when maintaining the TW memory system itself", text)
        self.assertIn("generate", text)
        self.assertIn("check", text)
```

- [ ] **Step 2: Run tests to verify they fail**

Run:

```powershell
python -m unittest tests.tw_memory.test_skills -v
```

Expected: FAIL because thin skill files do not exist.

- [ ] **Step 3: Create language skill template**

Create `.agents/skills/tw-dotnet/SKILL.md`:

````markdown
---
name: tw-dotnet
description: Use for .NET development tasks in this repository before reading project memory or editing .NET services, BuildingBlocks, README files, or tests.
---

# TW .NET Development

Use this skill for .NET tasks under `backend/dotnet`.

## Preflight

Before editing, summarize the user's task into a short semantic query and run:

```powershell
python tools\tw-memory\tw_memory.py preflight --task "<semantic query>" --stack dotnet --path "<target path>" --format brief
```

Read only the candidates returned by `preflight`, then call `query` and `read` for specific evidence chunks:

```powershell
python tools\tw-memory\tw_memory.py query --text "<specific query>" --stack dotnet --format brief --limit 5
python tools\tw-memory\tw_memory.py read --chunk-id "<chunk id>" --format evidence
```

## Development Rules

Prefer internal BuildingBlocks, service README files, and company standards before direct third-party usage. Do not open generated route shards for full parsing. Do not copy manuals or source bodies into `.tw-memory`.

## Postflight

After edits, classify memory impact:

```powershell
python tools\tw-memory\tw_memory.py postflight --changed-files "<changed files>" --format brief
```

If postflight requests regeneration, run:

```powershell
python tools\tw-memory\tw_memory.py generate
python tools\tw-memory\tw_memory.py check
```
````

Create `.agents/skills/tw-frontend/SKILL.md`:

````markdown
---
name: tw-frontend
description: Use for frontend development tasks in this repository before reading project memory or editing frontend apps, packages, README files, or tests.
---

# TW Frontend Development

Use this skill for frontend tasks under `frontend`.

## Preflight

Before editing, summarize the user's task into a short semantic query and run:

```powershell
python tools\tw-memory\tw_memory.py preflight --task "<semantic query>" --stack frontend --path "<target path>" --format brief
```

Read only the candidates returned by `preflight`, then call `query` and `read` for specific evidence chunks:

```powershell
python tools\tw-memory\tw_memory.py query --text "<specific query>" --stack frontend --format brief --limit 5
python tools\tw-memory\tw_memory.py read --chunk-id "<chunk id>" --format evidence
```

## Development Rules

Prefer internal frontend packages, app README files, and company standards before direct third-party usage. Do not open generated route shards for full parsing. Do not copy manuals or source bodies into `.tw-memory`.

## Postflight

After edits, classify memory impact:

```powershell
python tools\tw-memory\tw_memory.py postflight --changed-files "<changed files>" --format brief
```

If postflight requests regeneration, run:

```powershell
python tools\tw-memory\tw_memory.py generate
python tools\tw-memory\tw_memory.py check
```
````

Create `.agents/skills/tw-java/SKILL.md`:

````markdown
---
name: tw-java
description: Use for Java development tasks in this repository before reading project memory or editing Java services, packages, README files, or tests.
---

# TW Java Development

Use this skill for Java tasks under `backend/java`.

## Preflight

Before editing, summarize the user's task into a short semantic query and run:

```powershell
python tools\tw-memory\tw_memory.py preflight --task "<semantic query>" --stack java --path "<target path>" --format brief
```

Read only the candidates returned by `preflight`, then call `query` and `read` for specific evidence chunks:

```powershell
python tools\tw-memory\tw_memory.py query --text "<specific query>" --stack java --format brief --limit 5
python tools\tw-memory\tw_memory.py read --chunk-id "<chunk id>" --format evidence
```

## Development Rules

Prefer internal Java service README files and company standards before direct third-party usage. Do not open generated route shards for full parsing. Do not copy manuals or source bodies into `.tw-memory`.

## Postflight

After edits, classify memory impact:

```powershell
python tools\tw-memory\tw_memory.py postflight --changed-files "<changed files>" --format brief
```

If postflight requests regeneration, run:

```powershell
python tools\tw-memory\tw_memory.py generate
python tools\tw-memory\tw_memory.py check
```
````

Create `.agents/skills/tw-python/SKILL.md`:

````markdown
---
name: tw-python
description: Use for Python development tasks in this repository before reading project memory or editing Python services, scripts, README files, or tests.
---

# TW Python Development

Use this skill for Python tasks under `backend/python`.

## Preflight

Before editing, summarize the user's task into a short semantic query and run:

```powershell
python tools\tw-memory\tw_memory.py preflight --task "<semantic query>" --stack python --path "<target path>" --format brief
```

Read only the candidates returned by `preflight`, then call `query` and `read` for specific evidence chunks:

```powershell
python tools\tw-memory\tw_memory.py query --text "<specific query>" --stack python --format brief --limit 5
python tools\tw-memory\tw_memory.py read --chunk-id "<chunk id>" --format evidence
```

## Development Rules

Prefer internal Python service README files, scripts documented in the repository, and company standards before direct third-party usage. Do not open generated route shards for full parsing. Do not copy manuals or source bodies into `.tw-memory`.

## Postflight

After edits, classify memory impact:

```powershell
python tools\tw-memory\tw_memory.py postflight --changed-files "<changed files>" --format brief
```

If postflight requests regeneration, run:

```powershell
python tools\tw-memory\tw_memory.py generate
python tools\tw-memory\tw_memory.py check
```
````

Each language skill must be written in English and must not embed `.tw-memory` shard paths as instructions to read directly.

- [ ] **Step 4: Create memory maintenance skill**

Create `.agents/skills/tw-memory/SKILL.md`:

````markdown
---
name: tw-memory
description: Use only when maintaining the TW memory system itself: `.tw-memory`, `tools/tw-memory`, memory indexes, generated chunk metadata, or TW memory thin skills.
---

# TW Memory System Maintenance

Use only when maintaining the TW memory system itself. Do not use this skill for ordinary product code.

## Required Flow

1. Run focused tests for the memory engine before changing behavior.
2. Change `tools/tw-memory` or `.tw-memory` contracts through the CLI.
3. Run generation and checks after contract changes:

```powershell
python tools\tw-memory\tw_memory.py generate
python tools\tw-memory\tw_memory.py check
python -m unittest discover tests\tw_memory -v
```

## Boundaries

`.tw-memory` stores indexes, graph metadata, route indexes, chunk metadata, summaries, keywords, relations, hashes, and evidence pointers. It must not store manual bodies, standards bodies, source copies, third-party docs, source archives, chat logs, secrets, SQLite files, vector caches, or web captures.
````

- [ ] **Step 5: Run skill tests**

Run:

```powershell
python -m unittest tests.tw_memory.test_skills -v
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add .agents/skills/tw-frontend .agents/skills/tw-dotnet .agents/skills/tw-java .agents/skills/tw-python .agents/skills/tw-memory tests/tw_memory/test_skills.py
git commit -m "feat: add tw memory thin skills"
```

### Task 9: Final First-Phase Verification

**Files:**
- Verify: `tools/tw-memory/**`
- Verify: `.tw-memory/**`
- Verify: `.agents/skills/tw-*/SKILL.md`
- Verify: `tests/tw_memory/**`

- [ ] **Step 1: Regenerate memory**

Run:

```powershell
python tools\tw-memory\tw_memory.py generate --format brief
```

Expected: exits 0 and updates only `.tw-memory` generated metadata.

- [ ] **Step 2: Build local FTS**

Run:

```powershell
python tools\tw-memory\tw_memory.py build-search --backend fts --format brief
```

Expected: exits 0 and writes `.tw-memory/generated/fts/tw-memory.sqlite`, which remains ignored by Git.

- [ ] **Step 3: Run memory check**

Run:

```powershell
python tools\tw-memory\tw_memory.py check --format brief
```

Expected: exits 0 with no error diagnostics.

- [ ] **Step 4: Run full memory test suite**

Run:

```powershell
python -m unittest discover tests\tw_memory -v
```

Expected: PASS.

- [ ] **Step 5: Verify ignored runtime files are not staged**

Run:

```powershell
git status --short -- .tw-memory/generated/fts .tw-memory/generated/vector
```

Expected: no output.

- [ ] **Step 6: Run preflight and read smoke flow**

Run:

```powershell
python tools\tw-memory\tw_memory.py preflight --task "find .NET caching framework memory" --stack dotnet --path backend/dotnet --format brief
python tools\tw-memory\tw_memory.py query --text "dotnet caching building blocks" --stack dotnet --format brief --limit 3
```

Expected: both commands exit 0. Query prints at most 3 chunk candidates. If candidates exist, run one `read` command against a returned chunk ID and expect evidence text from the source file.

- [ ] **Step 7: Commit verification-generated metadata**

```bash
git add .tw-memory tools/tw-memory tests/tw_memory .agents/skills/tw-frontend .agents/skills/tw-dotnet .agents/skills/tw-java .agents/skills/tw-python .agents/skills/tw-memory .gitignore tools/README.md deploy/ci-cd
git commit -m "chore: verify tw memory first phase"
```

## Self-Review Checklist

- [ ] `.tw-memory` is not used as a company documentation directory.
- [ ] `.tw-memory` generated files do not contain manual bodies, standards bodies, source copies, third-party docs, chat logs, secrets, SQLite files, vector caches, or web captures.
- [ ] `tools/tw-memory` owns scan, generate, check, query, read, preflight, postflight, build-search, and sync-vector command boundaries.
- [ ] `preflight` is read-only and returns actions instead of modifying files.
- [ ] `postflight` classifies changed files before deciding whether `generate/check` is needed.
- [ ] `query` returns small candidate summaries.
- [ ] `read` verifies source hashes before returning line text.
- [ ] Local FTS stores searchable metadata only and is ignored by Git.
- [ ] Thin skills are English, task-triggered, and call the CLI rather than embedding memory.
- [ ] CI entry runs `check` only and does not generate files.
- [ ] The first phase excludes MCP, cloud vector upload, and third-party knowledge ingestion.

## Execution Handoff

Plan complete when all tasks above are checked and the final verification commit exists.

Recommended execution mode:

1. Subagent-Driven: dispatch a fresh worker per task, review task output against this plan, then run the task verification command.
2. Inline Execution: execute tasks in this session using `executing-plans` with a checkpoint after each task.
