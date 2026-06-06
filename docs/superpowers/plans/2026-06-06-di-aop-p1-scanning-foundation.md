# P1 扫描地基 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 新增执行引擎包 `Tw.DependencyInjection`，落地程序集发现、白/黑名单过滤、依赖拓扑排序与循环诊断、`UseAutofac()` 容器接管，以及 `ServiceRegistrationReport` 骨架。

**Architecture:** `Tw.DependencyInjection` 引用 `Tw.Core` 消费 P0 抽象，直接引用 Autofac 执行容器接管。本阶段把可测试的纯逻辑（过滤、拓扑）与运行时副作用（程序集加载、宿主接管）分离：`AssemblyFilter` 和 `AssemblyTopologySorter` 是纯函数，单元测试不依赖真实运行时；`IAssemblySource` 抽象程序集来源，`AssemblyDiscoverer` 编排过滤+拓扑并产出报告。注册仲裁、Options 装载、AOP 承载属于 P2–P4，不在本阶段实现，`ServiceRegistrationReport` 只填充扫描与拓扑段落。

**Tech Stack:** C# / .NET 10、Autofac 9.x、Autofac.Extensions.DependencyInjection 11.x、Microsoft.Extensions.DependencyModel、Microsoft.Extensions.Hosting.Abstractions、xunit、FluentAssertions、中央包管理（CPM）、NuGet 锁定文件。

**对应 spec：** [docs/superpowers/specs/2026-06-06-di-options-aop-design.md](../specs/2026-06-06-di-options-aop-design.md)，阶段 P1。

**前置阶段：** P0 抽象地基（[2026-06-06-di-aop-p0-core-abstractions.md](2026-06-06-di-aop-p0-core-abstractions.md)）已合并到 `master`。`Tw.Core` 已提供 `Tw.DependencyInjection.Abstractions` 等命名空间。

**前置规范（实现前必读）：**
- [docs/engineering-standards/03-project-and-code/language-specific/dotnet-core.md](../../engineering-standards/03-project-and-code/language-specific/dotnet-core.md)（命名空间=RootNamespace+文件夹、跨程序集不共享命名空间、XML 文档注释、共享包扩展命名）
- [docs/engineering-standards/03-project-and-code/shared-package-charter.md](../../engineering-standards/03-project-and-code/shared-package-charter.md)（charter 字段、新增包流程、能力使用文档与索引联动）

**通用约定：**
- 公开类型与公开成员必须带 DocFX XML 文档注释（`<summary>` 等）；内部类型不要求。
- 引擎执行类型落在无后缀命名空间，根命名空间为 `Tw.DependencyInjection`（区别于 `Tw.Core` 侧的 `Tw.DependencyInjection.Abstractions`），不向 `.Abstractions` 命名空间贡献类型。
- 测试命名 `成员_预期[_条件]`，命名空间 `Tw.DependencyInjection.Tests.<Area>`。
- 引擎构建命令：`dotnet build backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Tw.DependencyInjection.csproj`
- 引擎测试命令：`dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`
- `RestorePackagesWithLockFile` 全局开启，新增项目首次还原会生成 `packages.lock.json`，必须随对应提交一并提交。
- 当前分支：从最新 `master` 拉出 `feat/di-scanning-foundation`。

---

## 文件结构

**新增中央包版本（修改现有文件）：**
- `backend/dotnet/Build/Packages.Microsoft.props`：新增 `Microsoft.Extensions.DependencyModel`

**新增引擎包 `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/`（`RootNamespace = Tw.DependencyInjection`）：**
- `Tw.DependencyInjection.csproj`
- `package-charter.yaml`
- `ServiceRegistrationOptions.cs`（命名空间 `Tw.DependencyInjection`，公开）— 扫描白/黑名单选项 POCO
- `ServiceRegistrationException.cs`（命名空间 `Tw.DependencyInjection`，公开）— 启动期失败异常
- `AutofacHostBuilderExtensions.cs`（命名空间 `Tw.DependencyInjection`，公开）— `UseAutofac()`
- `Discovery/IAssemblySource.cs`（命名空间 `Tw.DependencyInjection.Discovery`，内部）
- `Discovery/RuntimeAssemblySource.cs`（内部）
- `Discovery/AssemblyFilter.cs`（内部，纯函数）
- `Discovery/AssemblyDescriptor.cs`（内部 record）
- `Discovery/AssemblyTopologySorter.cs`（内部，纯函数）
- `Discovery/AssemblyDiscoverer.cs`（内部）+ `AssemblyDiscoveryResult`（内部 record）
- `Diagnostics/AssemblyTopologyEntry.cs`（命名空间 `Tw.DependencyInjection.Diagnostics`，公开 record）
- `Diagnostics/ServiceRegistrationReport.cs`（公开，骨架）

**新增测试包 `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/`：**
- `Tw.DependencyInjection.Tests.csproj`
- `Discovery/AssemblyFilterTests.cs`
- `Discovery/AssemblyTopologySorterTests.cs`
- `Discovery/AssemblyDiscovererTests.cs`
- `Discovery/RuntimeAssemblySourceTests.cs`
- `Hosting/AutofacHostBuilderExtensionsTests.cs`

**修改解决方案与文档：**
- `backend/dotnet/Tw.SmartPlatform.slnx`：登记两个新项目
- `docs/shared-packages/dotnet/Tw.DependencyInjection/README.md`（新增，索引页）
- `docs/shared-packages/dotnet/Tw.DependencyInjection/assembly-scanning.md`（新增，How-to）
- `docs/shared-packages/dotnet/README.md`：包索引追加条目

---

## Task 1: 中央包管理新增 Microsoft.Extensions.DependencyModel

**Files:**
- Modify: `backend/dotnet/Build/Packages.Microsoft.props`

> 设计说明：`RuntimeAssemblySource`（Task 8）用 `DependencyContext.Default` 读取依赖上下文中的程序集名（spec「程序集发现」节「依赖上下文」）。该能力来自 `Microsoft.Extensions.DependencyModel`，CPM 下必须先在中央声明版本，引擎 csproj 才能无版本引用。版本对齐其他 Microsoft.Extensions 10.0 线。

- [ ] **Step 1: 新增 PackageVersion**

在 `backend/dotnet/Build/Packages.Microsoft.props` 的 `<!-- Microsoft.Extensions 核心包 -->` 分组内，`Microsoft.Extensions.DependencyInjection.Abstractions` 行下方追加：

```xml
    <PackageVersion Include="Microsoft.Extensions.DependencyModel" Version="10.0.0" />
```

