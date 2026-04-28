# Platform Reuse Knowledge Indexing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a cross-language reuse knowledge mechanism where framework-layer public APIs are discoverable through generated indexes, reusable capabilities are represented in the formal graph, and `Tw.TestBase` test helpers are discoverable through its project README without entering the production API index.

**Architecture:** Extend the existing single-file Knowledge Compiler so the existing graph generation pipeline also emits a generated public API index. Keep the formal graph as the capability decision layer, keep public API index generation tool-owned, keep `Tw.TestBase` README-only, and keep semantic search markers as ordinary comments with format checks. Update discovery/router skills and standards so future agents use the new knowledge flow consistently.

**Tech Stack:** Python standard library, `unittest`, JSON generated indexes, constrained YAML subset, Markdown, PowerShell-compatible verification commands.

---

## Scope Check

The design covers one coherent Knowledge Compiler enhancement with four connected outputs:

1. Formal graph capability alignment.
2. Generated public API index.
3. `Tw.TestBase/README.md` test helper declaration.
4. Discovery/router/standards rules that consume those outputs.

The implementation should land as one feature branch because each part is needed for a complete reuse discovery loop. The API parser layer must be modular enough to add or adjust a language parser without rewriting graph validation or query code.

## Standards

Implementation must follow these standards:

- `rules.comments-common#rules` v1.2.0, `docs/standards/rules/comments-common.md`: semantic marker comments must be Simplified Chinese, concise, synchronized with code, and generated files must not receive manual comments.
- `rules.comments-dotnet#rules` v1.3.0, `docs/standards/rules/comments-dotnet.md`: public .NET APIs need XML comments that state purpose and contracts.
- `rules.comments-java#rules` v1.2.0, `docs/standards/rules/comments-java.md`: Java public APIs use Simplified Chinese Javadoc where needed.
- `rules.comments-python#rules` v1.2.0, `docs/standards/rules/comments-python.md`: Python public APIs use docstrings and internal complex logic uses comments.
- `rules.comments-ts#rules` v1.1.0, `docs/standards/rules/comments-ts.md`: frontend exported APIs use comments where contracts are not obvious.
- `rules.repo-layout#rules` v1.1.0, `docs/standards/rules/repo-layout.md`: generated indexes must live under generated paths and must not be edited manually.
- `rules.test-strategy#rules` v1.1.0, `docs/standards/rules/test-strategy.md`: every behavior change gets focused unit tests and command-level verification.
- `rules.test-data-mock#rules` v1.1.0, `docs/standards/rules/test-data-mock.md`: test helper declarations must avoid real secrets and PII.

## File Structure

Modify these files:

- `docs/standards/rules/comments-common.md`: add the cross-language semantic marker rule and checklist item.
- `docs/standards/rules/comments-dotnet.md`: mention `// 语义标记：关键词1 关键词2` for C# internal search markers.
- `docs/standards/rules/comments-java.md`: mention `// 语义标记：关键词1 关键词2` for Java internal search markers.
- `docs/standards/rules/comments-python.md`: mention `# 语义标记：关键词1 关键词2` for Python internal search markers.
- `docs/standards/rules/comments-ts.md`: mention `// 语义标记：关键词1 关键词2` for TypeScript internal search markers.
- `docs/standards/index.generated.json` and `docs/standards/_index/**/*.generated.json`: regenerate through `python tools/standards/standards.py generate-index`.
- `docs/knowledge/taxonomy.yaml`: add reusable framework/test helper diagnostics and optional query aliases for reuse-related terms.
- `docs/knowledge/graph/modules/backend.dotnet.building-blocks.core.yaml`: connect `Tw.Core` to concrete reusable capabilities.
- `tools/knowledge/knowledge.py`: add API index entry model, language public API extractors, index generation, index checks, `Tw.TestBase` README checks, semantic marker checks, and query integration.
- `tools/knowledge/rules/knowledge-diagnostics.md`: document new diagnostics.
- `docs/knowledge/README.md`: document public API index generation and `Tw.TestBase` README behavior.
- `tests/knowledge/test_knowledge_tool.py`: add all unit and command-level tests.
- `.agents/skills/tw-knowledge-discovery/SKILL.md`: update discovery flow to use graph, API index, and `Tw.TestBase/README.md`.
- `.agents/skills/tw-requirement-router/SKILL.md`: update routing flow to check reusable capabilities and API entries before implementation.

Create these files:

- `docs/knowledge/graph/capabilities/backend.capability.core-foundation.yaml`: core guard, primitives, extensions, timing, configuration, exceptions, and utility reuse capability.
- `docs/knowledge/graph/capabilities/backend.capability.cryptography.yaml`: hashing, HMAC, symmetric cryptography, RSA, PBKDF2, and string cryptography reuse capability.
- `docs/knowledge/graph/capabilities/backend.capability.reflection.yaml`: reflection cache and type finder reuse capability.
- `backend/dotnet/BuildingBlocks/tests/Tw.TestBase/README.md`: marker-based declaration for test bytes, test streams, temporary directory, and cryptography test vectors.

Generated by commands:

- `docs/knowledge/generated/api-index.generated.json`: aggregate public API index.
- `docs/knowledge/generated/api-index/*.generated.json`: per-module public API indexes.
- `docs/knowledge/generated/**/*.generated.json`: existing graph indexes regenerated by `python tools/knowledge/knowledge.py generate`.

Do not create a formal graph module for `Tw.TestBase` in this plan. `Tw.TestBase` is README-only for discovery and is explicitly excluded from generated public API indexes.

## Implementation Tasks

### Task 1: Add Semantic Marker Standards

**Files:**
- Modify: `docs/standards/rules/comments-common.md`
- Modify: `docs/standards/rules/comments-dotnet.md`
- Modify: `docs/standards/rules/comments-java.md`
- Modify: `docs/standards/rules/comments-python.md`
- Modify: `docs/standards/rules/comments-ts.md`
- Generated: `docs/standards/index.generated.json`
- Generated: `docs/standards/_index/**/*.generated.json`

- [ ] **Step 1: Add the common semantic marker rule**

In `docs/standards/rules/comments-common.md`, increment `version` from `1.2.0` to `1.3.0`, update the summary if needed, add this rule after the existing internal comment rule:

```markdown
15. 仅用于增强搜索的内部语义标记必须使用 `语义标记：` 前缀，后跟 2 到 8 个简体中文关键词或必要技术缩写；语义标记只用于 `rg` 搜索，不得作为 public API、README 或正式图谱的替代契约
```

Add this example under the examples section:

```markdown
正例：

```csharp
// 语义标记：SHA3 回退哈希 流式读取 跨平台兼容
```
```

Add this checklist item:

```markdown
- 内部语义标记是否使用 `语义标记：` 前缀，且只用于增强搜索而不是声明复用契约。
```

Add this changelog row at the top of the changelog table:

```markdown
| 1.3.0 | 2026-04-29 | 补充跨语言语义标记注释规则，用于增强 `rg` 搜索但不改变复用契约。 |
```

- [ ] **Step 2: Add language-specific semantic marker notes**

In `docs/standards/rules/comments-dotnet.md`, increment `version` from `1.3.0` to `1.4.0`, add this rule before generated-code rule:

