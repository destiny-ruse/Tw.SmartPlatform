# backend/dotnet Governance Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 重整 `backend/dotnet` 的测试目录、tools 目录、Build 边界、charter 规则、注释规则和治理检查，使结构与规则都可自动验证。

**Architecture:** 先把治理规则固化到 `Tw.Architecture.Tests` 与 Python charter 校验中，再执行目录迁移和文件清理。正式工程规范只记录确定规则，具体代码迁移通过 `.slnx`、`ProjectReference`、charter 和测试一起收口。

**Tech Stack:** .NET 10、xUnit v3、AwesomeAssertions、Roslyn、PowerShell、Python、pytest、YAML charter

---

## File Structure

**Create**

- `backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/RepositoryLayout.cs`：架构测试共享的仓库路径与项目映射 helper
- `backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/BuildLayoutTests.cs`：验证 `Build` 目录只承载 `.props` 和锁定文件
- `backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/XmlDocumentationTests.cs`：验证人工维护 C# 类型和成员具备 XML 文档注释

**Modify**

- `backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj`：增加 Roslyn 解析依赖
- `backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/PackageTopologyTests.cs`：增加测试目录、Abstractions 测试、tools 拆分规则
- `backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/PackageCharterTests.cs`：增加正式 schema 和中文内容规则
- `backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/ForbiddenReferenceTests.cs`：改用共享路径 helper
- `tools/tests/test_charter.py`：增加中文自然语言校验测试
- `tools/src/tw_memory/charter.py`：实现中文自然语言校验
- `docs/engineering-standards/03-project-and-code/coding-standards.md`：写入私有、内部成员注释规则
- `docs/engineering-standards/03-project-and-code/language-specific/dotnet-core.md`：写入 .NET XML 注释范围规则
- `docs/engineering-standards/03-project-and-code/shared-package-charter.md`：写入 charter 中文内容规则
- `docs/engineering-standards/10-governance/dotnet-framework-governance.md`：替换无效 Build runner 和 QualityGates 命令
- `backend/dotnet/Tw.SmartPlatform.slnx`：更新测试项目和 tools 项目路径
- `backend/dotnet/tools/src/*/package-charter.yaml`：迁移 tools charter 到正式 schema
- `backend/dotnet/tools/src/Tw.Templates/content/building-block/src/Tw.Sample/package-charter.yaml`：迁移模板 charter 到正式 schema
- `backend/dotnet/BuildingBlocks/src/**/package-charter.yaml`：把正式 schema 下的自然语言内容统一为简体中文
- `backend/dotnet/BuildingBlocks/src/**/*.cs`、`backend/dotnet/BuildingBlocks/tests/**/*.cs`、`backend/dotnet/tools/src/**/*.cs`、`backend/dotnet/tools/tests/**/*.cs`：补齐 XML 文档注释并删除确认无引用的空壳成员

**Move**

- `backend/dotnet/BuildingBlocks/tests/Tw.Architecture.Tests` -> `backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests`
- `backend/dotnet/BuildingBlocks/tests/<Package.Tests>` -> `backend/dotnet/BuildingBlocks/tests/<Capability>/<Package.Tests>`
- `backend/dotnet/tools/Tw.Analyzers` -> `backend/dotnet/tools/src/Tw.Analyzers`
- `backend/dotnet/tools/Tw.Cli` -> `backend/dotnet/tools/src/Tw.Cli`
- `backend/dotnet/tools/Tw.Templates` -> `backend/dotnet/tools/src/Tw.Templates`
- `backend/dotnet/tools/Tw.Analyzers.Tests` -> `backend/dotnet/tools/tests/Tw.Analyzers.Tests`
- `backend/dotnet/tools/Tw.Cli.Tests` -> `backend/dotnet/tools/tests/Tw.Cli.Tests`
- `backend/dotnet/tools/Tw.Templates.Tests` -> `backend/dotnet/tools/tests/Tw.Templates.Tests`

**Delete**

- `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Abstractions.Tests`
- `backend/dotnet/BuildingBlocks/tests/Tw.Authorization.Abstractions.Tests`
- `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Abstractions.Tests`
- `backend/dotnet/BuildingBlocks/tests/Tw.Json.Abstractions.Tests`
- `backend/dotnet/BuildingBlocks/tests/Tw.Validation.Abstractions.Tests`
- `backend/dotnet/Build/Build.cs`
- `backend/dotnet/Build/Build.csproj`
- `backend/dotnet/Build/QualityGates`
- 空目录和只包含 `bin`、`obj` 的目录

## Execution Rules

- 每个任务开始前运行 `git status --short`，确认没有无关改动被混入
- 文件移动使用 `git mv`；删除已跟踪文件使用 `git rm`
- PowerShell 移动或删除前必须用 `Resolve-Path` 校验路径位于 `D:\DestinyWorkSpaces\Tw.SmartPlatform`
- 每个任务结束时运行任务内列出的验证命令并提交
- 正式规范文件禁止保留“后续”“待定”“暂定”“视情况”“可能”“大概”“如有需要”“按需补充”“待补充”“TODO”“TBD”

## Task 1: Add Failing Architecture Guards

**Files:**

- Create: `backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/RepositoryLayout.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/BuildLayoutTests.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/XmlDocumentationTests.cs`
- Modify: `backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj`
- Modify: `backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/PackageTopologyTests.cs`
- Modify: `backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/PackageCharterTests.cs`
- Modify: `backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/ForbiddenReferenceTests.cs`

- [ ] **Step 1: Move architecture test project first**

Run:

```powershell
$repo = (Resolve-Path ".").Path
if ($repo -ne "D:\DestinyWorkSpaces\Tw.SmartPlatform") { throw "Unexpected repo root: $repo" }
New-Item -ItemType Directory -Force backend\dotnet\BuildingBlocks\tests\Architecture | Out-Null
git mv backend\dotnet\BuildingBlocks\tests\Tw.Architecture.Tests backend\dotnet\BuildingBlocks\tests\Architecture\Tw.Architecture.Tests
```

Expected: `git status --short` shows the architecture test project as moved.

- [ ] **Step 2: Add the shared repository layout helper**

Create `backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/RepositoryLayout.cs`:

