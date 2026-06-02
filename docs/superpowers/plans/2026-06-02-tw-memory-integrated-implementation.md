# tw-memory Integrated Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the `tw-memory` generated memory layer, shared package charter gates, rules/memory loading boundary, generated cards/routes, and local/CI validation from `docs/superpowers/specs/tw-memory-integrated-design.md`.

**Architecture:** Build a Python CLI under `tools/src/tw_memory` with deterministic source discovery, source hashing, route generation, card rendering, and check gates. `.rules` remains the bootstrap and formal-standard routing layer; `.tw-memory` is generated from `.rules`, `docs/engineering-standards`, package charters, contracts, project files, and skill metadata. CodeGraph is represented only as generated query intent and is never required for generation, checks, or Skill execution.

**Tech Stack:** Python 3.12+ standard library, PyYAML, pytest, Git, Markdown text parsing, XML parsing via `xml.etree.ElementTree`, JSON via standard library, GitHub Actions.

---

## Scope Check

This plan implements one integrated subsystem: the generated AI memory layer and the repository rules boundary that controls how agents load it. The work touches tools, tests, `.rules`, package charters, generated `.tw-memory` files, and CI; each task has a local verification step and a commit boundary.

The plan does not implement CodeGraph itself, vector search, FTS caches, runtime caches, service cards for empty service directories, frontend cards for empty frontend directories, or contract cards for empty contract directories.

## File Structure

Tooling:

```text
tools/
|-- pyproject.toml
|-- src/tw_memory/
|   |-- __init__.py
|   |-- __main__.py
|   |-- cards.py
|   |-- charter.py
|   |-- check.py
|   |-- cli.py
|   |-- codegraph_routes.py
|   |-- discovery.py
|   |-- generate.py
|   |-- generated_io.py
|   |-- hashing.py
|   |-- markdown_segments.py
|   |-- packages.py
|   |-- repo.py
|   |-- rules_boundary.py
|   |-- routes.py
|   |-- secret_scan.py
|   |-- source_index.py
|   `-- yaml_io.py
`-- tests/
    |-- conftest.py
    |-- test_cards.py
    |-- test_charter.py
    |-- test_check.py
    |-- test_discovery.py
    |-- test_generate.py
    |-- test_markdown_segments.py
    |-- test_packages.py
    |-- test_rules_boundary.py
    |-- test_secret_scan.py
    `-- test_source_index.py
```

Generated memory:

```text
.tw-memory/
|-- manifest/
|   |-- taxonomy.generated.yaml
|   `-- source-index.generated.json
|-- routes/
|   |-- standards.generated.yaml
|   |-- skills.generated.yaml
|   |-- codegraph-queries.generated.yaml
|   |-- packages.generated.yaml
|   |-- services.generated.yaml
|   |-- apis.generated.yaml
|   `-- frontend.generated.yaml
`-- cards/
    |-- packages/*.generated.md
    `-- public-apis/*.generated.md
```

Source facts and governance:

```text
backend/dotnet/BuildingBlocks/src/Tw.Core/package-charter.yaml
backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/package-charter.yaml
docs/engineering-standards/03-project-and-code/shared-package-charter.md
.rules/ai-coding-rules/**/*.md
.pre-commit-config.yaml
.github/workflows/tw-memory.yml
```

---

## Task 0: Python CLI Package Skeleton

**Files:**
- Create: `tools/pyproject.toml`
- Create: `tools/src/tw_memory/__init__.py`
- Create: `tools/src/tw_memory/__main__.py`
- Create: `tools/src/tw_memory/cli.py`
- Create: `tools/tests/conftest.py`

- [ ] **Step 1: Write `tools/pyproject.toml`**

```toml
[project]
name = "tw-memory"
version = "0.1.0"
description = "Deterministic generated memory layer tooling for Tw.SmartPlatform."
requires-python = ">=3.12"
dependencies = ["PyYAML>=6.0"]

[project.scripts]
tw-memory = "tw_memory.cli:main"

[build-system]
requires = ["setuptools>=68"]
build-backend = "setuptools.build_meta"

[tool.setuptools.packages.find]
where = ["src"]

[tool.pytest.ini_options]
pythonpath = ["src"]
testpaths = ["tests"]
```

- [ ] **Step 2: Write `tools/src/tw_memory/__init__.py`**

```python
"""Generated memory layer tooling for Tw.SmartPlatform."""

__version__ = "0.1.0"
GENERATOR_VERSION = "tw-memory:0.1.0"
```

- [ ] **Step 3: Write `tools/src/tw_memory/__main__.py`**

```python
from tw_memory.cli import main

if __name__ == "__main__":
    raise SystemExit(main())
```

- [ ] **Step 4: Write `tools/src/tw_memory/cli.py`**

```python
from __future__ import annotations

import argparse


def main(argv: list[str] | None = None) -> int:
    """Run the tw-memory command line interface."""
    parser = argparse.ArgumentParser(prog="tw-memory")
    sub = parser.add_subparsers(dest="command", required=True)

    gen = sub.add_parser("generate", help="Generate .tw-memory from source facts.")
    gen.add_argument("--root", default=None, help="Repository root. Defaults to auto-detect.")

    chk = sub.add_parser("check", help="Validate generated memory and source facts.")
    chk.add_argument("--root", default=None, help="Repository root. Defaults to auto-detect.")
    chk.add_argument("--staged", action="store_true", help="Check git-staged paths for commit-boundary gates.")

    args = parser.parse_args(argv)

    if args.command == "generate":
        from tw_memory.generate import run_generate

        return run_generate(args.root)
    if args.command == "check":
        from tw_memory.check import run_check

        return run_check(args.root, staged=args.staged)
    parser.error(f"unknown command {args.command!r}")
    return 2
```

- [ ] **Step 5: Write `tools/tests/conftest.py`**

```python
from __future__ import annotations

import subprocess
from pathlib import Path

import pytest


@pytest.fixture
def repo(tmp_path: Path) -> Path:
    """Create a minimal repository shape with a .git marker."""
    (tmp_path / ".git").mkdir()
    (tmp_path / ".rules/ai-coding-rules/languages").mkdir(parents=True)
    (tmp_path / ".rules/ai-coding-rules/tasks").mkdir(parents=True)
    (tmp_path / "docs/engineering-standards/03-project-and-code").mkdir(parents=True)
    (tmp_path / "docs/engineering-standards/04-quality").mkdir(parents=True)
    (tmp_path / "backend/dotnet/BuildingBlocks/src").mkdir(parents=True)
    (tmp_path / "contracts/protos").mkdir(parents=True)
    (tmp_path / "frontend/apps").mkdir(parents=True)
    (tmp_path / "frontend/packages").mkdir(parents=True)
    return tmp_path


@pytest.fixture
def git_repo(tmp_path: Path) -> Path:
    """Create an initialized git repository for commit-boundary tests."""
    subprocess.run(["git", "init", "-q"], cwd=tmp_path, check=True)
    subprocess.run(["git", "config", "user.email", "t@t"], cwd=tmp_path, check=True)
    subprocess.run(["git", "config", "user.name", "t"], cwd=tmp_path, check=True)
    return tmp_path


def write_text(path: Path, content: str) -> Path:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8")
    return path


def make_csproj(root: Path, name: str, body: str = "") -> Path:
    package_dir = root / "backend/dotnet/BuildingBlocks/src" / name
    package_dir.mkdir(parents=True, exist_ok=True)
    content = body or '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>'
    (package_dir / f"{name}.csproj").write_text(content, encoding="utf-8")
    return package_dir
```

- [ ] **Step 6: Run skeleton command**

Run: `cd tools && python -m tw_memory --help`

Expected: command exits `0` and shows `generate` and `check` subcommands.

- [ ] **Step 7: Commit**

```bash
git add tools/pyproject.toml tools/src/tw_memory/__init__.py tools/src/tw_memory/__main__.py tools/src/tw_memory/cli.py tools/tests/conftest.py
git commit -m "chore(tw-memory): scaffold generated memory cli"
```

---

## Task 1: Repository Paths, Hashing, YAML, and Generated File Boundaries

**Files:**
- Create: `tools/src/tw_memory/repo.py`
- Create: `tools/src/tw_memory/hashing.py`
- Create: `tools/src/tw_memory/yaml_io.py`
- Create: `tools/src/tw_memory/generated_io.py`
- Test: `tools/tests/test_source_index.py`

- [ ] **Step 1: Write tests for path resolution, normalized hashing, YAML round-trip, and generated path guard**

Create `tools/tests/test_source_index.py`:

```python
from pathlib import Path