```markdown
13. C# 内部复杂逻辑需要增强搜索时，可以使用 `// 语义标记：关键词1 关键词2`；该标记只服务 `rg` 搜索，不得替代 XML 文档注释或正式能力声明。
```

Update the generated-code rule number from `13` to `14`.

Add this changelog row:

```markdown
| 1.4.0 | 2026-04-29 | 补充 C# 语义标记注释约定，用于增强内部逻辑搜索。 |
```

In `docs/standards/rules/comments-java.md`, increment `version` from `1.2.0` to `1.3.0`, add this rule before generated-code rule:

```markdown
13. Java 内部复杂逻辑需要增强搜索时，可以使用 `// 语义标记：关键词1 关键词2`；该标记只服务 `rg` 搜索，不得替代 Javadoc 或正式能力声明。
```

Add this changelog row:

```markdown
| 1.3.0 | 2026-04-29 | 补充 Java 语义标记注释约定，用于增强内部逻辑搜索。 |
```

In `docs/standards/rules/comments-python.md`, increment `version` from `1.2.0` to `1.3.0`, add this rule before generated-code rule:

```markdown
14. Python 内部复杂逻辑需要增强搜索时，可以使用 `# 语义标记：关键词1 关键词2`；该标记只服务 `rg` 搜索，不得替代 docstring 或正式能力声明。
```

Add this changelog row:

```markdown
| 1.3.0 | 2026-04-29 | 补充 Python 语义标记注释约定，用于增强内部逻辑搜索。 |
```

In `docs/standards/rules/comments-ts.md`, increment `version` from `1.1.0` to `1.2.0`, add this rule before generated-code rule:

```markdown
13. TypeScript 内部复杂逻辑需要增强搜索时，可以使用 `// 语义标记：关键词1 关键词2`；该标记只服务 `rg` 搜索，不得替代导出 API 文档或正式能力声明。
```

Add this changelog row:

```markdown
| 1.2.0 | 2026-04-29 | 补充 TypeScript 语义标记注释约定，用于增强内部逻辑搜索。 |
```

- [ ] **Step 3: Regenerate standards index**

Run:

```powershell
python tools\standards\standards.py generate-index
```

Expected: command exits `0` and updates `docs/standards/index.generated.json` plus matching `_index` shards.

- [ ] **Step 4: Verify standards**

Run:

```powershell
python tools\standards\standards.py check
```

Expected: `OK standards checks passed`.

- [ ] **Step 5: Commit standards changes**

Run:

```powershell
git add docs\standards
git commit -m "docs: add semantic marker comment standards"
```

Expected: commit succeeds.

### Task 2: Add Graph Capabilities And Tw.TestBase Declaration

**Files:**
- Create: `docs/knowledge/graph/capabilities/backend.capability.core-foundation.yaml`
- Create: `docs/knowledge/graph/capabilities/backend.capability.cryptography.yaml`
- Create: `docs/knowledge/graph/capabilities/backend.capability.reflection.yaml`
- Modify: `docs/knowledge/graph/modules/backend.dotnet.building-blocks.core.yaml`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.TestBase/README.md`
- Test: `tests/knowledge/test_knowledge_tool.py`

- [ ] **Step 1: Write failing graph alignment test**

Add this test to `KnowledgeToolTests`:

```python
    def test_core_module_provides_reuse_capabilities(self):
        nodes, messages = knowledge.load_graph_nodes()
        self.assertEqual([], [message for message in messages if message.level == "ERROR"], diagnostic_text(messages))
        by_id = {str(node.data.get("id")): node.data for node in nodes}

        core = by_id["backend.dotnet.building-blocks.core"]
        self.assertEqual(
            [
                "backend.capability.core-foundation",
                "backend.capability.cryptography",
                "backend.capability.reflection",
            ],
            core["provides"]["capabilities"],
        )
        self.assertIn("backend.capability.core-foundation", by_id)
        self.assertIn("backend.capability.cryptography", by_id)
        self.assertIn("backend.capability.reflection", by_id)
```

Run:

```powershell
python -m unittest tests.knowledge.test_knowledge_tool.KnowledgeToolTests.test_core_module_provides_reuse_capabilities -v
```

Expected: FAIL because the capabilities do not exist and `Tw.Core` has no `provides.capabilities`.

- [ ] **Step 2: Add failing Tw.TestBase README declaration test**

Add this test:

```python
    def test_testbase_readme_declares_test_shared_capabilities(self):
        readme = knowledge.REPO_ROOT / "backend/dotnet/BuildingBlocks/tests/Tw.TestBase/README.md"

        self.assertTrue(readme.exists(), "Tw.TestBase README.md 必须存在")
        content = readme.read_text(encoding="utf-8")

        expected_markers = [
            "标记：测试共享能力 测试字节 UTF-8 固定字节",
            "标记：测试共享能力 测试流 MemoryStream 文本流 字节流",
            "标记：测试共享能力 临时目录 文件写入 自动清理",
            "标记：测试共享能力 加密测试向量 固定盐 固定密钥 固定 IV",
        ]
        for marker in expected_markers:
            self.assertIn(marker, content)

        for entry in ["TestBytes.cs", "TestStreams.cs", "TemporaryDirectory.cs", "CryptoTestVectors.cs"]:
            self.assertIn(f"入口：{entry}", content)
            self.assertTrue((readme.parent / entry).exists(), entry)
```

Run:

```powershell
python -m unittest tests.knowledge.test_knowledge_tool.KnowledgeToolTests.test_testbase_readme_declares_test_shared_capabilities -v
```

Expected: FAIL because `Tw.TestBase/README.md` does not exist yet.

- [ ] **Step 3: Create core-foundation capability**

Create `docs/knowledge/graph/capabilities/backend.capability.core-foundation.yaml` with exactly this structure, adjusting only text if a validation error identifies a required repository convention:

```yaml
schema_version: 1.0.0
id: backend.capability.core-foundation
kind: capability
name: 核心基础能力
status: active
summary: 提供参数检查、基础类型、扩展方法、配置标记、时钟抽象、异常基类、随机数和释放辅助等后端通用基础能力。
owners:
  - platform
tags:
  - backend
  - dotnet
  - core
  - platform
domain: platform
provided_by:
  modules:
    - backend.dotnet.building-blocks.core
reuse:
  use_when:
    - 需要参数检查、通用扩展方法、基础类型、时钟抽象、随机数或释放辅助时
  do_not_reimplement:
    - 不要在服务或业务模块中重复编写基础参数检查、常用扩展方法或通用释放辅助
aliases:
  - 核心基础
  - 参数检查
  - 扩展方法
  - 随机数
  - 释放辅助
source:
  declared_in: docs/knowledge/graph/capabilities/backend.capability.core-foundation.yaml
  evidence:
    - backend/dotnet/BuildingBlocks/src/Tw.Core
provenance:
  created_by: ai-assisted:tw-knowledge-maintenance
  created_at: 2026-04-29
  updated_by: ai-assisted:tw-knowledge-maintenance
  updated_at: 2026-04-29
```

- [ ] **Step 4: Create cryptography capability**

Create `docs/knowledge/graph/capabilities/backend.capability.cryptography.yaml`:

```yaml
schema_version: 1.0.0
id: backend.capability.cryptography
kind: capability
name: 加密与哈希能力
status: active
summary: 提供哈希、HMAC、AES、DES、TripleDES、RSA、PBKDF2、字符串加解密和加密测试边界相关的通用安全能力。
owners:
  - platform
tags:
  - backend
  - dotnet
  - security
  - cryptography
domain: security
provided_by:
  modules:
    - backend.dotnet.building-blocks.core
reuse:
  use_when:
    - 需要计算哈希、验证哈希、执行 HMAC、对称加解密、RSA 加解密签名或 PBKDF2 密码哈希时
  do_not_reimplement:
    - 不要在服务内直接拼装底层加密算法调用，应优先复用 Tw.Core.Security.Cryptography 中的公共入口
aliases:
  - 加密
  - 哈希
  - HMAC
  - AES
  - RSA
  - PBKDF2
source:
  declared_in: docs/knowledge/graph/capabilities/backend.capability.cryptography.yaml
  evidence:
    - backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography
provenance:
  created_by: ai-assisted:tw-knowledge-maintenance
  created_at: 2026-04-29
  updated_by: ai-assisted:tw-knowledge-maintenance
  updated_at: 2026-04-29
```

- [ ] **Step 5: Create reflection capability**

Create `docs/knowledge/graph/capabilities/backend.capability.reflection.yaml`:

```yaml
schema_version: 1.0.0
id: backend.capability.reflection
kind: capability
name: 反射与类型发现能力
status: active
summary: 提供反射缓存、异步返回类型识别、特性读取、构造函数缓存、接口缓存和程序集类型发现能力。
owners:
  - platform
tags:
  - backend
  - dotnet
  - reflection
  - platform
domain: platform
provided_by:
  modules:
    - backend.dotnet.building-blocks.core
reuse:
  use_when:
    - 需要扫描程序集类型、读取特性、判断异步方法或缓存反射结果时
  do_not_reimplement:
    - 不要在业务模块中重复编写无缓存的反射扫描或类型发现逻辑
aliases:
  - 反射
  - 类型发现
  - 反射缓存
source:
  declared_in: docs/knowledge/graph/capabilities/backend.capability.reflection.yaml
  evidence:
    - backend/dotnet/BuildingBlocks/src/Tw.Core/Reflection
provenance:
  created_by: ai-assisted:tw-knowledge-maintenance
  created_at: 2026-04-29
  updated_by: ai-assisted:tw-knowledge-maintenance
  updated_at: 2026-04-29
```

- [ ] **Step 6: Link Tw.Core module to capabilities**

Modify `docs/knowledge/graph/modules/backend.dotnet.building-blocks.core.yaml`:

```yaml
summary: 承载后端通用参数检查、基础类型、扩展方法、反射、加密与哈希、随机数、配置标记、时钟抽象和异常基类，是服务和框架包复用底层能力的基础依赖。
tags:
  - backend
  - dotnet
  - core
  - cryptography
  - reflection
provides:
  capabilities:
    - backend.capability.core-foundation
    - backend.capability.cryptography
    - backend.capability.reflection
```

Keep existing `path`, `source`, and `provenance` fields, but set:

```yaml
  updated_by: ai-assisted:tw-knowledge-maintenance
  updated_at: 2026-04-29
```

- [ ] **Step 7: Create Tw.TestBase README declaration**

Create `backend/dotnet/BuildingBlocks/tests/Tw.TestBase/README.md`:

```markdown
# Tw.TestBase 测试共享能力

## 复用规则

编写测试前先检查本文件，优先复用已有测试辅助工具

## 能力声明

### 测试字节

标记：测试共享能力 测试字节 UTF-8 固定字节
入口：TestBytes.cs
关键词：TestBytes DeterministicBytes Utf8
适用：需要稳定字节数组、UTF-8 编码文本或固定长度测试字节

### 测试流

标记：测试共享能力 测试流 MemoryStream 文本流 字节流
入口：TestStreams.cs
关键词：TestStreams FromText FromBytes
适用：需要从文本或字节快速构造 MemoryStream

### 临时目录

标记：测试共享能力 临时目录 文件写入 自动清理
入口：TemporaryDirectory.cs
关键词：TemporaryDirectory WriteAllText GetPath
适用：需要隔离文件系统副作用并在测试结束后清理

### 加密测试向量

标记：测试共享能力 加密测试向量 固定盐 固定密钥 固定 IV
入口：CryptoTestVectors.cs
关键词：CryptoTestVectors Salt Key128 Key256 Iv128
适用：需要稳定加密测试输入，避免测试中生成临时密钥
```

- [ ] **Step 8: Run focused tests**

Run:

```powershell
python -m unittest tests.knowledge.test_knowledge_tool.KnowledgeToolTests.test_core_module_provides_reuse_capabilities tests.knowledge.test_knowledge_tool.KnowledgeToolTests.test_testbase_readme_declares_test_shared_capabilities -v
```

Expected: both tests pass.

- [ ] **Step 9: Commit graph and README declarations**

Run:

```powershell
git add docs\knowledge\graph\capabilities docs\knowledge\graph\modules\backend.dotnet.building-blocks.core.yaml backend\dotnet\BuildingBlocks\tests\Tw.TestBase\README.md tests\knowledge\test_knowledge_tool.py
git commit -m "docs: declare platform reuse capabilities"
```

Expected: commit succeeds.

### Task 3: Add API Index Data Model And Language Extractors

**Files:**
- Modify: `tools/knowledge/knowledge.py`
- Modify: `tests/knowledge/test_knowledge_tool.py`

- [ ] **Step 1: Add failing extractor tests**

Add these helpers near existing test helpers:

```python
def write_reuse_taxonomy(root: Path) -> None:
    write_taxonomy(root)


def write_capability_node(root: Path, capability_id: str, name: str, evidence: str) -> None:
    write_file(
        root,
        f"docs/knowledge/graph/capabilities/{capability_id}.yaml",
        f"""
        schema_version: 1.0.0
        id: {capability_id}
        kind: capability
        name: {name}
        status: active
        summary: {name}。
        owners:
          - platform
        tags:
          - backend
        domain: platform
        provided_by:
          modules:
            - backend.dotnet.building-blocks.core
        source:
          declared_in: docs/knowledge/graph/capabilities/{capability_id}.yaml
          evidence:
            - {evidence}
        provenance:
          created_by: human
          created_at: 2026-04-29
          updated_by: human
          updated_at: 2026-04-29
        """,
    )
```

Add this test:

```python
    def test_dotnet_public_api_extractor_filters_public_surface(self):
        with isolated_repo() as root:
            write_reuse_taxonomy(root)
            write_capability_node(root, "backend.capability.cryptography", "加密能力", "backend/dotnet/BuildingBlocks/src/Tw.Core/Security")
            write_file(
                root,
                "docs/knowledge/graph/modules/backend.dotnet.building-blocks.core.yaml",
                """
                schema_version: 1.0.0
                id: backend.dotnet.building-blocks.core
                kind: module
                name: 核心基础构件
                status: active
                summary: 提供加密与哈希能力。
                owners:
                  - platform
                tags:
                  - backend
                  - dotnet
                module_type: building-block
                stack: dotnet
                path: backend/dotnet/BuildingBlocks/src/Tw.Core
                provides:
                  capabilities:
                    - backend.capability.cryptography
                source:
                  declared_in: docs/knowledge/graph/modules/backend.dotnet.building-blocks.core.yaml
                  evidence:
                    - backend/dotnet/BuildingBlocks/src/Tw.Core
                provenance:
                  created_by: human
                  created_at: 2026-04-29
                  updated_by: human
                  updated_at: 2026-04-29
                """,
            )
            write_file(
                root,
                "backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/AesCryptography.cs",
                """
                namespace Tw.Core.Security.Cryptography;

                /// <summary>提供 AES 加密入口</summary>
                public static class AesCryptography
                {
                    /// <summary>加密字符串内容</summary>
                    public static string Encrypt(string input, byte[] key)
                    {
                        return input;
                    }

                    internal static string EncryptInternal(string input)
                    {
                        return input;
                    }

                    private static string EncryptPrivate(string input)
                    {
                        return input;
                    }
                }
                """,
            )

            nodes, messages = knowledge.load_graph_nodes()
            self.assertEqual([], [message for message in messages if message.level == "ERROR"], diagnostic_text(messages))
            entries, diagnostics = knowledge.build_public_api_entries(nodes)

            self.assertEqual([], diagnostics, diagnostic_text(diagnostics))
            ids = [entry["id"] for entry in entries]
            self.assertIn("dotnet:Tw.Core.Security.Cryptography.AesCryptography", ids)
            self.assertIn("dotnet:Tw.Core.Security.Cryptography.AesCryptography.Encrypt", ids)
            self.assertNotIn("dotnet:Tw.Core.Security.Cryptography.AesCryptography.EncryptInternal", ids)
            self.assertNotIn("dotnet:Tw.Core.Security.Cryptography.AesCryptography.EncryptPrivate", ids)
            encrypt = next(entry for entry in entries if entry["member_name"] == "Encrypt")
            self.assertEqual(["backend.capability.cryptography"], encrypt["capability_ids"])
            self.assertEqual("production", encrypt["usage_scope"])
            self.assertEqual("加密字符串内容", encrypt["summary"])
```

Run:

```powershell
python -m unittest tests.knowledge.test_knowledge_tool.KnowledgeToolTests.test_dotnet_public_api_extractor_filters_public_surface -v
```

Expected: FAIL with `AttributeError` because `build_public_api_entries` does not exist.

- [ ] **Step 2: Add extractor model and constants**

In `tools/knowledge/knowledge.py`, add this dataclass after `GraphNode`:

```python
@dataclass(frozen=True)
class PublicApiEntry:
    id: str
    stack: str
    module_id: str
    capability_ids: list[str]
    usage_scope: str
    symbol_kind: str
    namespace: str
    type_name: str
    member_name: str
    signature: str
    summary: str
    path: str
    keywords: list[str]

    def to_json(self) -> dict[str, Any]:
        return {
            "id": self.id,
            "stack": self.stack,
            "module_id": self.module_id,
            "capability_ids": self.capability_ids,
            "usage_scope": self.usage_scope,
            "symbol_kind": self.symbol_kind,
            "namespace": self.namespace,
            "type_name": self.type_name,
            "member_name": self.member_name,
            "signature": self.signature,
            "summary": self.summary,
            "path": self.path,
            "keywords": self.keywords,
        }
```

Add these constants near existing constants:

```python
API_INDEX_DIR_NAME = "api-index"
TESTBASE_PATH = "backend/dotnet/BuildingBlocks/tests/Tw.TestBase"
SEMANTIC_MARKER_PREFIX = "语义标记："
```

- [ ] **Step 3: Add source filtering helpers**

Add these functions near path helpers:

```python
def is_generated_or_build_path(path: Path) -> bool:
    parts = {part.lower() for part in path.parts}
    return bool(parts & {"bin", "obj", "generated", ".git", "node_modules"})


def is_testbase_path(path: str) -> bool:
    return normalized_repo_path(path).startswith(TESTBASE_PATH)


def module_capability_ids(node: GraphNode) -> list[str]:
    provides = node.data.get("provides")
    if not isinstance(provides, dict):
        return []
    capabilities = provides.get("capabilities")
    if not isinstance(capabilities, list):
        return []
    return [str(value) for value in capabilities]


def public_api_modules(nodes: list[GraphNode]) -> list[GraphNode]:
    supported_types = {"building-block", "framework-package", "frontend-package"}
    result: list[GraphNode] = []
    for node in nodes:
        if node.data.get("kind") != "module":
            continue
        module_path = normalized_repo_path(str(node.data.get("path") or ""))
        if not module_path or is_testbase_path(module_path):
            continue
        if node.data.get("module_type") not in supported_types:
            continue
        result.append(node)
    return sorted(result, key=lambda item: str(item.data.get("id", "")))
```

- [ ] **Step 4: Add .NET public API extractor**

Add a focused parser near other Knowledge Compiler helper functions:

```python
DOTNET_NAMESPACE_RE = re.compile(r"^\s*namespace\s+([A-Za-z_][A-Za-z0-9_.]*)\s*[;{]")
DOTNET_TYPE_RE = re.compile(
    r"^\s*public\s+(?:(?:static|sealed|abstract|partial|readonly)\s+)*(class|interface|record|struct|enum)\s+([A-Za-z_][A-Za-z0-9_]*)"
)
DOTNET_MEMBER_RE = re.compile(
    r"^\s*public\s+(?:(?:static|virtual|override|sealed|new|async|partial|readonly)\s+)*([A-Za-z_][A-Za-z0-9_<>,\[\].? ]*)\s+([A-Za-z_][A-Za-z0-9_]*)\s*\("
)
DOTNET_PROPERTY_RE = re.compile(
    r"^\s*public\s+(?:(?:static|virtual|override|sealed|new|readonly|required)\s+)*([A-Za-z_][A-Za-z0-9_<>,\[\].? ]*)\s+([A-Za-z_][A-Za-z0-9_]*)\s*\{"
)


def xml_summary_before(lines: list[str], index: int) -> str:
    summary_lines: list[str] = []
    cursor = index - 1
    while cursor >= 0 and lines[cursor].lstrip().startswith("///"):
        text = lines[cursor].strip()[3:].strip()
        summary_lines.append(text)
        cursor -= 1
    summary_lines.reverse()
    joined = " ".join(summary_lines)
    joined = re.sub(r"</?summary>", "", joined).strip()
    return re.sub(r"\s+", " ", joined)


def dotnet_signature(line: str) -> str:
    return re.sub(r"\s+", " ", line.strip().rstrip("{").strip())


def api_keywords(*values: str) -> list[str]:
    words: set[str] = set()
    for value in values:
        for token in re.split(r"[^A-Za-z0-9_\u4e00-\u9fff]+", value):
            if token:
                words.add(token)
    return sorted(words)


def extract_dotnet_public_api(node: GraphNode) -> tuple[list[PublicApiEntry], list[Diagnostic]]:
    module_id = str(node.data.get("id", ""))
    module_path = REPO_ROOT / normalized_repo_path(str(node.data.get("path") or ""))
    capabilities = module_capability_ids(node)
    diagnostics: list[Diagnostic] = []
    entries: list[PublicApiEntry] = []
    if not module_path.exists():
        return entries, diagnostics

    for path in sorted(module_path.rglob("*.cs")):
        if is_generated_or_build_path(path):
            continue
        relative = rel_path(path)
        lines = read_text(path).splitlines()
        namespace = ""
        current_type = ""
        for index, line in enumerate(lines):
            namespace_match = DOTNET_NAMESPACE_RE.match(line)
            if namespace_match:
                namespace = namespace_match.group(1)
                continue

            type_match = DOTNET_TYPE_RE.match(line)
            if type_match:
                current_type = type_match.group(2)
                symbol_id = f"dotnet:{namespace}.{current_type}" if namespace else f"dotnet:{current_type}"
                summary = xml_summary_before(lines, index)
                entries.append(
                    PublicApiEntry(
                        id=symbol_id,
                        stack="dotnet",
                        module_id=module_id,
                        capability_ids=capabilities,
                        usage_scope="production",
                        symbol_kind=type_match.group(1),
                        namespace=namespace,
                        type_name=current_type,
                        member_name="",
                        signature=dotnet_signature(line),
                        summary=summary,
                        path=relative,
                        keywords=api_keywords(namespace, current_type, summary),
                    )
                )
                continue

            member_match = DOTNET_MEMBER_RE.match(line) or DOTNET_PROPERTY_RE.match(line)
            if member_match and current_type:
                member_name = member_match.group(2)
                symbol_id = f"dotnet:{namespace}.{current_type}.{member_name}" if namespace else f"dotnet:{current_type}.{member_name}"
                summary = xml_summary_before(lines, index)
                entries.append(
                    PublicApiEntry(
                        id=symbol_id,
                        stack="dotnet",
                        module_id=module_id,
                        capability_ids=capabilities,
                        usage_scope="production",
                        symbol_kind="member",
                        namespace=namespace,
                        type_name=current_type,
                        member_name=member_name,
                        signature=dotnet_signature(line),
                        summary=summary,
                        path=relative,
                        keywords=api_keywords(namespace, current_type, member_name, summary),
                    )
                )
    return entries, diagnostics
```

- [ ] **Step 5: Add Java, Python, Go, and TypeScript extractors**

Add minimal public-surface parsers:

```python
JAVA_PACKAGE_RE = re.compile(r"^\s*package\s+([A-Za-z_][A-Za-z0-9_.]*)\s*;")
JAVA_TYPE_RE = re.compile(r"^\s*public\s+(class|interface|record|enum)\s+([A-Za-z_][A-Za-z0-9_]*)")
JAVA_METHOD_RE = re.compile(r"^\s*public\s+(?:static\s+)?[A-Za-z_][A-Za-z0-9_<>,\[\].? ]*\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(")
PY_PUBLIC_RE = re.compile(r"^(def|class)\s+([A-Za-z][A-Za-z0-9_]*)\s*[\(:]")
GO_PUBLIC_RE = re.compile(r"^(func|type|const|var)\s+([A-Z][A-Za-z0-9_]*)")
TS_EXPORT_RE = re.compile(r"^\s*export\s+(class|interface|type|function|const|let|var)\s+([A-Za-z_][A-Za-z0-9_]*)")
```

Add this shared extractor:

```python
def preceding_line_summary(lines: list[str], index: int) -> str:
    cursor = index - 1
    while cursor >= 0 and not lines[cursor].strip():
        cursor -= 1
    if cursor < 0:
        return ""
    text = lines[cursor].strip()
    for prefix in ("///", "//", "#", "*"):
        if text.startswith(prefix):
            return text[len(prefix):].strip().strip("/").strip("*").strip()
    if text.startswith('"""') or text.startswith("'''"):
        return text.strip('"').strip("'").strip()
    return ""


def extract_regex_public_api(
    node: GraphNode,
    stack: str,
    suffixes: set[str],
    symbol_pattern: re.Pattern[str],
    namespace_pattern: re.Pattern[str] | None = None,
) -> tuple[list[PublicApiEntry], list[Diagnostic]]:
    module_id = str(node.data.get("id", ""))
    module_path = REPO_ROOT / normalized_repo_path(str(node.data.get("path") or ""))
    capabilities = module_capability_ids(node)
    entries: list[PublicApiEntry] = []
    if not module_path.exists():
        return entries, []

    for path in sorted(module_path.rglob("*")):
        if not path.is_file() or path.suffix not in suffixes or is_generated_or_build_path(path):
            continue
        relative = rel_path(path)
        lines = read_text(path).splitlines()
        namespace = ""
        for index, line in enumerate(lines):
            if namespace_pattern:
                namespace_match = namespace_pattern.match(line)
                if namespace_match:
                    namespace = namespace_match.group(1)
                    continue
            match = symbol_pattern.match(line)
            if not match:
                continue
            symbol_kind = match.group(1)
            symbol_name = match.group(2)
            prefix = f"{namespace}." if namespace else ""
            entry_id = f"{stack}:{prefix}{symbol_name}"
            summary = preceding_line_summary(lines, index)
            entries.append(
                PublicApiEntry(
                    id=entry_id,
                    stack=stack,
                    module_id=module_id,
                    capability_ids=capabilities,
                    usage_scope="production",
                    symbol_kind=symbol_kind,
                    namespace=namespace,
                    type_name=symbol_name if symbol_kind in {"class", "interface", "record", "enum", "type"} else "",
                    member_name=symbol_name if symbol_kind not in {"class", "interface", "record", "enum", "type"} else "",
                    signature=re.sub(r"\s+", " ", line.strip()),
                    summary=summary,
                    path=relative,
                    keywords=api_keywords(namespace, symbol_name, summary),
                )
            )
    return entries, []
```

