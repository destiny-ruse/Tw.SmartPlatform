# tw-memory 基础设施 + 包域 charter 集成 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 建立 Python `tw-memory` 工具的提交层基础设施，并在其上落地共享包 charter——为每个包生成 package/public-api 卡片、用 8 条硬闸门校验 charter，接入 pre-commit 与 CI。

**Architecture:** 提交层（`.tw-memory`）确定性生成、不依赖 CodeGraph。`tw-memory generate` 扫描包目录与各包 `package-charter.yaml`，计算来源 hash 写入 `source-index.generated.json`，渲染带 provenance 标签的 package 卡片与 public-api 卡片。`tw-memory check` 执行存在性、schema、canonical key、依赖边界、公开能力互斥、占位词、provenance/hash、secret-scan、提交边界、预算共十类校验。人读 `docs/engineering-standards` 与各包 charter，AI 读 `.tw-memory` 卡片。

**Tech Stack:** Python 3.14（标准库 + PyYAML + pytest）；`.csproj`/`package.json` 解析用标准库 `xml.etree` 与 `json`；CLI 用 `argparse`；pre-commit 用本地 hook；CI 用 GitHub Actions。

**Scope（本计划边界）:** 仅覆盖「基础设施 + 包域/charter」。`contracts`、`services`、`frontend`、CodeGraph 读取层、skill-routes 各域属后续计划，因其源目录当前为空占位；本工具对这些空目录只记 advisory，不生成卡片。

**关键事实（实现前必读）:**
- 全仓当前唯一有真实代码的包域是 `backend/dotnet/BuildingBlocks/src` 下的 `Tw.Core`（62 个 `.cs`）与 `Tw.AspNetCore`（仅 `.csproj`，无 `.cs`）。
- `Tw.Core.csproj` 设了 `<RootNamespace>Tw</RootNamespace>`，源码命名空间不一致（`Tw`、`Tw.Collections`、甚至 `Tw.Core.Configuration`）。因此 **canonical key 取 `.csproj` 文件名去扩展名（assembly name），不取 RootNamespace**；`public_capabilities` 由 charter 手写声明，本计划不解析 C# 校验其与真实命名空间一致。
- `.gitignore` 已含 `.codegraph/` 与 `.tw-memory/runtime/`，无需新增忽略规则，但提交边界校验仍需断言这些路径未被 Git 跟踪。
- 仓库根目录由含 `.git` 的目录确定。

---

## File Structure

工具源码（Python 包，落 `tools/src` 与 `tools/tests`，遵循项目结构规范）：

```
tools/
  pyproject.toml                 # tw-memory 包元数据、依赖、pytest 配置
  src/tw_memory/
    __init__.py
    __main__.py                  # python -m tw_memory 入口 -> cli.main
    cli.py                       # argparse：generate / check 子命令
    repo.py                      # 仓库根发现、.tw-memory 路径常量
    hashing.py                   # sha256（UTF-8、LF 规范化）
    yaml_io.py                   # 安全加载 + 确定性转储（键排序、无时间戳）
    placeholders.py              # 未来承诺词扫描
    secret_scan.py               # 密钥/令牌/连接串扫描
    charter.py                   # charter 数据模型 + 加载 + schema 校验
    packages.py                  # 发现包、派生 canonical key、解析依赖
    source_index.py              # 构建/加载/比对 source-index.generated.json
    cards.py                     # 渲染 package 卡片与 public-api 卡片
    generate.py                  # generate 管线
    check.py                     # check 十类校验
  tests/
    conftest.py                  # 临时仓库夹具
    test_hashing.py
    test_placeholders.py
    test_secret_scan.py
    test_charter.py
    test_packages.py
    test_source_index.py
    test_cards.py
    test_generate.py
    test_check.py
```

提交层与文档（非工具源码）：

```
.tw-memory/
  README.md                                  # 目录职责、生成/校验命令、提交边界
  manifest/taxonomy.yaml                     # 语言、来源类型、memory type、lookup key、生成器版本
  manifest/source-index.generated.json       # 由 generate 维护
  cards/packages/<package>.generated.md      # 由 generate 维护
  cards/public-apis/<package>.generated.md   # 由 generate 维护
backend/dotnet/BuildingBlocks/src/Tw.Core/package-charter.yaml
backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/package-charter.yaml
docs/engineering-standards/03-project-and-code/shared-package-charter.md
.pre-commit-config.yaml
.github/workflows/tw-memory.yml
```

---

## Task 0: Python 工具脚手架

**Files:**
- Create: `tools/pyproject.toml`
- Create: `tools/src/tw_memory/__init__.py`
- Create: `tools/src/tw_memory/__main__.py`
- Create: `tools/src/tw_memory/cli.py`
- Create: `tools/tests/conftest.py`

- [ ] **Step 1: 写 `tools/pyproject.toml`**

```toml
[project]
name = "tw-memory"
version = "0.1.0"
description = "Deterministic commit-layer memory generator and checker for the Tw.SmartPlatform monorepo."
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

- [ ] **Step 2: 写 `tools/src/tw_memory/__init__.py`**

```python
"""tw-memory: deterministic commit-layer memory tooling."""

__version__ = "0.1.0"
GENERATOR_VERSION = "tw-memory:0.1.0"
```

- [ ] **Step 3: 写 `tools/src/tw_memory/__main__.py`**

```python
from tw_memory.cli import main

if __name__ == "__main__":
    raise SystemExit(main())
```

- [ ] **Step 4: 写 `tools/src/tw_memory/cli.py`（先只搭骨架，子命令在后续任务接实现）**

```python
import argparse
import sys


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(prog="tw-memory")
    sub = parser.add_subparsers(dest="command", required=True)

    gen = sub.add_parser("generate", help="Generate the commit-layer memory.")
    gen.add_argument("--root", default=None, help="Repository root (default: auto-detect).")

    chk = sub.add_parser("check", help="Validate the commit-layer memory and charters.")
    chk.add_argument("--root", default=None, help="Repository root (default: auto-detect).")
    chk.add_argument("--staged", action="store_true", help="Only consider git-staged files.")

    args = parser.parse_args(argv)

    if args.command == "generate":
        from tw_memory.generate import run_generate
        return run_generate(args.root)
    if args.command == "check":
        from tw_memory.check import run_check
        return run_check(args.root, staged=args.staged)
    parser.error(f"unknown command {args.command!r}")
    return 2


if __name__ == "__main__":
    sys.exit(main())
```

- [ ] **Step 5: 写 `tools/tests/conftest.py`（临时仓库夹具，后续测试复用）**

```python
import subprocess
from pathlib import Path

import pytest


@pytest.fixture
def repo(tmp_path: Path) -> Path:
    """A minimal git repo with the directory shape tw-memory expects."""
    (tmp_path / ".git").mkdir()
    (tmp_path / "backend/dotnet/BuildingBlocks/src").mkdir(parents=True)
    (tmp_path / "frontend/packages").mkdir(parents=True)
    (tmp_path / "docs/engineering-standards").mkdir(parents=True)
    return tmp_path


@pytest.fixture
def git_repo(tmp_path: Path) -> Path:
    """A real initialized git repo for commit-boundary tests."""
    subprocess.run(["git", "init", "-q"], cwd=tmp_path, check=True)
    subprocess.run(["git", "config", "user.email", "t@t"], cwd=tmp_path, check=True)
    subprocess.run(["git", "config", "user.name", "t"], cwd=tmp_path, check=True)
    return tmp_path


def make_csproj(root: Path, name: str, *, root_namespace: str | None = None,
                package_refs: list[str] | None = None) -> Path:
    """Create backend/dotnet/BuildingBlocks/src/<name>/<name>.csproj and return its dir."""
    pkg_dir = root / "backend/dotnet/BuildingBlocks/src" / name
    pkg_dir.mkdir(parents=True, exist_ok=True)
    rns = f"<RootNamespace>{root_namespace}</RootNamespace>" if root_namespace else ""
    refs = "".join(
        f'<PackageReference Include="{r}" Version="1.0.0" />' for r in (package_refs or [])
    )
    (pkg_dir / f"{name}.csproj").write_text(
        f'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>{rns}</PropertyGroup>'
        f"<ItemGroup>{refs}</ItemGroup></Project>",
        encoding="utf-8",
    )
    return pkg_dir
