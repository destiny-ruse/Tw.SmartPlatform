# Dotnet Framework P3 Application Auth Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement domain, application, authorization, feature, setting, and identity building blocks that execute business use cases through one MediatR-based application pipeline.

**Architecture:** `Tw.Application.Contracts` holds public commands, queries, DTOs, paging, and shared client contracts. `Tw.Application` composes MediatR pipeline behaviors in the fixed order from the spec. Authorization, feature, setting, and identity packages remain separate so services can validate JWT and permissions without depending on the identity center implementation.

**Tech Stack:** .NET 10, MediatR 12.5.0, FluentValidation 12.1.1, OpenIddict 7.5.0, xUnit, AwesomeAssertions, NSubstitute

---

## File Structure

- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Domain.Shared`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Domain`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Application.Contracts`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Application`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization.Abstractions`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Features`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Settings`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Identity.OpenIddict`
- Create matching test projects in `backend/dotnet/BuildingBlocks/tests`
- Modify: `backend/dotnet/Tw.SmartPlatform.slnx`

### Task 1: Create Domain And Contract Package Shells

**Files:**
- Create: package directories listed above
- Create: `package-charter.yaml` for each package
- Create: matching test projects

- [ ] **Step 1: Create domain package project**

Use this project file for `Tw.Domain`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>true</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Tw.Domain.Shared\Tw.Domain.Shared.csproj" />
    <ProjectReference Include="..\..\Foundation\Tw.Core\Tw.Core.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create contracts package project**

Use this project file for `Tw.Application.Contracts`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>true</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Tw.Domain.Shared\Tw.Domain.Shared.csproj" />
    <ProjectReference Include="..\..\Foundation\Tw.Core\Tw.Core.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Add package charters**

`Tw.Application.Contracts/package-charter.yaml` includes:

```yaml
schema_version: "1.0.0"
package: Tw.Application.Contracts
owner: platform-team
stability: experimental
compatibility: "experimental 阶段不承诺兼容"
responsibility: >
  Command、Query、DTO、应用服务契约、分页模型和客户端共享契约。
in_scope:
  - Command 和 Query 标记接口
  - DTO 与分页模型
  - 应用服务契约
  - 客户端共享契约
out_of_scope:
  - MediatR Handler
  - UoW 编排
  - 权限检查执行
public_capabilities:
  - Tw.Application.Contracts
dependency_rules:
  forbid:
    - "MediatR"
    - "FluentValidation"
    - "SqlSugar*"
  allow:
    - "Tw.Core"
    - "Tw.Domain.Shared"
```

- [ ] **Step 4: Add solution entries**

Add projects in `/BuildingBlocks/src/Application/` and test projects in `/BuildingBlocks/tests/`.

- [ ] **Step 5: Run architecture tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj`

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Application backend/dotnet/BuildingBlocks/tests backend/dotnet/Tw.SmartPlatform.slnx
git commit -m "feat: add application layer package shells"
```

### Task 2: Implement Core Application Contracts

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Application.Contracts/ICommand.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Application.Contracts/IQuery.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Application.Contracts/PagedRequest.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Application.Contracts/PagedResult.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Application.Contracts.Tests/PagingContractTests.cs`

- [ ] **Step 1: Write paging contract test**

```csharp
using AwesomeAssertions;
using Tw.Application.Contracts;

namespace Tw.Application.Contracts.Tests;

public sealed class PagingContractTests
{
    [Fact]
    public void PagedResult_StoresItemsAndTotalCount()
    {
        var result = new PagedResult<string>(["a", "b"], 10);

        result.Items.Should().Equal("a", "b");
        result.TotalCount.Should().Be(10);
    }
}
```

- [ ] **Step 2: Implement contracts**

```csharp
namespace Tw.Application.Contracts;

public interface ICommand;

public interface ICommand<out TResult>;

public interface IQuery<out TResult>;