Add the language functions:

```python
def extract_java_public_api(node: GraphNode) -> tuple[list[PublicApiEntry], list[Diagnostic]]:
    return extract_regex_public_api(node, "java", {".java"}, JAVA_TYPE_RE, JAVA_PACKAGE_RE)


def extract_python_public_api(node: GraphNode) -> tuple[list[PublicApiEntry], list[Diagnostic]]:
    return extract_regex_public_api(node, "python", {".py"}, PY_PUBLIC_RE)


def extract_go_public_api(node: GraphNode) -> tuple[list[PublicApiEntry], list[Diagnostic]]:
    return extract_regex_public_api(node, "go", {".go"}, GO_PUBLIC_RE)


def extract_typescript_public_api(node: GraphNode) -> tuple[list[PublicApiEntry], list[Diagnostic]]:
    return extract_regex_public_api(node, "vue-ts", {".ts", ".tsx", ".vue"}, TS_EXPORT_RE)
```

- [ ] **Step 6: Add dispatcher and builder**

Add:

```python
def extract_public_api_for_module(node: GraphNode) -> tuple[list[PublicApiEntry], list[Diagnostic]]:
    stack = str(node.data.get("stack") or "")
    if stack == "dotnet":
        return extract_dotnet_public_api(node)
    if stack == "java":
        return extract_java_public_api(node)
    if stack == "python":
        return extract_python_public_api(node)
    if stack == "go":
        return extract_go_public_api(node)
    if stack in {"vue-ts", "uniapp"}:
        return extract_typescript_public_api(node)
    return [], []


def build_public_api_entries(nodes: list[GraphNode]) -> tuple[list[dict[str, Any]], list[Diagnostic]]:
    entries: list[PublicApiEntry] = []
    messages: list[Diagnostic] = []
    for node in public_api_modules(nodes):
        module_entries, module_messages = extract_public_api_for_module(node)
        entries.extend(module_entries)
        messages.extend(module_messages)
        if module_entries and not module_capability_ids(node):
            messages.append(
                warn(
                    "knowledge.api-index-unassigned",
                    str(node.data.get("path") or node.path),
                    "public API 无法归属到 capability。",
                    "请在对应 module 节点中补充 provides.capabilities。",
                )
            )
    return [entry.to_json() for entry in sorted(entries, key=lambda item: item.id)], messages
```

- [ ] **Step 7: Run extractor test**

Run:

```powershell
python -m unittest tests.knowledge.test_knowledge_tool.KnowledgeToolTests.test_dotnet_public_api_extractor_filters_public_surface -v
```

Expected: PASS.

- [ ] **Step 8: Commit extractor model**

Run:

```powershell
git add tools\knowledge\knowledge.py tests\knowledge\test_knowledge_tool.py
git commit -m "feat: extract public API reuse entries"
```

Expected: commit succeeds.

### Task 4: Generate And Check Public API Indexes

**Files:**
- Modify: `tools/knowledge/knowledge.py`
- Modify: `tests/knowledge/test_knowledge_tool.py`
- Generated: `docs/knowledge/generated/api-index.generated.json`
- Generated: `docs/knowledge/generated/api-index/*.generated.json`

- [ ] **Step 1: Add failing generation test**

Add:

```python
    def test_generate_writes_public_api_indexes_and_excludes_testbase(self):
        with isolated_repo() as root:
            write_reuse_taxonomy(root)
            write_capability_node(root, "backend.capability.cryptography", "加密能力", "backend/dotnet/BuildingBlocks/src/Tw.Core/Security")
            write_file(
                root,
                "docs/knowledge/graph/modules/backend.dotnet.building-blocks.core.yaml",
                """
                schema_version: 1.0.0
                id: backend.dotnet.building-blocks.core
                kind: module
                name: 核心基础构件
                status: active
                summary: 提供加密与哈希能力。
                owners:
                  - platform
                tags:
                  - backend
                  - dotnet
                module_type: building-block
                stack: dotnet
                path: backend/dotnet/BuildingBlocks/src/Tw.Core
                provides:
                  capabilities:
                    - backend.capability.cryptography
                source:
                  declared_in: docs/knowledge/graph/modules/backend.dotnet.building-blocks.core.yaml
                  evidence:
                    - backend/dotnet/BuildingBlocks/src/Tw.Core
                provenance:
                  created_by: human
                  created_at: 2026-04-29
                  updated_by: human
                  updated_at: 2026-04-29
                """,
            )
            write_file(
                root,
                "backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/AesCryptography.cs",
                """
                namespace Tw.Core.Security.Cryptography;
                /// <summary>提供 AES 加密入口</summary>
                public static class AesCryptography
                {
                    /// <summary>加密字符串内容</summary>
                    public static string Encrypt(string input, byte[] key) => input;
                }
                """,
            )
            write_file(
                root,
                "backend/dotnet/BuildingBlocks/tests/Tw.TestBase/TestBytes.cs",
                """
                namespace Tw.TestBase;
                public static class TestBytes
                {
                    public static byte[] Utf8(string text) => [];
                }
                """,
            )

            exit_code = knowledge.command_generate(argparse.Namespace())

            self.assertEqual(0, exit_code)
            aggregate_path = root / "docs/knowledge/generated/api-index.generated.json"
            module_path = root / "docs/knowledge/generated/api-index/dotnet.backend.dotnet.building-blocks.core.generated.json"
            self.assertTrue(aggregate_path.exists())
            self.assertTrue(module_path.exists())
            aggregate = json.loads(aggregate_path.read_text(encoding="utf-8"))
            ids = [entry["id"] for entry in aggregate["apis"]]
            self.assertIn("dotnet:Tw.Core.Security.Cryptography.AesCryptography.Encrypt", ids)
            self.assertNotIn("dotnet:Tw.TestBase.TestBytes.Utf8", ids)
```

Run:

```powershell
python -m unittest tests.knowledge.test_knowledge_tool.KnowledgeToolTests.test_generate_writes_public_api_indexes_and_excludes_testbase -v
```

Expected: FAIL because generated API index files are not created.

- [ ] **Step 2: Add API index payload builder**

Add:

```python
def api_index_path(stack: str, module_id: str) -> str:
    return generated_path(API_INDEX_DIR_NAME, f"{stack}.{module_id}.generated.json")


def build_api_index_payloads(nodes: list[GraphNode], generated_at: str) -> tuple[dict[str, Any], list[Diagnostic]]:
    entries, messages = build_public_api_entries(nodes)
    grouped: dict[tuple[str, str], list[dict[str, Any]]] = {}
    for entry in entries:
        grouped.setdefault((entry["stack"], entry["module_id"]), []).append(entry)

    payloads: dict[str, Any] = {
        generated_path("api-index.generated.json"): {
            **generator_metadata(generated_at),
            "apis": entries,
        }
    }
    for (stack, module_id), module_entries in sorted(grouped.items()):
        payloads[api_index_path(stack, module_id)] = {
            **generator_metadata(generated_at),
            "module_id": module_id,
            "stack": stack,
            "apis": sorted(module_entries, key=lambda item: item["id"]),
        }
    return payloads, messages
```

- [ ] **Step 3: Merge API payloads into build_indexes**

In `build_indexes`, after section indexes are created and before `return payloads, messages`, add:

```python
    api_payloads, api_messages = build_api_index_payloads(sorted_nodes, generated_at)
    payloads.update(api_payloads)
    messages.extend(api_messages)
```

- [ ] **Step 4: Add stale check test**

Add:

```python
    def test_check_detects_stale_public_api_index(self):
        with isolated_repo() as root:
            write_reuse_taxonomy(root)
            write_capability_node(root, "backend.capability.cryptography", "加密能力", "backend/dotnet/BuildingBlocks/src/Tw.Core/Security")
            write_file(
                root,
                "docs/knowledge/graph/modules/backend.dotnet.building-blocks.core.yaml",
                """
                schema_version: 1.0.0
                id: backend.dotnet.building-blocks.core
                kind: module
                name: 核心基础构件
                status: active
                summary: 提供加密能力。
                owners:
                  - platform
                tags:
                  - backend
                  - dotnet
                module_type: building-block
                stack: dotnet
                path: backend/dotnet/BuildingBlocks/src/Tw.Core
                provides:
                  capabilities:
                    - backend.capability.cryptography
                source:
                  declared_in: docs/knowledge/graph/modules/backend.dotnet.building-blocks.core.yaml
                  evidence:
                    - backend/dotnet/BuildingBlocks/src/Tw.Core
                provenance:
                  created_by: human
                  created_at: 2026-04-29
                  updated_by: human
                  updated_at: 2026-04-29
                """,
            )
            source = write_file(
                root,
                "backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/AesCryptography.cs",
                """
                namespace Tw.Core.Security.Cryptography;
                public static class AesCryptography
                {
                    public static string Encrypt(string input, byte[] key) => input;
                }
                """,
            )
            payloads, messages = knowledge.build_indexes(existing_generated_at="2026-04-27T00:00:00Z")
            self.assertEqual([], [message for message in messages if message.level == "ERROR"], diagnostic_text(messages))
            knowledge.write_indexes(payloads)
            source.write_text(
                source.read_text(encoding="utf-8") + "\npublic static class NewApi { }\n",
                encoding="utf-8",
            )

            messages = knowledge.collect_index_messages()

            self.assertIn("knowledge.index-stale", [message.code for message in messages])
            self.assertIn("api-index.generated.json", diagnostic_text(messages))
```

- [ ] **Step 5: Run API index tests**

Run:

```powershell
python -m unittest tests.knowledge.test_knowledge_tool.KnowledgeToolTests.test_generate_writes_public_api_indexes_and_excludes_testbase tests.knowledge.test_knowledge_tool.KnowledgeToolTests.test_check_detects_stale_public_api_index -v
```

Expected: PASS.

- [ ] **Step 6: Commit API index generation**

Run:

```powershell
git add tools\knowledge\knowledge.py tests\knowledge\test_knowledge_tool.py
git commit -m "feat: generate public API reuse indexes"
```

Expected: commit succeeds.

### Task 5: Add README And Semantic Marker Diagnostics

**Files:**
- Modify: `docs/knowledge/taxonomy.yaml`
- Modify: `tools/knowledge/knowledge.py`
- Modify: `tools/knowledge/rules/knowledge-diagnostics.md`
- Modify: `tests/knowledge/test_knowledge_tool.py`

- [ ] **Step 1: Add failing Tw.TestBase README validation tests**

Add:

```python
    def test_check_reports_missing_testbase_readme(self):
        with isolated_repo() as root:
            write_reuse_taxonomy(root)
            testbase = root / "backend/dotnet/BuildingBlocks/tests/Tw.TestBase"
            testbase.mkdir(parents=True)
            (testbase / "TestBytes.cs").write_text("namespace Tw.TestBase;\n", encoding="utf-8")

            messages = knowledge.collect_validation_messages()

            self.assertIn("knowledge.testbase-readme-missing", [message.code for message in messages])

    def test_check_reports_invalid_testbase_readme_entry(self):
        with isolated_repo() as root:
            write_reuse_taxonomy(root)
            write_file(
                root,
                "backend/dotnet/BuildingBlocks/tests/Tw.TestBase/README.md",
                """
                # Tw.TestBase 测试共享能力

                标记：测试共享能力 测试字节 UTF-8 固定字节
                入口：MissingFile.cs
                关键词：MissingFile
                """,
            )

            messages = knowledge.collect_validation_messages()

            self.assertIn("knowledge.testbase-readme-invalid", [message.code for message in messages])
            self.assertIn("MissingFile.cs", diagnostic_text(messages))
```

- [ ] **Step 2: Add failing semantic marker validation test**

Add:

```python
    def test_check_reports_invalid_semantic_marker_format(self):
        with isolated_repo() as root:
            write_reuse_taxonomy(root)
            write_file(
                root,
                "backend/dotnet/BuildingBlocks/src/Tw.Core/Utilities/InternalHelper.cs",
                """
                namespace Tw.Core.Utilities;

                internal static class InternalHelper
                {
                    // 语义标记: 错误冒号
                    private static void Run()
                    {
                    }
                }
                """,
            )

            messages = knowledge.collect_validation_messages()

            self.assertIn("knowledge.semantic-marker-invalid", [message.code for message in messages])
```

Run:

```powershell
python -m unittest tests.knowledge.test_knowledge_tool.KnowledgeToolTests.test_check_reports_missing_testbase_readme tests.knowledge.test_knowledge_tool.KnowledgeToolTests.test_check_reports_invalid_testbase_readme_entry tests.knowledge.test_knowledge_tool.KnowledgeToolTests.test_check_reports_invalid_semantic_marker_format -v
```

Expected: FAIL because diagnostics are not implemented.

- [ ] **Step 3: Implement Tw.TestBase README validation**

Add:

```python
TESTBASE_MARKERS = [
    "标记：测试共享能力 测试字节 UTF-8 固定字节",
    "标记：测试共享能力 测试流 MemoryStream 文本流 字节流",
    "标记：测试共享能力 临时目录 文件写入 自动清理",
    "标记：测试共享能力 加密测试向量 固定盐 固定密钥 固定 IV",
]


def collect_testbase_readme_messages() -> list[Diagnostic]:
    messages: list[Diagnostic] = []
    testbase_root = REPO_ROOT / TESTBASE_PATH
    if not testbase_root.exists():
        return messages
    readme = testbase_root / "README.md"
    if not readme.exists():
        return [
            warn(
                "knowledge.testbase-readme-missing",
                readme,
                "Tw.TestBase 缺少测试共享能力声明 README。",
                "请创建 backend/dotnet/BuildingBlocks/tests/Tw.TestBase/README.md 并声明测试共享能力。",
            )
        ]
    content = read_text(readme)
    for marker in TESTBASE_MARKERS:
        if marker not in content:
            messages.append(
                warn(
                    "knowledge.testbase-readme-invalid",
                    readme,
                    f"Tw.TestBase README 缺少能力声明: {marker}",
                    "请补充标记、入口、关键词和适用说明。",
                )
            )
    for match in re.finditer(r"入口：([^\s]+)", content):
        entry = match.group(1).strip()
        if not (testbase_root / entry).exists():
            messages.append(
                warn(
                    "knowledge.testbase-readme-invalid",
                    readme,
                    f"Tw.TestBase README 声明的入口文件不存在: {entry}",
                    "请修正入口文件名或补充对应测试共享工具。",
                )
            )
    return messages
```

Call `messages.extend(collect_testbase_readme_messages())` inside `collect_validation_messages()` after graph validation messages are collected.

- [ ] **Step 4: Implement semantic marker validation**

Add:

```python
SEMANTIC_MARKER_LINE_RE = re.compile(r"^\s*(?://|#|/\*|\*)\s*语义标记：\S+(?:\s+\S+){1,7}\s*$")


def source_files_for_semantic_marker_scan() -> list[Path]:
    roots = [
        REPO_ROOT / "backend",
        REPO_ROOT / "frontend",
    ]
    suffixes = {".cs", ".java", ".py", ".go", ".ts", ".tsx", ".vue"}
    files: list[Path] = []
    for root in roots:
        if not root.exists():
            continue
        for path in root.rglob("*"):
            if path.is_file() and path.suffix in suffixes and not is_generated_or_build_path(path):
                files.append(path)
    return sorted(files)


def collect_semantic_marker_messages() -> list[Diagnostic]:
    messages: list[Diagnostic] = []
    for path in source_files_for_semantic_marker_scan():
        for line_number, line in enumerate(read_text(path).splitlines(), start=1):
            if "语义标记" not in line:
                continue
            if not SEMANTIC_MARKER_LINE_RE.match(line):
                messages.append(
                    warn(
                        "knowledge.semantic-marker-invalid",
                        f"{rel_path(path)}:{line_number}",
                        "语义标记注释格式不符合约定。",
                        "请使用格式：语义标记：关键词1 关键词2，且末尾不加句号。",
                    )
                )
    return messages
```

Call `messages.extend(collect_semantic_marker_messages())` inside `collect_validation_messages()`.

- [ ] **Step 5: Document diagnostics**

Add rows to `tools/knowledge/rules/knowledge-diagnostics.md`:

```markdown
| `knowledge.api-index-unassigned` | warning | public API 无法归属到 capability | `provides.capabilities` |
| `knowledge.testbase-readme-missing` | warning | `Tw.TestBase` 缺少测试共享能力 README | `backend/dotnet/BuildingBlocks/tests/Tw.TestBase/README.md` |
| `knowledge.testbase-readme-invalid` | warning | `Tw.TestBase` README 缺少标记或入口文件不存在 | `backend/dotnet/BuildingBlocks/tests/Tw.TestBase/README.md` |
| `knowledge.semantic-marker-invalid` | warning | 语义标记注释格式不符合约定 | source comments |
```

- [ ] **Step 6: Run validation tests**

Run:

```powershell
python -m unittest tests.knowledge.test_knowledge_tool.KnowledgeToolTests.test_check_reports_missing_testbase_readme tests.knowledge.test_knowledge_tool.KnowledgeToolTests.test_check_reports_invalid_testbase_readme_entry tests.knowledge.test_knowledge_tool.KnowledgeToolTests.test_check_reports_invalid_semantic_marker_format tests.knowledge.test_knowledge_tool.KnowledgeToolTests.test_diagnostics_rules_document_all_emitted_codes -v
```

Expected: PASS.

- [ ] **Step 7: Commit diagnostics**

Run:

```powershell
git add docs\knowledge\taxonomy.yaml tools\knowledge\knowledge.py tools\knowledge\rules\knowledge-diagnostics.md tests\knowledge\test_knowledge_tool.py
git commit -m "feat: validate reuse declarations"
```

Expected: commit succeeds.

### Task 6: Integrate API Index And Tw.TestBase README Into Query

**Files:**
- Modify: `tools/knowledge/knowledge.py`
- Modify: `tests/knowledge/test_knowledge_tool.py`

- [ ] **Step 1: Add failing query tests**

Add:

```python
    def test_query_returns_api_entries_for_reuse_capability(self):
        with isolated_repo() as root:
            write_reuse_taxonomy(root)
            write_capability_node(root, "backend.capability.cryptography", "加密能力", "backend/dotnet/BuildingBlocks/src/Tw.Core/Security")
            write_file(
                root,
                "docs/knowledge/graph/modules/backend.dotnet.building-blocks.core.yaml",
                """
                schema_version: 1.0.0
                id: backend.dotnet.building-blocks.core
                kind: module
                name: 核心基础构件
                status: active
                summary: 提供加密能力。
                owners:
                  - platform
                tags:
                  - backend
                  - dotnet
                  - cryptography
                module_type: building-block
                stack: dotnet
                path: backend/dotnet/BuildingBlocks/src/Tw.Core
                provides:
                  capabilities:
                    - backend.capability.cryptography
                source:
                  declared_in: docs/knowledge/graph/modules/backend.dotnet.building-blocks.core.yaml
                  evidence:
                    - backend/dotnet/BuildingBlocks/src/Tw.Core
                provenance:
                  created_by: human
                  created_at: 2026-04-29
                  updated_by: human
                  updated_at: 2026-04-29
                """,
            )
            write_file(
                root,
                "backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/AesCryptography.cs",
                """
                namespace Tw.Core.Security.Cryptography;
                /// <summary>提供 AES 加密入口</summary>
                public static class AesCryptography
                {
                    /// <summary>加密字符串内容</summary>
                    public static string Encrypt(string input, byte[] key) => input;
                }
                """,
            )
            payloads, messages = knowledge.build_indexes(existing_generated_at="2026-04-27T00:00:00Z")
            self.assertEqual([], [message for message in messages if message.level == "ERROR"], diagnostic_text(messages))
            knowledge.write_indexes(payloads)

            results = knowledge.query_nodes("AES 加密", limit=5)

            self.assertTrue(any(result["id"] == "backend.capability.cryptography" for result in results))
            api_results = [result for result in results if result["kind"] == "public-api"]
            self.assertTrue(api_results)
            self.assertEqual("dotnet:Tw.Core.Security.Cryptography.AesCryptography.Encrypt", api_results[0]["id"])
            self.assertIn("backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/AesCryptography.cs", api_results[0]["read"])

    def test_query_returns_testbase_readme_for_test_helper_need(self):
        with isolated_repo() as root:
            write_reuse_taxonomy(root)
            write_file(
                root,
                "backend/dotnet/BuildingBlocks/tests/Tw.TestBase/README.md",
                """
                # Tw.TestBase 测试共享能力

                标记：测试共享能力 临时目录 文件写入 自动清理
                入口：TemporaryDirectory.cs
                关键词：TemporaryDirectory WriteAllText GetPath
                适用：需要隔离文件系统副作用并在测试结束后清理
                """,
            )
            write_file(root, "backend/dotnet/BuildingBlocks/tests/Tw.TestBase/TemporaryDirectory.cs", "namespace Tw.TestBase;\n")

            results = knowledge.query_nodes("临时目录测试", limit=5)

            self.assertTrue(any(result["id"] == "testbase.readme" for result in results))
            testbase = next(result for result in results if result["id"] == "testbase.readme")
            self.assertEqual("test-helper-readme", testbase["kind"])
            self.assertIn("backend/dotnet/BuildingBlocks/tests/Tw.TestBase/README.md", testbase["read"])
```

Run:

```powershell
python -m unittest tests.knowledge.test_knowledge_tool.KnowledgeToolTests.test_query_returns_api_entries_for_reuse_capability tests.knowledge.test_knowledge_tool.KnowledgeToolTests.test_query_returns_testbase_readme_for_test_helper_need -v
```

Expected: FAIL because query does not return public API or README results.

- [ ] **Step 2: Add API index loader and scorer**

Add:

```python
def load_public_api_index() -> list[dict[str, Any]]:
    path = GENERATED_DIR / "api-index.generated.json"
    if not path.exists():
        return []
    try:
        payload = json.loads(read_text(path))
    except json.JSONDecodeError:
        return []
    apis = payload.get("apis") if isinstance(payload, dict) else None
    if not isinstance(apis, list):
        return []
    return [api for api in apis if isinstance(api, dict)]


def api_search_text(api: dict[str, Any]) -> str:
    values = [
        api.get("id"),
        api.get("module_id"),
        api.get("namespace"),
        api.get("type_name"),
        api.get("member_name"),
        api.get("signature"),
        api.get("summary"),
        " ".join(str(value) for value in api.get("capability_ids", []) if value),
        " ".join(str(value) for value in api.get("keywords", []) if value),
    ]
    return " ".join(str(value).lower() for value in values if value)
```

- [ ] **Step 3: Add TestBase README query helper**

Add:

```python
TEST_QUERY_TOKENS = {"测试", "test", "临时目录", "测试字节", "测试流", "测试向量", "fixture"}


def testbase_readme_query_result(tokens: list[str]) -> dict[str, Any] | None:
    if not any(token in TEST_QUERY_TOKENS for token in tokens):
        return None
    readme = REPO_ROOT / TESTBASE_PATH / "README.md"
    if not readme.exists():
        return None
    content = read_text(readme).lower()
    score = sum(1 for token in tokens if token in content)
    if score == 0:
        return None
    return {
        "id": "testbase.readme",
        "kind": "test-helper-readme",
        "name": "Tw.TestBase 测试共享能力",
        "summary": "测试共享能力通过 Tw.TestBase README 声明，编写测试时按标记和关键词搜索入口文件。",
        "read": [
            f"{TESTBASE_PATH}/README.md",
        ],
    }
```

- [ ] **Step 4: Extend query_nodes**

Modify `query_nodes` so it:

1. Keeps existing graph node ranking.
2. Appends public API results from `load_public_api_index()`.
3. Appends `testbase_readme_query_result(tokens)` when matching.
4. Sorts graph results before public API results when scores tie.

Use this result shape for public API entries:

```python
{
    "id": str(api.get("id", "")),
    "kind": "public-api",
    "name": str(api.get("member_name") or api.get("type_name") or api.get("id", "")),
    "summary": str(api.get("summary") or api.get("signature") or ""),
    "read": [
        str(api.get("path", "")),
        generated_path(API_INDEX_DIR_NAME, f"{api.get('stack')}.{api.get('module_id')}.generated.json"),
    ],
}
```

- [ ] **Step 5: Run query tests**

Run:

```powershell
python -m unittest tests.knowledge.test_knowledge_tool.KnowledgeToolTests.test_query_returns_api_entries_for_reuse_capability tests.knowledge.test_knowledge_tool.KnowledgeToolTests.test_query_returns_testbase_readme_for_test_helper_need -v
```

Expected: PASS.

- [ ] **Step 6: Commit query integration**

Run:

```powershell
git add tools\knowledge\knowledge.py tests\knowledge\test_knowledge_tool.py
git commit -m "feat: query public API reuse entries"
```

