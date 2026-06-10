# P7 gRPC 包边界 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 新增 `Tw.AspNetCore.Grpc` 包与使用文档，明确 gRPC 使用原生 interceptor，不接入统一 `IInterceptorPipeline`。

**Architecture:** `Tw.AspNetCore.Grpc` 引用 host 包 `Tw.AspNetCore` 与 `Grpc.AspNetCore`。该包只建立 gRPC 专属共享包边界、注册入口与文档，不引用 P4 MVC/Castle adapter，也不把 `Grpc.Core.Interceptors.Interceptor` 映射到 `Tw.DynamicProxy.Abstractions.IInterceptor`。

**Tech Stack:** C# / .NET 10、Grpc.AspNetCore 2.80.0、xunit、FluentAssertions。`Grpc.AspNetCore` 官方 NuGet 包页显示 2.80.0 是当前版本，且 `Grpc.AspNetCore.Server` 2.80.0 目标框架为 .NET 8.0、兼容更高版本。

---

## 文件结构

**修改：**
- `backend/dotnet/Build/Packages.Microsoft.props`
- `backend/dotnet/Tw.SmartPlatform.slnx`
- `docs/shared-packages/dotnet/README.md`

**新增：**
- `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore.Grpc/Tw.AspNetCore.Grpc.csproj`
- `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore.Grpc/package-charter.yaml`
- `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore.Grpc/DependencyInjection/GrpcIntegrationServiceCollectionExtensions.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Grpc.Tests/Tw.AspNetCore.Grpc.Tests.csproj`
- `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Grpc.Tests/DependencyInjection/GrpcIntegrationServiceCollectionExtensionsTests.cs`
- `docs/shared-packages/dotnet/Tw.AspNetCore.Grpc/README.md`
- `docs/shared-packages/dotnet/Tw.AspNetCore.Grpc/grpc-integration.md`

## Task 1: 中央包版本与项目脚手架

**Files:**
- Modify: `backend/dotnet/Build/Packages.Microsoft.props`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore.Grpc/Tw.AspNetCore.Grpc.csproj`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Grpc.Tests/Tw.AspNetCore.Grpc.Tests.csproj`
- Modify: `backend/dotnet/Tw.SmartPlatform.slnx`

- [ ] **Step 1: 写失败测试**

```csharp
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.AspNetCore.Grpc;
using Xunit;

namespace Tw.AspNetCore.Grpc.Tests.DependencyInjection;

public class GrpcIntegrationServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGrpcIntegration_RegistersGrpcServices()
    {
        var services = new ServiceCollection();

        services.AddGrpcIntegration();

        services.Should().Contain(descriptor =>
            descriptor.ServiceType.FullName!.Contains("Grpc", StringComparison.Ordinal));
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Grpc.Tests/Tw.AspNetCore.Grpc.Tests.csproj --nologo`

Expected: 项目或扩展方法不存在。

- [ ] **Step 3: 添加中央版本**

在 `Packages.Microsoft.props` 增加：

```xml
    <PackageVersion Include="Grpc.AspNetCore" Version="2.80.0" />
```

- [ ] **Step 4: 创建项目**

生产项目使用 `Microsoft.NET.Sdk.Web`、`OutputType=Library`、`IsPackable=true`，引用 `..\Tw.AspNetCore\Tw.AspNetCore.csproj` 并添加：

```xml
    <PackageReference Include="Grpc.AspNetCore" />
```

测试项目引用 `Tw.AspNetCore.Grpc`、`Microsoft.NET.Test.Sdk`、`xunit`、`FluentAssertions`。

- [ ] **Step 5: 登记解决方案并运行测试**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Grpc.Tests/Tw.AspNetCore.Grpc.Tests.csproj --nologo`

Expected: 编译失败，`AddGrpcIntegration` 不存在。

## Task 2: gRPC 注册入口

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore.Grpc/DependencyInjection/GrpcIntegrationServiceCollectionExtensions.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Grpc.Tests/DependencyInjection/GrpcIntegrationServiceCollectionExtensionsTests.cs`

- [ ] **Step 1: 实现注册入口**

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace Tw.AspNetCore.Grpc;

/// <summary>gRPC 专属集成注册入口</summary>
public static class GrpcIntegrationServiceCollectionExtensions
{
    /// <summary>
    /// 注册 gRPC 服务端能力；gRPC 横切能力使用 gRPC 原生 interceptor
    /// </summary>
    public static IServiceCollection AddGrpcIntegration(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddGrpc();
        return services;
    }
}
```

- [ ] **Step 2: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Grpc.Tests/Tw.AspNetCore.Grpc.Tests.csproj --nologo`

Expected: PASS。

- [ ] **Step 3: 确认未接入统一 AOP**

Run: `rg -n "IInterceptorPipeline|Tw.DynamicProxy.Abstractions|Castle|Mvc" backend/dotnet/BuildingBlocks/src/Tw.AspNetCore.Grpc backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Grpc.Tests`

Expected: 无命中。

## Task 3: Charter、文档与索引

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore.Grpc/package-charter.yaml`
- Create: `docs/shared-packages/dotnet/Tw.AspNetCore.Grpc/README.md`
- Create: `docs/shared-packages/dotnet/Tw.AspNetCore.Grpc/grpc-integration.md`
- Modify: `docs/shared-packages/dotnet/README.md`

- [ ] **Step 1: 新增 charter**

声明：
- `in_scope`: gRPC 服务端注册、gRPC 原生 interceptor 使用边界、gRPC 包治理入口。
- `out_of_scope`: 统一 `IInterceptorPipeline` adapter、MVC Filter、HTTP Middleware、业务 proto 契约。
- `dependency_rules.allow`: `Tw.AspNetCore`、`Grpc.AspNetCore`、`Microsoft.Extensions.*`。

- [ ] **Step 2: 新增 README 与 How-to**

`grpc-integration.md` 必须给出：

```csharp
using Tw.AspNetCore;
using Tw.AspNetCore.Grpc;

var builder = WebApplication.CreateBuilder(args);
builder.UseTwHostStartup();
builder.Services.AddGrpcIntegration();
```

并明确 gRPC 横切能力直接使用：

```csharp
public sealed class AuditGrpcInterceptor : Grpc.Core.Interceptors.Interceptor
{
}
```

- [ ] **Step 3: 更新总索引**

在 `docs/shared-packages/dotnet/README.md` 加入 `Tw.AspNetCore.Grpc` 包入口。

- [ ] **Step 4: 运行验证**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Grpc.Tests/Tw.AspNetCore.Grpc.Tests.csproj --nologo`

Expected: PASS。

Run: `dotnet test backend/dotnet/Tw.SmartPlatform.slnx --nologo`

Expected: PASS。