import pytest

from tw_memory.generated_io import assert_generated_memory_path
from tw_memory.hashing import sha256_normalized
from tw_memory.repo import RepoPaths, find_repo_root
from tw_memory.yaml_io import dump_yaml, load_yaml


def test_find_repo_root_from_nested_directory(repo: Path):
    nested = repo / "backend/dotnet/BuildingBlocks/src"
    assert find_repo_root(nested) == repo


def test_repo_paths_point_to_generated_memory(repo: Path):
    paths = RepoPaths(repo)
    assert paths.source_index == repo / ".tw-memory/manifest/source-index.generated.json"
    assert paths.standards_route == repo / ".tw-memory/routes/standards.generated.yaml"
    assert paths.package_cards == repo / ".tw-memory/cards/packages"


def test_sha256_normalizes_line_endings(tmp_path: Path):
    lf = tmp_path / "lf.txt"
    crlf = tmp_path / "crlf.txt"
    lf.write_bytes(b"a\nb\n")
    crlf.write_bytes(b"a\r\nb\r\n")
    assert sha256_normalized(lf) == sha256_normalized(crlf)


def test_yaml_dump_is_deterministic(tmp_path: Path):
    path = tmp_path / "x.yaml"
    dump_yaml(path, {"b": 2, "a": 1})
    assert path.read_text(encoding="utf-8") == "a: 1\nb: 2\n"
    assert load_yaml(path) == {"a": 1, "b": 2}


def test_generated_memory_path_rejects_manual_file(repo: Path):
    with pytest.raises(ValueError, match="generated"):
        assert_generated_memory_path(repo, repo / ".tw-memory/README.md")
```

- [ ] **Step 2: Run tests to verify failure**

Run: `cd tools && python -m pytest tests/test_source_index.py -q`

Expected: FAIL because modules are not created.

- [ ] **Step 3: Implement `repo.py`**

```python
from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path


def find_repo_root(start: Path | None = None) -> Path:
    """Find the nearest parent directory that contains .git."""
    current = (start or Path.cwd()).resolve()
    for candidate in (current, *current.parents):
        if (candidate / ".git").exists():
            return candidate
    raise FileNotFoundError("repository root with .git not found")


@dataclass(frozen=True)
class RepoPaths:
    root: Path

    @property
    def tw_memory(self) -> Path:
        return self.root / ".tw-memory"

    @property
    def manifest(self) -> Path:
        return self.tw_memory / "manifest"

    @property
    def routes(self) -> Path:
        return self.tw_memory / "routes"

    @property
    def cards(self) -> Path:
        return self.tw_memory / "cards"

    @property
    def source_index(self) -> Path:
        return self.manifest / "source-index.generated.json"

    @property
    def taxonomy(self) -> Path:
        return self.manifest / "taxonomy.generated.yaml"

    @property
    def standards_route(self) -> Path:
        return self.routes / "standards.generated.yaml"

    @property
    def skills_route(self) -> Path:
        return self.routes / "skills.generated.yaml"

    @property
    def codegraph_queries_route(self) -> Path:
        return self.routes / "codegraph-queries.generated.yaml"

    @property
    def packages_route(self) -> Path:
        return self.routes / "packages.generated.yaml"

    @property
    def services_route(self) -> Path:
        return self.routes / "services.generated.yaml"

    @property
    def apis_route(self) -> Path:
        return self.routes / "apis.generated.yaml"

    @property
    def frontend_route(self) -> Path:
        return self.routes / "frontend.generated.yaml"

    @property
    def package_cards(self) -> Path:
        return self.cards / "packages"

    @property
    def public_api_cards(self) -> Path:
        return self.cards / "public-apis"

    @property
    def dotnet_packages_root(self) -> Path:
        return self.root / "backend/dotnet/BuildingBlocks/src"

    @property
    def frontend_packages_root(self) -> Path:
        return self.root / "frontend/packages"
```

- [ ] **Step 4: Implement `hashing.py`**

```python
from __future__ import annotations

import hashlib
from pathlib import Path


def sha256_normalized(path: Path) -> str:
    """Return sha256 for bytes with CRLF and CR normalized to LF."""
    raw = path.read_bytes()
    normalized = raw.replace(b"\r\n", b"\n").replace(b"\r", b"\n")
    return hashlib.sha256(normalized).hexdigest()
```

- [ ] **Step 5: Implement `yaml_io.py`**

```python
from __future__ import annotations

from pathlib import Path
from typing import Any

import yaml


def load_yaml(path: Path) -> Any:
    """Load YAML with safe parsing."""
    with path.open("r", encoding="utf-8") as f:
        return yaml.safe_load(f) or {}


def dump_yaml(path: Path, data: Any) -> None:
    """Write deterministic YAML."""
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="\n") as f:
        yaml.safe_dump(data, f, allow_unicode=True, sort_keys=True, default_flow_style=False)
```

- [ ] **Step 6: Implement `generated_io.py`**

```python
from __future__ import annotations

from pathlib import Path


def repo_relative(root: Path, path: Path) -> str:
    """Return a repository-relative path with slash separators."""
    return path.resolve().relative_to(root.resolve()).as_posix()


def assert_generated_memory_path(root: Path, path: Path) -> None:
    """Ensure .tw-memory writes are generated files, not manual memory files."""
    rel = repo_relative(root, path)
    if not rel.startswith(".tw-memory/"):
        raise ValueError(f"{rel} is outside .tw-memory")
    if "/runtime/" in f"/{rel}/":
        raise ValueError(f"{rel} is runtime state, not generated commit memory")
    if not (rel.endswith(".generated.yaml") or rel.endswith(".generated.json") or rel.endswith(".generated.md")):
        raise ValueError(f"{rel} is not a generated memory file")


def write_generated_text(root: Path, path: Path, content: str) -> None:
    """Write generated text with LF newlines after checking the path boundary."""
    assert_generated_memory_path(root, path)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content.replace("\r\n", "\n").replace("\r", "\n"), encoding="utf-8", newline="\n")
```

- [ ] **Step 7: Run tests**

Run: `cd tools && python -m pytest tests/test_source_index.py -q`

Expected: PASS.

- [ ] **Step 8: Commit**

```bash
git add tools/src/tw_memory/repo.py tools/src/tw_memory/hashing.py tools/src/tw_memory/yaml_io.py tools/src/tw_memory/generated_io.py tools/tests/test_source_index.py
git commit -m "feat(tw-memory): add repo paths and deterministic IO"
```

---

## Task 2: Markdown Standards Segmentation and `.rules` Boundary Scanner

**Files:**
- Create: `tools/src/tw_memory/markdown_segments.py`
- Create: `tools/src/tw_memory/rules_boundary.py`
- Test: `tools/tests/test_markdown_segments.py`
- Test: `tools/tests/test_rules_boundary.py`

- [ ] **Step 1: Write tests for Markdown section splitting**

Create `tools/tests/test_markdown_segments.py`:

```python
from pathlib import Path

from tw_memory.markdown_segments import segment_markdown


def test_segment_markdown_keeps_heading_ranges(tmp_path: Path):
    doc = tmp_path / "standard.md"
    doc.write_text("# 标准\n\n## 目标\n\nA\n\n## 检查清单\n\n- X\n", encoding="utf-8")
    segments = segment_markdown(tmp_path, doc)
    assert [s["title"] for s in segments] == ["标准", "目标", "检查清单"]
    assert segments[0]["start_line"] == 1
    assert segments[1]["path"] == "standard.md"
    assert segments[2]["segment_id"] == "standard.md#检查清单"
```

- [ ] **Step 2: Write tests for `.rules` formal-standard extraction and summary detection**

Create `tools/tests/test_rules_boundary.py`:

```python
from pathlib import Path

from conftest import write_text
from tw_memory.rules_boundary import find_formal_standard_refs, find_rules_boundary_violations