Expected: commit succeeds.

### Task 7: Update Documentation And Skills

**Files:**
- Modify: `docs/knowledge/README.md`
- Modify: `.agents/skills/tw-knowledge-discovery/SKILL.md`
- Modify: `.agents/skills/tw-requirement-router/SKILL.md`
- Modify: `tests/knowledge/test_knowledge_tool.py`

- [ ] **Step 1: Add failing documentation tests**

Add:

```python
    def test_knowledge_readme_documents_public_api_index_and_testbase(self):
        content = (knowledge.REPO_ROOT / "docs/knowledge/README.md").read_text(encoding="utf-8")

        self.assertIn("api-index.generated.json", content)
        self.assertIn("public API", content)
        self.assertIn("Tw.TestBase/README.md", content)
        self.assertIn("python tools/knowledge/knowledge.py generate", content)

    def test_skills_document_reuse_discovery_flow(self):
        discovery = (knowledge.REPO_ROOT / ".agents/skills/tw-knowledge-discovery/SKILL.md").read_text(encoding="utf-8")
        router = (knowledge.REPO_ROOT / ".agents/skills/tw-requirement-router/SKILL.md").read_text(encoding="utf-8")

        self.assertIn("public API", discovery)
        self.assertIn("api-index.generated.json", discovery)
        self.assertIn("Tw.TestBase/README.md", discovery)
        self.assertIn("public API", router)
        self.assertIn("Tw.TestBase/README.md", router)
```

Run:

```powershell
python -m unittest tests.knowledge.test_knowledge_tool.KnowledgeToolTests.test_knowledge_readme_documents_public_api_index_and_testbase tests.knowledge.test_knowledge_tool.KnowledgeToolTests.test_skills_document_reuse_discovery_flow -v
```

Expected: FAIL because docs and skills do not mention the new flow.

- [ ] **Step 2: Update docs/knowledge/README.md**

Add a section:

```markdown
## Public API 轻量索引

`python tools/knowledge/knowledge.py generate` 会在 `docs/knowledge/generated/api-index.generated.json` 和 `docs/knowledge/generated/api-index/` 下生成 public API 轻量索引。索引只收录生产和框架侧底层封装路径中的 public API，不收录 internal、private、generated、bin、obj 或 `Tw.TestBase`。

查询复用能力时先通过正式图谱定位 capability/module，再通过 public API 轻量索引定位源码入口。生成索引不得手工编辑。

## Tw.TestBase 测试共享能力

`backend/dotnet/BuildingBlocks/tests/Tw.TestBase` 不进入 public API 轻量索引。编写测试时先读取 `backend/dotnet/BuildingBlocks/tests/Tw.TestBase/README.md`，根据其中的标记、关键词和入口文件使用 `rg` 搜索并复用测试辅助工具。
```

- [ ] **Step 3: Update tw-knowledge-discovery skill**

In `.agents/skills/tw-knowledge-discovery/SKILL.md`, add this flow under its lookup instructions:

```markdown
## 底层封装复用发现

1. 先运行或模拟 `python tools/knowledge/knowledge.py query --text "<request>" --limit 5`。
2. 如果命中 capability/module，读取返回的 graph 文件和 section index。
3. 如果返回 `public-api` 结果，读取对应源码路径和 `docs/knowledge/generated/api-index/*.generated.json`。
4. 如果需求是测试编写，读取 `backend/dotnet/BuildingBlocks/tests/Tw.TestBase/README.md`，按其中标记和关键词使用 `rg` 搜索测试辅助工具。
5. internal/private 语义标记只作为 `rg` 搜索辅助，不作为跨项目复用契约。
```

- [ ] **Step 4: Update tw-requirement-router skill**

In `.agents/skills/tw-requirement-router/SKILL.md`, add:

```markdown
## 底层封装复用路由

在实现框架层、底层包、公共能力或测试代码前，先查询正式记忆图谱。命中复用能力时，优先使用 capability/module 和 public API 轻量索引返回的入口。测试编写场景必须检查 `backend/dotnet/BuildingBlocks/tests/Tw.TestBase/README.md`，再按标记和关键词使用 `rg` 搜索已有测试辅助工具。
```

- [ ] **Step 5: Run docs and skill tests**

Run:

```powershell
python -m unittest tests.knowledge.test_knowledge_tool.KnowledgeToolTests.test_knowledge_readme_documents_public_api_index_and_testbase tests.knowledge.test_knowledge_tool.KnowledgeToolTests.test_skills_document_reuse_discovery_flow -v
```

Expected: PASS.

- [ ] **Step 6: Commit docs and skills**

Run:

```powershell
git add docs\knowledge\README.md .agents\skills\tw-knowledge-discovery\SKILL.md .agents\skills\tw-requirement-router\SKILL.md tests\knowledge\test_knowledge_tool.py
git commit -m "docs: document reuse discovery flow"
```

Expected: commit succeeds.

### Task 8: Regenerate Knowledge Indexes And Verify End To End

**Files:**
- Generated: `docs/knowledge/generated/**/*.generated.json`
- Generated: `docs/knowledge/generated/api-index.generated.json`
- Generated: `docs/knowledge/generated/api-index/*.generated.json`

- [ ] **Step 1: Regenerate knowledge indexes**

Run:

```powershell
python tools\knowledge\knowledge.py generate
```

Expected: command exits `0` and prints generated index count. New API index files exist under `docs/knowledge/generated/api-index/`.

- [ ] **Step 2: Run full knowledge tests**

Run:

```powershell
python -m unittest tests.knowledge.test_knowledge_tool -v
```

Expected: all tests pass with `OK`.

- [ ] **Step 3: Run knowledge checks**

Run:

```powershell
python tools\knowledge\knowledge.py check
python tools\knowledge\knowledge.py scan
```

Expected:

```text
OK knowledge checks passed
OK knowledge scan passed
```

- [ ] **Step 4: Run standards checks**

Run:

```powershell
python tools\standards\standards.py check
```

Expected:

```text
OK standards checks passed
```

- [ ] **Step 5: Run representative queries**

Run:

```powershell
python tools\knowledge\knowledge.py query --text "加密 AES" --limit 5
python tools\knowledge\knowledge.py query --text "反射 类型发现" --limit 5
python tools\knowledge\knowledge.py query --text "临时目录测试" --limit 5
python tools\knowledge\knowledge.py query --text "测试字节" --limit 5
```

Expected:

- The encryption query includes `backend.capability.cryptography`, `backend.dotnet.building-blocks.core`, or `public-api` entries under `Tw.Core.Security.Cryptography`.
- The reflection query includes `backend.capability.reflection`, `backend.dotnet.building-blocks.core`, or `public-api` entries under `Tw.Core.Reflection`.
- The test helper queries include `testbase.readme` and read target `backend/dotnet/BuildingBlocks/tests/Tw.TestBase/README.md`.

- [ ] **Step 6: Check whitespace and status**

Run:

```powershell
git diff --check
git status --short --untracked-files=all
```

Expected: `git diff --check` exits `0`; status shows only intended generated/doc/code/test changes.

- [ ] **Step 7: Commit generated indexes and final verification**

Run:

```powershell
git add docs\knowledge\generated docs\standards\index.generated.json docs\standards\_index tools\knowledge\knowledge.py tests\knowledge\test_knowledge_tool.py docs\knowledge docs\standards .agents\skills backend\dotnet\BuildingBlocks\tests\Tw.TestBase\README.md
git commit -m "feat: index platform reuse knowledge"
```

Expected: commit succeeds.

- [ ] **Step 8: Final status**

Run:

```powershell
git status --short --untracked-files=all
git log -5 --oneline
```

Expected: working tree is clean and recent commits include the task commits above.

## Self-Review Checklist

- Spec coverage:
  - Formal capability graph alignment: Task 2.
  - public API lightweight index: Tasks 3, 4, 6, 8.
  - `Tw.TestBase` README-only discovery: Tasks 2, 5, 6, 7.
  - Semantic marker comments: Tasks 1 and 5.
  - Discovery/router behavior: Task 7.
  - Verification and generated indexes: Task 8.
- No placeholder strings are used as implementation instructions.
- Types and function names introduced in earlier tasks match later references:
  - `PublicApiEntry`
  - `build_public_api_entries`
  - `build_api_index_payloads`
  - `load_public_api_index`
  - `collect_testbase_readme_messages`
  - `collect_semantic_marker_messages`
- The plan keeps `Tw.TestBase` out of public API generated indexes and uses `Tw.TestBase/README.md` for test helper discovery.