```

- [ ] **Step 6: 验证脚手架可运行**

Run: `cd tools && python -m tw_memory --help`
Expected: 打印 usage，列出 `generate` 与 `check` 子命令，退出码 0。（此时调用子命令会因模块未建而 ImportError，属正常，后续任务补齐。）

- [ ] **Step 7: Commit**

```bash
git add tools/pyproject.toml tools/src/tw_memory/__init__.py tools/src/tw_memory/__main__.py tools/src/tw_memory/cli.py tools/tests/conftest.py
git commit -m "chore(tw-memory): scaffold python cli package"
```

---

## Task 1: 仓库路径解析 `repo.py`

**Files:**
- Create: `tools/src/tw_memory/repo.py`
- Test: `tools/tests/test_repo.py`

- [ ] **Step 1: 写失败测试 `tools/tests/test_repo.py`**

```python
from pathlib import Path

from tw_memory.repo import find_repo_root, RepoPaths


def test_find_repo_root_from_nested_dir(repo: Path):
    nested = repo / "backend/dotnet/BuildingBlocks/src"
    assert find_repo_root(nested) == repo


def test_repo_paths_are_under_tw_memory(repo: Path):
    paths = RepoPaths(repo)
    assert paths.source_index == repo / ".tw-memory/manifest/source-index.generated.json"
    assert paths.packages_cards_dir == repo / ".tw-memory/cards/packages"
    assert paths.public_api_cards_dir == repo / ".tw-memory/cards/public-apis"
```

- [ ] **Step 2: 运行确认失败**

Run: `cd tools && python -m pytest tests/test_repo.py -q`
Expected: FAIL（`ModuleNotFoundError: tw_memory.repo`）

- [ ] **Step 3: 写实现 `tools/src/tw_memory/repo.py`**

```python
from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path


def find_repo_root(start: Path | None = None) -> Path:
    """Walk upward from start until a directory containing .git is found."""
    current = (start or Path.cwd()).resolve()
    for candidate in [current, *current.parents]:
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
    def manifest_dir(self) -> Path:
        return self.tw_memory / "manifest"

    @property
    def source_index(self) -> Path:
        return self.manifest_dir / "source-index.generated.json"

    @property
    def taxonomy(self) -> Path:
        return self.manifest_dir / "taxonomy.yaml"

    @property
    def packages_cards_dir(self) -> Path:
        return self.tw_memory / "cards" / "packages"

    @property
    def public_api_cards_dir(self) -> Path:
        return self.tw_memory / "cards" / "public-apis"

    @property
    def dotnet_packages_glob_root(self) -> Path:
        return self.root / "backend/dotnet/BuildingBlocks/src"

    @property
    def frontend_packages_root(self) -> Path:
        return self.root / "frontend/packages"
```

- [ ] **Step 4: 运行确认通过**

Run: `cd tools && python -m pytest tests/test_repo.py -q`
Expected: PASS（2 passed）

- [ ] **Step 5: Commit**

```bash
git add tools/src/tw_memory/repo.py tools/tests/test_repo.py
git commit -m "feat(tw-memory): repo root discovery and path resolver"
```

---

## Task 2: 确定性 hash `hashing.py`

**Files:**
- Create: `tools/src/tw_memory/hashing.py`
- Test: `tools/tests/test_hashing.py`

- [ ] **Step 1: 写失败测试 `tools/tests/test_hashing.py`**

```python
from pathlib import Path

from tw_memory.hashing import sha256_normalized


def test_crlf_and_lf_hash_identically(tmp_path: Path):
    lf = tmp_path / "lf.txt"
    crlf = tmp_path / "crlf.txt"
    lf.write_bytes(b"a\nb\n")
    crlf.write_bytes(b"a\r\nb\r\n")
    assert sha256_normalized(lf) == sha256_normalized(crlf)


def test_known_value(tmp_path: Path):
    f = tmp_path / "x.txt"
    f.write_bytes(b"abc\n")
    # sha256 of b"abc\n"
    assert sha256_normalized(f) == (
        "edeaaff3f1774ad2888673770c6d64097e391bc362d7d6fb34982ddf0efd18cb"
    )
```

- [ ] **Step 2: 运行确认失败**

Run: `cd tools && python -m pytest tests/test_hashing.py -q`
Expected: FAIL（`ModuleNotFoundError`）

- [ ] **Step 3: 写实现 `tools/src/tw_memory/hashing.py`**

```python
from __future__ import annotations

import hashlib
from pathlib import Path


def sha256_normalized(path: Path) -> str:
    """sha256 over UTF-8 content with CRLF/CR normalized to LF."""
    raw = path.read_bytes()
    normalized = raw.replace(b"\r\n", b"\n").replace(b"\r", b"\n")
    return hashlib.sha256(normalized).hexdigest()
```

- [ ] **Step 4: 运行确认通过**

Run: `cd tools && python -m pytest tests/test_hashing.py -q`
Expected: PASS（2 passed）

- [ ] **Step 5: Commit**

```bash
git add tools/src/tw_memory/hashing.py tools/tests/test_hashing.py
git commit -m "feat(tw-memory): LF-normalized sha256 hashing"
```

---

## Task 3: 占位词扫描 `placeholders.py`

**Files:**
- Create: `tools/src/tw_memory/placeholders.py`
- Test: `tools/tests/test_placeholders.py`

- [ ] **Step 1: 写失败测试 `tools/tests/test_placeholders.py`**

```python
from tw_memory.placeholders import find_placeholder_terms


def test_detects_future_promise_terms():
    hits = find_placeholder_terms("职责后续补充，TODO 待定")
    assert "后续" in hits
    assert "待定" in hits
    assert "TODO" in hits


def test_clean_text_has_no_hits():
    assert find_placeholder_terms("跨服务复用的基础原语") == []


def test_case_insensitive_ascii_terms():
    assert "TBD" in find_placeholder_terms("status: tbd")
```

- [ ] **Step 2: 运行确认失败**

Run: `cd tools && python -m pytest tests/test_placeholders.py -q`
Expected: FAIL

- [ ] **Step 3: 写实现 `tools/src/tw_memory/placeholders.py`**

```python
from __future__ import annotations

# Mirrors the forbidden future-promise vocabulary in
# .rules/common-agent-instructions.md and the engineering standards.
PLACEHOLDER_TERMS = (
    "后续", "待定", "暂定", "视情况", "可能", "大概", "如有需要",
    "按需补充", "待补充", "TODO", "TBD", "FIXME",
)


def find_placeholder_terms(text: str) -> list[str]:
    """Return the placeholder terms present in text (ASCII terms matched case-insensitively)."""
    lowered = text.lower()
    hits: list[str] = []
    for term in PLACEHOLDER_TERMS:
        if term.isascii():
            if term.lower() in lowered:
                hits.append(term)
        elif term in text:
            hits.append(term)
    return hits
```

- [ ] **Step 4: 运行确认通过**

Run: `cd tools && python -m pytest tests/test_placeholders.py -q`
Expected: PASS（3 passed）

- [ ] **Step 5: Commit**

```bash
git add tools/src/tw_memory/placeholders.py tools/tests/test_placeholders.py
git commit -m "feat(tw-memory): placeholder/future-promise term scanner"
```

---

## Task 4: secret 扫描 `secret_scan.py`

**Files:**
- Create: `tools/src/tw_memory/secret_scan.py`
- Test: `tools/tests/test_secret_scan.py`

- [ ] **Step 1: 写失败测试 `tools/tests/test_secret_scan.py`**

```python
from tw_memory.secret_scan import scan_secrets


def test_detects_connection_string():
    hits = scan_secrets("Server=db;Password=hunter2;")
    assert any(h.kind == "connection-string" for h in hits)


def test_detects_bearer_token():
    hits = scan_secrets("Authorization: Bearer abcdef0123456789abcdef0123456789")
    assert any(h.kind == "bearer-token" for h in hits)


def test_detects_private_key_header():
    hits = scan_secrets("-----BEGIN RSA PRIVATE KEY-----")
    assert any(h.kind == "private-key" for h in hits)


def test_clean_text_is_empty():
    assert scan_secrets("Tw.Core.Primitives 公共能力") == []
```

- [ ] **Step 2: 运行确认失败**

Run: `cd tools && python -m pytest tests/test_secret_scan.py -q`
Expected: FAIL

- [ ] **Step 3: 写实现 `tools/src/tw_memory/secret_scan.py`**

```python
from __future__ import annotations

import re
from dataclasses import dataclass

_PATTERNS: tuple[tuple[str, re.Pattern[str]], ...] = (
    ("connection-string", re.compile(r"(?i)(password|pwd)\s*=\s*[^;\s]+")),
    ("bearer-token", re.compile(r"(?i)bearer\s+[A-Za-z0-9._\-]{20,}")),
    ("private-key", re.compile(r"-----BEGIN [A-Z ]*PRIVATE KEY-----")),
    ("aws-access-key", re.compile(r"AKIA[0-9A-Z]{16}")),
)