- [ ] **Step 2: 提交**

```bash
git add backend/dotnet/Build/Packages.Microsoft.props
git commit -m "build(deps): add Microsoft.Extensions.DependencyModel central version"
```

---

## Task 2: 脚手架 Tw.DependencyInjection 引擎包与测试包

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Tw.DependencyInjection.csproj`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`
- Modify: `backend/dotnet/Tw.SmartPlatform.slnx`

> 设计说明：引擎包 `RootNamespace` 直接设为 `Tw.DependencyInjection`，使根目录文件落在 `Tw.DependencyInjection` 命名空间、子文件夹落在 `Tw.DependencyInjection.<Folder>`，与 dotnet-core.md「命名空间=RootNamespace+文件夹」一致，且与 `Tw.Core` 的 `.Abstractions` 命名空间互斥。`TargetFramework`/`Nullable`/`ImplicitUsings` 由 [Directory.Build.props](../../../backend/dotnet/Directory.Build.props) 提供，不在 csproj 重复。`InternalsVisibleTo` 让测试项目访问内部发现/拓扑类型，保持引擎公开面最小。本阶段只需 Autofac 与 Autofac.Extensions.DependencyInjection；Castle 在 P4 引入。

- [ ] **Step 1: 创建引擎 csproj**

新建 `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Tw.DependencyInjection.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <RootNamespace>Tw.DependencyInjection</RootNamespace>
    <IsPackable>true</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Autofac" />
    <PackageReference Include="Autofac.Extensions.DependencyInjection" />
    <PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.DependencyModel" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Tw.Core\Tw.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="Tw.DependencyInjection.Tests" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: 创建测试 csproj**

新建 `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="Microsoft.Extensions.Hosting" />
    <PackageReference Include="Autofac.Extensions.DependencyInjection" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Tw.DependencyInjection\Tw.DependencyInjection.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: 登记到解决方案**

在 `backend/dotnet/Tw.SmartPlatform.slnx` 的 `/BuildingBlocks/src/` 文件夹内追加引擎项目（放在 `Tw.Core` 行下方）：

```xml
    <Project Path="BuildingBlocks/src/Tw.DependencyInjection/Tw.DependencyInjection.csproj" />
```

在 `/BuildingBlocks/tests/` 文件夹内追加测试项目（放在 `Tw.Core.Tests` 行下方）：

```xml
    <Project Path="BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj" />
```

- [ ] **Step 4: 还原并构建确认空项目可编译**

Run: `dotnet build backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`
Expected: 构建成功（无 .cs 文件的引擎程序集为空程序集，正常编译）；两个项目各生成 `packages.lock.json`。

- [ ] **Step 5: 提交**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests backend/dotnet/Tw.SmartPlatform.slnx
git commit -m "build(di): scaffold Tw.DependencyInjection engine and test projects"
```

---

## Task 3: ServiceRegistrationOptions 扫描选项

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/ServiceRegistrationOptions.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Discovery/AssemblyFilterTests.cs`（本任务先建文件验证选项可构造，Task 4 补全过滤断言）

> 设计说明：spec「程序集发现」节四个配置项 `IncludeAssemblies` / `ExcludeAssemblies` / `IncludeAssemblyPrefixes` / `ExcludeAssemblyPrefixes`。P2 的 `AddServiceRegistration(IConfiguration)` 会把 `Tw:DependencyInjection` 节绑定到本类型；本阶段只定义可被配置绑定器填充的可读集合属性（get-only + 初始化集合，绑定器向其中追加），不实现绑定。

- [ ] **Step 1: 写失败测试**

新建 `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Discovery/AssemblyFilterTests.cs`：

```csharp
using FluentAssertions;
using Tw.DependencyInjection;
using Xunit;

namespace Tw.DependencyInjection.Tests.Discovery;

public class AssemblyFilterTests
{
    [Fact]
    public void Options_DefaultsToEmptyLists()
    {
        var options = new ServiceRegistrationOptions();

        options.IncludeAssemblies.Should().BeEmpty();
        options.ExcludeAssemblies.Should().BeEmpty();
        options.IncludeAssemblyPrefixes.Should().BeEmpty();
        options.ExcludeAssemblyPrefixes.Should().BeEmpty();
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`
Expected: 编译失败，`ServiceRegistrationOptions` 不存在。

- [ ] **Step 3: 实现 ServiceRegistrationOptions**

新建 `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/ServiceRegistrationOptions.cs`：

```csharp
namespace Tw.DependencyInjection;

/// <summary>
/// 控制程序集发现与注册规划的引擎选项，对应配置节 <c>Tw:DependencyInjection</c>
/// </summary>
public sealed class ServiceRegistrationOptions
{
    /// <summary>额外纳入扫描的程序集名（精确匹配），在内置 <c>Tw.</c> 前缀之外补充白名单</summary>
    public IList<string> IncludeAssemblies { get; } = new List<string>();

    /// <summary>排除出扫描的程序集名（精确匹配），优先于任何白名单</summary>
    public IList<string> ExcludeAssemblies { get; } = new List<string>();

    /// <summary>额外纳入扫描的程序集名前缀，叠加在内置 <c>Tw.</c> 前缀之上</summary>
    public IList<string> IncludeAssemblyPrefixes { get; } = new List<string>();

    /// <summary>排除出扫描的程序集名前缀，优先于任何白名单</summary>
    public IList<string> ExcludeAssemblyPrefixes { get; } = new List<string>();
}
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`
Expected: PASS。

- [ ] **Step 5: 提交**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/ServiceRegistrationOptions.cs backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Discovery/AssemblyFilterTests.cs
git commit -m "feat(di): add ServiceRegistrationOptions scan options"
```

---

## Task 4: AssemblyFilter 白/黑名单过滤

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Discovery/AssemblyFilter.cs`
- Test: 追加到 `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Discovery/AssemblyFilterTests.cs`

> 设计说明：spec「程序集发现」节——默认扫描 `Tw.` 前缀，配置项叠加白名单、收窄黑名单，**黑名单优先于白名单**。过滤是纯函数（输入程序集名集合 + 选项 → 入选名集合），与运行时程序集加载解耦，便于穷举边界测试。判定顺序：先判是否入选（精确白名单 或 `Tw.` 前缀 或 自定义白名单前缀），再判是否排除（精确黑名单 或 黑名单前缀）；排除胜出。

- [ ] **Step 1: 追加失败测试**

把 `Discovery/AssemblyFilterTests.cs` 替换为：