```csharp
namespace Tw.Architecture.Tests;

/// <summary>
/// 提供架构测试访问仓库目录和项目映射的统一入口
/// </summary>
internal static class RepositoryLayout
{
    /// <summary>
    /// 仓库根目录
    /// </summary>
    public static string Root { get; } = FindRepositoryRoot();

    /// <summary>
    /// .NET 工作区根目录
    /// </summary>
    public static string DotnetRoot => Path.Combine(Root, "backend", "dotnet");

    /// <summary>
    /// BuildingBlocks 生产源码根目录
    /// </summary>
    public static string BuildingBlocksSrc => Path.Combine(DotnetRoot, "BuildingBlocks", "src");

    /// <summary>
    /// BuildingBlocks 测试根目录
    /// </summary>
    public static string BuildingBlocksTests => Path.Combine(DotnetRoot, "BuildingBlocks", "tests");

    /// <summary>
    /// .NET tools 根目录
    /// </summary>
    public static string ToolsRoot => Path.Combine(DotnetRoot, "tools");

    /// <summary>
    /// Build 配置根目录
    /// </summary>
    public static string BuildRoot => Path.Combine(DotnetRoot, "Build");

    /// <summary>
    /// 返回生产包名到能力目录名的映射
    /// </summary>
    public static IReadOnlyDictionary<string, string> RuntimeCapabilitiesByPackage()
    {
        return Directory.GetFiles(BuildingBlocksSrc, "*.csproj", SearchOption.AllDirectories)
            .ToDictionary(
                Path.GetFileNameWithoutExtension,
                path => new DirectoryInfo(Path.GetDirectoryName(path)!).Parent!.Name,
                StringComparer.Ordinal);
    }

    /// <summary>
    /// 返回测试项目对应的生产包名
    /// </summary>
    public static string RuntimePackageNameForTestProject(string testProjectName)
    {
        if (testProjectName.EndsWith(".Tests.Fixtures", StringComparison.Ordinal))
        {
            return testProjectName[..^".Tests.Fixtures".Length];
        }

        if (testProjectName.EndsWith(".Tests", StringComparison.Ordinal))
        {
            return testProjectName[..^".Tests".Length];
        }

        throw new InvalidOperationException($"测试项目名称不符合约定: {testProjectName}");
    }

    /// <summary>
    /// 判断测试项目是否属于 Abstractions 测试项目
    /// </summary>
    public static bool IsAbstractionsTestProject(string testProjectName)
    {
        return testProjectName.EndsWith(".Abstractions.Tests", StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("无法定位仓库根目录");
    }
}
```

- [ ] **Step 3: Add Roslyn dependency for XML documentation guard**

Modify `backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.NET.Test.Sdk" />
  <PackageReference Include="xunit.v3" />
  <PackageReference Include="xunit.runner.visualstudio" />
  <PackageReference Include="AwesomeAssertions" />
  <PackageReference Include="Microsoft.CodeAnalysis.CSharp" />
</ItemGroup>
```

- [ ] **Step 4: Replace duplicated repository root helpers in existing tests**

In `PackageTopologyTests.cs`, `PackageCharterTests.cs`, and `ForbiddenReferenceTests.cs`, remove each private `RepositoryRoot` field and `FindRepositoryRoot` method. Use `RepositoryLayout.Root`, `RepositoryLayout.BuildingBlocksSrc`, and `RepositoryLayout.BuildingBlocksTests`.

Example replacement in `ForbiddenReferenceTests.cs`:

```csharp
var srcRoot = RepositoryLayout.BuildingBlocksSrc;
```

- [ ] **Step 5: Extend `PackageTopologyTests.cs`**

Add these tests to `PackageTopologyTests`:

```csharp
[Fact]
public void BuildingBlocks_TestProjects_LiveUnderCapabilityFolders()
{
    var testProjects = Directory.GetFiles(RepositoryLayout.BuildingBlocksTests, "*.csproj", SearchOption.AllDirectories);

    testProjects.Should().NotBeEmpty();
    testProjects.Should().OnlyContain(
        path => Path.GetRelativePath(RepositoryLayout.BuildingBlocksTests, path).Replace('\\', '/').Count(ch => ch == '/') == 2,
        "test projects must use tests/<Capability>/<TestProject>/<TestProject>.csproj");
}

[Fact]
public void BuildingBlocks_TestProjects_MirrorRuntimeCapabilityFolders()
{
    var runtimeCapabilities = RepositoryLayout.RuntimeCapabilitiesByPackage();
    var violations = Directory.GetFiles(RepositoryLayout.BuildingBlocksTests, "*.csproj", SearchOption.AllDirectories)
        .Select(path => new
        {
            Path = path,
            ProjectName = Path.GetFileNameWithoutExtension(path),
            Capability = Path.GetRelativePath(RepositoryLayout.BuildingBlocksTests, path).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0]
        })
        .Where(project => project.ProjectName != "Tw.Architecture.Tests")
        .Where(project => !RepositoryLayout.IsAbstractionsTestProject(project.ProjectName))
        .Select(project => new
        {
            project.Path,
            project.ProjectName,
            project.Capability,
            RuntimePackage = RepositoryLayout.RuntimePackageNameForTestProject(project.ProjectName)
        })
        .Where(project => runtimeCapabilities.TryGetValue(project.RuntimePackage, out var capability) && capability != project.Capability)
        .Select(project => $"{project.ProjectName} expected {runtimeCapabilities[project.RuntimePackage]} but was {project.Capability}")
        .ToArray();

    violations.Should().BeEmpty("test projects must stay beside the capability of the runtime package they validate");
}

[Fact]
public void BuildingBlocks_DoesNotContainAbstractionsTestProjects()
{
    var abstractionsTests = Directory.GetFiles(RepositoryLayout.BuildingBlocksTests, "*.csproj", SearchOption.AllDirectories)
        .Select(Path.GetFileNameWithoutExtension)
        .Where(RepositoryLayout.IsAbstractionsTestProject)
        .ToArray();

    abstractionsTests.Should().BeEmpty("Abstractions packages define contracts and are validated through consuming packages");
}

[Fact]
public void DotnetTools_ProjectsLiveUnderSrcOrTests()
{
    var toolProjects = Directory.GetFiles(RepositoryLayout.ToolsRoot, "*.csproj", SearchOption.AllDirectories)
        .Where(path => !Path.GetRelativePath(RepositoryLayout.ToolsRoot, path).Replace('\\', '/').Contains("/content/", StringComparison.Ordinal))
        .ToArray();

    toolProjects.Should().NotBeEmpty();
    toolProjects.Should().OnlyContain(path =>
    {
        var relative = Path.GetRelativePath(RepositoryLayout.ToolsRoot, path).Replace('\\', '/');
        var parts = relative.Split('/');
        return parts.Length == 3 && (parts[0] == "src" || parts[0] == "tests");
    }, "tools projects must use tools/src/<Project> or tools/tests/<Project>");
}
```