@dataclass(frozen=True)
class SecretHit:
    kind: str
    match: str


def scan_secrets(text: str) -> list[SecretHit]:
    """Return secret hits found in text. Patterns are intentionally conservative."""
    hits: list[SecretHit] = []
    for kind, pattern in _PATTERNS:
        for m in pattern.finditer(text):
            hits.append(SecretHit(kind=kind, match=m.group(0)))
    return hits
```

- [ ] **Step 4: 运行确认通过**

Run: `cd tools && python -m pytest tests/test_secret_scan.py -q`
Expected: PASS（4 passed）

- [ ] **Step 5: Commit**

```bash
git add tools/src/tw_memory/secret_scan.py tools/tests/test_secret_scan.py
git commit -m "feat(tw-memory): conservative secret scanner"
```

---

## Task 5: charter 模型与 schema 校验 `charter.py`

**Files:**
- Create: `tools/src/tw_memory/charter.py`
- Test: `tools/tests/test_charter.py`

- [ ] **Step 1: 写失败测试 `tools/tests/test_charter.py`**

```python
from pathlib import Path

import pytest

from tw_memory.charter import load_charter, validate_charter, REQUIRED_FIELDS

VALID = """\
schema_version: "1.0.0"
package: Tw.Core
owner: platform-team
responsibility: 跨服务复用的基础原语。
in_scope:
  - 基础值对象
out_of_scope:
  - HTTP 中间件
public_capabilities:
  - Tw.Primitives
dependency_rules:
  forbid:
    - "Microsoft.AspNetCore.*"
  allow: []
"""


def write(tmp_path: Path, text: str) -> Path:
    p = tmp_path / "package-charter.yaml"
    p.write_text(text, encoding="utf-8")
    return p


def test_load_valid_charter(tmp_path: Path):
    c = load_charter(write(tmp_path, VALID))
    assert c.package == "Tw.Core"
    assert c.out_of_scope == ["HTTP 中间件"]
    assert c.dependency_rules.forbid == ["Microsoft.AspNetCore.*"]


def test_validate_valid_charter_has_no_errors(tmp_path: Path):
    c = load_charter(write(tmp_path, VALID))
    assert validate_charter(c) == []


def test_empty_out_of_scope_fails(tmp_path: Path):
    bad = VALID.replace("out_of_scope:\n  - HTTP 中间件", "out_of_scope: []")
    c = load_charter(write(tmp_path, bad))
    errors = validate_charter(c)
    assert any("out_of_scope" in e for e in errors)


def test_missing_required_field_fails(tmp_path: Path):
    bad = VALID.replace('owner: platform-team\n', "")
    c = load_charter(write(tmp_path, bad))
    errors = validate_charter(c)
    assert any("owner" in e for e in errors)


def test_placeholder_in_responsibility_fails(tmp_path: Path):
    bad = VALID.replace("跨服务复用的基础原语。", "职责待定")
    c = load_charter(write(tmp_path, bad))
    errors = validate_charter(c)
    assert any("待定" in e for e in errors)
```

- [ ] **Step 2: 运行确认失败**

Run: `cd tools && python -m pytest tests/test_charter.py -q`
Expected: FAIL

- [ ] **Step 3: 写实现 `tools/src/tw_memory/charter.py`**

```python
from __future__ import annotations

from dataclasses import dataclass, field
from pathlib import Path

import yaml

from tw_memory.placeholders import find_placeholder_terms

REQUIRED_FIELDS = (
    "schema_version", "package", "owner", "responsibility",
    "in_scope", "out_of_scope", "public_capabilities", "dependency_rules",
)
NON_EMPTY_LIST_FIELDS = ("in_scope", "out_of_scope", "public_capabilities")
TEXT_FIELDS_FOR_PLACEHOLDER = ("responsibility",)
LIST_FIELDS_FOR_PLACEHOLDER = ("in_scope", "out_of_scope")
STABILITY_VALUES = ("experimental", "stable", "deprecated")


@dataclass(frozen=True)
class DependencyRules:
    forbid: list[str] = field(default_factory=list)
    allow: list[str] = field(default_factory=list)


@dataclass(frozen=True)
class Charter:
    path: Path
    raw: dict
    package: str
    owner: str
    responsibility: str
    in_scope: list[str]
    out_of_scope: list[str]
    public_capabilities: list[str]
    dependency_rules: DependencyRules
    schema_version: str
    stability: str = "stable"


def load_charter(path: Path) -> Charter:
    data = yaml.safe_load(path.read_text(encoding="utf-8")) or {}
    dr = data.get("dependency_rules") or {}
    return Charter(
        path=path,
        raw=data,
        package=str(data.get("package", "")),
        owner=str(data.get("owner", "")),
        responsibility=str(data.get("responsibility", "")),
        in_scope=list(data.get("in_scope") or []),
        out_of_scope=list(data.get("out_of_scope") or []),
        public_capabilities=list(data.get("public_capabilities") or []),
        dependency_rules=DependencyRules(
            forbid=list(dr.get("forbid") or []),
            allow=list(dr.get("allow") or []),
        ),
        schema_version=str(data.get("schema_version", "")),
        stability=str(data.get("stability", "stable")),
    )


def validate_charter(charter: Charter) -> list[str]:
    """Return human-readable schema errors; empty means valid (check #2 and #6)."""
    errors: list[str] = []
    rel = charter.path
    for f in REQUIRED_FIELDS:
        if f not in charter.raw or charter.raw.get(f) in (None, "", [], {}):
            errors.append(f"{rel}: 必填字段 '{f}' 缺失或为空")
    for f in NON_EMPTY_LIST_FIELDS:
        value = charter.raw.get(f)
        if isinstance(value, list) and len(value) == 0:
            errors.append(f"{rel}: 字段 '{f}' 不得为空列表")
    if charter.stability not in STABILITY_VALUES:
        errors.append(f"{rel}: stability '{charter.stability}' 非法，须为 {STABILITY_VALUES}")
    # 占位词扫描（check #6）
    for f in TEXT_FIELDS_FOR_PLACEHOLDER:
        for term in find_placeholder_terms(str(charter.raw.get(f, ""))):
            errors.append(f"{rel}: 字段 '{f}' 含未来承诺语义 '{term}'")
    for f in LIST_FIELDS_FOR_PLACEHOLDER:
        for item in charter.raw.get(f, []) or []:
            for term in find_placeholder_terms(str(item)):
                errors.append(f"{rel}: 字段 '{f}' 含未来承诺语义 '{term}'")
    return errors
```

- [ ] **Step 4: 运行确认通过**

Run: `cd tools && python -m pytest tests/test_charter.py -q`
Expected: PASS（5 passed）

- [ ] **Step 5: Commit**

```bash
git add tools/src/tw_memory/charter.py tools/tests/test_charter.py
git commit -m "feat(tw-memory): charter model, loader, schema+placeholder validation"
```

---

## Task 6: 包发现与依赖解析 `packages.py`

**Files:**
- Create: `tools/src/tw_memory/packages.py`
- Test: `tools/tests/test_packages.py`

- [ ] **Step 1: 写失败测试 `tools/tests/test_packages.py`**

```python
import json
from pathlib import Path

from tw_memory.conftest_helpers import make_csproj  # see Step 3 note
from tw_memory.packages import discover_packages, DiscoveredPackage


def test_discovers_dotnet_package_by_assembly_name(repo: Path):
    make_csproj(repo, "Tw.Core", root_namespace="Tw")
    pkgs = discover_packages(repo)
    assert [p.canonical_key for p in pkgs] == ["Tw.Core"]
    assert pkgs[0].ecosystem == "dotnet"


def test_canonical_key_ignores_root_namespace(repo: Path):
    # RootNamespace=Tw must NOT change the key; assembly name wins.
    make_csproj(repo, "Tw.Core", root_namespace="Tw")
    pkgs = discover_packages(repo)
    assert pkgs[0].canonical_key == "Tw.Core"


def test_parses_dotnet_dependencies(repo: Path):
    make_csproj(repo, "Tw.Core", package_refs=["Microsoft.AspNetCore.Mvc", "Newtonsoft.Json"])
    pkgs = discover_packages(repo)
    assert set(pkgs[0].dependencies) == {"Microsoft.AspNetCore.Mvc", "Newtonsoft.Json"}