```csharp
using FluentAssertions;
using Tw.DependencyInjection;
using Tw.DependencyInjection.Discovery;
using Xunit;

namespace Tw.DependencyInjection.Tests.Discovery;

public class AssemblyFilterTests
{
    [Fact]
    public void Options_DefaultsToEmptyLists()
    {
        var options = new ServiceRegistrationOptions();

        options.IncludeAssemblies.Should().BeEmpty();
        options.ExcludeAssemblies.Should().BeEmpty();
        options.IncludeAssemblyPrefixes.Should().BeEmpty();
        options.ExcludeAssemblyPrefixes.Should().BeEmpty();
    }

    [Fact]
    public void Filter_KeepsTwPrefix_ByDefault()
    {
        var result = AssemblyFilter.Filter(
            ["Tw.Core", "Tw.Order.Application", "System.Text.Json", "Newtonsoft.Json"],
            new ServiceRegistrationOptions());

        result.Should().BeEquivalentTo("Tw.Core", "Tw.Order.Application");
    }

    [Fact]
    public void Filter_IncludesExplicitAssembly_WithoutTwPrefix()
    {
        var options = new ServiceRegistrationOptions();
        options.IncludeAssemblies.Add("Acme.Payments");

        var result = AssemblyFilter.Filter(["Acme.Payments", "Contoso.Crm"], options);

        result.Should().BeEquivalentTo("Acme.Payments");
    }

    [Fact]
    public void Filter_IncludesCustomPrefix_InAdditionToTw()
    {
        var options = new ServiceRegistrationOptions();
        options.IncludeAssemblyPrefixes.Add("Acme.");

        var result = AssemblyFilter.Filter(["Tw.Core", "Acme.Payments", "Contoso.Crm"], options);

        result.Should().BeEquivalentTo("Tw.Core", "Acme.Payments");
    }

    [Fact]
    public void Filter_ExcludesByName_EvenWhenTwPrefix()
    {
        var options = new ServiceRegistrationOptions();
        options.ExcludeAssemblies.Add("Tw.Legacy");

        var result = AssemblyFilter.Filter(["Tw.Core", "Tw.Legacy"], options);

        result.Should().BeEquivalentTo("Tw.Core");
    }

    [Fact]
    public void Filter_ExcludesByPrefix_EvenWhenTwPrefix()
    {
        var options = new ServiceRegistrationOptions();
        options.ExcludeAssemblyPrefixes.Add("Tw.Test");

        var result = AssemblyFilter.Filter(["Tw.Core", "Tw.TestKit"], options);

        result.Should().BeEquivalentTo("Tw.Core");
    }

    [Fact]
    public void Filter_BlacklistWins_WhenNameBothIncludedAndExcluded()
    {
        var options = new ServiceRegistrationOptions();
        options.IncludeAssemblies.Add("Tw.Order");
        options.ExcludeAssemblies.Add("Tw.Order");

        var result = AssemblyFilter.Filter(["Tw.Order"], options);

        result.Should().BeEmpty();
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`
Expected: 编译失败，`AssemblyFilter` 不存在。

- [ ] **Step 3: 实现 AssemblyFilter**

新建 `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Discovery/AssemblyFilter.cs`：

```csharp
namespace Tw.DependencyInjection.Discovery;

internal static class AssemblyFilter
{
    private const string DefaultPrefix = "Tw.";

    public static IReadOnlyList<string> Filter(
        IEnumerable<string> assemblyNames, ServiceRegistrationOptions options)
    {
        ArgumentNullException.ThrowIfNull(assemblyNames);
        ArgumentNullException.ThrowIfNull(options);

        var included = new List<string>();
        foreach (var name in assemblyNames)
        {
            if (IsIncluded(name, options) && !IsExcluded(name, options))
            {
                included.Add(name);
            }
        }

        return included;
    }

    private static bool IsIncluded(string name, ServiceRegistrationOptions options)
    {
        if (options.IncludeAssemblies.Contains(name, StringComparer.Ordinal))
        {
            return true;
        }

        if (name.StartsWith(DefaultPrefix, StringComparison.Ordinal))
        {
            return true;
        }

        foreach (var prefix in options.IncludeAssemblyPrefixes)
        {
            if (name.StartsWith(prefix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsExcluded(string name, ServiceRegistrationOptions options)
    {
        if (options.ExcludeAssemblies.Contains(name, StringComparer.Ordinal))
        {
            return true;
        }

        foreach (var prefix in options.ExcludeAssemblyPrefixes)
        {
            if (name.StartsWith(prefix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`
Expected: PASS。

- [ ] **Step 5: 提交**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Discovery/AssemblyFilter.cs backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Discovery/AssemblyFilterTests.cs
git commit -m "feat(di): add assembly include/exclude filter with blacklist precedence"
```

---

## Task 5: ServiceRegistrationException 启动失败异常

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/ServiceRegistrationException.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Discovery/AssemblyTopologySorterTests.cs`（本任务先建文件并断言异常类型，Task 6 补全拓扑断言）

> 设计说明：spec「启动失败规则」要求拓扑循环等场景启动失败。引擎统一抛 `ServiceRegistrationException`，继承 `Tw.Core` 的 `Tw.Exceptions.TwException`，使调用方可按既有 Tw 异常基类统一处理。

- [ ] **Step 1: 写失败测试**

新建 `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Discovery/AssemblyTopologySorterTests.cs`：

```csharp
using FluentAssertions;
using Tw.DependencyInjection;
using Tw.Exceptions;
using Xunit;

namespace Tw.DependencyInjection.Tests.Discovery;

public class AssemblyTopologySorterTests
{
    [Fact]
    public void ServiceRegistrationException_DerivesFromTwException()
    {
        var exception = new ServiceRegistrationException("boom");

        exception.Should().BeAssignableTo<TwException>();
        exception.Message.Should().Be("boom");
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`
Expected: 编译失败，`ServiceRegistrationException` 不存在。

- [ ] **Step 3: 实现异常**

新建 `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/ServiceRegistrationException.cs`：

```csharp
using Tw.Exceptions;

namespace Tw.DependencyInjection;

/// <summary>
/// 服务注册规划在启动期失败时抛出，例如程序集拓扑存在循环依赖
/// </summary>
public sealed class ServiceRegistrationException : TwException
{
    /// <summary>使用错误消息初始化 <see cref="ServiceRegistrationException"/> 的新实例</summary>
    /// <param name="message">描述失败原因的消息</param>
    public ServiceRegistrationException(string message)
        : base(message)
    {
    }
}
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`
Expected: PASS。