def test_find_formal_standard_refs(repo: Path):
    rule = write_text(
        repo / ".rules/ai-coding-rules/tasks/testing.md",
        "# Testing\n\n## Required Formal Standards\n\n- `docs/engineering-standards/04-quality/testing-standards.md`\n",
    )
    refs = find_formal_standard_refs(repo, [rule])
    assert refs == ["docs/engineering-standards/04-quality/testing-standards.md"]


def test_rules_boundary_flags_engineering_requirement_summary(repo: Path):
    rule = write_text(
        repo / ".rules/ai-coding-rules/tasks/security.md",
        "# Security\n\n## Execution Requirements\n\n- Never rely on front-end checks as the only authorization or validation control.\n",
    )
    hits = find_rules_boundary_violations(repo, [rule])
    assert hits == [".rules/ai-coding-rules/tasks/security.md:4: rule-summary"]


def test_rules_boundary_allows_loading_flow(repo: Path):
    rule = write_text(
        repo / ".rules/ai-coding-rules/tasks/security.md",
        "# Security\n\n## Execution Requirements\n\n- Read the formal security standard before changing security boundaries.\n- For API changes, also load `tasks/api-design.md`.\n",
    )
    assert find_rules_boundary_violations(repo, [rule]) == []
```

- [ ] **Step 3: Run tests to verify failure**

Run: `cd tools && python -m pytest tests/test_markdown_segments.py tests/test_rules_boundary.py -q`

Expected: FAIL because modules are not created.

- [ ] **Step 4: Implement `markdown_segments.py`**

```python
from __future__ import annotations

import re
from pathlib import Path

from tw_memory.hashing import sha256_normalized

_HEADING = re.compile(r"^(#{1,6})\s+(.+?)\s*$")


def _slug(title: str) -> str:
    return re.sub(r"\s+", "-", title.strip())


def segment_markdown(root: Path, path: Path) -> list[dict[str, object]]:
    """Split a Markdown file into deterministic heading-based segments."""
    text = path.read_text(encoding="utf-8")
    lines = text.splitlines()
    headings: list[tuple[int, int, str]] = []
    for index, line in enumerate(lines, start=1):
        match = _HEADING.match(line)
        if match:
            headings.append((index, len(match.group(1)), match.group(2)))

    if not headings:
        return [{
            "segment_id": f"{path.relative_to(root).as_posix()}#document",
            "path": path.relative_to(root).as_posix(),
            "title": "document",
            "level": 0,
            "start_line": 1,
            "end_line": len(lines),
            "sha256": sha256_normalized(path),
        }]

    result: list[dict[str, object]] = []
    rel = path.relative_to(root).as_posix()
    for pos, (start, level, title) in enumerate(headings):
        end = headings[pos + 1][0] - 1 if pos + 1 < len(headings) else len(lines)
        segment_text = "\n".join(lines[start - 1:end]) + "\n"
        result.append({
            "segment_id": f"{rel}#{_slug(title)}",
            "path": rel,
            "title": title,
            "level": level,
            "start_line": start,
            "end_line": end,
            "sha256": __import__("hashlib").sha256(segment_text.encode("utf-8")).hexdigest(),
        })
    return result
```

- [ ] **Step 5: Implement `rules_boundary.py`**

```python
from __future__ import annotations

import re
from pathlib import Path

from tw_memory.generated_io import repo_relative

_FORMAL_REF = re.compile(r"`(docs/engineering-standards/[^`]+\.md)`")
_ALLOWED_EXECUTION_PREFIXES = (
    "Read the formal ",
    "Load ",
    "Apply the formal ",
    "For ",
    "When ",
    "Do not infer rules ",
    "Treat `docs/engineering-standards` ",
    "Combine this baseline ",
    "If a task conflicts ",
    "When `.tw-memory/",
    "Do not load both ",
)


def find_rule_files(root: Path) -> list[Path]:
    """Return all Markdown rule index files."""
    rules = root / ".rules"
    if not rules.exists():
        return []
    return sorted(rules.rglob("*.md"))


def find_formal_standard_refs(root: Path, rule_files: list[Path] | None = None) -> list[str]:
    """Extract formal standard references from rule indexes."""
    refs: set[str] = set()
    for path in rule_files or find_rule_files(root):
        text = path.read_text(encoding="utf-8")
        refs.update(_FORMAL_REF.findall(text))
    return sorted(refs)


def find_rules_boundary_violations(root: Path, rule_files: list[Path] | None = None) -> list[str]:
    """Find .rules lines that look like copied engineering-rule summaries."""
    hits: list[str] = []
    for path in rule_files or find_rule_files(root):
        in_execution = False
        for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
            stripped = line.strip()
            if stripped == "## Execution Requirements":
                in_execution = True
                continue
            if stripped.startswith("## ") and stripped != "## Execution Requirements":
                in_execution = False
            if not in_execution or not stripped.startswith("- "):
                continue
            item = stripped[2:]
            if item.startswith(_ALLOWED_EXECUTION_PREFIXES):
                continue
            hits.append(f"{repo_relative(root, path)}:{number}: rule-summary")
    return hits
```

- [ ] **Step 6: Run tests**

Run: `cd tools && python -m pytest tests/test_markdown_segments.py tests/test_rules_boundary.py -q`

Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add tools/src/tw_memory/markdown_segments.py tools/src/tw_memory/rules_boundary.py tools/tests/test_markdown_segments.py tools/tests/test_rules_boundary.py
git commit -m "feat(tw-memory): segment standards and validate rules boundary"
```

---

## Task 3: Charter Model and Package Discovery

**Files:**
- Create: `tools/src/tw_memory/charter.py`
- Create: `tools/src/tw_memory/packages.py`
- Test: `tools/tests/test_charter.py`
- Test: `tools/tests/test_packages.py`

- [ ] **Step 1: Write charter tests**

Create `tools/tests/test_charter.py`:

```python
from pathlib import Path

from conftest import write_text
from tw_memory.charter import load_charter, validate_charter

VALID = """\
schema_version: "1.0.0"
package: Tw.Core
owner: platform-team
responsibility: 跨服务复用的基础原语与无框架依赖工具。
in_scope:
  - 基础值对象
out_of_scope:
  - HTTP 中间件
public_capabilities:
  - Tw.Core.Primitives
dependency_rules:
  forbid:
    - "Microsoft.AspNetCore.*"
  allow: []
"""


def test_load_valid_charter(tmp_path: Path):
    path = write_text(tmp_path / "package-charter.yaml", VALID)
    charter = load_charter(path)
    assert charter.package == "Tw.Core"
    assert charter.public_capabilities == ["Tw.Core.Primitives"]


def test_validate_rejects_empty_out_of_scope(tmp_path: Path):
    path = write_text(tmp_path / "package-charter.yaml", VALID.replace("out_of_scope:\n  - HTTP 中间件", "out_of_scope: []"))
    errors = validate_charter(load_charter(path))
    assert any("out_of_scope" in e for e in errors)


def test_validate_rejects_future_promise_text(tmp_path: Path):
    path = write_text(tmp_path / "package-charter.yaml", VALID.replace("跨服务复用的基础原语与无框架依赖工具。", "职责" + "\u5f85\u5b9a"))
    errors = validate_charter(load_charter(path))
    assert any("placeholder" in e for e in errors)
```

- [ ] **Step 2: Write package discovery tests**

Create `tools/tests/test_packages.py`:

```python
from pathlib import Path

from conftest import make_csproj
from tw_memory.packages import discover_packages


def test_discover_dotnet_package_uses_csproj_file_name(repo: Path):
    package_dir = make_csproj(repo, "Tw.Core", '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><RootNamespace>Tw</RootNamespace></PropertyGroup></Project>')
    packages = discover_packages(repo)
    assert packages[0].canonical_key == "Tw.Core"
    assert packages[0].root_dir == package_dir
    assert packages[0].charter_path == package_dir / "package-charter.yaml"


def test_discover_package_references(repo: Path):
    make_csproj(repo, "Tw.AspNetCore", '<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="1.0.0" /></ItemGroup></Project>')
    package = discover_packages(repo)[0]
    assert package.dependencies == ["Microsoft.AspNetCore.OpenApi"]
```

- [ ] **Step 3: Run tests to verify failure**