public sealed record PagedRequest(int PageNumber, int PageSize);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, long TotalCount);
```

- [ ] **Step 3: Run tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Application.Contracts.Tests/Tw.Application.Contracts.Tests.csproj`

Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Application/Tw.Application.Contracts backend/dotnet/BuildingBlocks/tests/Tw.Application.Contracts.Tests
git commit -m "feat: add application contract primitives"
```

### Task 3: Implement Application Pipeline Behavior Order

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Application/Pipeline/ApplicationPipelineOrder.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Application/Pipeline/IApplicationPipelineBehavior.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Application/Pipeline/ICompletedHook.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Application/Pipeline/ApplicationPipelineExecutor.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Application.Tests/Pipeline/ApplicationPipelineExecutorTests.cs`

- [ ] **Step 1: Write pipeline order test**

```csharp
using AwesomeAssertions;
using Tw.Application.Pipeline;

namespace Tw.Application.Tests.Pipeline;

public sealed class ApplicationPipelineExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_RunsBehaviorsInSpecOrder()
    {
        var calls = new List<string>();
        var behaviors = ApplicationPipelineOrder.CreateOrderedBehaviors([
            new RecordingBehavior("Auditing", calls),
            new RecordingBehavior("Validation", calls),
            new RecordingBehavior("Authorization", calls)
        ]);
        var executor = new ApplicationPipelineExecutor(behaviors);

        await executor.ExecuteAsync(() =>
        {
            calls.Add("Handler");
            return Task.CompletedTask;
        });

        calls.Should().Equal(
            "Authorization-before",
            "Validation-before",
            "Auditing-before",
            "Handler",
            "Auditing-after",
            "Validation-after",
            "Authorization-after");
    }

    [Fact]
    public async Task ExecuteAsync_RunsCompletedHooksAfterHandler()
    {
        var calls = new List<string>();
        var executor = new ApplicationPipelineExecutor(
            Array.Empty<IApplicationPipelineBehavior>(),
            [new RecordingCompletedHook(calls)]);

        await executor.ExecuteAsync(() =>
        {
            calls.Add("Handler");
            return Task.CompletedTask;
        });

        calls.Should().Equal("Handler", "CompletedHook");
    }

    private sealed class RecordingBehavior(string name, List<string> calls) : IApplicationPipelineBehavior
    {
        public string Name => name;

        public async Task InvokeAsync(Func<Task> next, CancellationToken cancellationToken)
        {
            calls.Add($"{name}-before");
            await next();
            calls.Add($"{name}-after");
        }
    }

    private sealed class RecordingCompletedHook(List<string> calls) : ICompletedHook
    {
        public Task RunAsync(CancellationToken cancellationToken)
        {
            calls.Add("CompletedHook");
            return Task.CompletedTask;
        }
    }
}
```

- [ ] **Step 2: Implement pipeline interfaces**

```csharp
namespace Tw.Application.Pipeline;

public interface IApplicationPipelineBehavior
{
    string Name { get; }

    Task InvokeAsync(Func<Task> next, CancellationToken cancellationToken);
}

public interface ICompletedHook
{
    Task RunAsync(CancellationToken cancellationToken);
}
```

- [ ] **Step 3: Implement order**

```csharp
namespace Tw.Application.Pipeline;

public static class ApplicationPipelineOrder
{
    private static readonly string[] Order =
    [
        "ExecutionContext",
        "Feature",
        "Authorization",
        "Validation",
        "Idempotency",
        "Sharding",
        "Uow",
        "Concurrency",
        "Auditing"
    ];

    public static IReadOnlyList<IApplicationPipelineBehavior> CreateOrderedBehaviors(IEnumerable<IApplicationPipelineBehavior> behaviors)
    {
        return behaviors
            .OrderBy(behavior =>
            {
                var index = Array.IndexOf(Order, behavior.Name);
                return index < 0 ? int.MaxValue : index;
            })
            .ToArray();
    }
}
```

- [ ] **Step 4: Implement executor**

```csharp
namespace Tw.Application.Pipeline;

public sealed class ApplicationPipelineExecutor(
    IReadOnlyList<IApplicationPipelineBehavior> behaviors,
    IReadOnlyList<ICompletedHook>? completedHooks = null)
{
    public async Task ExecuteAsync(Func<Task> handler, CancellationToken cancellationToken = default)
    {
        Func<Task> next = handler;
        for (var index = behaviors.Count - 1; index >= 0; index--)
        {
            var behavior = behaviors[index];
            var current = next;
            next = () => behavior.InvokeAsync(current, cancellationToken);
        }

        await next();

        foreach (var completedHook in completedHooks ?? Array.Empty<ICompletedHook>())
        {
            await completedHook.RunAsync(cancellationToken);
        }
    }
}
```