- [ ] **Step 5: 提交**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/ServiceRegistrationException.cs backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Discovery/AssemblyTopologySorterTests.cs
git commit -m "feat(di): add ServiceRegistrationException for startup failures"
```

---

## Task 6: 程序集拓扑排序与循环诊断

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Diagnostics/AssemblyTopologyEntry.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Discovery/AssemblyDescriptor.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Discovery/AssemblyTopologySorter.cs`
- Test: 追加到 `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Discovery/AssemblyTopologySorterTests.cs`

> 设计说明：spec「程序集发现」节——按引用关系拓扑排序，被依赖在前、依赖方在后；循环则启动失败并输出完整环路。用 DFS 三色标记同时完成拓扑序（后序天然得到「依赖在前」）、层级计算（节点层级=范围内依赖层级最大值+1，无范围内依赖为 0）和环检测（命中 InProgress 节点即回路，从 DFS 栈切出完整环链）。`AssemblyDescriptor` 只携带名字与「引用的程序集名」，排序逻辑不触碰真实 `Assembly`，便于纯逻辑测试。`Level` 为 P2 的 `TopologyBaseValue` 提供层级输入。

- [ ] **Step 1: 追加失败测试**

把 `Discovery/AssemblyTopologySorterTests.cs` 替换为：

```csharp
using FluentAssertions;
using Tw.DependencyInjection;
using Tw.DependencyInjection.Diagnostics;
using Tw.DependencyInjection.Discovery;
using Tw.Exceptions;
using Xunit;

namespace Tw.DependencyInjection.Tests.Discovery;

public class AssemblyTopologySorterTests
{
    private static AssemblyDescriptor Node(string name, params string[] references) =>
        new(name, references);

    [Fact]
    public void ServiceRegistrationException_DerivesFromTwException()
    {
        var exception = new ServiceRegistrationException("boom");

        exception.Should().BeAssignableTo<TwException>();
        exception.Message.Should().Be("boom");
    }

    [Fact]
    public void Sort_OrdersDependenciesBeforeDependents()
    {
        var result = AssemblyTopologySorter.Sort(
        [
            Node("Tw.App", "Tw.Domain"),
            Node("Tw.Domain", "Tw.Core"),
            Node("Tw.Core"),
        ]);

        result.Select(e => e.AssemblyName).Should()
            .ContainInOrder("Tw.Core", "Tw.Domain", "Tw.App");
    }

    [Fact]
    public void Sort_AssignsLevels_ByDependencyDepth()
    {
        var result = AssemblyTopologySorter.Sort(
        [
            Node("Tw.App", "Tw.Domain"),
            Node("Tw.Domain", "Tw.Core"),
            Node("Tw.Core"),
        ]);

        result.Should().Contain(e => e.AssemblyName == "Tw.Core" && e.Level == 0);
        result.Should().Contain(e => e.AssemblyName == "Tw.Domain" && e.Level == 1);
        result.Should().Contain(e => e.AssemblyName == "Tw.App" && e.Level == 2);
    }

    [Fact]
    public void Sort_IgnoresReferences_OutsideScannedSet()
    {
        var result = AssemblyTopologySorter.Sort(
        [
            Node("Tw.Core", "System.Text.Json"),
        ]);

        result.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new AssemblyTopologyEntry("Tw.Core", 0));
    }

    [Fact]
    public void Sort_Throws_WithFullCycleChain_OnCircularDependency()
    {
        var act = () => AssemblyTopologySorter.Sort(
        [
            Node("Tw.A", "Tw.B"),
            Node("Tw.B", "Tw.C"),
            Node("Tw.C", "Tw.A"),
        ]);

        act.Should().Throw<ServiceRegistrationException>()
            .WithMessage("*Tw.A -> Tw.B -> Tw.C -> Tw.A*");
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`
Expected: 编译失败，`AssemblyDescriptor`、`AssemblyTopologyEntry`、`AssemblyTopologySorter` 不存在。

- [ ] **Step 3: 实现 AssemblyTopologyEntry**

新建 `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Diagnostics/AssemblyTopologyEntry.cs`：

```csharp
namespace Tw.DependencyInjection.Diagnostics;

/// <summary>
/// 程序集在依赖拓扑中的位置
/// </summary>
/// <param name="AssemblyName">程序集名</param>
/// <param name="Level">拓扑层级；在扫描范围内无依赖的程序集为 0，依赖方层级递增</param>
public sealed record AssemblyTopologyEntry(string AssemblyName, int Level);
```

- [ ] **Step 4: 实现 AssemblyDescriptor**

新建 `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Discovery/AssemblyDescriptor.cs`：

```csharp
namespace Tw.DependencyInjection.Discovery;

/// <summary>拓扑排序输入：程序集名与其引用的程序集名</summary>
internal sealed record AssemblyDescriptor(string Name, IReadOnlyList<string> ReferencedAssemblyNames);
```

- [ ] **Step 5: 实现 AssemblyTopologySorter**

新建 `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Discovery/AssemblyTopologySorter.cs`：

```csharp
using Tw.DependencyInjection.Diagnostics;

namespace Tw.DependencyInjection.Discovery;

internal static class AssemblyTopologySorter
{
    public static IReadOnlyList<AssemblyTopologyEntry> Sort(IReadOnlyList<AssemblyDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);

        var byName = descriptors.ToDictionary(d => d.Name, StringComparer.Ordinal);
        var state = new Dictionary<string, Mark>(StringComparer.Ordinal);
        var levels = new Dictionary<string, int>(StringComparer.Ordinal);
        var order = new List<string>();
        var path = new List<string>();

        foreach (var descriptor in descriptors)
        {
            Visit(descriptor.Name);
        }

        return order.Select(name => new AssemblyTopologyEntry(name, levels[name])).ToList();

        int Visit(string name)
        {
            if (state.TryGetValue(name, out var mark))
            {
                if (mark == Mark.InProgress)
                {
                    var cycleStart = path.IndexOf(name);
                    var chain = path.Skip(cycleStart).Append(name);
                    throw new ServiceRegistrationException(
                        "检测到程序集循环依赖: " + string.Join(" -> ", chain));
                }

                return levels[name];
            }

            state[name] = Mark.InProgress;
            path.Add(name);

            var level = 0;
            foreach (var reference in byName[name].ReferencedAssemblyNames)
            {
                if (byName.ContainsKey(reference))
                {
                    level = Math.Max(level, Visit(reference) + 1);
                }
            }

            path.RemoveAt(path.Count - 1);
            state[name] = Mark.Done;
            levels[name] = level;
            order.Add(name);
            return level;
        }
    }

    private enum Mark
    {
        InProgress,
        Done,
    }
}
```