Run: `cd tools && python -m pytest tests/test_charter.py tests/test_packages.py -q`

Expected: FAIL because modules are not created.

- [ ] **Step 4: Implement `charter.py`**

```python
from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
from typing import Any

from tw_memory.yaml_io import load_yaml

PLACEHOLDER_TERMS = (
    "\u540e\u7eed",
    "\u5f85\u5b9a",
    "\u6682\u5b9a",
    "\u89c6\u60c5\u51b5",
    "\u53ef\u80fd",
    "\u5927\u6982",
    "\u5982\u6709\u9700\u8981",
    "\u6309\u9700\u8865\u5145",
    "\u5f85\u8865\u5145",
    "TO" + "DO",
    "T" + "BD",
)
REQUIRED_FIELDS = ("schema_version", "package", "owner", "responsibility", "in_scope", "out_of_scope", "public_capabilities", "dependency_rules")


@dataclass(frozen=True)
class DependencyRules:
    forbid: list[str]
    allow: list[str]


@dataclass(frozen=True)
class Charter:
    path: Path
    schema_version: str
    package: str
    owner: str
    responsibility: str
    in_scope: list[str]
    out_of_scope: list[str]
    public_capabilities: list[str]
    dependency_rules: DependencyRules
    stability: str
    compatibility: str | None
    migration_ref: str | None
    raw: dict[str, Any]


def _list(value: Any) -> list[str]:
    return [str(x) for x in value] if isinstance(value, list) else []


def load_charter(path: Path) -> Charter:
    data = load_yaml(path)
    rules = data.get("dependency_rules") or {}
    return Charter(
        path=path,
        schema_version=str(data.get("schema_version", "")),
        package=str(data.get("package", "")),
        owner=str(data.get("owner", "")),
        responsibility=str(data.get("responsibility", "")).strip(),
        in_scope=_list(data.get("in_scope")),
        out_of_scope=_list(data.get("out_of_scope")),
        public_capabilities=_list(data.get("public_capabilities")),
        dependency_rules=DependencyRules(forbid=_list(rules.get("forbid")), allow=_list(rules.get("allow"))),
        stability=str(data.get("stability", "stable")),
        compatibility=data.get("compatibility"),
        migration_ref=data.get("migration_ref"),
        raw=data,
    )


def validate_charter(charter: Charter) -> list[str]:
    errors: list[str] = []
    for field in REQUIRED_FIELDS:
        if not charter.raw.get(field):
            errors.append(f"{charter.path}: missing {field}")
    for field in ("in_scope", "out_of_scope", "public_capabilities"):
        if not getattr(charter, field):
            errors.append(f"{charter.path}: {field} must be non-empty")
    if charter.stability not in {"experimental", "stable", "deprecated"}:
        errors.append(f"{charter.path}: invalid stability {charter.stability!r}")
    text = "\n".join([charter.responsibility, *charter.in_scope, *charter.out_of_scope])
    lowered = text.lower()
    for term in PLACEHOLDER_TERMS:
        if (term.isascii() and term.lower() in lowered) or (not term.isascii() and term in text):
            errors.append(f"{charter.path}: placeholder term {term}")
    return errors
```

- [ ] **Step 5: Implement `packages.py`**

```python
from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
from xml.etree import ElementTree


@dataclass(frozen=True)
class DiscoveredPackage:
    canonical_key: str
    ecosystem: str
    root_dir: Path
    project_file: Path
    charter_path: Path
    dependencies: list[str]


def _parse_package_refs(csproj: Path) -> list[str]:
    root = ElementTree.fromstring(csproj.read_text(encoding="utf-8"))
    refs: list[str] = []
    for item in root.iter():
        tag = item.tag.rsplit("}", 1)[-1]
        if tag in {"PackageReference", "ProjectReference"}:
            include = item.attrib.get("Include")
            if include:
                refs.append(Path(include).stem if tag == "ProjectReference" else include)
    return sorted(refs)


def discover_packages(root: Path) -> list[DiscoveredPackage]:
    packages: list[DiscoveredPackage] = []
    dotnet_root = root / "backend/dotnet/BuildingBlocks/src"
    if dotnet_root.exists():
        for csproj in sorted(dotnet_root.glob("*/*.csproj")):
            canonical_key = csproj.stem
            packages.append(DiscoveredPackage(
                canonical_key=canonical_key,
                ecosystem="dotnet",
                root_dir=csproj.parent,
                project_file=csproj,
                charter_path=csproj.parent / "package-charter.yaml",
                dependencies=_parse_package_refs(csproj),
            ))
    frontend_root = root / "frontend/packages"
    if frontend_root.exists():
        for package_json in sorted(frontend_root.glob("*/package.json")):
            import json

            data = json.loads(package_json.read_text(encoding="utf-8"))
            deps = sorted({*(data.get("dependencies") or {}), *(data.get("peerDependencies") or {})})
            packages.append(DiscoveredPackage(
                canonical_key=str(data["name"]),
                ecosystem="frontend",
                root_dir=package_json.parent,
                project_file=package_json,
                charter_path=package_json.parent / "package-charter.yaml",
                dependencies=deps,
            ))
    return sorted(packages, key=lambda p: p.canonical_key)
```

- [ ] **Step 6: Run tests**

Run: `cd tools && python -m pytest tests/test_charter.py tests/test_packages.py -q`

Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add tools/src/tw_memory/charter.py tools/src/tw_memory/packages.py tools/tests/test_charter.py tools/tests/test_packages.py
git commit -m "feat(tw-memory): load package charters and discover packages"
```

---

## Task 4: Source Index, Routes, and Card Rendering

**Files:**
- Create: `tools/src/tw_memory/source_index.py`
- Create: `tools/src/tw_memory/routes.py`
- Create: `tools/src/tw_memory/codegraph_routes.py`
- Create: `tools/src/tw_memory/cards.py`
- Test: `tools/tests/test_cards.py`

- [ ] **Step 1: Write card and route tests**

Create `tools/tests/test_cards.py`:

```python
from pathlib import Path

from conftest import write_text
from tw_memory.cards import render_package_card, render_public_api_card
from tw_memory.charter import load_charter


CHARTER = """\
schema_version: "1.0.0"
package: Tw.Core
owner: platform-team
responsibility: 跨服务复用的基础原语与无框架依赖工具。
in_scope:
  - 基础值对象
out_of_scope:
  - HTTP 中间件
public_capabilities:
  - Tw.Core.Primitives
dependency_rules:
  forbid:
    - "Microsoft.AspNetCore.*"
  allow: []
"""


def test_render_package_card_contains_fixed_slots(tmp_path: Path):
    path = write_text(tmp_path / "package-charter.yaml", CHARTER)
    charter = load_charter(path)
    card = render_package_card("Tw.Core", "backend/dotnet/BuildingBlocks/src/Tw.Core", charter, ["manual:package-charter:Tw.Core"])
    assert "标识：Tw.Core / backend/dotnet/BuildingBlocks/src/Tw.Core / platform-team" in card
    assert "职责：跨服务复用的基础原语与无框架依赖工具。" in card
    assert "不适用范围：" in card
    assert "source_refs:" in card


def test_render_public_api_card_contains_capabilities(tmp_path: Path):
    path = write_text(tmp_path / "package-charter.yaml", CHARTER)
    charter = load_charter(path)
    card = render_public_api_card("Tw.Core", "backend/dotnet/BuildingBlocks/src/Tw.Core", charter, ["manual:package-charter:Tw.Core"])
    assert "- Tw.Core.Primitives" in card
```

- [ ] **Step 2: Run tests to verify failure**

Run: `cd tools && python -m pytest tests/test_cards.py -q`

Expected: FAIL because modules are not created.

- [ ] **Step 3: Implement `source_index.py`**

```python
from __future__ import annotations

import json
from pathlib import Path

from tw_memory.generated_io import repo_relative
from tw_memory.hashing import sha256_normalized


def source_id(source_type: str, key: str) -> str:
    return f"{source_type}:{key}"


def make_source_entry(root: Path, path: Path, source_type: str, source_key: str, extractor: str) -> dict[str, str]:
    return {
        "source_id": source_id(source_type, source_key),
        "source_type": source_type,
        "path": repo_relative(root, path),
        "hash_algorithm": "sha256",
        "sha256": sha256_normalized(path),
        "extractor": extractor,
    }