- [ ] **Step 5: Run tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Application.Tests/Tw.Application.Tests.csproj --filter ApplicationPipelineExecutorTests`

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Application/Tw.Application backend/dotnet/BuildingBlocks/tests/Tw.Application.Tests
git commit -m "feat: add application pipeline ordering"
```

### Task 4: Implement Authorization Contracts And Checker

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization.Abstractions/PermissionDefinition.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization.Abstractions/AuthorizationContext.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization.Abstractions/AuthorizationResult.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization.Abstractions/IPermissionChecker.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization.Abstractions/IGrantStore.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization.Abstractions/IPermissionGrantCache.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization/PermissionChecker.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Authorization.Tests/PermissionCheckerTests.cs`

- [ ] **Step 1: Write permission checker test**

```csharp
using AwesomeAssertions;
using Tw.Authorization;
using Tw.Authorization.Abstractions;

namespace Tw.Authorization.Tests;

public sealed class PermissionCheckerTests
{
    [Fact]
    public async Task CheckAsync_ReturnsDenied_WhenPermissionMissing()
    {
        var checker = new PermissionChecker(new InMemoryGrantStore([]), new InMemoryPermissionGrantCache());
        var context = new AuthorizationContext(
            SubjectId: "user-1",
            TenantId: "tenant-1",
            Permission: "orders.approve",
            ResourceType: "Order",
            ResourceId: "order-1",
            Roles: ["cashier"]);

        var result = await checker.CheckAsync(context, CancellationToken.None);

        result.Allowed.Should().BeFalse();
        result.Code.Should().Be("AUTHORIZATION:000001");
    }

    private sealed class InMemoryGrantStore(IReadOnlySet<string> grants) : IGrantStore
    {
        public Task<bool> HasGrantAsync(AuthorizationContext context, CancellationToken cancellationToken)
        {
            var key = $"{context.SubjectId}:{context.TenantId}:{context.Permission}:{context.ResourceType}:{context.ResourceId}";
            return Task.FromResult(grants.Contains(key));
        }
    }

    private sealed class InMemoryPermissionGrantCache : IPermissionGrantCache
    {
        private readonly Dictionary<PermissionGrantCacheKey, bool> _values = new();

        public Task<bool?> GetAsync(PermissionGrantCacheKey key, CancellationToken cancellationToken)
        {
            return Task.FromResult(_values.TryGetValue(key, out var allowed) ? allowed : (bool?)null);
        }

        public Task SetAsync(PermissionGrantCacheKey key, bool allowed, CancellationToken cancellationToken)
        {
            _values[key] = allowed;
            return Task.CompletedTask;
        }
    }
}
```

- [ ] **Step 2: Implement contracts**

```csharp
namespace Tw.Authorization.Abstractions;

public sealed record PermissionDefinition(string Name, string DisplayName);

public sealed record AuthorizationContext(
    string SubjectId,
    string TenantId,
    string Permission,
    string? ResourceType,
    string? ResourceId,
    IReadOnlySet<string> Roles);

public sealed record AuthorizationResult(bool Allowed, string Code, string Message)
{
    public static AuthorizationResult Success() => new(true, "SYSTEM:000000", "success");
    public static AuthorizationResult Denied(string code, string message) => new(false, code, message);
}

public interface IPermissionChecker
{
    Task<AuthorizationResult> CheckAsync(AuthorizationContext context, CancellationToken cancellationToken);
}

public sealed record PermissionGrantCacheKey(
    string SubjectId,
    string TenantId,
    string Permission,
    string? ResourceType,
    string? ResourceId);

public interface IPermissionGrantCache
{
    Task<bool?> GetAsync(PermissionGrantCacheKey key, CancellationToken cancellationToken);

    Task SetAsync(PermissionGrantCacheKey key, bool allowed, CancellationToken cancellationToken);
}
```

- [ ] **Step 3: Implement checker and grant store**

```csharp
namespace Tw.Authorization.Abstractions;