- [ ] **Step 6: Add `BuildLayoutTests.cs`**

Create `backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/BuildLayoutTests.cs`:

```csharp
using AwesomeAssertions;
using Xunit;

namespace Tw.Architecture.Tests;

/// <summary>
/// 验证 Build 目录只承载中央包版本与构建级 MSBuild 配置
/// </summary>
public sealed class BuildLayoutTests
{
    [Fact]
    public void BuildDirectory_ContainsOnlyPropsAndLockFile()
    {
        var files = Directory.GetFiles(RepositoryLayout.BuildRoot, "*", SearchOption.AllDirectories)
            .Where(path => !Path.GetRelativePath(RepositoryLayout.BuildRoot, path).StartsWith("obj", StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(RepositoryLayout.BuildRoot, path).Replace('\\', '/'))
            .ToArray();

        files.Should().OnlyContain(
            path => path.EndsWith(".props", StringComparison.Ordinal) || path == "packages.lock.json",
            "Build is reserved for central MSBuild props and its lock file");
    }

    [Fact]
    public void BuildDirectory_DoesNotContainQualityGatesOrRunnerProject()
    {
        Directory.Exists(Path.Combine(RepositoryLayout.BuildRoot, "QualityGates")).Should().BeFalse();
        File.Exists(Path.Combine(RepositoryLayout.BuildRoot, "Build.cs")).Should().BeFalse();
        File.Exists(Path.Combine(RepositoryLayout.BuildRoot, "Build.csproj")).Should().BeFalse();
    }
}
```

- [ ] **Step 7: Add XML documentation guard**

Create `backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/XmlDocumentationTests.cs`:

```csharp
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Tw.Architecture.Tests;

/// <summary>
/// 验证人工维护的 C# 类型和成员具备 XML 文档注释
/// </summary>
public sealed class XmlDocumentationTests
{
    [Fact]
    public void MaintainedCSharpMembers_HaveXmlDocumentation()
    {
        var roots = new[]
        {
            Path.Combine(RepositoryLayout.DotnetRoot, "BuildingBlocks", "src"),
            Path.Combine(RepositoryLayout.DotnetRoot, "BuildingBlocks", "tests"),
            Path.Combine(RepositoryLayout.DotnetRoot, "tools", "src"),
            Path.Combine(RepositoryLayout.DotnetRoot, "tools", "tests")
        };

        var violations = roots
            .Where(Directory.Exists)
            .SelectMany(root => Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            .Where(IsMaintainedSourceFile)
            .SelectMany(FindUndocumentedDeclarations)
            .ToArray();

        violations.Should().BeEmpty("all maintained C# declarations must explain their contract in Simplified Chinese XML documentation");
    }

    private static bool IsMaintainedSourceFile(string path)
    {
        var relative = Path.GetRelativePath(RepositoryLayout.DotnetRoot, path).Replace('\\', '/');
        return !relative.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
            && !relative.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
            && !relative.EndsWith(".g.cs", StringComparison.Ordinal)
            && !relative.EndsWith(".Designer.cs", StringComparison.Ordinal)
            && !relative.EndsWith("GlobalUsings.cs", StringComparison.Ordinal);
    }

    private static IEnumerable<string> FindUndocumentedDeclarations(string path)
    {
        var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path));
        var root = tree.GetCompilationUnitRoot();
        foreach (var declaration in root.DescendantNodes().OfType<MemberDeclarationSyntax>())
        {
            if (!RequiresDocumentation(declaration) || HasXmlDocumentation(declaration))
            {
                continue;
            }

            var line = declaration.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            yield return $"{Path.GetRelativePath(RepositoryLayout.Root, path).Replace('\\', '/')}:{line} {DeclarationName(declaration)}";
        }
    }

    private static bool RequiresDocumentation(MemberDeclarationSyntax declaration)
    {
        return declaration is BaseTypeDeclarationSyntax
            or DelegateDeclarationSyntax
            or EnumMemberDeclarationSyntax
            or BaseMethodDeclarationSyntax
            or PropertyDeclarationSyntax
            or FieldDeclarationSyntax
            or EventDeclarationSyntax
            or EventFieldDeclarationSyntax;
    }

    private static bool HasXmlDocumentation(MemberDeclarationSyntax declaration)
    {
        return declaration.GetLeadingTrivia()
            .Any(trivia => trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));
    }

    private static string DeclarationName(MemberDeclarationSyntax declaration)
    {
        return declaration switch
        {
            BaseTypeDeclarationSyntax type => type.Identifier.Text,
            DelegateDeclarationSyntax @delegate => @delegate.Identifier.Text,
            EnumMemberDeclarationSyntax enumMember => enumMember.Identifier.Text,
            BaseMethodDeclarationSyntax method => method switch
            {
                ConstructorDeclarationSyntax constructor => constructor.Identifier.Text,
                MethodDeclarationSyntax namedMethod => namedMethod.Identifier.Text,
                ConversionOperatorDeclarationSyntax conversion => conversion.OperatorKeyword.Text,
                OperatorDeclarationSyntax @operator => @operator.OperatorToken.Text,
                _ => method.Kind().ToString()
            },
            PropertyDeclarationSyntax property => property.Identifier.Text,
            FieldDeclarationSyntax field => string.Join(", ", field.Declaration.Variables.Select(variable => variable.Identifier.Text)),
            EventDeclarationSyntax @event => @event.Identifier.Text,
            EventFieldDeclarationSyntax eventField => string.Join(", ", eventField.Declaration.Variables.Select(variable => variable.Identifier.Text)),
            _ => declaration.Kind().ToString()
        };
    }
}
```

- [ ] **Step 8: Extend `PackageCharterTests.cs`**

Replace the existing text-only assertion with required fields and CJK checks:

```csharp
private static readonly string[] RequiredFields =
[
    "schema_version:",
    "package:",
    "owner:",
    "responsibility:",
    "in_scope:",
    "out_of_scope:",
    "public_capabilities:",
    "dependency_rules:"
];

[Fact]
public void EveryRuntimeProject_HasPackageCharterWithCanonicalPackageName()
{
    var projects = Directory.GetFiles(RepositoryLayout.BuildingBlocksSrc, "*.csproj", SearchOption.AllDirectories);

    foreach (var project in projects)
    {
        var projectName = Path.GetFileNameWithoutExtension(project);
        var charter = Path.Combine(Path.GetDirectoryName(project)!, "package-charter.yaml");

        File.Exists(charter).Should().BeTrue($"{projectName} must declare package-charter.yaml");

        var text = File.ReadAllText(charter);
        text.Should().Contain($"package: {projectName}");
        foreach (var field in RequiredFields)
        {
            text.Should().Contain(field, $"{projectName} charter must use the formal schema");
        }
    }
}

[Fact]
public void EveryRuntimeProject_UsesChineseNaturalLanguageCharterContent()
{
    var charters = Directory.GetFiles(RepositoryLayout.BuildingBlocksSrc, "package-charter.yaml", SearchOption.AllDirectories);
    var violations = charters
        .Where(path => !ContainsChineseValue(File.ReadAllLines(path), "responsibility")
            || !ContainsChineseListValue(File.ReadAllLines(path), "in_scope")
            || !ContainsChineseListValue(File.ReadAllLines(path), "out_of_scope"))
        .Select(path => Path.GetRelativePath(RepositoryLayout.Root, path).Replace('\\', '/'))
        .ToArray();

    violations.Should().BeEmpty("charter responsibility, in_scope and out_of_scope must be written in Simplified Chinese");
}

private static bool ContainsChineseValue(string[] lines, string key)
{
    return lines.Any(line => line.StartsWith($"{key}:", StringComparison.Ordinal) && ContainsCjk(line));
}

private static bool ContainsChineseListValue(string[] lines, string key)
{
    var start = Array.FindIndex(lines, line => line.StartsWith($"{key}:", StringComparison.Ordinal));
    if (start < 0)
    {
        return false;
    }

    return lines.Skip(start + 1)
        .TakeWhile(line => line.StartsWith("  - ", StringComparison.Ordinal))
        .Any(ContainsCjk);
}

private static bool ContainsCjk(string text)
{
    return text.Any(ch => ch >= '\u4e00' && ch <= '\u9fff');
}
```

- [ ] **Step 9: Run architecture tests and verify they fail**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj
```

Expected: FAIL. The failure must include current flat test paths, existing Abstractions test projects, `Build.csproj` or `Build/QualityGates`, English or old-schema charters, and missing XML documentation.

- [ ] **Step 10: Commit failing guards**

Run:

```powershell
git add backend/dotnet/BuildingBlocks/tests
git commit -m "test: add dotnet governance architecture guards"
```

Expected: commit succeeds with only architecture test changes.

## Task 2: Add Python Charter Language Validation

**Files:**

- Modify: `tools/tests/test_charter.py`
- Modify: `tools/src/tw_memory/charter.py`

- [ ] **Step 1: Add failing pytest cases**

Append to `tools/tests/test_charter.py`:

```python
def test_validate_rejects_english_responsibility(tmp_path: Path) -> None:
    path = write_text(
        tmp_path / "package-charter.yaml",
        VALID.replace("跨服务复用的基础原语与无框架依赖工具。", "Reusable primitives for services."),
    )

    errors = validate_charter(load_charter(path))

    assert any("responsibility must use Simplified Chinese" in error for error in errors)


def test_validate_rejects_english_scope_items(tmp_path: Path) -> None:
    path = write_text(
        tmp_path / "package-charter.yaml",
        VALID.replace("  - 基础值对象", "  - Value objects").replace("  - HTTP 中间件", "  - HTTP middleware"),
    )

    errors = validate_charter(load_charter(path))

    assert any("in_scope must use Simplified Chinese" in error for error in errors)
    assert any("out_of_scope must use Simplified Chinese" in error for error in errors)


def test_validate_allows_english_public_capability_identifiers(tmp_path: Path) -> None:
    path = write_text(tmp_path / "package-charter.yaml", VALID)

    errors = validate_charter(load_charter(path))

    assert errors == []
```

- [ ] **Step 2: Run pytest and verify it fails**

Run:

```powershell
python -m pytest tools/tests/test_charter.py
```

Expected: FAIL because `validate_charter` does not yet enforce Chinese content.

- [ ] **Step 3: Implement Chinese content validation**

Modify `tools/src/tw_memory/charter.py`:

```python
def _contains_cjk(text: str) -> bool:
    """判断文本是否包含中文字符。"""
    return any("\u4e00" <= char <= "\u9fff" for char in text)
```

Add this block near the end of `validate_charter`, before `return errors`:

```python
    if charter.responsibility and not _contains_cjk(charter.responsibility):
        errors.append(f"{charter.path}: responsibility must use Simplified Chinese")

    for field_name in ("in_scope", "out_of_scope"):
        values = getattr(charter, field_name)
        if values and not any(_contains_cjk(value) for value in values):
            errors.append(f"{charter.path}: {field_name} must use Simplified Chinese")

    if charter.compatibility and not _contains_cjk(charter.compatibility):
        errors.append(f"{charter.path}: compatibility must use Simplified Chinese")
```

- [ ] **Step 4: Run pytest and verify it passes**

Run:

```powershell
python -m pytest tools/tests/test_charter.py
```

Expected: PASS.

- [ ] **Step 5: Commit Python charter validation**

Run:

```powershell
git add tools/src/tw_memory/charter.py tools/tests/test_charter.py
git commit -m "test: enforce Chinese package charter content"
```

Expected: commit succeeds.

## Task 3: Update Formal Governance Standards

**Files:**

- Modify: `docs/engineering-standards/03-project-and-code/coding-standards.md`
- Modify: `docs/engineering-standards/03-project-and-code/language-specific/dotnet-core.md`
- Modify: `docs/engineering-standards/03-project-and-code/shared-package-charter.md`

- [ ] **Step 1: Update common documentation comment rule**

In `coding-standards.md`, replace the first sentence under `### 文档注释` with:

```markdown
人工维护的类型、接口、函数、方法、构造函数、属性、字段、组件、事件、配置对象和导出 API 必须具备文档注释或等价契约说明。该规则覆盖 `public`、`internal`、`protected` 和 `private` 可见性。
```

- [ ] **Step 2: Update .NET XML documentation rule**

In `dotnet-core.md`, replace:

```markdown
.NET 公共 API 必须使用 XML 文档注释，并满足通用注释规则。标签使用要求如下：
```

with:

```markdown
.NET 人工维护的类型、成员、构造函数、字段、属性和事件必须使用 XML 文档注释，并满足通用注释规则。规则覆盖公开 API、跨程序集 API、内部成员和私有成员。标签使用要求如下：
```