def test_discovers_frontend_package_by_name(repo: Path):
    pkg_dir = repo / "frontend/packages/ui"
    pkg_dir.mkdir(parents=True)
    (pkg_dir / "package.json").write_text(
        json.dumps({"name": "@tw/ui", "dependencies": {"vue": "^3"}}), encoding="utf-8"
    )
    pkgs = discover_packages(repo)
    fe = [p for p in pkgs if p.ecosystem == "frontend"][0]
    assert fe.canonical_key == "@tw/ui"
    assert "vue" in fe.dependencies
```

> **Note for Step 1:** `make_csproj` was defined in `conftest.py` (Task 0). To import it from a test module, also export it: add `from tests.conftest import make_csproj` is not portable, so instead re-declare a thin re-export. Simplest: in Task 0 `conftest.py`, `make_csproj` is a module-level function; in this test import it directly via `from conftest import make_csproj` (pytest puts the test dir on `sys.path`). Replace the import line in Step 1 with `from conftest import make_csproj` and delete the `conftest_helpers` import.

- [ ] **Step 2: 修正导入并运行确认失败**

将 Step 1 测试首行改为 `from conftest import make_csproj`。
Run: `cd tools && python -m pytest tests/test_packages.py -q`
Expected: FAIL（`ModuleNotFoundError: tw_memory.packages`）

- [ ] **Step 3: 写实现 `tools/src/tw_memory/packages.py`**

```python
from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path
from xml.etree import ElementTree

from tw_memory.repo import RepoPaths


@dataclass(frozen=True)
class DiscoveredPackage:
    canonical_key: str          # dotnet: .csproj stem; frontend: package.json name
    ecosystem: str              # "dotnet" | "frontend"
    root_dir: Path              # package root directory
    charter_path: Path          # root_dir / package-charter.yaml
    dependencies: list[str]     # dependency identifiers (package ids / referenced assembly names)


def _strip_ns(tag: str) -> str:
    return tag.split("}", 1)[-1]


def _parse_csproj(csproj: Path) -> list[str]:
    deps: list[str] = []
    tree = ElementTree.parse(csproj)
    for item in tree.iter():
        tag = _strip_ns(item.tag)
        if tag == "PackageReference":
            inc = item.get("Include")
            if inc:
                deps.append(inc)
        elif tag == "ProjectReference":
            inc = item.get("Include")
            if inc:
                deps.append(Path(inc.replace("\\", "/")).stem)
    return deps


def _parse_package_json(pkg_json: Path) -> tuple[str, list[str]]:
    data = json.loads(pkg_json.read_text(encoding="utf-8"))
    name = str(data.get("name", pkg_json.parent.name))
    deps: list[str] = []
    for key in ("dependencies", "peerDependencies"):
        deps.extend((data.get(key) or {}).keys())
    return name, deps


def discover_packages(root: Path) -> list[DiscoveredPackage]:
    paths = RepoPaths(root)
    found: list[DiscoveredPackage] = []

    dotnet_root = paths.dotnet_packages_glob_root
    if dotnet_root.exists():
        for csproj in sorted(dotnet_root.glob("*/*.csproj")):
            pkg_dir = csproj.parent
            found.append(DiscoveredPackage(
                canonical_key=csproj.stem,
                ecosystem="dotnet",
                root_dir=pkg_dir,
                charter_path=pkg_dir / "package-charter.yaml",
                dependencies=_parse_csproj(csproj),
            ))

    fe_root = paths.frontend_packages_root
    if fe_root.exists():
        for pkg_json in sorted(fe_root.glob("*/package.json")):
            pkg_dir = pkg_json.parent
            name, deps = _parse_package_json(pkg_json)
            found.append(DiscoveredPackage(
                canonical_key=name,
                ecosystem="frontend",
                root_dir=pkg_dir,
                charter_path=pkg_dir / "package-charter.yaml",
                dependencies=deps,
            ))

    return sorted(found, key=lambda p: p.canonical_key)
```

- [ ] **Step 4: 运行确认通过**

Run: `cd tools && python -m pytest tests/test_packages.py -q`
Expected: PASS（4 passed）

- [ ] **Step 5: Commit**

```bash
git add tools/src/tw_memory/packages.py tools/tests/test_packages.py
git commit -m "feat(tw-memory): discover dotnet/frontend packages and parse dependencies"
```

---

## Task 7: source-index `source_index.py`

**Files:**
- Create: `tools/src/tw_memory/source_index.py`
- Test: `tools/tests/test_source_index.py`

source-index 条目字段（来自 ai-memory-design.md）：`source_id`、`source_type`、`path`、`hash_algorithm`、`sha256`、`extractor`。charter 来源：`source_type=manual`、`extractor=package-charter:v1`、`source_id=manual:package-charter:<package>`。

- [ ] **Step 1: 写失败测试 `tools/tests/test_source_index.py`**

```python
import json
from pathlib import Path

from conftest import make_csproj
from tw_memory.source_index import build_source_index, charter_source_id, write_source_index, load_source_index
from tw_memory.packages import discover_packages


def _charter(pkg_dir: Path):
    (pkg_dir / "package-charter.yaml").write_text("package: Tw.Core\n", encoding="utf-8")


def test_charter_source_id_format():
    assert charter_source_id("Tw.Core") == "manual:package-charter:Tw.Core"


def test_build_index_has_charter_entry(repo: Path):
    d = make_csproj(repo, "Tw.Core")
    _charter(d)
    pkgs = discover_packages(repo)
    index = build_source_index(repo, pkgs)
    entry = index["sources"]["manual:package-charter:Tw.Core"]
    assert entry["source_type"] == "manual"
    assert entry["extractor"] == "package-charter:v1"
    assert entry["hash_algorithm"] == "sha256"
    assert entry["path"] == "backend/dotnet/BuildingBlocks/src/Tw.Core/package-charter.yaml"
    assert len(entry["sha256"]) == 64


def test_write_is_deterministic(repo: Path):
    d = make_csproj(repo, "Tw.Core")
    _charter(d)
    pkgs = discover_packages(repo)
    index = build_source_index(repo, pkgs)
    target = repo / ".tw-memory/manifest/source-index.generated.json"
    write_source_index(target, index)
    first = target.read_bytes()
    write_source_index(target, build_source_index(repo, discover_packages(repo)))
    assert target.read_bytes() == first  # no volatile fields, sorted keys


def test_load_roundtrip(repo: Path):
    d = make_csproj(repo, "Tw.Core")
    _charter(d)
    pkgs = discover_packages(repo)
    target = repo / ".tw-memory/manifest/source-index.generated.json"
    write_source_index(target, build_source_index(repo, pkgs))
    loaded = load_source_index(target)
    assert "manual:package-charter:Tw.Core" in loaded["sources"]
```

- [ ] **Step 2: 运行确认失败**

Run: `cd tools && python -m pytest tests/test_source_index.py -q`
Expected: FAIL

- [ ] **Step 3: 写实现 `tools/src/tw_memory/source_index.py`**

```python
from __future__ import annotations

import json
from pathlib import Path

from tw_memory.hashing import sha256_normalized
from tw_memory.packages import DiscoveredPackage

SCHEMA_VERSION = "1.0.0"


def charter_source_id(package: str) -> str:
    return f"manual:package-charter:{package}"


def _rel(root: Path, path: Path) -> str:
    return path.relative_to(root).as_posix()


def build_source_index(root: Path, packages: list[DiscoveredPackage]) -> dict:
    sources: dict[str, dict] = {}
    for pkg in packages:
        if not pkg.charter_path.exists():
            continue
        sources[charter_source_id(pkg.canonical_key)] = {
            "source_type": "manual",
            "path": _rel(root, pkg.charter_path),
            "hash_algorithm": "sha256",
            "sha256": sha256_normalized(pkg.charter_path),
            "extractor": "package-charter:v1",
        }
    return {"schema_version": SCHEMA_VERSION, "sources": sources}


def write_source_index(target: Path, index: dict) -> None:
    target.parent.mkdir(parents=True, exist_ok=True)
    # Deterministic: sorted keys, fixed indent, trailing newline, no timestamps.
    text = json.dumps(index, ensure_ascii=False, indent=2, sort_keys=True) + "\n"
    target.write_text(text, encoding="utf-8", newline="\n")


def load_source_index(target: Path) -> dict:
    return json.loads(target.read_text(encoding="utf-8"))