public interface IGrantStore
{
    Task<bool> HasGrantAsync(AuthorizationContext context, CancellationToken cancellationToken);
}
```

```csharp
using Tw.Authorization.Abstractions;

namespace Tw.Authorization;

public sealed class PermissionChecker(IGrantStore grantStore, IPermissionGrantCache grantCache) : IPermissionChecker
{
    public async Task<AuthorizationResult> CheckAsync(AuthorizationContext context, CancellationToken cancellationToken)
    {
        var key = new PermissionGrantCacheKey(
            context.SubjectId,
            context.TenantId,
            context.Permission,
            context.ResourceType,
            context.ResourceId);

        var cached = await grantCache.GetAsync(key, cancellationToken);
        if (cached is not null)
        {
            return cached.Value
                ? AuthorizationResult.Success()
                : AuthorizationResult.Denied("AUTHORIZATION:000001", "没有操作权限");
        }

        var allowed = await grantStore.HasGrantAsync(context, cancellationToken);
        await grantCache.SetAsync(key, allowed, cancellationToken);

        return allowed
            ? AuthorizationResult.Success()
            : AuthorizationResult.Denied("AUTHORIZATION:000001", "没有操作权限");
    }
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Authorization.Tests/Tw.Authorization.Tests.csproj`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization* backend/dotnet/BuildingBlocks/tests/Tw.Authorization.Tests
git commit -m "feat: add permission authorization contracts"
```

### Task 5: Implement Feature And Setting Read Models

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Features/FeatureScope.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Features/FeatureDefinition.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Features/FeatureValue.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Features/FeatureCacheKey.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Features/IFeatureStore.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Features/IFeatureCache.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Features/FeatureRefreshRequest.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Features/FeatureChecker.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Settings/SettingScope.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Settings/SettingDefinition.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Settings/SettingValue.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Settings/SettingCacheKey.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Settings/ISettingStore.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Settings/ISettingCache.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Settings/SettingRefreshRequest.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Settings/SettingProvider.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Features.Tests/FeatureCheckerTests.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Settings.Tests/SettingProviderTests.cs`

- [ ] **Step 1: Write feature scope, cache, and refresh tests**

`FeatureCheckerTests` must verify these behaviors:

- Tenant-scoped values override service defaults for the same feature name.
- Cached feature results are reused until a matching `FeatureRefreshRequest` is processed.
- Disabled features return stable error code `FEATURE:000001`.

```csharp
using AwesomeAssertions;
using Tw.Features;

namespace Tw.Features.Tests;

public sealed class FeatureCheckerTests
{
    [Fact]
    public async Task CheckAsync_UsesTenantOverrideBeforeServiceDefault()
    {
        var store = new InMemoryFeatureStore([
            new FeatureValue("billing.approval", FeatureScope.Service, "billing-service", true, 1),
            new FeatureValue("billing.approval", FeatureScope.Tenant, "tenant-a", false, 2)
        ]);
        var checker = new FeatureChecker(store, new InMemoryFeatureCache());

        var result = await checker.CheckAsync("billing.approval", "tenant-a", "billing-service", CancellationToken.None);

        result.Enabled.Should().BeFalse();
        result.Code.Should().Be("FEATURE:000001");
    }