- [ ] **Step 3: Update shared package charter language rule**

In `shared-package-charter.md`, add these bullets under `## 规范要求` after the required fields bullet:

```markdown
- `responsibility`、`in_scope`、`out_of_scope`、`compatibility` 中的自然语言内容必须使用简体中文。
- 包名、命名空间、依赖名、命令名、错误码、协议名和 `public_capabilities` 中的能力标识可以保留原文。
```

- [ ] **Step 4: Scan formal standards for prohibited uncertainty terms**

Run:

```powershell
Select-String -Path docs\engineering-standards\03-project-and-code\coding-standards.md,docs\engineering-standards\03-project-and-code\language-specific\dotnet-core.md,docs\engineering-standards\03-project-and-code\shared-package-charter.md -Pattern '后续|待定|暂定|视情况|可能|大概|如有需要|按需补充|待补充|TODO|TBD'
```

Expected: no matches from the changed rule text.

- [ ] **Step 5: Commit standards**

Run:

```powershell
git add docs/engineering-standards/03-project-and-code/coding-standards.md docs/engineering-standards/03-project-and-code/language-specific/dotnet-core.md docs/engineering-standards/03-project-and-code/shared-package-charter.md
git commit -m "docs: tighten dotnet comments and charter language rules"
```

Expected: commit succeeds.

## Task 4: Split backend/dotnet/tools Into src and tests

**Files:**

- Move: `backend/dotnet/tools/Tw.Analyzers` -> `backend/dotnet/tools/src/Tw.Analyzers`
- Move: `backend/dotnet/tools/Tw.Cli` -> `backend/dotnet/tools/src/Tw.Cli`
- Move: `backend/dotnet/tools/Tw.Templates` -> `backend/dotnet/tools/src/Tw.Templates`
- Move: `backend/dotnet/tools/Tw.Analyzers.Tests` -> `backend/dotnet/tools/tests/Tw.Analyzers.Tests`
- Move: `backend/dotnet/tools/Tw.Cli.Tests` -> `backend/dotnet/tools/tests/Tw.Cli.Tests`
- Move: `backend/dotnet/tools/Tw.Templates.Tests` -> `backend/dotnet/tools/tests/Tw.Templates.Tests`
- Modify: `backend/dotnet/tools/tests/Tw.Analyzers.Tests/Tw.Analyzers.Tests.csproj`
- Modify: `backend/dotnet/tools/tests/Tw.Cli.Tests/Tw.Cli.Tests.csproj`
- Modify: `backend/dotnet/Tw.SmartPlatform.slnx`

- [ ] **Step 1: Move tools projects**

Run:

```powershell
$repo = (Resolve-Path ".").Path
if ($repo -ne "D:\DestinyWorkSpaces\Tw.SmartPlatform") { throw "Unexpected repo root: $repo" }
New-Item -ItemType Directory -Force backend\dotnet\tools\src,backend\dotnet\tools\tests | Out-Null
git mv backend\dotnet\tools\Tw.Analyzers backend\dotnet\tools\src\Tw.Analyzers
git mv backend\dotnet\tools\Tw.Cli backend\dotnet\tools\src\Tw.Cli
git mv backend\dotnet\tools\Tw.Templates backend\dotnet\tools\src\Tw.Templates
git mv backend\dotnet\tools\Tw.Analyzers.Tests backend\dotnet\tools\tests\Tw.Analyzers.Tests
git mv backend\dotnet\tools\Tw.Cli.Tests backend\dotnet\tools\tests\Tw.Cli.Tests
git mv backend\dotnet\tools\Tw.Templates.Tests backend\dotnet\tools\tests\Tw.Templates.Tests
```

Expected: `backend/dotnet/tools` contains only `src` and `tests` as project-bearing directories.

- [ ] **Step 2: Update tools test project references**

In `backend/dotnet/tools/tests/Tw.Analyzers.Tests/Tw.Analyzers.Tests.csproj`, replace:

```xml
<ProjectReference Include="..\Tw.Analyzers\Tw.Analyzers.csproj" />
```

with:

```xml
<ProjectReference Include="..\..\src\Tw.Analyzers\Tw.Analyzers.csproj" />
```

In `backend/dotnet/tools/tests/Tw.Cli.Tests/Tw.Cli.Tests.csproj`, replace:

```xml
<ProjectReference Include="..\Tw.Cli\Tw.Cli.csproj" />
```

with:

```xml
<ProjectReference Include="..\..\src\Tw.Cli\Tw.Cli.csproj" />
```

- [ ] **Step 3: Update solution paths for tools**

In `backend/dotnet/Tw.SmartPlatform.slnx`, replace the `/tools/` folder block with:

```xml
  <Folder Name="/tools/" />
  <Folder Name="/tools/src/">
    <Project Path="tools/src/Tw.Analyzers/Tw.Analyzers.csproj" />
    <Project Path="tools/src/Tw.Cli/Tw.Cli.csproj" />
    <Project Path="tools/src/Tw.Templates/Tw.Templates.csproj" />
  </Folder>
  <Folder Name="/tools/tests/">
    <Project Path="tools/tests/Tw.Analyzers.Tests/Tw.Analyzers.Tests.csproj" />
    <Project Path="tools/tests/Tw.Cli.Tests/Tw.Cli.Tests.csproj" />
    <Project Path="tools/tests/Tw.Templates.Tests/Tw.Templates.Tests.csproj" />
  </Folder>
```

- [ ] **Step 4: Run tools tests**

Run:

```powershell
dotnet test backend/dotnet/tools/tests/Tw.Analyzers.Tests/Tw.Analyzers.Tests.csproj
dotnet test backend/dotnet/tools/tests/Tw.Cli.Tests/Tw.Cli.Tests.csproj
dotnet test backend/dotnet/tools/tests/Tw.Templates.Tests/Tw.Templates.Tests.csproj
```

Expected: each command exits with code `0`.

- [ ] **Step 5: Commit tools split**

Run:

```powershell
git add backend/dotnet/tools backend/dotnet/Tw.SmartPlatform.slnx
git commit -m "refactor: split dotnet tools source and tests"
```

Expected: commit succeeds.

## Task 5: Migrate Tools Charters to Formal Schema

**Files:**