```

- [ ] **Step 4: 运行确认通过**

Run: `cd tools && python -m pytest tests/test_source_index.py -q`
Expected: PASS（4 passed）

- [ ] **Step 5: Commit**

```bash
git add tools/src/tw_memory/source_index.py tools/tests/test_source_index.py
git commit -m "feat(tw-memory): deterministic source-index with charter provenance"
```

---

## Task 8: 卡片渲染 `cards.py`

**Files:**
- Create: `tools/src/tw_memory/cards.py`
- Test: `tools/tests/test_cards.py`

package 卡片固定槽位（来自 ai-memory-design.md，新增「不适用范围」槽）：标识 / 职责 / 公共面 / 不适用范围 / 依赖边界 / 验证入口。每条 `[manual]` 事实带 `source_refs`，指向 charter 的 source_id。public-api 卡片列 `public_capabilities`。

- [ ] **Step 1: 写失败测试 `tools/tests/test_cards.py`**

```python
from pathlib import Path

from tw_memory.charter import load_charter
from tw_memory.packages import DiscoveredPackage
from tw_memory.cards import render_package_card, render_public_api_card

CHARTER = """\
schema_version: "1.0.0"
package: Tw.Core
owner: platform-team
responsibility: 跨服务复用的基础原语。
in_scope: [基础值对象]
out_of_scope: [HTTP 中间件]
public_capabilities: [Tw.Primitives, Tw.Exceptions]
dependency_rules:
  forbid: ["Microsoft.AspNetCore.*"]
  allow: []
"""


def _pkg(tmp_path: Path) -> tuple[DiscoveredPackage, object]:
    p = tmp_path / "package-charter.yaml"
    p.write_text(CHARTER, encoding="utf-8")
    pkg = DiscoveredPackage("Tw.Core", "dotnet", tmp_path, p, [])
    return pkg, load_charter(p)


def test_package_card_has_slots_and_provenance(tmp_path: Path):
    pkg, charter = _pkg(tmp_path)
    md = render_package_card(pkg, charter)
    assert "## 职责" in md
    assert "## 不适用范围" in md
    assert "HTTP 中间件" in md
    assert "[manual]" in md
    assert "manual:package-charter:Tw.Core" in md


def test_public_api_card_lists_capabilities(tmp_path: Path):
    pkg, charter = _pkg(tmp_path)
    md = render_public_api_card(pkg, charter)
    assert "Tw.Primitives" in md
    assert "Tw.Exceptions" in md


def test_render_is_stable(tmp_path: Path):
    pkg, charter = _pkg(tmp_path)
    assert render_package_card(pkg, charter) == render_package_card(pkg, charter)
```

- [ ] **Step 2: 运行确认失败**

Run: `cd tools && python -m pytest tests/test_cards.py -q`
Expected: FAIL

- [ ] **Step 3: 写实现 `tools/src/tw_memory/cards.py`**

```python
from __future__ import annotations

from tw_memory.charter import Charter
from tw_memory.packages import DiscoveredPackage
from tw_memory.source_index import charter_source_id

_GENERATED_HEADER = (
    "<!-- 本文件由 tw-memory 从 package-charter.yaml 生成，事实以 charter 为准，禁止手工编辑。 -->"
)


def _bullet_list(items: list[str]) -> str:
    return "\n".join(f"- {item}" for item in items) if items else "- （无）"


def render_package_card(pkg: DiscoveredPackage, charter: Charter) -> str:
    sid = charter_source_id(pkg.canonical_key)
    forbid = ", ".join(charter.dependency_rules.forbid) or "（无）"
    return "\n".join([
        _GENERATED_HEADER,
        f"# 包卡片：{pkg.canonical_key}",
        "",
        "## 标识",
        f"- canonical-key: {pkg.canonical_key}",
        f"- ecosystem: {pkg.ecosystem}",
        f"- owner: {charter.owner} `[manual]` `source_refs: [{sid}]`",
        f"- stability: {charter.stability} `[manual]` `source_refs: [{sid}]`",
        "",
        "## 职责",
        f"{charter.responsibility} `[manual]` `source_refs: [{sid}]`",
        "",
        "## 公共面",
        f"{_bullet_list(sorted(charter.public_capabilities))}",
        f"`[manual]` `source_refs: [{sid}]`",
        "",
        "## 不适用范围",
        f"{_bullet_list(charter.out_of_scope)}",
        f"`[manual]` `source_refs: [{sid}]`",
        "",
        "## 依赖边界",
        f"- forbid: {forbid} `[manual]` `source_refs: [{sid}]`",
        "",
    ]) + "\n"


def render_public_api_card(pkg: DiscoveredPackage, charter: Charter) -> str:
    sid = charter_source_id(pkg.canonical_key)
    return "\n".join([
        _GENERATED_HEADER,
        f"# 公开能力：{pkg.canonical_key}",
        "",
        "## 公开命名空间/模块",
        f"{_bullet_list(sorted(charter.public_capabilities))}",
        f"`[manual]` `source_refs: [{sid}]`",
        "",
    ]) + "\n"
```

- [ ] **Step 4: 运行确认通过**

Run: `cd tools && python -m pytest tests/test_cards.py -q`
Expected: PASS（3 passed）

- [ ] **Step 5: Commit**

```bash
git add tools/src/tw_memory/cards.py tools/tests/test_cards.py
git commit -m "feat(tw-memory): deterministic package and public-api card rendering"
```

---

## Task 9: generate 管线 `generate.py`

**Files:**
- Create: `tools/src/tw_memory/generate.py`
- Test: `tools/tests/test_generate.py`

- [ ] **Step 1: 写失败测试 `tools/tests/test_generate.py`**

```python
from pathlib import Path

from conftest import make_csproj
from tw_memory.generate import run_generate

CHARTER = """\
schema_version: "1.0.0"
package: Tw.Core
owner: platform-team
responsibility: 跨服务复用的基础原语。
in_scope: [基础值对象]
out_of_scope: [HTTP 中间件]
public_capabilities: [Tw.Primitives]
dependency_rules:
  forbid: ["Microsoft.AspNetCore.*"]
  allow: []
"""


def test_generate_writes_index_and_cards(repo: Path):
    d = make_csproj(repo, "Tw.Core")
    (d / "package-charter.yaml").write_text(CHARTER, encoding="utf-8")
    rc = run_generate(str(repo))
    assert rc == 0
    assert (repo / ".tw-memory/manifest/source-index.generated.json").exists()
    assert (repo / ".tw-memory/cards/packages/Tw.Core.generated.md").exists()
    assert (repo / ".tw-memory/cards/public-apis/Tw.Core.generated.md").exists()


def test_generate_is_byte_stable(repo: Path):
    d = make_csproj(repo, "Tw.Core")
    (d / "package-charter.yaml").write_text(CHARTER, encoding="utf-8")
    run_generate(str(repo))
    card = repo / ".tw-memory/cards/packages/Tw.Core.generated.md"
    first = card.read_bytes()
    run_generate(str(repo))
    assert card.read_bytes() == first


def test_generate_blocks_on_secret_in_charter(repo: Path):
    d = make_csproj(repo, "Tw.Core")
    bad = CHARTER.replace("跨服务复用的基础原语。", "连接串 Password=hunter2;")
    (d / "package-charter.yaml").write_text(bad, encoding="utf-8")
    rc = run_generate(str(repo))
    assert rc != 0
    assert not (repo / ".tw-memory/cards/packages/Tw.Core.generated.md").exists()


def test_generate_does_not_touch_runtime(repo: Path):
    d = make_csproj(repo, "Tw.Core")
    (d / "package-charter.yaml").write_text(CHARTER, encoding="utf-8")
    run_generate(str(repo))
    assert not (repo / ".tw-memory/runtime").exists()
```

- [ ] **Step 2: 运行确认失败**

Run: `cd tools && python -m pytest tests/test_generate.py -q`
Expected: FAIL

- [ ] **Step 3: 写实现 `tools/src/tw_memory/generate.py`**

```python
from __future__ import annotations

import sys
from pathlib import Path

from tw_memory.cards import render_package_card, render_public_api_card
from tw_memory.charter import load_charter
from tw_memory.packages import discover_packages
from tw_memory.repo import RepoPaths, find_repo_root
from tw_memory.secret_scan import scan_secrets
from tw_memory.source_index import build_source_index, write_source_index


def run_generate(root: str | None) -> int:
    repo = Path(root).resolve() if root else find_repo_root()
    paths = RepoPaths(repo)
    packages = [p for p in discover_packages(repo) if p.charter_path.exists()]

    # secret-scan gate over charter content BEFORE any commit-layer write.
    for pkg in packages:
        text = pkg.charter_path.read_text(encoding="utf-8")
        hits = scan_secrets(text)
        if hits:
            kinds = ", ".join(sorted({h.kind for h in hits}))
            print(f"[generate] 阻断：{pkg.charter_path} 命中疑似密钥（{kinds}）", file=sys.stderr)
            return 1

    index = build_source_index(repo, packages)
    write_source_index(paths.source_index, index)

    paths.packages_cards_dir.mkdir(parents=True, exist_ok=True)
    paths.public_api_cards_dir.mkdir(parents=True, exist_ok=True)
    for pkg in packages:
        charter = load_charter(pkg.charter_path)
        (paths.packages_cards_dir / f"{pkg.canonical_key}.generated.md").write_text(
            render_package_card(pkg, charter), encoding="utf-8", newline="\n")
        (paths.public_api_cards_dir / f"{pkg.canonical_key}.generated.md").write_text(
            render_public_api_card(pkg, charter), encoding="utf-8", newline="\n")

    print(f"[generate] 完成：{len(packages)} 个包卡片已生成。")
    return 0
