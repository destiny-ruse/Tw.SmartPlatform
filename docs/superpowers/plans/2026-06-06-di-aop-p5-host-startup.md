# P5 宿主聚合 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 `Tw.AspNetCore` 收窄为跨协议宿主启动包，并把 `UseAutofac()` 与 `AddServiceRegistration(IConfiguration)` 封装为统一聚合入口。

**Architecture:** `Tw.AspNetCore` 引用 `Tw.DependencyInjection`，只承载 host-level 启动能力。Web 专属能力仍暂存于本包，P6 会把 `HttpContextCancellationTokenProvider` 迁移到 `Tw.AspNetCore.Mvc` 并同步收窄 charter。P5 不实现 MVC Filter 或 gRPC 专属能力。

**Tech Stack:** C# / .NET 10、ASP.NET Core shared framework、Microsoft.Extensions.Hosting、xunit、FluentAssertions。

---

## 文件结构

**修改：**
- `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Tw.AspNetCore.csproj`
- `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/package-charter.yaml`
- `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj`
- `docs/shared-packages/dotnet/Tw.AspNetCore/README.md`

**新增：**
- `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/DependencyInjection/HostStartupBuilderExtensions.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/DependencyInjection/HostStartupBuilderExtensionsTests.cs`
- `docs/shared-packages/dotnet/Tw.AspNetCore/host-startup.md`

## Task 1: 项目引用与聚合入口

**Files:**
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Tw.AspNetCore.csproj`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/DependencyInjection/HostStartupBuilderExtensions.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/DependencyInjection/HostStartupBuilderExtensionsTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
using Autofac.Extensions.DependencyInjection;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Tw.AspNetCore;
using Tw.DependencyInjection.Diagnostics;
using Xunit;

namespace Tw.AspNetCore.Tests.DependencyInjection;

public class HostStartupBuilderExtensionsTests
{
    [Fact]
    public void UseTwHostStartup_ConfiguresAutofacAndServiceRegistration()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["Tw:DependencyInjection:IncludeAssemblies:0"] =
            typeof(HostStartupBuilderExtensionsTests).Assembly.GetName().Name!;

        builder.UseTwHostStartup();
        using var app = builder.Build();

        app.Services.Should().BeOfType<AutofacServiceProvider>();
        app.Services.GetRequiredService<ServiceRegistrationReport>().Should().NotBeNull();
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --nologo`

Expected: 编译失败，`UseTwHostStartup` 不存在。

- [ ] **Step 3: 添加项目引用**

```xml
  <ItemGroup>
    <ProjectReference Include="..\Tw.Core\Tw.Core.csproj" />
    <ProjectReference Include="..\Tw.DependencyInjection\Tw.DependencyInjection.csproj" />
  </ItemGroup>
```

- [ ] **Step 4: 实现聚合入口**

```csharp
using Microsoft.AspNetCore.Builder;
using Tw.DependencyInjection;

namespace Tw.AspNetCore;

/// <summary>跨协议宿主启动聚合入口</summary>
public static class HostStartupBuilderExtensions
{
    /// <summary>
    /// 使用 Tw 统一宿主启动能力，包含 Autofac 容器接管与服务、Options、AOP 自动注册
    /// </summary>
    public static WebApplicationBuilder UseTwHostStartup(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Host.UseAutofac();
        builder.Services.AddServiceRegistration(builder.Configuration);
        return builder;
    }
}
```

- [ ] **Step 5: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --nologo`

Expected: PASS。

## Task 2: Charter 与文档

**Files:**
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/package-charter.yaml`
- Modify: `docs/shared-packages/dotnet/Tw.AspNetCore/README.md`
- Create: `docs/shared-packages/dotnet/Tw.AspNetCore/host-startup.md`

- [ ] **Step 1: 更新 charter**

`responsibility` 改为跨协议宿主启动与聚合入口。`dependency_rules.allow` 增加 `Tw.DependencyInjection`。`in_scope` 保留宿主启动与依赖注入聚合，暂不在 P5 删除 HTTP provider，删除动作在 P6 与代码迁移同批完成。

- [ ] **Step 2: 更新 README**

能力索引增加：

```markdown
- [宿主启动聚合入口](host-startup.md)：统一调用 `UseAutofac()` 与 `AddServiceRegistration(builder.Configuration)`。
```

- [ ] **Step 3: 创建 How-to**

`host-startup.md` 必须给出：

```csharp
using Tw.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.UseTwHostStartup();

var app = builder.Build();
app.Run();
```

并说明该入口适用于 Web API、后台服务宿主和 gRPC 宿主的组合根。

- [ ] **Step 4: 运行验证**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --nologo`

Expected: PASS。

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj --nologo`

Expected: PASS。