- Modify: `backend/dotnet/tools/src/Tw.Analyzers/package-charter.yaml`
- Modify: `backend/dotnet/tools/src/Tw.Cli/package-charter.yaml`
- Modify: `backend/dotnet/tools/src/Tw.Templates/package-charter.yaml`
- Modify: `backend/dotnet/tools/src/Tw.Templates/content/building-block/src/Tw.Sample/package-charter.yaml`

- [ ] **Step 1: Replace `Tw.Analyzers` charter**

Set `backend/dotnet/tools/src/Tw.Analyzers/package-charter.yaml` to:

```yaml
schema_version: "1.0.0"
package: Tw.Analyzers
owner: dotnet-framework
responsibility: 提供 .NET 框架治理规则的 Roslyn 编译期诊断。
in_scope:
  - 框架包命名、项目引用、第三方依赖、用户密钥和 long ID 契约的编译期诊断
out_of_scope:
  - 运行时业务能力
  - 服务启动和依赖注入注册
public_capabilities:
  - TWGOV001
  - TWGOV002
  - TWGOV003
  - TWGOV004
  - TWGOV005
  - TWGOV006
dependency_rules:
  forbid:
    - runtime framework packages
  allow:
    - Microsoft.CodeAnalysis.CSharp
    - Microsoft.CodeAnalysis.Analyzers
stability: experimental
compatibility: 诊断 ID 保持稳定，诊断触发条件随治理规则同步调整。
```

- [ ] **Step 2: Replace `Tw.Cli` charter**

Set `backend/dotnet/tools/src/Tw.Cli/package-charter.yaml` to:

```yaml
schema_version: "1.0.0"
package: Tw.Cli
owner: dotnet-framework
responsibility: 提供项目创建、能力接入、契约校验、依赖审计和诊断的命令行入口。
in_scope:
  - `tw new` 项目创建入口
  - `tw add capability` 能力接入入口
  - `tw validate contracts` 契约校验入口
  - `tw audit dependencies` 依赖审计入口
  - `tw diagnose` 诊断入口
out_of_scope:
  - 业务运行时代码
  - 微服务内的领域逻辑
public_capabilities:
  - tw new
  - tw add capability
  - tw validate contracts
  - tw audit dependencies
  - tw diagnose
dependency_rules:
  forbid:
    - runtime-only framework implementation packages outside command services
  allow:
    - System.CommandLine
    - Spectre.Console
stability: experimental
compatibility: 命令名称和非零退出码作为自动化脚本契约保持稳定。
```

- [ ] **Step 3: Replace `Tw.Templates` charter**

Set `backend/dotnet/tools/src/Tw.Templates/package-charter.yaml` to:

```yaml
schema_version: "1.0.0"
package: Tw.Templates
owner: dotnet-framework
responsibility: 提供 service、gateway、building-block 和 contract-package 的官方 `dotnet new` 模板。
in_scope:
  - `tw-service` 模板
  - `tw-gateway` 模板
  - `tw-building-block` 模板
  - `tw-contract-package` 模板
out_of_scope:
  - 模板生成后的业务功能实现
  - 运行时框架包兼容壳
public_capabilities:
  - dotnet new tw-service
  - dotnet new tw-gateway
  - dotnet new tw-building-block
  - dotnet new tw-contract-package
dependency_rules:
  forbid:
    - runtime framework packages
    - compatibility aliases
  allow:
    - Microsoft.NET.Sdk
stability: experimental
compatibility: 模板短名称和输出目录结构作为项目创建契约保持稳定。
```

- [ ] **Step 4: Replace template sample charter**

Set `backend/dotnet/tools/src/Tw.Templates/content/building-block/src/Tw.Sample/package-charter.yaml` to:

```yaml
schema_version: "1.0.0"
package: Tw.Sample
owner: dotnet-framework
responsibility: 示例构建块模板的占位职责说明，生成后必须改为真实包职责。
in_scope:
  - 示例公共能力占位
out_of_scope:
  - 测试专用包
  - 已退役框架包名
public_capabilities:
  - Tw.Sample
dependency_rules:
  forbid:
    - test-only packages
    - retired framework package names
  allow: []
stability: experimental
compatibility: 模板生成内容不承诺对外兼容，生成后的真实包必须声明自身兼容性。
```

- [ ] **Step 5: Run charter validation tests**

Run:

```powershell
python -m pytest tools/tests/test_charter.py
dotnet test backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj --filter PackageCharterTests
```

Expected: Python tests pass. Architecture charter test still fails for remaining BuildingBlocks charters until Task 8 completes.

- [ ] **Step 6: Commit tools charters**

Run:

```powershell
git add backend/dotnet/tools/src/Tw.Analyzers/package-charter.yaml backend/dotnet/tools/src/Tw.Cli/package-charter.yaml backend/dotnet/tools/src/Tw.Templates/package-charter.yaml backend/dotnet/tools/src/Tw.Templates/content/building-block/src/Tw.Sample/package-charter.yaml
git commit -m "docs: migrate dotnet tools charters"
```

Expected: commit succeeds.

## Task 6: Mirror BuildingBlocks Tests by Capability

**Files:**

- Move: `backend/dotnet/BuildingBlocks/tests/<Project>` -> `backend/dotnet/BuildingBlocks/tests/<Capability>/<Project>`
- Delete: `backend/dotnet/BuildingBlocks/tests/*.Abstractions.Tests`
- Modify: `backend/dotnet/BuildingBlocks/tests/**/*.csproj`
- Modify: `backend/dotnet/Tw.SmartPlatform.slnx`

- [ ] **Step 1: Delete Abstractions test projects**

Run:

```powershell
$repo = (Resolve-Path ".").Path
if ($repo -ne "D:\DestinyWorkSpaces\Tw.SmartPlatform") { throw "Unexpected repo root: $repo" }
git rm -r backend\dotnet\BuildingBlocks\tests\Tw.AspNetCore.Abstractions.Tests
git rm -r backend\dotnet\BuildingBlocks\tests\Tw.Authorization.Abstractions.Tests
git rm -r backend\dotnet\BuildingBlocks\tests\Tw.DependencyInjection.Abstractions.Tests
git rm -r backend\dotnet\BuildingBlocks\tests\Tw.Json.Abstractions.Tests
git rm -r backend\dotnet\BuildingBlocks\tests\Tw.Validation.Abstractions.Tests
```

Expected: production `*.Abstractions` projects under `backend/dotnet/BuildingBlocks/src` remain untouched.

- [ ] **Step 2: Move non-Abstractions test projects to capability folders**

Run this PowerShell script from the repository root:

```powershell
$repo = (Resolve-Path ".").Path
if ($repo -ne "D:\DestinyWorkSpaces\Tw.SmartPlatform") { throw "Unexpected repo root: $repo" }
$srcRoot = Join-Path $repo "backend\dotnet\BuildingBlocks\src"
$testsRoot = Join-Path $repo "backend\dotnet\BuildingBlocks\tests"
$runtimeCapabilities = @{}
Get-ChildItem -Path $srcRoot -Filter *.csproj -Recurse | ForEach-Object {
    $runtimeCapabilities[$_.BaseName] = $_.Directory.Parent.Name
}
Get-ChildItem -Path $testsRoot -Directory | Where-Object { $_.Name -like "*.Tests*" } | Sort-Object Name | ForEach-Object {
    $projectName = $_.Name
    if ($projectName -eq "Tw.Architecture.Tests") {
        $capability = "Architecture"
    } elseif ($projectName.EndsWith(".Tests.Fixtures")) {
        $runtimeName = $projectName.Substring(0, $projectName.Length - ".Tests.Fixtures".Length)
        $capability = $runtimeCapabilities[$runtimeName]
    } else {
        $runtimeName = $projectName.Substring(0, $projectName.Length - ".Tests".Length)
        $capability = $runtimeCapabilities[$runtimeName]
    }
    if ([string]::IsNullOrWhiteSpace($capability)) { throw "Cannot resolve capability for $projectName" }
    $destinationParent = Join-Path $testsRoot $capability
    $destination = Join-Path $destinationParent $projectName
    if ((Resolve-Path -LiteralPath $_.FullName).Path -like "$repo*" -and $destination -like "$repo*") {
        New-Item -ItemType Directory -Force $destinationParent | Out-Null
        git mv $_.FullName $destination
    } else {
        throw "Refusing to move outside repository: $projectName"
    }
}
```

Expected: each test project path has shape `backend/dotnet/BuildingBlocks/tests/<Capability>/<Project>/<Project>.csproj`.

- [ ] **Step 3: Update BuildingBlocks test project references**

Run:

```powershell
Get-ChildItem backend\dotnet\BuildingBlocks\tests -Filter *.csproj -Recurse | ForEach-Object {
    $text = Get-Content -Raw -Encoding UTF8 $_.FullName
    $text = $text.Replace("..\..\src\", "..\..\..\src\")
    Set-Content -Encoding UTF8 -NoNewline -Path $_.FullName -Value $text
}
```

Expected: production references in test projects point from `tests/<Capability>/<Project>` back to `src/<Capability>/<RuntimeProject>`.

- [ ] **Step 4: Update solution BuildingBlocks test paths**

Generate the replacement project lines:

```powershell
Get-ChildItem backend\dotnet\BuildingBlocks\tests -Filter *.csproj -Recurse |
    Sort-Object FullName |
    ForEach-Object {
        $relative = (Resolve-Path -Relative $_.FullName).Substring(2).Replace("\", "/").Replace("backend/dotnet/", "")
        "    <Project Path=""$relative"" />"
    }
```

In `backend/dotnet/Tw.SmartPlatform.slnx`, replace every old `<Project Path="BuildingBlocks/tests/...` entry in the `/BuildingBlocks/tests/` folder block with the generated lines.

- [ ] **Step 5: Run topology tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj --filter PackageTopologyTests
```

Expected: topology tests pass.

- [ ] **Step 6: Commit BuildingBlocks test layout**

Run:

```powershell
git add backend/dotnet/BuildingBlocks/tests backend/dotnet/Tw.SmartPlatform.slnx
git commit -m "refactor: mirror building block test layout"
```

Expected: commit succeeds.

## Task 7: Clean Build Directory and Governance Documentation

**Files:**

- Delete: `backend/dotnet/Build/Build.cs`
- Delete: `backend/dotnet/Build/Build.csproj`
- Delete: `backend/dotnet/Build/QualityGates`
- Modify: `docs/engineering-standards/10-governance/dotnet-framework-governance.md`

- [ ] **Step 1: Remove placeholder Build runner and QualityGates scripts**

Run:

```powershell
$repo = (Resolve-Path ".").Path
if ($repo -ne "D:\DestinyWorkSpaces\Tw.SmartPlatform") { throw "Unexpected repo root: $repo" }
git rm backend\dotnet\Build\Build.cs
git rm backend\dotnet\Build\Build.csproj
git rm -r backend\dotnet\Build\QualityGates
```

Expected: `backend/dotnet/Build` retains `Packages.*.props` and `packages.lock.json`.

- [ ] **Step 2: Replace governance local commands**

In `docs/engineering-standards/10-governance/dotnet-framework-governance.md`, replace the `## Local Commands` section with:

````markdown
## Local Commands

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj
python -m pytest tools/tests/test_charter.py
dotnet test backend/dotnet/Tw.SmartPlatform.slnx
```

治理检查由架构测试、Python charter 校验和解决方案测试承载。`backend/dotnet/Build` 只保存中央包版本 `.props` 与必要锁定文件。
````

- [ ] **Step 3: Run Build layout test**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj --filter BuildLayoutTests
```

Expected: PASS.

- [ ] **Step 4: Scan governance doc for removed command paths**

Run:

```powershell
Select-String -Path docs\engineering-standards\10-governance\dotnet-framework-governance.md -Pattern 'Build.csproj|QualityGates'
```

Expected: no matches.

- [ ] **Step 5: Commit Build cleanup**

Run:

```powershell
git add backend/dotnet/Build docs/engineering-standards/10-governance/dotnet-framework-governance.md
git commit -m "refactor: remove placeholder dotnet build gates"
```

Expected: commit succeeds.

## Task 8: Migrate BuildingBlocks Charters to Chinese Formal Schema

**Files:**

- Modify: `backend/dotnet/BuildingBlocks/src/**/package-charter.yaml`

- [ ] **Step 1: List invalid runtime charters**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj --filter PackageCharterTests
```

Expected: FAIL with a list of charters that still use old schema or non-Chinese natural-language content.

- [ ] **Step 2: Convert each failing charter to the formal schema**

For each failing `package-charter.yaml`, keep `package` equal to the `.csproj` filename without extension and write these fields in this order. The values below show the canonical shape; the edited file must use package-specific Chinese content and existing package-specific dependency rules.

```yaml
schema_version: "1.0.0"
package: Tw.Core
owner: dotnet-framework
responsibility: 提供跨服务复用的基础原语与无框架依赖工具。
in_scope:
  - 基础值对象
  - 通用结果类型