- [ ] **Step 6: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`
Expected: PASS。

- [ ] **Step 7: 提交**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Diagnostics/AssemblyTopologyEntry.cs backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Discovery/AssemblyDescriptor.cs backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Discovery/AssemblyTopologySorter.cs backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Discovery/AssemblyTopologySorterTests.cs
git commit -m "feat(di): add assembly topology sort with cycle diagnostics"
```

---

## Task 7: ServiceRegistrationReport 骨架

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Diagnostics/ServiceRegistrationReport.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Discovery/AssemblyDiscovererTests.cs`（本任务建文件并断言报告可构造，Task 8 补全发现编排断言）

> 设计说明：spec「诊断报告」节列出 `ServiceRegistrationReport` 的完整字段（候选服务、最终注册、仲裁、keyed、跳过、冲突等）。这些字段对应 P2+ 能力，本阶段只落地扫描与拓扑两段，得到可工作的最小报告；后续阶段在同一类型上追加属性。这是真实的最小骨架，不是占位。

- [ ] **Step 1: 写失败测试**

新建 `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Discovery/AssemblyDiscovererTests.cs`：

```csharp
using FluentAssertions;
using Tw.DependencyInjection.Diagnostics;
using Xunit;

namespace Tw.DependencyInjection.Tests.Discovery;