```

- [ ] **Step 4: 运行确认通过**

Run: `cd tools && python -m pytest tests/test_generate.py -q`
Expected: PASS（4 passed）

- [ ] **Step 5: Commit**

```bash
git add tools/src/tw_memory/generate.py tools/tests/test_generate.py
git commit -m "feat(tw-memory): generate pipeline with secret-scan gate"
```

---

## Task 10: check 校验 `check.py`

**Files:**
- Create: `tools/src/tw_memory/check.py`
- Test: `tools/tests/test_check.py`

实现 10 类校验：① charter 存在性、② schema（含 out_of_scope 非空）、③ canonical key 一致、④ 依赖边界（forbid + allow 白名单）、⑤ 公开能力命名空间互斥、⑥ 占位词、⑦ source-index hash 新鲜 + 卡片 source_refs 指向已登记 source_id、⑧ secret-scan、⑨ 提交边界（`.tw-memory/runtime`、`.codegraph/` 未被 Git 跟踪）、⑩ 预算（每包 ≤1 package + ≤1 public-api 卡片）。`run_check` 返回非零即失败，并打印每条违规。

- [ ] **Step 1: 写失败测试 `tools/tests/test_check.py`**

```python
import subprocess
from pathlib import Path

from conftest import make_csproj
from tw_memory.generate import run_generate
from tw_memory.check import run_check

GOOD = """\
schema_version: "1.0.0"
package: {key}
owner: platform-team
responsibility: 跨服务复用的基础原语。
in_scope: [基础值对象]
out_of_scope: [HTTP 中间件]
public_capabilities: [{ns}]
dependency_rules:
  forbid: ["Microsoft.AspNetCore.*"]
  allow: []
"""


def _charter(pkg_dir: Path, key: str, ns: str):
    (pkg_dir / "package-charter.yaml").write_text(GOOD.format(key=key, ns=ns), encoding="utf-8")


def test_check_passes_for_valid_generated_repo(repo: Path):
    d = make_csproj(repo, "Tw.Core")
    _charter(d, "Tw.Core", "Tw.Primitives")
    assert run_generate(str(repo)) == 0
    assert run_check(str(repo)) == 0


def test_missing_charter_fails(repo: Path):
    make_csproj(repo, "Tw.Core")  # no charter
    assert run_check(str(repo)) != 0


def test_canonical_key_mismatch_fails(repo: Path):
    d = make_csproj(repo, "Tw.Core")
    _charter(d, "Tw.Wrong", "Tw.Primitives")
    assert run_check(str(repo)) != 0


def test_forbidden_dependency_fails(repo: Path):
    d = make_csproj(repo, "Tw.Core", package_refs=["Microsoft.AspNetCore.Mvc"])
    _charter(d, "Tw.Core", "Tw.Primitives")
    assert run_check(str(repo)) != 0


def test_public_capability_overlap_fails(repo: Path):
    a = make_csproj(repo, "Tw.Core")
    b = make_csproj(repo, "Tw.AspNetCore")
    _charter(a, "Tw.Core", "Tw.Shared")
    _charter(b, "Tw.AspNetCore", "Tw.Shared")  # same namespace -> overlap
    assert run_check(str(repo)) != 0


def test_stale_source_hash_fails(repo: Path):
    d = make_csproj(repo, "Tw.Core")
    _charter(d, "Tw.Core", "Tw.Primitives")
    run_generate(str(repo))
    (d / "package-charter.yaml").write_text(
        GOOD.format(key="Tw.Core", ns="Tw.Primitives") + "# changed\n", encoding="utf-8")
    assert run_check(str(repo)) != 0  # index hash no longer matches


def test_tracked_runtime_dir_fails(git_repo: Path):
    d = make_csproj(git_repo, "Tw.Core")
    _charter(d, "Tw.Core", "Tw.Primitives")
    run_generate(str(git_repo))
    runtime = git_repo / ".tw-memory/runtime"
    runtime.mkdir(parents=True)
    (runtime / "cache.bin").write_text("x", encoding="utf-8")
    subprocess.run(["git", "add", "-A", "-f"], cwd=git_repo, check=True)
    assert run_check(str(git_repo)) != 0
```

- [ ] **Step 2: 运行确认失败**

Run: `cd tools && python -m pytest tests/test_check.py -q`
Expected: FAIL

- [ ] **Step 3: 写实现 `tools/src/tw_memory/check.py`**

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
from tw_memory.secret_scan import scan_secrets
from tw_memory.source_index import charter_source_id, load_source_index

_FORBIDDEN_TRACKED = (".tw-memory/runtime", ".codegraph")


def _git_tracked(root: Path, staged: bool) -> list[str]:
    args = ["git", "diff", "--cached", "--name-only"] if staged else ["git", "ls-files"]
    out = subprocess.run(args, cwd=root, capture_output=True, text=True)
    if out.returncode != 0:
        return []
    return [line.strip() for line in out.stdout.splitlines() if line.strip()]


def _namespace_overlap(a: list[str], b: list[str]) -> list[str]:
    """Two capabilities overlap if one is equal to or a dotted-prefix of the other."""
    overlaps: list[str] = []
    for x in a:
        for y in b:
            if x == y or x.startswith(y + ".") or y.startswith(x + "."):
                overlaps.append(f"{x} <-> {y}")
    return overlaps


def run_check(root: str | None, *, staged: bool = False) -> int:
    repo = Path(root).resolve() if root else find_repo_root()
    paths = RepoPaths(repo)
    errors: list[str] = []
    packages = discover_packages(repo)

    charters = {}
    for pkg in packages:
        # ① 存在性
        if not pkg.charter_path.exists():
            errors.append(f"包 {pkg.canonical_key} 缺少 package-charter.yaml")
            continue
        charter = load_charter(pkg.charter_path)
        charters[pkg.canonical_key] = (pkg, charter)
        # ② schema + ⑥ 占位词
        errors.extend(validate_charter(charter))
        # ③ canonical key 一致
        if charter.package != pkg.canonical_key:
            errors.append(
                f"{pkg.charter_path}: package '{charter.package}' 与实际键 '{pkg.canonical_key}' 不符")
        # ④ 依赖边界
        for dep in pkg.dependencies:
            if any(fnmatch.fnmatch(dep, pat) for pat in charter.dependency_rules.forbid):
                errors.append(f"{pkg.canonical_key}: 依赖 '{dep}' 命中 forbid 规则")
            if charter.dependency_rules.allow and not any(
                    fnmatch.fnmatch(dep, pat) for pat in charter.dependency_rules.allow):
                errors.append(f"{pkg.canonical_key}: 依赖 '{dep}' 不在 allow 白名单内")
        # ⑧ secret-scan
        for hit in scan_secrets(pkg.charter_path.read_text(encoding="utf-8")):
            errors.append(f"{pkg.charter_path}: 命中疑似密钥（{hit.kind}）")

    # ⑤ 公开能力互斥（两两比较）
    keys = sorted(charters)
    for i, ka in enumerate(keys):
        for kb in keys[i + 1:]:
            caps_a = charters[ka][1].public_capabilities
            caps_b = charters[kb][1].public_capabilities
            for pair in _namespace_overlap(caps_a, caps_b):
                errors.append(f"公开能力重叠：{ka} 与 {kb}（{pair}）")

    # ⑦ source-index hash 新鲜 + 卡片存在 + ⑩ 预算
    if paths.source_index.exists():
        index = load_source_index(paths.source_index)
        sources = index.get("sources", {})
        for key, (pkg, _charter) in charters.items():
            sid = charter_source_id(key)
            entry = sources.get(sid)
            if not entry:
                errors.append(f"source-index 缺少 {sid}")
                continue
            if entry["sha256"] != sha256_normalized(pkg.charter_path):
                errors.append(f"{sid}: source hash 过期，需重新 generate")
            card = paths.packages_cards_dir / f"{key}.generated.md"
            if not card.exists():
                errors.append(f"包卡片缺失：{card.name}，需重新 generate")
    elif charters:
        errors.append("source-index 不存在，需先运行 tw-memory generate")

    # ⑩ 预算：每包至多 1 张 package 卡片、1 张 public-api 卡片（命名即一一对应，超出即多余文件）
    if paths.packages_cards_dir.exists():
        for card in paths.packages_cards_dir.glob("*.generated.md"):
            if card.stem.removesuffix(".generated") not in charters:
                errors.append(f"多余包卡片：{card.name}（无对应包/charter）")

    # ⑨ 提交边界
    tracked = _git_tracked(repo, staged)
    for path in tracked:
        if any(path == p or path.startswith(p + "/") for p in _FORBIDDEN_TRACKED):
            errors.append(f"禁止提交的路径被 Git 跟踪：{path}")

    if errors:
        print("[check] 失败：", file=sys.stderr)
        for e in errors:
            print(f"  - {e}", file=sys.stderr)
        return 1
    print(f"[check] 通过：{len(charters)} 个包 charter 全部合规。")
    return 0
```