out_of_scope:
  - HTTP 中间件
  - 数据访问实现
public_capabilities:
  - Tw.Core
dependency_rules:
  forbid: []
  allow: []
stability: experimental
compatibility: 采纳前阶段允许破坏性调整，退出采纳前阶段后按迁移说明沟通。
```

Use `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/package-charter.yaml` as the content style reference. Preserve existing package-specific dependency rules when present.

- [ ] **Step 3: Verify all runtime charters**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj --filter PackageCharterTests
```

Expected: PASS.

- [ ] **Step 4: Commit runtime charters**

Run:

```powershell
git add backend/dotnet/BuildingBlocks/src
git commit -m "docs: normalize building block package charters"
```

Expected: commit succeeds.

## Task 9: Fill XML Documentation and Remove Confirmed Empty Shells

**Files:**

- Modify: `backend/dotnet/BuildingBlocks/src/**/*.cs`
- Modify: `backend/dotnet/BuildingBlocks/tests/**/*.cs`
- Modify: `backend/dotnet/tools/src/**/*.cs`
- Modify: `backend/dotnet/tools/tests/**/*.cs`
- Delete: empty directories and directories containing only `bin` or `obj`

- [ ] **Step 1: Produce the current XML documentation violation list**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj --filter XmlDocumentationTests
```

Expected: FAIL with file and line entries for undocumented declarations.

- [ ] **Step 2: Fix documentation by capability order**

Edit files in this order and rerun the XML documentation test after each group:

```text
backend/dotnet/BuildingBlocks/src/Foundation
backend/dotnet/BuildingBlocks/src/Application
backend/dotnet/BuildingBlocks/src/Web
backend/dotnet/BuildingBlocks/src/Auditing
backend/dotnet/BuildingBlocks/src/BackgroundJobs
backend/dotnet/BuildingBlocks/src/Caching
backend/dotnet/BuildingBlocks/src/Configuration
backend/dotnet/BuildingBlocks/src/Data
backend/dotnet/BuildingBlocks/src/DistributedLocking
backend/dotnet/BuildingBlocks/src/EventBus
backend/dotnet/BuildingBlocks/src/Excel
backend/dotnet/BuildingBlocks/src/Gateway
backend/dotnet/BuildingBlocks/src/Grpc
backend/dotnet/BuildingBlocks/src/Http
backend/dotnet/BuildingBlocks/src/IdGeneration
backend/dotnet/BuildingBlocks/src/Idempotency
backend/dotnet/BuildingBlocks/src/Localization
backend/dotnet/BuildingBlocks/src/MultiTenancy
backend/dotnet/BuildingBlocks/src/Observability
backend/dotnet/BuildingBlocks/src/Resilience
backend/dotnet/BuildingBlocks/src/Sharding
backend/dotnet/BuildingBlocks/src/TestBase
backend/dotnet/BuildingBlocks/src/TextTemplating
backend/dotnet/BuildingBlocks/tests
backend/dotnet/tools/src
backend/dotnet/tools/tests
```

For every declaration in the failure list, add XML comments that describe responsibility, parameters, return value, exception semantics, boundaries, or side effects. Do not use “获取”“设置”“获取或设置” in property and field comments.

- [ ] **Step 3: Delete only confirmed empty shell directories**

Run this check:

```powershell
Get-ChildItem backend\dotnet -Directory -Recurse |
    Where-Object {
        $children = Get-ChildItem -Force $_.FullName
        $children.Count -gt 0 -and
        ($children | Where-Object { $_.Name -notin @("bin", "obj") }).Count -eq 0
    } |
    Select-Object -ExpandProperty FullName
```

For each listed directory, confirm it does not contain `.csproj`, `package-charter.yaml`, template content, documentation, or source files. Remove it with:

```powershell
$repo = (Resolve-Path ".").Path
if ($repo -ne "D:\DestinyWorkSpaces\Tw.SmartPlatform") { throw "Unexpected repo root: $repo" }
$emptyShellDirectories = Get-ChildItem backend\dotnet -Directory -Recurse |
    Where-Object {
        $children = Get-ChildItem -Force $_.FullName
        $children.Count -gt 0 -and
        ($children | Where-Object { $_.Name -notin @("bin", "obj") }).Count -eq 0
    }
foreach ($directory in $emptyShellDirectories) {
    $resolved = (Resolve-Path -LiteralPath $directory.FullName).Path
    if ($resolved -notlike "$repo*") { throw "Refusing to remove outside repository: $resolved" }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
```

- [ ] **Step 4: Verify XML documentation test passes**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj --filter XmlDocumentationTests
```

Expected: PASS.

- [ ] **Step 5: Commit documentation cleanup**

Run:

```powershell
git add backend/dotnet/BuildingBlocks backend/dotnet/tools
git commit -m "docs: add dotnet XML documentation coverage"
```

Expected: commit succeeds.

## Task 10: Final Verification

**Files:**

- Verify all changed files

- [ ] **Step 1: Run architecture tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj
```

Expected: PASS.

- [ ] **Step 2: Run Python tests**

Run:

```powershell
python -m pytest tools/tests/test_charter.py
```

Expected: PASS.

- [ ] **Step 3: Run tools tests**

Run:

```powershell
dotnet test backend/dotnet/tools/tests/Tw.Analyzers.Tests/Tw.Analyzers.Tests.csproj
dotnet test backend/dotnet/tools/tests/Tw.Cli.Tests/Tw.Cli.Tests.csproj
dotnet test backend/dotnet/tools/tests/Tw.Templates.Tests/Tw.Templates.Tests.csproj
```

Expected: PASS for all three commands.

- [ ] **Step 4: Run solution tests**

Run:

```powershell
dotnet test backend/dotnet/Tw.SmartPlatform.slnx
```

Expected: PASS.

- [ ] **Step 5: Confirm deleted paths stay gone**

Run:

```powershell
Test-Path backend\dotnet\Build\Build.cs
Test-Path backend\dotnet\Build\Build.csproj
Test-Path backend\dotnet\Build\QualityGates
Get-ChildItem backend\dotnet\BuildingBlocks\tests -Filter *.Abstractions.Tests.csproj -Recurse
```

Expected: the three `Test-Path` commands print `False`; the final command prints no projects.

- [ ] **Step 6: Review final diff**

Run:

```powershell
git status --short
git diff --stat HEAD
```

Expected: only intended governance refactor files are changed since the last task commit.