public class AssemblyDiscovererTests
{
    [Fact]
    public void Report_ExposesScanAndTopologySections()
    {
        var report = new ServiceRegistrationReport(
            scannedAssemblies: ["Tw.Core"],
            excludedAssemblies: ["System.Text.Json"],
            topology: [new AssemblyTopologyEntry("Tw.Core", 0)]);

        report.ScannedAssemblies.Should().ContainSingle().Which.Should().Be("Tw.Core");
        report.ExcludedAssemblies.Should().ContainSingle().Which.Should().Be("System.Text.Json");
        report.Topology.Should().ContainSingle().Which.Level.Should().Be(0);
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`
Expected: 编译失败，`ServiceRegistrationReport` 不存在。

- [ ] **Step 3: 实现 ServiceRegistrationReport**

新建 `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Diagnostics/ServiceRegistrationReport.cs`：

```csharp
namespace Tw.DependencyInjection.Diagnostics;

/// <summary>
/// 服务注册规划诊断报告
/// </summary>
/// <remarks>
/// P1 仅填充程序集扫描与拓扑段落；候选服务、最终注册、仲裁结果、keyed 注册、
/// 跳过与冲突原因等段落由后续阶段（P2 起）在本类型上扩展。报告只承载摘要元数据，不输出敏感配置值。
/// </remarks>
public sealed class ServiceRegistrationReport
{
    /// <summary>初始化 <see cref="ServiceRegistrationReport"/> 的新实例</summary>
    /// <param name="scannedAssemblies">按拓扑顺序（被依赖在前）纳入扫描的程序集名</param>
    /// <param name="excludedAssemblies">被白/黑名单排除的程序集名</param>
    /// <param name="topology">程序集拓扑层级</param>
    public ServiceRegistrationReport(
        IReadOnlyList<string> scannedAssemblies,
        IReadOnlyList<string> excludedAssemblies,
        IReadOnlyList<AssemblyTopologyEntry> topology)
    {
        ScannedAssemblies = scannedAssemblies;
        ExcludedAssemblies = excludedAssemblies;
        Topology = topology;
    }

    /// <summary>按拓扑顺序（被依赖在前）纳入扫描的程序集名</summary>
    public IReadOnlyList<string> ScannedAssemblies { get; }

    /// <summary>被白/黑名单排除的程序集名</summary>
    public IReadOnlyList<string> ExcludedAssemblies { get; }

    /// <summary>程序集拓扑层级</summary>
    public IReadOnlyList<AssemblyTopologyEntry> Topology { get; }
}
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`
Expected: PASS。

- [ ] **Step 5: 提交**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Diagnostics/ServiceRegistrationReport.cs backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Discovery/AssemblyDiscovererTests.cs
git commit -m "feat(di): add ServiceRegistrationReport scan/topology skeleton"
```

---

## Task 8: 程序集来源与发现编排

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Discovery/IAssemblySource.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Discovery/RuntimeAssemblySource.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Discovery/AssemblyDiscoverer.cs`
- Test: 追加到 `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Discovery/AssemblyDiscovererTests.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Discovery/RuntimeAssemblySourceTests.cs`

> 设计说明：`IAssemblySource` 把「候选程序集从哪来」与「如何过滤排序」解耦：默认实现 `RuntimeAssemblySource` 合并 `AppDomain` 已加载程序集与 `DependencyContext.Default` 依赖上下文（spec「程序集发现」节）；`AssemblyDiscoverer.Discover` 用真实 `Assembly.GetReferencedAssemblies()` 构造描述符，复用 `AssemblyFilter` 与 `AssemblyTopologySorter`，产出按拓扑排序的 `Assembly` 列表与 `ServiceRegistrationReport`。发现编排测试用受控的假 `IAssemblySource` 喂入已知程序集，保证确定性。

- [ ] **Step 1: 追加发现编排失败测试**

把 `Discovery/AssemblyDiscovererTests.cs` 替换为：

```csharp
using System.Reflection;
using FluentAssertions;
using Tw.DependencyInjection;
using Tw.DependencyInjection.Diagnostics;
using Tw.DependencyInjection.Discovery;
using Xunit;

namespace Tw.DependencyInjection.Tests.Discovery;

public class AssemblyDiscovererTests
{
    private sealed class FakeAssemblySource(params Assembly[] assemblies) : IAssemblySource
    {
        public IReadOnlyList<Assembly> GetCandidateAssemblies() => assemblies;
    }

    [Fact]
    public void Report_ExposesScanAndTopologySections()
    {
        var report = new ServiceRegistrationReport(
            scannedAssemblies: ["Tw.Core"],
            excludedAssemblies: ["System.Text.Json"],
            topology: [new AssemblyTopologyEntry("Tw.Core", 0)]);

        report.ScannedAssemblies.Should().ContainSingle().Which.Should().Be("Tw.Core");
        report.ExcludedAssemblies.Should().ContainSingle().Which.Should().Be("System.Text.Json");
        report.Topology.Should().ContainSingle().Which.Level.Should().Be(0);
    }

    [Fact]
    public void Discover_FiltersToTwPrefix_AndOrdersCoreBeforeEngine()
    {
        var coreAssembly = typeof(Tw.Check).Assembly;
        var engineAssembly = typeof(ServiceRegistrationOptions).Assembly;
        var systemAssembly = typeof(string).Assembly;
        var source = new FakeAssemblySource(engineAssembly, systemAssembly, coreAssembly);

        var result = AssemblyDiscoverer.Discover(new ServiceRegistrationOptions(), source);

        result.OrderedAssemblies.Select(a => a.GetName().Name)
            .Should().ContainInOrder("Tw.Core", "Tw.DependencyInjection");
        result.Report.ScannedAssemblies.Should().Contain(["Tw.Core", "Tw.DependencyInjection"]);
        result.Report.ExcludedAssemblies.Should().Contain(systemAssembly.GetName().Name!);
    }
}
```

> 备注：`Tw.Check` 是 `Tw.Core` 根命名空间下的 `Check` 类型；`ServiceRegistrationOptions` 在引擎程序集 `Tw.DependencyInjection`。引擎引用 `Tw.Core`，故拓扑中 `Tw.Core`（层级 0）排在引擎（层级 1）之前。

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`
Expected: 编译失败，`IAssemblySource`、`AssemblyDiscoverer` 不存在。

- [ ] **Step 3: 实现 IAssemblySource**

新建 `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Discovery/IAssemblySource.cs`：

```csharp
using System.Reflection;

namespace Tw.DependencyInjection.Discovery;

/// <summary>候选程序集来源，供发现器过滤排序前取数</summary>
internal interface IAssemblySource
{
    /// <summary>返回参与发现的候选程序集</summary>
    IReadOnlyList<Assembly> GetCandidateAssemblies();
}
```

- [ ] **Step 4: 实现 RuntimeAssemblySource**

新建 `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Discovery/RuntimeAssemblySource.cs`：

```csharp
using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace Tw.DependencyInjection.Discovery;

/// <summary>合并 AppDomain 已加载程序集与依赖上下文的默认候选来源</summary>
internal sealed class RuntimeAssemblySource : IAssemblySource
{
    public IReadOnlyList<Assembly> GetCandidateAssemblies()
    {
        var byName = new Dictionary<string, Assembly>(StringComparer.Ordinal);

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            TryAdd(byName, assembly);
        }

        var context = DependencyContext.Default;
        if (context is not null)
        {
            foreach (var assemblyName in context.GetDefaultAssemblyNames())
            {
                try
                {
                    TryAdd(byName, Assembly.Load(assemblyName));
                }
                catch (Exception ex) when (ex is FileNotFoundException or BadImageFormatException)
                {
                }
            }
        }

        return byName.Values.ToList();
    }

    private static void TryAdd(Dictionary<string, Assembly> byName, Assembly assembly)
    {
        var name = assembly.GetName().Name;
        if (name is not null)
        {
            byName[name] = assembly;
        }
    }
}
```

- [ ] **Step 5: 实现 AssemblyDiscoverer**

新建 `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Discovery/AssemblyDiscoverer.cs`：

```csharp
using System.Reflection;
using Tw.DependencyInjection.Diagnostics;

namespace Tw.DependencyInjection.Discovery;

/// <summary>发现结果：按拓扑排序的程序集与诊断报告</summary>
internal sealed record AssemblyDiscoveryResult(
    IReadOnlyList<Assembly> OrderedAssemblies, ServiceRegistrationReport Report);

internal static class AssemblyDiscoverer
{
    public static AssemblyDiscoveryResult Discover(ServiceRegistrationOptions options, IAssemblySource source)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(source);

        var byName = new Dictionary<string, Assembly>(StringComparer.Ordinal);
        foreach (var assembly in source.GetCandidateAssemblies())
        {
            var name = assembly.GetName().Name;
            if (name is not null)
            {
                byName[name] = assembly;
            }
        }

        var included = AssemblyFilter.Filter(byName.Keys, options);
        var includedSet = new HashSet<string>(included, StringComparer.Ordinal);

        var excluded = byName.Keys
            .Where(name => !includedSet.Contains(name))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        var descriptors = included
            .Select(name => new AssemblyDescriptor(name, ReferencedNames(byName[name])))
            .ToList();

        var topology = AssemblyTopologySorter.Sort(descriptors);
        var orderedAssemblies = topology.Select(entry => byName[entry.AssemblyName]).ToList();

        var report = new ServiceRegistrationReport(
            topology.Select(entry => entry.AssemblyName).ToList(),
            excluded,
            topology);

        return new AssemblyDiscoveryResult(orderedAssemblies, report);
    }

    private static IReadOnlyList<string> ReferencedNames(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null)
            .Select(name => name!)
            .ToList();
}
```

- [ ] **Step 6: 运行发现编排测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`
Expected: PASS。

- [ ] **Step 7: 写 RuntimeAssemblySource 烟雾测试**

新建 `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Discovery/RuntimeAssemblySourceTests.cs`：

```csharp
using FluentAssertions;
using Tw.DependencyInjection.Discovery;
using Xunit;

namespace Tw.DependencyInjection.Tests.Discovery;

public class RuntimeAssemblySourceTests
{
    [Fact]
    public void GetCandidateAssemblies_IncludesLoadedTwAssemblies()
    {
        // 触碰 Tw.Core 类型，确保其程序集已加载
        _ = typeof(Tw.Check).Assembly;
        var source = new RuntimeAssemblySource();

        var names = source.GetCandidateAssemblies().Select(a => a.GetName().Name).ToList();

        names.Should().Contain("Tw.Core");
        names.Should().Contain("Tw.DependencyInjection");
    }
}
```

- [ ] **Step 8: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`
Expected: PASS。

- [ ] **Step 9: 提交**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Discovery/IAssemblySource.cs backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Discovery/RuntimeAssemblySource.cs backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Discovery/AssemblyDiscoverer.cs backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Discovery
git commit -m "feat(di): add assembly source and discovery orchestration"
```

---

## Task 9: UseAutofac 容器接管

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/AutofacHostBuilderExtensions.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Hosting/AutofacHostBuilderExtensionsTests.cs`

> 设计说明：spec「聚合注册入口」——`builder.Host.UseAutofac()` 是引擎级启动原语，用 Autofac 接管 .NET 默认容器。实现委托 `AutofacServiceProviderFactory`。扩展方法名 `UseAutofac` 由 spec 直接规定；扩展类名表达「宿主容器接管」职责，不用包名作宽泛入口（符合 dotnet-core.md「共享包服务注册」要求；Autofac 是本包直接选用的实现库，名称可出现）。

- [ ] **Step 1: 写失败测试**

新建 `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Hosting/AutofacHostBuilderExtensionsTests.cs`：

```csharp
using Autofac.Extensions.DependencyInjection;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Tw.DependencyInjection;
using Xunit;

namespace Tw.DependencyInjection.Tests.Hosting;

public class AutofacHostBuilderExtensionsTests
{
    [Fact]
    public void UseAutofac_ReplacesServiceProvider_WithAutofac()
    {
        var builder = new HostBuilder().UseAutofac();

        using var host = builder.Build();

        host.Services.Should().BeOfType<AutofacServiceProvider>();
    }

    [Fact]
    public void UseAutofac_Throws_WhenHostBuilderIsNull()
    {
        IHostBuilder hostBuilder = null!;

        var act = () => hostBuilder.UseAutofac();

        act.Should().Throw<ArgumentNullException>();
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`
Expected: 编译失败，`UseAutofac` 不存在。

- [ ] **Step 3: 实现 AutofacHostBuilderExtensions**

新建 `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/AutofacHostBuilderExtensions.cs`：

```csharp
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Tw.DependencyInjection;

/// <summary>
/// 在宿主构建阶段用 Autofac 接管默认依赖注入容器的扩展
/// </summary>
public static class AutofacHostBuilderExtensions
{
    /// <summary>
    /// 使用 Autofac 接管宿主的服务提供程序工厂
    /// </summary>
    /// <param name="hostBuilder">宿主构建器</param>
    /// <returns>同一宿主构建器，便于链式调用</returns>
    /// <exception cref="ArgumentNullException"><paramref name="hostBuilder"/> 为 <see langword="null"/> 时抛出</exception>
    public static IHostBuilder UseAutofac(this IHostBuilder hostBuilder)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);
        return hostBuilder.UseServiceProviderFactory(new AutofacServiceProviderFactory());
    }
}
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`
Expected: PASS。

- [ ] **Step 5: 提交**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/AutofacHostBuilderExtensions.cs backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Hosting/AutofacHostBuilderExtensionsTests.cs
git commit -m "feat(di): add UseAutofac host container takeover"
```

---

## Task 10: 新增 Tw.DependencyInjection package-charter.yaml

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/package-charter.yaml`

> 依据 [shared-package-charter.md](../../engineering-standards/03-project-and-code/shared-package-charter.md)：新增包必须同时提交 charter，含全部必填字段，`public_capabilities` 与其他包互斥（引擎用无后缀 `Tw.DependencyInjection`，与 `Tw.Core` 的 `Tw.DependencyInjection.Abstractions` 不重叠），`dependency_rules` 允许 Autofac/Castle 与 Microsoft.Extensions.*、禁止 ASP.NET Core 与 EF Core。

- [ ] **Step 1: 创建 charter**

新建 `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/package-charter.yaml`：

```yaml
schema_version: "1.0.0"
package: Tw.DependencyInjection
owner: platform-team
stability: experimental
compatibility: "experimental 阶段不承诺兼容"
responsibility: >
  框架绑定的依赖注入执行引擎：程序集发现、白/黑名单过滤、依赖拓扑排序与循环诊断、
  Autofac 容器接管、注册规划诊断报告。消费 Tw.Core 的框架无关抽象，向宿主提供引擎级启动原语。
in_scope:
  - 程序集发现与白/黑名单过滤
  - 程序集依赖拓扑排序与循环诊断
  - Autofac 容器接管启动原语
  - 服务注册规划诊断报告
out_of_scope:
  - DI 注册标记、特性、Options 与 AOP 抽象（归 Tw.Core 的 .Abstractions 命名空间）
  - ASP.NET Core 宿主启动、MVC 与 gRPC 承载
  - 数据访问、ORM、仓储实现
public_capabilities:
  - Tw.DependencyInjection
dependency_rules:
  forbid:
    - "Microsoft.AspNetCore.*"
    - "Microsoft.EntityFrameworkCore*"
  allow:
    - "Tw.Core"
    - "Autofac*"
    - "Castle.*"
    - "Microsoft.Extensions.*"
```

- [ ] **Step 2: 校验 charter 与实际依赖一致**

人工核对：`Tw.DependencyInjection.csproj` 的 `PackageReference`（Autofac、Autofac.Extensions.DependencyInjection、Microsoft.Extensions.Hosting.Abstractions、Microsoft.Extensions.DependencyModel）与 `ProjectReference`（Tw.Core）都落在 `allow` 列表内，无 `forbid` 命中。

- [ ] **Step 3: 提交**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/package-charter.yaml
git commit -m "docs(di): add Tw.DependencyInjection package charter"
```

---

## Task 11: 使用文档与索引联动

**Files:**
- Create: `docs/shared-packages/dotnet/Tw.DependencyInjection/README.md`
- Create: `docs/shared-packages/dotnet/Tw.DependencyInjection/assembly-scanning.md`
- Modify: `docs/shared-packages/dotnet/README.md`

> 依据 [shared-package-charter.md](../../engineering-standards/03-project-and-code/shared-package-charter.md)「能力使用文档」：新增包公开能力必须建 How-to 文档并更新索引。本阶段公开能力为「程序集扫描地基 + `UseAutofac()`」，对应 How-to `assembly-scanning.md`；DI 自动注册（`service-registration.md`）随 P2 落地。索引页用 Reference 类型，How-to 用 How-to Guide 类型。

- [ ] **Step 1: 创建包索引 README**

新建 `docs/shared-packages/dotnet/Tw.DependencyInjection/README.md`：

```markdown
# Tw.DependencyInjection

`Tw.DependencyInjection` 是框架绑定的依赖注入执行引擎，消费 `Tw.Core` 的框架无关抽象，承载程序集发现、拓扑排序、Autofac 容器接管与注册规划诊断。本页按功能跳转到使用文档。

## 能力索引

- [程序集扫描与容器接管](assembly-scanning.md)：扫描白/黑名单、依赖拓扑排序与循环诊断、`UseAutofac()` 启动原语（P1 落地）。
```

- [ ] **Step 2: 创建 assembly-scanning How-to**

新建 `docs/shared-packages/dotnet/Tw.DependencyInjection/assembly-scanning.md`：

```markdown
# 程序集扫描与容器接管

## 能力定位

`Tw.DependencyInjection` 是依赖注入执行引擎包，引用 `Tw.Core` 消费其框架无关抽象，直接引用 Autofac 执行容器接管。P1 阶段提供扫描地基：程序集发现、白/黑名单过滤、依赖拓扑排序与循环诊断、`UseAutofac()` 容器接管，以及 `ServiceRegistrationReport` 诊断骨架。服务注册仲裁、Options 装载与 AOP 承载属于后续阶段。

## 容器接管

在宿主构建阶段用 `UseAutofac()` 接管默认依赖注入容器：

```csharp
using Tw.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseAutofac();

var app = builder.Build();
app.Run();
```

`UseAutofac()` 是 `IHostBuilder` 扩展，内部委托 `AutofacServiceProviderFactory`，接管后 `host.Services` 为 Autofac 服务提供程序。

## 扫描选项

扫描行为由 `ServiceRegistrationOptions` 控制，对应配置节 `Tw:DependencyInjection`：

| 选项 | 类型 | 用途 |
| --- | --- | --- |
| `IncludeAssemblies` | 字符串列表 | 在内置 `Tw.` 前缀之外精确补充纳入的程序集名 |
| `ExcludeAssemblies` | 字符串列表 | 精确排除的程序集名，优先于任何白名单 |
| `IncludeAssemblyPrefixes` | 字符串列表 | 叠加在内置 `Tw.` 前缀之上的额外白名单前缀 |
| `ExcludeAssemblyPrefixes` | 字符串列表 | 排除的程序集名前缀，优先于任何白名单 |

默认扫描运行时已加载程序集与依赖上下文中的 `Tw.` 前缀程序集。黑名单（`Exclude*`）优先于白名单。配置示例：

```json
{
  "Tw": {
    "DependencyInjection": {
      "IncludeAssemblyPrefixes": ["Acme."],
      "ExcludeAssemblies": ["Tw.Legacy"]
    }
  }
}
```

> 说明：把配置节绑定到 `ServiceRegistrationOptions` 并驱动注册的入口 `AddServiceRegistration(IConfiguration)` 随 DI 注册阶段（P2）落地。

## 拓扑与诊断

扫描结果按程序集引用关系拓扑排序：被依赖程序集排在前、依赖方排在后，层级（`AssemblyTopologyEntry.Level`）随依赖深度递增。`ServiceRegistrationReport` 记录纳入扫描的程序集、被排除的程序集与拓扑层级。

## 注意事项

- 发现循环引用时启动失败，异常信息输出完整环路链路（如 `Tw.A -> Tw.B -> Tw.C -> Tw.A`）。
- 引擎只应由组合根（宿主启动）引用；业务服务只依赖 `Tw.Core` 抽象。
- 诊断报告只承载摘要元数据，不输出敏感配置值。
```

- [ ] **Step 3: 更新 .NET 包索引**

在 `docs/shared-packages/dotnet/README.md` 的「包索引」列表中，`Tw.Core` 条目下方追加：

```markdown
- [Tw.DependencyInjection](Tw.DependencyInjection/README.md)：框架绑定的依赖注入执行引擎，承载程序集扫描、拓扑排序与 Autofac 容器接管。
```

- [ ] **Step 4: 提交**

```bash
git add docs/shared-packages/dotnet/Tw.DependencyInjection docs/shared-packages/dotnet/README.md
git commit -m "docs(di): add Tw.DependencyInjection usage docs and index"
```

---

## 阶段收尾验证

- [ ] **全量测试**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`
Expected: 全部 PASS。

- [ ] **回归 Tw.Core 测试**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj`
Expected: 全部 PASS（确认 P0 抽象未受影响）。

- [ ] **依赖边界自检**

确认 `Tw.DependencyInjection.csproj` 无 `Microsoft.AspNetCore.*` / `Microsoft.EntityFrameworkCore*` 引用；引擎类型只落在 `Tw.DependencyInjection`、`Tw.DependencyInjection.Discovery`、`Tw.DependencyInjection.Diagnostics`，未向 `Tw.DependencyInjection.Abstractions` 贡献类型；文件夹路径与命名空间一一对应。

- [ ] **P1 完成标志**

`Tw.DependencyInjection` 提供程序集发现、白/黑名单、拓扑排序与循环诊断、`UseAutofac()` 接管、`ServiceRegistrationReport` 骨架，charter 与使用文档同步。P2（DI 注册：参与判定、生命周期、暴露、keyed、open generic、单实现仲裁、`AddServiceRegistration()`）可在此基础上开工。

---

## Self-Review

**Spec 覆盖（P1 行：程序集发现、白/黑名单、拓扑排序、循环诊断、`UseAutofac()` 接管、`ServiceRegistrationReport` 骨架、新增包与 charter）：**
- 程序集发现 → Task 8（`IAssemblySource` / `RuntimeAssemblySource` / `AssemblyDiscoverer`）
- 白/黑名单（含黑名单优先、四个配置项）→ Task 3（选项）+ Task 4（过滤）
- 拓扑排序（被依赖在前）→ Task 6
- 循环诊断（启动失败 + 完整环路）→ Task 5（异常）+ Task 6（环链）
- `UseAutofac()` 接管 → Task 9
- `ServiceRegistrationReport` 骨架 → Task 7
- 新增 `Tw.DependencyInjection` 包 → Task 1（中央包）+ Task 2（脚手架）
- charter → Task 10
- 使用文档与索引联动（spec「实现分期」要求每阶段含文档）→ Task 11
- 配置绑定路径 `Tw:DependencyInjection` → 在 Task 3 选项 XML 注释与 Task 11 文档中声明；实际绑定入口 `AddServiceRegistration` 明确划归 P2，未越界实现。

**类型一致性核对：** `ServiceRegistrationOptions`、`AssemblyFilter.Filter`、`AssemblyDescriptor(Name, ReferencedAssemblyNames)`、`AssemblyTopologyEntry(AssemblyName, Level)`、`AssemblyTopologySorter.Sort`、`IAssemblySource.GetCandidateAssemblies`、`AssemblyDiscoverer.Discover` → `AssemblyDiscoveryResult(OrderedAssemblies, Report)`、`ServiceRegistrationReport(ScannedAssemblies, ExcludedAssemblies, Topology)`、`ServiceRegistrationException`、`UseAutofac` 在定义任务与消费/测试任务中签名一致。

**占位扫描：** 无 TBD/TODO；`ServiceRegistrationReport` 的「骨架」是真实最小类型（后续阶段在同一类型追加属性），非占位。
</content>
</invoke>