def write_source_index(path: Path, entries: list[dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    data = {"schema_version": "1.0.0", "sources": {e["source_id"]: e for e in sorted(entries, key=lambda x: x["source_id"])}}
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def load_source_index(path: Path) -> dict[str, object]:
    return json.loads(path.read_text(encoding="utf-8"))
```

- [ ] **Step 4: Implement `routes.py`**

```python
from __future__ import annotations

from tw_memory.yaml_io import dump_yaml


def write_route(path, schema_version: str, entries: dict) -> None:
    dump_yaml(path, {"schema_version": schema_version, **entries})
```

- [ ] **Step 5: Implement `codegraph_routes.py`**

```python
from __future__ import annotations


def default_codegraph_queries() -> dict[str, object]:
    """Return non-blocking CodeGraph query intents."""
    return {
        "queries": {
            "find_symbol": {"requires": ["symbol"], "verify_with_source": True},
            "callers": {"requires": ["symbol"], "verify_with_source": True},
            "callees": {"requires": ["symbol"], "verify_with_source": True},
            "impact": {"requires": ["path"], "verify_with_source": True},
            "route_handlers": {"requires": ["api"], "verify_with_source": True},
        }
    }
```

- [ ] **Step 6: Implement `cards.py`**

```python
from __future__ import annotations

from tw_memory.charter import Charter


def _bullets(items: list[str]) -> str:
    return "\n".join(f"- {item}" for item in items)


def render_package_card(package: str, path: str, charter: Charter, source_refs: list[str]) -> str:
    """Render a generated package card."""
    compatibility = charter.compatibility or ""
    migration = charter.migration_ref or ""
    return f"""# Package: {package}

标识：{package} / {path} / {charter.owner}
职责：{charter.responsibility}

适用范围：
{_bullets(charter.in_scope)}

不适用范围：
{_bullets(charter.out_of_scope)}

依赖边界：
- forbid: {", ".join(charter.dependency_rules.forbid)}
- allow: {", ".join(charter.dependency_rules.allow)}

稳定性：{charter.stability}
兼容性：{compatibility}
迁移指针：{migration}

source_refs:
{_bullets(source_refs)}
"""


def render_public_api_card(package: str, path: str, charter: Charter, source_refs: list[str]) -> str:
    """Render a generated public API card."""
    return f"""# Public API: {package}

标识：{package} / {path}

公开能力：
{_bullets(charter.public_capabilities)}

契约关联：
- none

消费提示：
- 公开能力来自 package-charter.yaml

source_refs:
{_bullets(source_refs)}
"""
```

- [ ] **Step 7: Run tests**

Run: `cd tools && python -m pytest tests/test_cards.py -q`

Expected: PASS.

- [ ] **Step 8: Commit**

```bash
git add tools/src/tw_memory/source_index.py tools/src/tw_memory/routes.py tools/src/tw_memory/codegraph_routes.py tools/src/tw_memory/cards.py tools/tests/test_cards.py
git commit -m "feat(tw-memory): render generated routes and cards"
```

---

## Task 5: Generate Pipeline

**Files:**
- Create: `tools/src/tw_memory/discovery.py`
- Create: `tools/src/tw_memory/generate.py`
- Test: `tools/tests/test_generate.py`

- [ ] **Step 1: Write end-to-end generate test**

Create `tools/tests/test_generate.py`:

```python
from pathlib import Path

from conftest import make_csproj, write_text
from tw_memory.generate import run_generate


def test_generate_writes_only_generated_memory(repo: Path):
    write_text(repo / ".rules/ai-coding-rules/00-always-load.md", "# Always\n\n## Required Formal Standards\n\n- `docs/engineering-standards/03-project-and-code/coding-standards.md`\n")
    write_text(repo / "docs/engineering-standards/03-project-and-code/coding-standards.md", "# 通用编码规范\n\n## 目标\n\n清晰。\n")
    pkg = make_csproj(repo, "Tw.Core")
    write_text(pkg / "package-charter.yaml", """\
schema_version: "1.0.0"
package: Tw.Core
owner: platform-team
responsibility: 跨服务复用的基础原语与无框架依赖工具。
in_scope:
  - 基础值对象
out_of_scope:
  - HTTP 中间件
public_capabilities:
  - Tw.Core.Primitives
dependency_rules:
  forbid: []
  allow: []
""")
    assert run_generate(str(repo)) == 0
    assert (repo / ".tw-memory/manifest/source-index.generated.json").exists()
    assert (repo / ".tw-memory/routes/standards.generated.yaml").exists()
    assert (repo / ".tw-memory/cards/packages/Tw.Core.generated.md").exists()
    assert not (repo / ".tw-memory/runtime").exists()
```

- [ ] **Step 2: Run test to verify failure**

Run: `cd tools && python -m pytest tests/test_generate.py -q`

Expected: FAIL because `generate.py` is not implemented.

- [ ] **Step 3: Implement `discovery.py`**

```python
from __future__ import annotations

from pathlib import Path


def existing_files(paths: list[Path]) -> list[Path]:
    return [p for p in paths if p.exists()]


def discover_contract_files(root: Path) -> list[Path]:
    contracts = root / "contracts"
    if not contracts.exists():
        return []
    return sorted(p for p in contracts.rglob("*") if p.is_file())


def discover_skill_files(root: Path) -> list[Path]:
    skills = root / ".agents/skills"
    if not skills.exists():
        return []
    return sorted(skills.rglob("SKILL.md"))
```

- [ ] **Step 4: Implement `generate.py`**

```python
from __future__ import annotations

from pathlib import Path

from tw_memory import GENERATOR_VERSION
from tw_memory.cards import render_package_card, render_public_api_card
from tw_memory.charter import load_charter
from tw_memory.codegraph_routes import default_codegraph_queries
from tw_memory.generated_io import repo_relative, write_generated_text
from tw_memory.markdown_segments import segment_markdown
from tw_memory.packages import discover_packages
from tw_memory.repo import RepoPaths, find_repo_root
from tw_memory.rules_boundary import find_formal_standard_refs, find_rule_files
from tw_memory.routes import write_route
from tw_memory.source_index import make_source_entry, write_source_index
from tw_memory.yaml_io import dump_yaml


def run_generate(root: str | None = None) -> int:
    repo = Path(root).resolve() if root else find_repo_root()
    paths = RepoPaths(repo)
    entries: list[dict[str, str]] = []

    rule_files = find_rule_files(repo)
    standard_refs = find_formal_standard_refs(repo, rule_files)
    standards: list[dict[str, object]] = []
    for rel in standard_refs:
        path = repo / rel
        if not path.exists():
            continue
        entries.append(make_source_entry(repo, path, "standard", rel.replace("/", ":"), "engineering-standard-segment:v1"))
        standards.extend(segment_markdown(repo, path))
    write_route(paths.standards_route, "1.0.0", {"standards": standards})

    packages_route: dict[str, object] = {}
    for package in discover_packages(repo):
        if not package.charter_path.exists():
            continue
        charter = load_charter(package.charter_path)
        rel = repo_relative(repo, package.root_dir)
        source_key = f"package-charter:{package.canonical_key}"
        source_ref = f"charter:{source_key}"
        entries.append(make_source_entry(repo, package.charter_path, "charter", source_key, "package-charter:v1"))
        packages_route[package.canonical_key] = {
            "path": rel,
            "card": f".tw-memory/cards/packages/{package.canonical_key}.generated.md",
            "public_api_card": f".tw-memory/cards/public-apis/{package.canonical_key}.generated.md",
            "source_refs": [source_ref],
        }
        write_generated_text(repo, paths.package_cards / f"{package.canonical_key}.generated.md", render_package_card(package.canonical_key, rel, charter, [source_ref]))
        write_generated_text(repo, paths.public_api_cards / f"{package.canonical_key}.generated.md", render_public_api_card(package.canonical_key, rel, charter, [source_ref]))

    write_route(paths.packages_route, "1.0.0", {"packages": packages_route})
    write_route(paths.skills_route, "1.0.0", {"skills": {}})
    write_route(paths.services_route, "1.0.0", {"services": {}})
    write_route(paths.apis_route, "1.0.0", {"apis": {}})
    write_route(paths.frontend_route, "1.0.0", {"frontend": {}})
    write_route(paths.codegraph_queries_route, "1.0.0", default_codegraph_queries())
    dump_yaml(paths.taxonomy, {
        "schema_version": "1.0.0",
        "generator": GENERATOR_VERSION,
        "source_types": ["standard", "charter", "contract", "structure", "skill"],
        "memory_types": ["package-summary", "public-api-summary", "service-summary", "api-summary", "frontend-summary"],
        "lookup_keys": ["package", "service", "api", "frontend-app", "symbol"],
    })
    write_source_index(paths.source_index, entries)
    print(f"[generate] wrote .tw-memory for {len(packages_route)} packages and {len(standards)} standard segments")
    return 0
```

- [ ] **Step 5: Run generate test**

Run: `cd tools && python -m pytest tests/test_generate.py -q`

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add tools/src/tw_memory/discovery.py tools/src/tw_memory/generate.py tools/tests/test_generate.py
git commit -m "feat(tw-memory): generate routes source index and package cards"
```

---

## Task 6: Check Gates

**Files:**
- Create: `tools/src/tw_memory/secret_scan.py`
- Create: `tools/src/tw_memory/check.py`
- Test: `tools/tests/test_secret_scan.py`
- Test: `tools/tests/test_check.py`

- [ ] **Step 1: Write secret scanner tests**

Create `tools/tests/test_secret_scan.py`:

```python
from tw_memory.secret_scan import scan_secrets


def test_detects_password_assignment():
    assert scan_secrets("Password=hunter2;")[0].kind == "connection-string"


def test_detects_bearer_token():
    assert scan_secrets("Authorization: Bearer abcdef0123456789abcdef0123456789")[0].kind == "bearer-token"


def test_clean_text_has_no_hits():
    assert scan_secrets("Tw.Core public API card") == []
```

- [ ] **Step 2: Write check tests**

Create `tools/tests/test_check.py`:

```python
from pathlib import Path

from conftest import make_csproj, write_text
from tw_memory.check import run_check
from tw_memory.generate import run_generate

CHARTER = """\
schema_version: "1.0.0"
package: Tw.Core
owner: platform-team
responsibility: 跨服务复用的基础原语与无框架依赖工具。
in_scope:
  - 基础值对象
out_of_scope:
  - HTTP 中间件
public_capabilities:
  - Tw.Core.Primitives
dependency_rules:
  forbid:
    - "Microsoft.AspNetCore.*"
  allow: []
"""


def seed(repo: Path) -> None:
    write_text(repo / ".rules/ai-coding-rules/00-always-load.md", "# Always\n\n## Required Formal Standards\n\n- `docs/engineering-standards/03-project-and-code/coding-standards.md`\n\n## Execution Requirements\n\n- Read the formal coding standard before changing source.\n")
    write_text(repo / "docs/engineering-standards/03-project-and-code/coding-standards.md", "# 通用编码规范\n\n## 目标\n\n清晰。\n")
    pkg = make_csproj(repo, "Tw.Core")
    write_text(pkg / "package-charter.yaml", CHARTER)


def test_check_passes_after_generate(repo: Path):
    seed(repo)
    assert run_generate(str(repo)) == 0
    assert run_check(str(repo)) == 0


def test_check_fails_without_charter(repo: Path):
    make_csproj(repo, "Tw.Core")
    assert run_check(str(repo)) == 1


def test_check_fails_when_rules_contain_summary(repo: Path):
    seed(repo)
    write_text(repo / ".rules/ai-coding-rules/tasks/security.md", "# Security\n\n## Execution Requirements\n\n- Never rely on front-end checks as the only authorization control.\n")
    assert run_generate(str(repo)) == 0
    assert run_check(str(repo)) == 1
```

- [ ] **Step 3: Run tests to verify failure**

Run: `cd tools && python -m pytest tests/test_secret_scan.py tests/test_check.py -q`

Expected: FAIL because check and scanner are not implemented.

- [ ] **Step 4: Implement `secret_scan.py`**

```python
from __future__ import annotations

import re
from dataclasses import dataclass

_PATTERNS = (
    ("connection-string", re.compile(r"(?i)(password|pwd)\s*=\s*[^;\s]+")),
    ("bearer-token", re.compile(r"(?i)bearer\s+[A-Za-z0-9._\-]{20,}")),
    ("private-key", re.compile(r"-----BEGIN [A-Z ]*PRIVATE KEY-----")),
)


@dataclass(frozen=True)
class SecretHit:
    kind: str
    match: str


def scan_secrets(text: str) -> list[SecretHit]:
    hits: list[SecretHit] = []
    for kind, pattern in _PATTERNS:
        hits.extend(SecretHit(kind, match.group(0)) for match in pattern.finditer(text))
    return hits
```

- [ ] **Step 5: Implement `check.py`**

```python
from __future__ import annotations

import fnmatch
import subprocess
import sys
from pathlib import Path

from tw_memory.charter import load_charter, validate_charter
from tw_memory.hashing import sha256_normalized
from tw_memory.packages import discover_packages
from tw_memory.repo import RepoPaths, find_repo_root
from tw_memory.rules_boundary import find_rule_files, find_rules_boundary_violations
from tw_memory.secret_scan import scan_secrets
from tw_memory.source_index import load_source_index

_FORBIDDEN_TRACKED = (".codegraph", ".tw-memory/runtime")


def _tracked(root: Path, staged: bool) -> list[str]:
    args = ["git", "diff", "--cached", "--name-only"] if staged else ["git", "ls-files"]
    try:
        result = subprocess.run(args, cwd=root, text=True, capture_output=True, check=True)
    except (FileNotFoundError, subprocess.CalledProcessError):
        return []
    return [line.strip().replace("\\", "/") for line in result.stdout.splitlines() if line.strip()]


def _overlaps(a: list[str], b: list[str]) -> list[str]:
    result: list[str] = []
    for x in a:
        for y in b:
            if x == y or x.startswith(y + ".") or y.startswith(x + "."):
                result.append(f"{x} <-> {y}")
    return result


def run_check(root: str | None = None, *, staged: bool = False) -> int:
    repo = Path(root).resolve() if root else find_repo_root()
    paths = RepoPaths(repo)
    errors: list[str] = []

    errors.extend(find_rules_boundary_violations(repo, find_rule_files(repo)))

    charters: dict[str, object] = {}
    packages = discover_packages(repo)
    for package in packages:
        if not package.charter_path.exists():
            errors.append(f"{package.canonical_key}: missing package-charter.yaml")
            continue
        charter = load_charter(package.charter_path)
        charters[package.canonical_key] = charter
        errors.extend(validate_charter(charter))
        if charter.package != package.canonical_key:
            errors.append(f"{package.charter_path}: package must be {package.canonical_key}")
        for dep in package.dependencies:
            if any(fnmatch.fnmatch(dep, pat) for pat in charter.dependency_rules.forbid):
                errors.append(f"{package.canonical_key}: dependency {dep} violates forbid rule")
            if charter.dependency_rules.allow and not any(fnmatch.fnmatch(dep, pat) for pat in charter.dependency_rules.allow):
                errors.append(f"{package.canonical_key}: dependency {dep} is outside allow list")
        for hit in scan_secrets(package.charter_path.read_text(encoding="utf-8")):
            errors.append(f"{package.charter_path}: secret hit {hit.kind}")

    keys = sorted(charters)
    for index, left in enumerate(keys):
        for right in keys[index + 1:]:
            for pair in _overlaps(charters[left].public_capabilities, charters[right].public_capabilities):
                errors.append(f"public capabilities overlap: {left} {right} {pair}")

    if paths.source_index.exists():
        index = load_source_index(paths.source_index)
        sources = index.get("sources", {})
        for source in sources.values():
            source_path = repo / source["path"]
            if not source_path.exists():
                errors.append(f"source missing: {source['path']}")
            elif source["sha256"] != sha256_normalized(source_path):
                errors.append(f"source hash stale: {source['path']}")
    elif charters:
        errors.append("missing .tw-memory/manifest/source-index.generated.json")

    for rel in _tracked(repo, staged):
        if any(rel == p or rel.startswith(p + "/") for p in _FORBIDDEN_TRACKED):
            errors.append(f"forbidden tracked path: {rel}")

    if errors:
        print("[check] failed", file=sys.stderr)
        for error in errors:
            print(f"  - {error}", file=sys.stderr)
        return 1
    print(f"[check] passed for {len(charters)} package charters")
    return 0
```

- [ ] **Step 6: Run check tests**

Run: `cd tools && python -m pytest tests/test_secret_scan.py tests/test_check.py -q`

Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add tools/src/tw_memory/secret_scan.py tools/src/tw_memory/check.py tools/tests/test_secret_scan.py tools/tests/test_check.py
git commit -m "feat(tw-memory): validate generated memory and charter gates"
```

---

## Task 7: `.rules` Boundary Cleanup

**Files:**
- Modify: `.rules/ai-coding-rules/languages/*.md`
- Modify: `.rules/ai-coding-rules/tasks/*.md`

- [ ] **Step 1: Rewrite language indexes to contain only trigger, formal paths, and loading flow**

For each file under `.rules/ai-coding-rules/languages`, keep:

```markdown
# <Language Name>

## When To Load

Load this index when a task touches <language-specific trigger list>.

## Required Formal Standards

- `docs/engineering-standards/03-project-and-code/language-specific/<file>.md`

## Execution Requirements

- Load `00-always-load.md` together with this language index.
- Read the formal language standard before changing source in this technology.
- For API, testing, security, dependency, CI/CD, runtime, or observability changes, also load the matching task index.
```

Keep each file's existing `When To Load` trigger text and formal standard path.

- [ ] **Step 2: Rewrite task indexes to remove copied formal-rule summaries**

For each file under `.rules/ai-coding-rules/tasks`, keep:

```markdown
# <Task Name>

## When To Load

Load this index when a task touches <task trigger list>.

## Required Formal Standards

- `<existing formal standard path>`

## Execution Requirements

- Read the referenced formal standard files before changing this task area.
- If this task overlaps another task category, load the matching task index from `01-task-router.md`.
- Use `.tw-memory/routes/standards.generated.yaml` only to locate formal-standard sections when the generated memory index is current.
```

Preserve existing `Required Formal Standards` paths.

- [ ] **Step 3: Run boundary check**

Run: `cd tools && python -m tw_memory check --root ..`

Expected: no `rule-summary` entries. If package charters are not created yet, expected remaining failures are limited to missing `package-charter.yaml`.

- [ ] **Step 4: Commit**

```bash
git add .rules/ai-coding-rules
git commit -m "refactor(rules): keep indexes as loading routes only"
```

---

## Task 8: Shared Package Charter Standard for Employees

**Files:**
- Create: `docs/engineering-standards/03-project-and-code/shared-package-charter.md`
- Modify: `docs/engineering-standards/03-project-and-code/project-structure.md`
- Modify: `docs/engineering-standards/README.md`

- [ ] **Step 1: Create `shared-package-charter.md`**

Write:

```markdown
# 共享包 charter 规范

## 目标

统一共享包职责与边界的声明形式，使开发、测试、评审和包 owner 能判断能力归属、公开能力、依赖边界和兼容性承诺。

## 适用范围

适用于 `backend/dotnet/BuildingBlocks/src` 下的 .NET 公共构建块，以及 `frontend/packages` 下的前端共享包。

## 规范要求

- 每个共享包根目录必须包含 `package-charter.yaml`。
- charter 必须包含 `schema_version`、`package`、`owner`、`responsibility`、`in_scope`、`out_of_scope`、`public_capabilities`、`dependency_rules`。
- `in_scope`、`out_of_scope`、`public_capabilities` 必须为非空列表。
- `out_of_scope` 必须声明本包不承担的能力边界。
- `.NET` 包的 `package` 必须等于 `.csproj` 文件名去扩展名。
- 前端共享包的 `package` 必须等于 `package.json` 的 `name`。
- `dependency_rules.forbid` 声明禁止依赖；`dependency_rules.allow` 非空时声明允许依赖。
- `stability` 取值为 `experimental`、`stable`、`deprecated`，缺省为 `stable`。
- `compatibility` 用短文本声明兼容性承诺。
- `migration_ref` 指向仓库内 CHANGELOG、迁移说明或契约版本。

## 新增包流程

- 新能力落在已有包 `in_scope` 时进入该包。
- 新能力命中某包 `out_of_scope` 时不得放入该包。
- 建立新包必须同时满足单一职责清晰、存在跨服务或跨应用复用、依赖边界独立、公开能力不与现有包重叠。
- 建立新包必须同时提交 `package-charter.yaml`。

## 重叠处理

- `public_capabilities` 命名空间重叠必须重新划分。
- 职责语义重叠必须在代码评审中裁决，处理结论必须反映到相关包 charter。

## 检查清单

- 共享包是否包含 `package-charter.yaml`？
- `out_of_scope` 是否非空？
- `package` 是否等于 canonical key？
- 实际依赖是否符合 `dependency_rules`？
- `public_capabilities` 是否与其他共享包互斥？
```

- [ ] **Step 2: Add project-structure matrix row**

In `docs/engineering-standards/03-project-and-code/project-structure.md`, add this row after `.NET 后端公共构建块源码`:

```markdown
| 共享包职责与边界声明 | `<package-root>/package-charter.yaml` |
```

- [ ] **Step 3: Update project-structure documentation convention**

Replace:

```markdown
公共组件、SDK、框架库和跨团队工具必须额外说明适用范围、不适用范围、兼容性承诺、升级方式和迁移注意事项。
```

With:

```markdown
公共组件、SDK、框架库和跨团队工具的适用范围、不适用范围、依赖边界、兼容性承诺和迁移指针，必须在该包根目录的 `package-charter.yaml` 中声明。
```

- [ ] **Step 4: Add README navigation**

In `docs/engineering-standards/README.md`, add under 项目与编码规范:

```markdown
- [共享包 charter 规范](03-project-and-code/shared-package-charter.md)
```

- [ ] **Step 5: Run forbidden future-promise scan on formal standards**

Run:

```powershell
$terms = @(
  [char]0x540e + [char]0x7eed,
  [char]0x5f85 + [char]0x5b9a,
  [char]0x6682 + [char]0x5b9a,
  [char]0x89c6 + [char]0x60c5 + [char]0x51b5,
  [char]0x53ef + [char]0x80fd,
  [char]0x5927 + [char]0x6982,
  [char]0x5982 + [char]0x6709 + [char]0x9700 + [char]0x8981,
  [char]0x6309 + [char]0x9700 + [char]0x8865 + [char]0x5145,
  [char]0x5f85 + [char]0x8865 + [char]0x5145,
  'TO' + 'DO',
  'T' + 'BD'
)
$pattern = ($terms | ForEach-Object { [regex]::Escape($_) }) -join '|'
rg -n $pattern docs/engineering-standards/03-project-and-code/shared-package-charter.md docs/engineering-standards/03-project-and-code/project-structure.md docs/engineering-standards/README.md
```

Expected: no matches outside controlled terminology examples.

- [ ] **Step 6: Commit**

```bash
git add docs/engineering-standards/03-project-and-code/shared-package-charter.md docs/engineering-standards/03-project-and-code/project-structure.md docs/engineering-standards/README.md
git commit -m "docs(standards): add shared package charter standard"
```

---

## Task 9: Seed Charters for Tw.Core and Tw.AspNetCore

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/package-charter.yaml`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/package-charter.yaml`

- [ ] **Step 1: Write `Tw.Core/package-charter.yaml`**

```yaml
schema_version: "1.0.0"
package: Tw.Core
owner: platform-team
stability: stable
compatibility: "semver-minor 内向后兼容"
responsibility: >
  跨服务复用的基础原语与无框架依赖工具：值对象、命名对象、领域异常、
  配置异常、类型查找、反射缓存、加密哈希、通用扩展方法与一次性资源工具。
in_scope:
  - 基础值对象与命名对象原语
  - 通用领域异常与配置异常
  - 类型查找、反射缓存与类型扩展
  - 加密、哈希与安全随机
  - 通用集合、字符串、时间、数字等扩展方法
out_of_scope:
  - HTTP、中间件、过滤器、ASP.NET Core 集成
  - 数据访问、ORM、仓储实现
  - 具体业务领域模型
public_capabilities:
  - Tw
  - Tw.Collections
  - Tw.Core.Configuration
  - Tw.Core.Primitives
  - Tw.Core.Reflection
  - Tw.Core.Security.Cryptography
  - Tw.Exceptions
  - Tw.Extensions
  - Tw.Utilities
dependency_rules:
  forbid:
    - "Microsoft.AspNetCore.*"
    - "Microsoft.EntityFrameworkCore*"
  allow: []
```

- [ ] **Step 2: Write `Tw.AspNetCore/package-charter.yaml`**

```yaml
schema_version: "1.0.0"
package: Tw.AspNetCore
owner: platform-team
stability: experimental
compatibility: "experimental 阶段不承诺兼容"
responsibility: >
  ASP.NET Core 宿主集成的公共构建块：中间件、过滤器、模型绑定、
  结果封装、启动扩展与 Web 层横切关注点。
in_scope:
  - ASP.NET Core 中间件与过滤器
  - Web 层模型绑定与结果封装
  - 宿主启动与依赖注入扩展
out_of_scope:
  - 与框架无关的基础原语
  - 数据访问、ORM、仓储实现
  - 具体业务领域模型
public_capabilities:
  - Tw.AspNetCore
dependency_rules:
  forbid:
    - "Microsoft.EntityFrameworkCore*"
  allow: []
```

- [ ] **Step 3: Run charter checks**

Run: `cd tools && python -m tw_memory check --root ..`

Expected: no missing charter errors. Source index may be missing until Task 10 generate step; if so, expected failure is `missing .tw-memory/manifest/source-index.generated.json`.

- [ ] **Step 4: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/package-charter.yaml backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/package-charter.yaml
git commit -m "feat(buildingblocks): add package charters"
```

---

## Task 10: Generate `.tw-memory` Commit-Layer Files

**Files:**
- Create generated files under `.tw-memory/manifest`
- Create generated files under `.tw-memory/routes`
- Create generated files under `.tw-memory/cards`

- [ ] **Step 1: Run generator**

Run:

```powershell
cd tools
python -m tw_memory generate --root ..
```

Expected: command exits `0` and prints a generated summary with package and standard segment counts.

- [ ] **Step 2: Run check**

Run:

```powershell
cd tools
python -m tw_memory check --root ..
```

Expected: command exits `0` and prints `[check] passed`.

- [ ] **Step 3: Verify determinism**

Run:

```powershell
cd tools
python -m tw_memory generate --root ..
cd ..
git diff -- .tw-memory
```

Expected: no diff after the second generation.

- [ ] **Step 4: Verify generated-only memory**

Run:

```powershell
Get-ChildItem -Recurse -File .tw-memory | Where-Object { $_.Name -notmatch '\.generated\.(yaml|json|md)$' }
```

Expected: no output.

- [ ] **Step 5: Commit**

```bash
git add .tw-memory
git commit -m "feat(tw-memory): generate commit-layer memory"
```

---

## Task 11: Pre-Commit and CI Gate

**Files:**
- Create: `.pre-commit-config.yaml`
- Create: `.github/workflows/tw-memory.yml`

- [ ] **Step 1: Write `.pre-commit-config.yaml`**

```yaml
repos:
  - repo: local
    hooks:
      - id: tw-memory-check
        name: tw-memory check
        entry: python -m tw_memory check --staged
        language: system
        pass_filenames: false
        always_run: true
```

- [ ] **Step 2: Write `.github/workflows/tw-memory.yml`**

```yaml
name: tw-memory

on:
  push:
  pull_request:

jobs:
  check:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-python@v5
        with:
          python-version: "3.12"
      - name: Install tw-memory
        run: pip install -e tools
      - name: Run tests
        run: cd tools && python -m pytest -q
      - name: Validate generated memory
        run: python -m tw_memory check
```

- [ ] **Step 3: Verify local command used by CI**

Run:

```powershell
python -m pip install -e tools
cd tools
python -m pytest -q
cd ..
python -m tw_memory check
```

Expected: pytest passes; `tw-memory check` exits `0`.

- [ ] **Step 4: Commit**

```bash
git add .pre-commit-config.yaml .github/workflows/tw-memory.yml
git commit -m "ci(tw-memory): validate generated memory"
```

---

## Task 12: Full Verification and Regression Probes

**Files:**
- No source files created
- Temporary edits are reverted within the task

- [ ] **Step 1: Run full Python tests**

Run:

```powershell
cd tools
python -m pytest -q
```

Expected: all tests pass.

- [ ] **Step 2: Run generation and check without requiring CodeGraph**

Run:

```powershell
if (Test-Path ..\.codegraph) { Rename-Item ..\.codegraph ..\.codegraph.__tw_memory_probe }
python -m tw_memory generate --root ..
python -m tw_memory check --root ..
if (Test-Path ..\.codegraph.__tw_memory_probe) { Rename-Item ..\.codegraph.__tw_memory_probe ..\.codegraph }
```

Expected: generate and check exit `0` while `.codegraph` is absent.

- [ ] **Step 3: Probe missing charter failure**

Run:

```powershell
Rename-Item ..\backend\dotnet\BuildingBlocks\src\Tw.Core\package-charter.yaml package-charter.yaml.__probe
python -m tw_memory check --root ..
Rename-Item ..\backend\dotnet\BuildingBlocks\src\Tw.Core\package-charter.yaml.__probe package-charter.yaml
```

Expected: check exits `1` and reports missing `package-charter.yaml`.

- [ ] **Step 4: Probe stale generated memory failure**

Run:

```powershell
Add-Content ..\backend\dotnet\BuildingBlocks\src\Tw.Core\package-charter.yaml "`n# probe"
python -m tw_memory check --root ..
git checkout -- ..\backend\dotnet\BuildingBlocks\src\Tw.Core\package-charter.yaml
python -m tw_memory generate --root ..
python -m tw_memory check --root ..
```

Expected: first check exits `1` for stale source hash; final check exits `0`.

- [ ] **Step 5: Probe forbidden tracked runtime path failure**

Run:

```powershell
New-Item -ItemType Directory -Force ..\.tw-memory\runtime | Out-Null
Set-Content ..\.tw-memory\runtime\probe.txt "probe"
git add -f ..\.tw-memory\runtime\probe.txt
python -m tw_memory check --root .. --staged
git reset -- ..\.tw-memory\runtime\probe.txt
Remove-Item -Recurse -Force ..\.tw-memory\runtime
```

Expected: staged check exits `1` and reports forbidden tracked path.

- [ ] **Step 6: Run final clean status check for planned scope**

Run:

```powershell
git status --short .rules .tw-memory tools docs/engineering-standards backend/dotnet/BuildingBlocks .pre-commit-config.yaml .github/workflows/tw-memory.yml
```

Expected: only intentional files from this plan are modified, added, or deleted.

- [ ] **Step 7: Commit verification adjustments if generation changed files**

```bash
git add .tw-memory
git commit -m "chore(tw-memory): refresh generated memory after verification"
```

If `git diff --cached --quiet` is true, skip this commit.

---

## Self-Review

**Spec coverage:**
- Generated `.tw-memory` source index, routes, cards: Tasks 1, 4, 5, 10.
- Engineering standards segmented index: Tasks 2, 5, 10.
- `.rules` as bootstrap only and no duplicate formal-rule summaries: Tasks 2, 7, 12.
- CodeGraph optional and query-intent-only: Tasks 4, 5, 12.
- Package charters and package/public-api cards: Tasks 3, 4, 8, 9, 10.
- Check gates for hash, source refs, charter schema, dependency boundaries, public API overlap, secret scan, generated-only memory, forbidden tracked paths: Tasks 6, 10, 12.
- CI and pre-commit validation: Task 11.

**Placeholder scan:** This plan avoids placeholder instructions and names exact files, commands, and expected outcomes for every task. Implementation code blocks define all referenced modules and functions before use.

**Type consistency:** `DiscoveredPackage`, `Charter`, `DependencyRules`, `RepoPaths`, `run_generate(root)`, and `run_check(root, staged=...)` are used consistently across tasks.