- [ ] **Step 4: 运行确认通过**

Run: `cd tools && python -m pytest tests/test_check.py -q`
Expected: PASS（7 passed）

- [ ] **Step 5: 运行全量单测**

Run: `cd tools && python -m pytest -q`
Expected: 全绿（所有任务的测试通过）

- [ ] **Step 6: Commit**

```bash
git add tools/src/tw_memory/check.py tools/tests/test_check.py
git commit -m "feat(tw-memory): check with 10 validation rules"
```

---

## Task 11: 提交层种子文件（README + taxonomy）

**Files:**
- Create: `.tw-memory/README.md`
- Create: `.tw-memory/manifest/taxonomy.yaml`

- [ ] **Step 1: 写 `.tw-memory/README.md`**

```markdown
# .tw-memory（AI 提交层记忆）

本目录是 AI 工具使用的提交层记忆，不面向人阅读；人请阅读 `docs/engineering-standards` 与各包 `package-charter.yaml`。

## 内容

- `manifest/taxonomy.yaml`：语言、来源类型、memory type、lookup key、生成器版本。
- `manifest/source-index.generated.json`：提交层事实的来源登记簿，由生成器维护。
- `cards/packages/*.generated.md`：包卡片，由 `package-charter.yaml` 派生。
- `cards/public-apis/*.generated.md`：公开能力卡片。

## 命令（从仓库根目录）

- 生成：`python -m tw_memory generate`（先 `pip install -e tools`，或 `cd tools && python -m tw_memory generate --root ..`）
- 校验：`python -m tw_memory check`

## 提交边界

- `*.generated.md` 与 `*.generated.json` 由工具维护，禁止手工编辑。
- `.tw-memory/runtime/` 与 `.codegraph/` 不提交（见 `.gitignore`）。
```

- [ ] **Step 2: 写 `.tw-memory/manifest/taxonomy.yaml`**

```yaml
schema_version: "1.0.0"
generator: "tw-memory:0.1.0"
languages:
  - dotnet
  - frontend
source_types:
  - contract
  - standard
  - structure
  - manual
memory_types:
  - package-summary
  - public-api-summary
lookup_keys:
  - package
```

- [ ] **Step 3: 生成并校验**

Run（从仓库根）: `cd tools && python -m tw_memory generate --root .. && python -m tw_memory check --root ..`
Expected: generate 与 check 均退出码 0。（此时 `Tw.Core`/`Tw.AspNetCore` 的 charter 尚未写，check 会报缺 charter——这是预期，下个任务补 charter 后再次校验。）

> 注：若此步因缺 charter 失败，属正常，Task 12 写完 charter 后整体复跑。本步只验证种子文件格式与工具可运行。可临时跳过 check，仅确认 generate 不因种子文件报错。

- [ ] **Step 4: Commit**

```bash
git add .tw-memory/README.md .tw-memory/manifest/taxonomy.yaml
git commit -m "feat(tw-memory): seed README and taxonomy manifest"
```

---

## Task 12: 为 Tw.Core 与 Tw.AspNetCore 写 charter

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/package-charter.yaml`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/package-charter.yaml`

- [ ] **Step 1: 写 `Tw.Core/package-charter.yaml`**

> `public_capabilities` 取自该包真实命名空间前缀（RootNamespace=Tw，故为 `Tw.*`）。

```yaml
schema_version: "1.0.0"
package: Tw.Core
owner: platform-team
stability: stable
compatibility: "semver-minor 内向后兼容"
responsibility: >
  跨服务复用的基础原语与无框架依赖工具：值对象、命名对象、领域异常、
  类型查找与反射缓存、加密哈希、通用扩展方法与一次性资源工具。
in_scope:
  - 基础值对象与命名对象原语
  - 通用领域异常与配置异常
  - 类型查找、反射缓存与类型扩展
  - 加密、哈希与安全随机
  - 通用集合、字符串、时间、数字等扩展方法
out_of_scope:
  - HTTP、中间件、过滤器、ASP.NET Core 集成（属于 Tw.AspNetCore）
  - 数据访问、ORM、仓储实现
  - 具体业务领域模型
public_capabilities:
  - Tw.Primitives
  - Tw.Exceptions
  - Tw.Collections
  - Tw.Reflection
  - Tw.Security
  - Tw.Extensions
  - Tw.Utilities
dependency_rules:
  forbid:
    - "Microsoft.AspNetCore.*"
    - "Microsoft.EntityFrameworkCore*"
  allow: []
```

- [ ] **Step 2: 写 `Tw.AspNetCore/package-charter.yaml`**

> 该包目前仅有 `.csproj`、无 `.cs`，charter 声明其意图边界；`public_capabilities` 声明计划暴露的命名空间前缀，与 `Tw.Core` 不相交。

```yaml
schema_version: "1.0.0"
package: Tw.AspNetCore
owner: platform-team
stability: experimental
compatibility: "experimental 阶段不承诺兼容"
responsibility: >
  ASP.NET Core 宿主集成的公共构建块：中间件、过滤器、模型绑定与
  Web 层横切关注点。依赖 Tw.Core 的基础原语，不重复实现基础能力。
in_scope:
  - ASP.NET Core 中间件与过滤器
  - Web 层模型绑定与结果封装
  - 宿主启动与依赖注入扩展
out_of_scope:
  - 与框架无关的基础原语（属于 Tw.Core）
  - 数据访问、ORM、仓储实现
  - 具体业务领域模型
public_capabilities:
  - Tw.AspNetCore
dependency_rules:
  forbid:
    - "Microsoft.EntityFrameworkCore*"
  allow: []
```

- [ ] **Step 3: 生成并校验（端到端）**

Run（从仓库根）:
```bash
cd tools && python -m tw_memory generate --root .. && python -m tw_memory check --root ..
```
Expected: generate 打印 `2 个包卡片已生成`，check 打印 `2 个包 charter 全部合规`，两者退出码 0。生成 `.tw-memory/cards/packages/Tw.Core.generated.md`、`Tw.AspNetCore.generated.md` 及对应 public-apis 卡片。

- [ ] **Step 4: 确认确定性（再次生成无 diff）**

Run（从仓库根）:
```bash
cd tools && python -m tw_memory generate --root .. && cd .. && git diff --stat .tw-memory
```
Expected: `git diff` 对已提交的 `.tw-memory` 卡片无变化（第二次生成字节一致）。

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/package-charter.yaml \
        backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/package-charter.yaml \
        .tw-memory/manifest/source-index.generated.json \
        .tw-memory/cards
git commit -m "feat(buildingblocks): add package charters and generate package cards"
```

---

## Task 13: engineering-standards 文档（人读规则源）

**Files:**
- Create: `docs/engineering-standards/03-project-and-code/shared-package-charter.md`
- Modify: `docs/engineering-standards/03-project-and-code/project-structure.md`（落点矩阵加一行；文档约定收敛）
- Modify: `docs/engineering-standards/README.md`（目录导航补登）

> 正式规范禁用未来承诺语义（见 `.rules/common-agent-instructions.md`），下文用语遵守该约束。

- [ ] **Step 1: 写 `shared-package-charter.md`**

```markdown
# 共享包 charter 规范

## 目标

统一共享包职责与边界的声明形式，使人能直接判断某能力的归属、新功能是否独立成包，使 AI 工具能从派生卡片获得每个包做什么、不做什么。

## 适用范围

适用于 `backend/dotnet/BuildingBlocks/src` 下的 .NET 公共构建块与 `frontend/packages` 下的前端共享包。

## 规范要求

- 每个共享包根目录必须包含 `package-charter.yaml`。
- charter 必须包含字段：`schema_version`、`package`、`owner`、`responsibility`、`in_scope`、`out_of_scope`、`public_capabilities`、`dependency_rules`。
- `in_scope`、`out_of_scope`、`public_capabilities` 必须为非空列表。
- `out_of_scope` 必须显式声明本包不承担的能力。
- `package` 必须等于 canonical key：.NET 为 `.csproj` 文件名去扩展名，前端为 `package.json` 的 `name`。
- `dependency_rules.forbid` 列出禁止依赖；`allow` 非空时作为依赖白名单。
- charter 文本不得包含未来承诺语义。
- `stability` 取值限 `experimental`、`stable`、`deprecated`，缺省 `stable`。
- `compatibility` 与 `migration_ref` 可选：`compatibility` 为 semver 级承诺短文本，`migration_ref` 为指向 CHANGELOG 或契约版本的指针。

## 新增包流程

- 新能力落在某包 `in_scope` 时进入该包；命中某包 `out_of_scope` 但属另一包 `in_scope` 时进入另一包。
- 建立新包必须同时满足：单一职责可一句话表述且不属任何现有包 `in_scope`；存在至少两个消费方或明确的跨服务、跨应用复用；依赖边界独立；不与现有包 `public_capabilities` 命名空间冲突。
- 建立新包时必须同时提交 charter，并在 `out_of_scope` 中划清与相邻包的边界。

## 重叠处理

- `public_capabilities` 命名空间相交由校验拦截，必须重新划分。
- 语义级别的职责重叠由代码评审裁决，处理方式为合并、迁移能力或调整 `in_scope` 与 `out_of_scope`。

## 强制机制

- `tw-memory check` 校验存在性、schema、canonical key、依赖边界、公开能力互斥、占位词、来源 hash、secret-scan、提交边界与卡片预算，接入 pre-commit 钩子与 CI 闸门。
- `package-charter.yaml` 是该包职责与边界的唯一事实来源；`.tw-memory` 下的派生卡片由工具生成，禁止手工编辑，与 charter 不一致时以 charter 为准。

## 检查清单

- 每个共享包是否包含 `package-charter.yaml`？
- `out_of_scope` 是否非空且划清相邻边界？
- `package` 是否等于 canonical key？
- 实际依赖是否落在 `dependency_rules` 约束内？
- `public_capabilities` 是否与其他包互斥？
```

- [ ] **Step 2: 修改 `project-structure.md` 落点矩阵**

在 [project-structure.md](docs/engineering-standards/03-project-and-code/project-structure.md) 落点矩阵表格中，`.NET 后端公共构建块源码` 行之后增加一行：

```
| 共享包职责与边界声明 (charter) | `<package-root>/package-charter.yaml` |
```

- [ ] **Step 3: 修改 `project-structure.md` 文档约定**

将「文档约定」小节中这一句：

```
公共组件、SDK、框架库和跨团队工具必须额外说明适用范围、不适用范围、兼容性承诺、升级方式和迁移注意事项。
```

改为：

```
公共组件、SDK、框架库和跨团队工具的适用范围、不适用范围、依赖边界、兼容性承诺和迁移指针，必须在该包根目录的 `package-charter.yaml` 中声明，见《共享包 charter 规范》。
```

- [ ] **Step 4: 修改 `engineering-standards/README.md` 目录导航**

在「项目与编码规范」列表中 `[项目结构规范]` 一行之后增加：

```
- [共享包 charter 规范](03-project-and-code/shared-package-charter.md)
```

- [ ] **Step 5: Commit**

```bash
git add docs/engineering-standards/03-project-and-code/shared-package-charter.md \
        docs/engineering-standards/03-project-and-code/project-structure.md \
        docs/engineering-standards/README.md
git commit -m "docs(standards): add shared-package charter standard and matrix row"
```

---

## Task 14: ai-memory-design.md 接缝修订

**Files:**
- Modify: `docs/superpowers/specs/ai-memory-design.md`

将 charter 的接入写回记忆层设计，消除 `[manual] 或 README 派生` 的模糊接缝。

- [ ] **Step 1: 修订 package card 槽位与 provenance 说明**

在 [ai-memory-design.md](docs/superpowers/specs/ai-memory-design.md) 中：
- 「Card 结构」一节的 package card（类比 service card）说明里，将「职责」槽来源由 `[manual] 或 README 派生` 改为 `[manual]，来源为 package-charter.yaml 的 responsibility`，并增加「不适用范围」槽，来源为 charter 的 `out_of_scope`。
- 「Provenance 事实来源模型」中 `[manual]` 行的「来源」由「人工 decision card」扩为「人工 decision card 与 package charter」，并说明 charter 以 `source_type: manual`、`extractor: package-charter:v1`、`source_id: manual:package-charter:<package>` 登记。

- [ ] **Step 2: 修订生成管线与实施影响**

- 「生成管线」`tw-memory generate` 顺序第 1 步「扫描…包目录…」补充「读取各包 `package-charter.yaml`」。
- 「实施影响」清单补充三项：共享包 charter 规范、生成器解析 charter 填充卡片槽位、校验器 charter 闸门。

- [ ] **Step 3: Commit**

```bash
git add docs/superpowers/specs/ai-memory-design.md
git commit -m "docs(spec): wire package charter into memory layer design seams"
```

---

## Task 15: pre-commit 与 CI 闸门

**Files:**
- Create: `.pre-commit-config.yaml`
- Create: `.github/workflows/tw-memory.yml`

- [ ] **Step 1: 写 `.pre-commit-config.yaml`**

```yaml
repos:
  - repo: local
    hooks:
      - id: tw-memory-check
        name: tw-memory check (staged)
        entry: python -m tw_memory check --staged
        language: system
        pass_filenames: false
        always_run: true
```

- [ ] **Step 2: 写 `.github/workflows/tw-memory.yml`**

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
      - name: tw-memory check
        run: python -m tw_memory check
```

- [ ] **Step 3: 本地验证 hook 命令可运行**

Run（从仓库根，已 `pip install -e tools` 后）: `python -m tw_memory check`
Expected: 退出码 0，打印 `2 个包 charter 全部合规`。

> 若未安装为可编辑包，hook 的 `entry` 改为 `python tools/src/tw_memory/__main__.py`，但优先 `pip install -e tools` 以保证 `python -m tw_memory` 可用。

- [ ] **Step 4: Commit**

```bash
git add .pre-commit-config.yaml .github/workflows/tw-memory.yml
git commit -m "ci(tw-memory): wire check into pre-commit and github actions"
```

---

## Self-Review

**1. Spec coverage（对照 shared-package-charter-design.md）:**
- 受众分离与权威边界 → Task 11 README + Task 13 标准（人读）+ 卡片仅 AI（Task 8）。✓
- charter 字段 schema（含 compatibility/migration_ref）→ Task 5 + Task 12 charter + Task 13 标准。✓
- 规范落地（新标准、落点矩阵行、文档约定收敛、README 导航）→ Task 13。✓
- 记忆层接入（职责/不适用范围槽、provenance、生成管线、实施影响）→ Task 8 卡片 + Task 14 spec 修订。✓
- 硬闸门 8 条 → Task 10 实现 ①–⑧，另含 ⑨ 提交边界、⑩ 预算。✓
- 新增包流程与重叠处理 → Task 13 标准正文。✓
- 确定性、不依赖 CodeGraph → Task 9/10 全部从 charter+csproj+json+目录推导，无 CodeGraph 调用。✓
- 验证项（生成一致、删 charter 失败、forbid 失败、重叠失败、占位/密钥失败、key 不符失败、无 CodeGraph 一致、字节一致）→ Task 9/10/12 测试覆盖。✓

**2. Placeholder scan:** 计划内代码步骤均给出完整代码；正式规范文本（Task 13）遵守禁用未来承诺语义。无 TODO/TBD 占位。✓

**3. Type consistency:** `DiscoveredPackage(canonical_key, ecosystem, root_dir, charter_path, dependencies)` 在 Task 6/7/8/9/10 一致；`Charter` 字段在 Task 5/8/10 一致；`charter_source_id` 在 Task 7/8/10 同名同义；`run_generate(root)` / `run_check(root, staged=)` 在 cli.py(Task 0)、generate.py(Task 9)、check.py(Task 10) 签名一致。✓

> 已知注意点：Task 6 测试 import 用 `from conftest import make_csproj`（pytest 将测试目录加入 sys.path），Task 7/10 测试同此。执行时若环境未自动加入，改用 `from tests.conftest import make_csproj` 并确保 `tools` 为工作目录。