    [Fact]
    public async Task RefreshAsync_RemovesMatchingCachedValue()
    {
        var cache = new InMemoryFeatureCache();
        var store = new InMemoryFeatureStore([
            new FeatureValue("billing.approval", FeatureScope.Tenant, "tenant-a", true, 1)
        ]);
        var checker = new FeatureChecker(store, cache);

        await checker.CheckAsync("billing.approval", "tenant-a", "billing-service", CancellationToken.None);
        store.Replace(new FeatureValue("billing.approval", FeatureScope.Tenant, "tenant-a", false, 2));
        await checker.RefreshAsync(new FeatureRefreshRequest("billing.approval", FeatureScope.Tenant, "tenant-a"), CancellationToken.None);

        var refreshed = await checker.CheckAsync("billing.approval", "tenant-a", "billing-service", CancellationToken.None);

        refreshed.Enabled.Should().BeFalse();
    }
}
```

- [ ] **Step 2: Implement feature contracts and checker**

```csharp
namespace Tw.Features;

public enum FeatureScope
{
    Service = 1,
    Tenant = 2,
    User = 3
}

public sealed record FeatureDefinition(string Name, bool DefaultEnabled);

public sealed record FeatureValue(string Name, FeatureScope Scope, string ScopeKey, bool Enabled, long Version);

public sealed record FeatureCacheKey(string Name, FeatureScope Scope, string ScopeKey);

public sealed record FeatureRefreshRequest(string Name, FeatureScope Scope, string ScopeKey);

public sealed record FeatureCheckResult(bool Enabled, string Code, string Message)
{
    public static FeatureCheckResult EnabledResult() => new(true, "SYSTEM:000000", "success");
    public static FeatureCheckResult Disabled(string feature) => new(false, "FEATURE:000001", $"功能未启用：{feature}");
}

public interface IFeatureStore
{
    Task<FeatureValue?> FindAsync(string name, FeatureScope scope, string scopeKey, CancellationToken cancellationToken);
}

public interface IFeatureCache
{
    Task<FeatureValue?> GetAsync(FeatureCacheKey key, CancellationToken cancellationToken);
    Task SetAsync(FeatureCacheKey key, FeatureValue value, CancellationToken cancellationToken);
    Task RemoveAsync(FeatureCacheKey key, CancellationToken cancellationToken);
}

public interface IFeatureChecker
{
    Task<FeatureCheckResult> CheckAsync(string feature, string tenantId, string serviceName, CancellationToken cancellationToken);

    Task RefreshAsync(FeatureRefreshRequest request, CancellationToken cancellationToken);
}
```

`FeatureChecker` must resolve values in this order: tenant, service, definition default. Each lookup uses `IFeatureCache` before `IFeatureStore`. `RefreshAsync` removes the exact cache key carried by `FeatureRefreshRequest`.

- [ ] **Step 3: Write setting scope, fallback, and refresh tests**

`SettingProviderTests` must verify these behaviors:

- User scope overrides tenant scope, tenant scope overrides service scope, and service scope overrides the definition default.
- Cache refresh removes the matching user, tenant, or service setting value.
- Missing settings return `null` only when no definition exists.

```csharp
using AwesomeAssertions;
using Tw.Settings;

namespace Tw.Settings.Tests;

public sealed class SettingProviderTests
{
    [Fact]
    public async Task GetAsync_UsesUserTenantServiceThenDefaultFallback()
    {
        var store = new InMemorySettingStore([
            new SettingValue("orders.page-size", SettingScope.Service, "order-service", "20", 1),
            new SettingValue("orders.page-size", SettingScope.Tenant, "tenant-a", "50", 2),
            new SettingValue("orders.page-size", SettingScope.User, "user-a", "100", 3)
        ]);
        var provider = new SettingProvider(store, new InMemorySettingCache(), [
            new SettingDefinition("orders.page-size", "10")
        ]);

        var value = await provider.GetAsync("orders.page-size", "tenant-a", "order-service", "user-a", CancellationToken.None);

        value.Should().Be("100");
    }
}
```

- [ ] **Step 4: Implement setting contracts and provider**

```csharp
namespace Tw.Settings;

public enum SettingScope
{
    Service = 1,
    Tenant = 2,
    User = 3
}

public sealed record SettingDefinition(string Name, string DefaultValue);

public sealed record SettingValue(string Name, SettingScope Scope, string ScopeKey, string Value, long Version);

public sealed record SettingCacheKey(string Name, SettingScope Scope, string ScopeKey);

public sealed record SettingRefreshRequest(string Name, SettingScope Scope, string ScopeKey);

public interface ISettingStore
{
    Task<SettingValue?> FindAsync(string name, SettingScope scope, string scopeKey, CancellationToken cancellationToken);
}

public interface ISettingCache
{
    Task<SettingValue?> GetAsync(SettingCacheKey key, CancellationToken cancellationToken);
    Task SetAsync(SettingCacheKey key, SettingValue value, CancellationToken cancellationToken);
    Task RemoveAsync(SettingCacheKey key, CancellationToken cancellationToken);
}

public interface ISettingProvider
{
    Task<string?> GetAsync(string name, string tenantId, string serviceName, string? userId, CancellationToken cancellationToken);

    Task RefreshAsync(SettingRefreshRequest request, CancellationToken cancellationToken);
}
```

`SettingProvider` must resolve values in this order: user, tenant, service, definition default. The cache key must include setting name, scope, and scope key to prevent cross-tenant or cross-user leakage.

- [ ] **Step 5: Run tests**

Run: `dotnet test backend/dotnet/Tw.SmartPlatform.slnx --filter "FullyQualifiedName~Features|FullyQualifiedName~Settings"`

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Application/Tw.Features backend/dotnet/BuildingBlocks/src/Application/Tw.Settings backend/dotnet/BuildingBlocks/tests/Tw.Features.Tests backend/dotnet/BuildingBlocks/tests/Tw.Settings.Tests
git commit -m "feat: add scoped feature and setting read models"
```

### Task 6: Implement OpenIddict Identity Center Boundary

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Identity.OpenIddict/Tw.Identity.OpenIddict.csproj`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Identity.OpenIddict/OpenIddictIdentityOptions.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Identity.OpenIddict/IdentityTokenRequest.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Identity.OpenIddict/IdentityTokenValidationRequest.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Identity.OpenIddict/IdentityTokenValidationResult.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Identity.OpenIddict/IIdentityTokenIssuer.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Identity.OpenIddict/IIdentityTokenValidator.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Identity.OpenIddict/IIdentitySigningCertificateResolver.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Application/Tw.Identity.OpenIddict/OpenIddictIdentityServiceCollectionExtensions.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Identity.OpenIddict.Tests/OpenIddictIdentityOptionsTests.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Identity.OpenIddict.Tests/OpenIddictIdentityServiceCollectionExtensionsTests.cs`

- [ ] **Step 1: Write identity options validation tests**

```csharp
using AwesomeAssertions;
using Tw.Identity.OpenIddict;

namespace Tw.Identity.OpenIddict.Tests;

public sealed class OpenIddictIdentityOptionsTests
{
    [Fact]
    public void Validate_RejectsMissingSigningCertificate()
    {
        var options = new OpenIddictIdentityOptions
        {
            Issuer = new Uri("https://identity.smart-platform.local")
        };
        options.Audiences.Add("smart-platform-api");

        var act = options.Validate;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("OpenIddict token signing certificate is required");
    }

    [Fact]
    public void Defaults_DoNotEnablePasswordGrant()
    {
        var options = new OpenIddictIdentityOptions();

        options.AllowedGrantTypes.Should().NotContain("password");
        options.AllowedGrantTypes.Should().Contain(["authorization_code", "client_credentials", "refresh_token"]);
    }
}
```

- [ ] **Step 2: Write OpenIddict registration tests**

```csharp
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.Identity.OpenIddict;

namespace Tw.Identity.OpenIddict.Tests;

public sealed class OpenIddictIdentityServiceCollectionExtensionsTests
{
    [Fact]
    public void AddIdentityOpenIddict_RegistersIssuerValidatorAndOpenIddictServices()
    {
        var services = new ServiceCollection();

        services.AddIdentityOpenIddict(options =>
        {
            options.Issuer = new Uri("https://identity.smart-platform.local");
            options.Audiences.Add("smart-platform-api");
            options.SigningCertificateName = "smart-platform-token-signing";
        });

        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IIdentityTokenIssuer));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IIdentityTokenValidator));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IIdentitySigningCertificateResolver));
    }
}
```

- [ ] **Step 3: Implement identity contracts and options**

```csharp
namespace Tw.Identity.OpenIddict;

public sealed class OpenIddictIdentityOptions
{
    public Uri? Issuer { get; set; }

    public ISet<string> Audiences { get; } = new HashSet<string>(StringComparer.Ordinal);

    public ISet<string> AllowedGrantTypes { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "authorization_code",
        "client_credentials",
        "refresh_token"
    };

    public string? SigningCertificateName { get; set; }

    public bool RequireProofKey { get; set; } = true;

    public void Validate()
    {
        if (Issuer is null)
        {
            throw new InvalidOperationException("OpenIddict issuer is required");
        }

        if (Audiences.Count == 0)
        {
            throw new InvalidOperationException("OpenIddict token audience is required");
        }

        if (string.IsNullOrWhiteSpace(SigningCertificateName))
        {
            throw new InvalidOperationException("OpenIddict token signing certificate is required");
        }

        if (AllowedGrantTypes.Contains("password"))
        {
            throw new InvalidOperationException("OpenIddict password grant is not allowed");
        }
    }
}

public sealed record IdentityTokenRequest(string SubjectId, string ClientId, IReadOnlySet<string> Scopes);

public sealed record IdentityTokenValidationRequest(string AccessToken, string Audience);

public sealed record IdentityTokenValidationResult(bool Succeeded, string? SubjectId, IReadOnlySet<string> Scopes, string Code);

public interface IIdentityTokenIssuer
{
    Task<string> IssueAsync(IdentityTokenRequest request, CancellationToken cancellationToken);
}

public interface IIdentityTokenValidator
{
    Task<IdentityTokenValidationResult> ValidateAsync(IdentityTokenValidationRequest request, CancellationToken cancellationToken);
}

public interface IIdentitySigningCertificateResolver
{
    Task<X509Certificate2> ResolveAsync(string certificateName, CancellationToken cancellationToken);
}
```

- [ ] **Step 4: Implement OpenIddict service registration**

`AddIdentityOpenIddict` must validate options at registration time, call `services.AddOpenIddict()`, configure server and validation components, register token issuer and validator adapters, and bind token signing through `IIdentitySigningCertificateResolver`. It must configure authorization code, client credentials, and refresh token flows. It must not enable password grant by default.

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Tw.Identity.OpenIddict;

public static class OpenIddictIdentityServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityOpenIddict(
        this IServiceCollection services,
        Action<OpenIddictIdentityOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new OpenIddictIdentityOptions();
        configure(options);
        options.Validate();

        services.AddOptions<OpenIddictIdentityOptions>()
            .Configure(configure)
            .Validate(identityOptions =>
            {
                identityOptions.Validate();
                return true;
            })
            .ValidateOnStart();

        services.AddOpenIddict()
            .AddServer(server =>
            {
                server.SetIssuer(options.Issuer!);
                server.AllowAuthorizationCodeFlow();
                server.AllowClientCredentialsFlow();
                server.AllowRefreshTokenFlow();

                if (options.RequireProofKey)
                {
                    server.RequireProofKeyForCodeExchange();
                }

                server.UseAspNetCore();
            })
            .AddValidation(validation =>
            {
                validation.SetIssuer(options.Issuer!);
                foreach (var audience in options.Audiences)
                {
                    validation.AddAudiences(audience);
                }

                validation.UseLocalServer();
                validation.UseAspNetCore();
            });

        services.TryAddScoped<IIdentitySigningCertificateResolver, StoreIdentitySigningCertificateResolver>();
        services.TryAddScoped<IIdentityTokenIssuer, OpenIddictIdentityTokenIssuer>();
        services.TryAddScoped<IIdentityTokenValidator, OpenIddictIdentityTokenValidator>();

        return services;
    }
}
```

- [ ] **Step 5: Add project dependencies**

`Tw.Identity.OpenIddict.csproj` includes:

```xml
<PackageReference Include="OpenIddict" />
<PackageReference Include="OpenIddict.Server.AspNetCore" />
<PackageReference Include="OpenIddict.Validation.AspNetCore" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
<PackageReference Include="Microsoft.Extensions.Options" />
```

- [ ] **Step 6: Run tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Identity.OpenIddict.Tests/Tw.Identity.OpenIddict.Tests.csproj`

Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Application/Tw.Identity.OpenIddict backend/dotnet/BuildingBlocks/tests/Tw.Identity.OpenIddict.Tests
git commit -m "feat: add openiddict identity boundary"
```

## Plan Self-Review

- Spec coverage: Domain, Contracts, Application pipeline including completed hooks, Authorization with grant store, permission cache, tenant, role, and resource context, scoped Feature and Setting read models with cache refresh, and OpenIddict identity issuance and validation boundaries are covered.
- Placeholder scan: no placeholder tokens are present.
- Type consistency: behavior names match the final pipeline order.
