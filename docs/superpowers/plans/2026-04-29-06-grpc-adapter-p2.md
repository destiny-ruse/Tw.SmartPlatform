# gRPC Adapter P2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the P2 gRPC Entry-scope adapter while preserving Service AOP behavior for gRPC services.

**Architecture:** gRPC support stays in `Tw.AspNetCore` because it is an ASP.NET Core host integration. The adapter is enabled only through `AddGrpcInterceptors()` during `AddGrpc` configuration and injects gRPC call data as an Entry feature.

**Tech Stack:** .NET 10, Grpc.AspNetCore, Grpc.Core.Api, ASP.NET Core, Tw.Core AOP, xUnit, FluentAssertions

---

## Execution Order

Run after `2026-04-29-05-cross-cutting-quality-gates.md`. This plan is P2 and is not required for the P0/P1 first delivery.

## Source Inputs

- Design sections: § 2.2, § 6.5, § 10.3
- Standards: `rules.test-strategy#rules` version `1.1.0`, `docs/standards/rules/test-strategy.md`; `rules.dependency-policy#rules` version `1.1.0`, `docs/standards/rules/dependency-policy.md`; `rules.comments-dotnet#rules` version `1.3.0`, `docs/standards/rules/comments-dotnet.md`

## File Structure

- Modify: `backend/dotnet/Build/Packages.ThirdParty.props` only if `Grpc.AspNetCore` and `Grpc.Core.Api` were not pinned by plan 00.
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Tw.AspNetCore.csproj` only if gRPC package references were not added by plan 00.
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Aop/Grpc/IGrpcCallFeature.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Aop/Grpc/GrpcCallFeature.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Aop/Grpc/GrpcInterceptorRegistrationMarker.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Aop/Grpc/GrpcServiceOptionsExtensions.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Aop/Grpc/TwGrpcAopInterceptor.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Aop/Grpc/GrpcServiceOptionsExtensionsTests.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Aop/Grpc/TwGrpcAopInterceptorTests.cs`

## Tasks

### Task 1: Verify gRPC Dependency Gate

**Files:**
- Verify: `docs/superpowers/dependencies/2026-04-29-autofac-castle-grpc-admission.md`
- Verify: `backend/dotnet/Build/Packages.ThirdParty.props`
- Verify: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Tw.AspNetCore.csproj`

- [ ] **Step 1: Confirm pinned packages**

Check that `Packages.ThirdParty.props` contains:

```xml
<PackageVersion Include="Grpc.AspNetCore" Version="2.76.0" />
<PackageVersion Include="Grpc.Core.Api" Version="2.76.0" />
```

- [ ] **Step 2: Confirm project references**

Check that `Tw.AspNetCore.csproj` contains:

```xml
<PackageReference Include="Grpc.AspNetCore" />
<PackageReference Include="Grpc.Core.Api" />
```

- [ ] **Step 3: Run package scan**

Run:

```powershell
dotnet list backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Tw.AspNetCore.csproj package --vulnerable --include-transitive
```

Expected: no high or critical vulnerability.

- [ ] **Step 4: Commit evidence if the dependency record needs P2 scan data**

```bash
git add docs/superpowers/dependencies/2026-04-29-autofac-castle-grpc-admission.md
git commit -m "docs: add grpc dependency validation evidence"
```

### Task 2: Add gRPC Feature and Idempotent Registration

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Aop/Grpc/IGrpcCallFeature.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Aop/Grpc/GrpcCallFeature.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Aop/Grpc/GrpcInterceptorRegistrationMarker.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Aop/Grpc/GrpcServiceOptionsExtensions.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Aop/Grpc/GrpcServiceOptionsExtensionsTests.cs`

- [ ] **Step 1: Write failing extension tests**

```csharp
[Fact]
public void AddGrpcInterceptors_Is_Idempotent()
{
    var options = new GrpcServiceOptions();

    options.AddGrpcInterceptors();
    options.AddGrpcInterceptors();

    options.Interceptors.Count(registration => registration.Type == typeof(TwGrpcAopInterceptor))
        .Should()
        .Be(1);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --filter AddGrpcInterceptors_Is_Idempotent
```

Expected: FAIL because gRPC extension does not exist.

- [ ] **Step 3: Create gRPC feature**

```csharp
namespace Tw.AspNetCore.Aop.Grpc;

using global::Grpc.Core;

/// <summary>提供 gRPC 拦截链中的调用上下文</summary>
public interface IGrpcCallFeature
{
    /// <summary>当前 gRPC 服务端调用上下文</summary>
    ServerCallContext ServerCallContext { get; }
}

/// <summary>默认 gRPC 调用特性</summary>
public sealed class GrpcCallFeature(ServerCallContext serverCallContext) : IGrpcCallFeature
{
    /// <inheritdoc />
    public ServerCallContext ServerCallContext { get; } = serverCallContext;
}
```

- [ ] **Step 4: Create idempotent extension**

```csharp
namespace Tw.AspNetCore.Aop.Grpc;

using global::Grpc.AspNetCore.Server;

/// <summary>提供 Tw gRPC Entry AOP 注册扩展</summary>
public static class GrpcServiceOptionsExtensions
{
    /// <summary>启用 Tw gRPC Entry AOP Adapter</summary>
    public static GrpcServiceOptions AddGrpcInterceptors(this GrpcServiceOptions options)
    {
        var checkedOptions = Tw.Core.Check.NotNull(options);
        if (checkedOptions.Interceptors.Any(registration => registration.Type == typeof(TwGrpcAopInterceptor)))
        {
            return checkedOptions;
        }

        checkedOptions.Interceptors.Add<TwGrpcAopInterceptor>();
        return checkedOptions;
    }
}
```

- [ ] **Step 5: Run extension tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --filter FullyQualifiedName~GrpcServiceOptionsExtensionsTests
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Aop/Grpc backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Aop/Grpc/GrpcServiceOptionsExtensionsTests.cs
git commit -m "feat: add grpc aop registration extension"
```

### Task 3: Add gRPC Interceptor Adapter

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Aop/Grpc/TwGrpcAopInterceptor.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Aop/Grpc/TwGrpcAopInterceptorTests.cs`

- [ ] **Step 1: Write unary adapter test**

```csharp
[Fact]
public async Task UnaryServerHandler_Provides_Grpc_Feature_And_CancellationToken()
{
    var interceptor = BuildGrpcInterceptor(new CaptureGrpcFeatureInterceptor());
    using var cancellation = new CancellationTokenSource();
    var context = CreateTestServerCallContext(cancellation.Token);

    await interceptor.UnaryServerHandler(
        request: new object(),
        context,
        continuation: (_, _) => Task.FromResult<object>("ok"));

    CaptureGrpcFeatureInterceptor.CapturedFeature.Should().NotBeNull();
    CaptureGrpcFeatureInterceptor.CapturedFeature!.ServerCallContext.Should().BeSameAs(context);
}

private static ServerCallContext CreateTestServerCallContext(CancellationToken cancellationToken)
{
    return new TestServerCallContext(cancellationToken);
}

private sealed class TestServerCallContext(CancellationToken cancellationToken) : ServerCallContext
{
    protected override string MethodCore => "tw.orders.OrderGrpc/Get";
    protected override string HostCore => "localhost";
    protected override string PeerCore => "ipv4:127.0.0.1:5000";
    protected override DateTime DeadlineCore => DateTime.UtcNow.AddMinutes(1);
    protected override Metadata RequestHeadersCore { get; } = [];
    protected override CancellationToken CancellationTokenCore => cancellationToken;
    protected override Metadata ResponseTrailersCore { get; } = [];
    protected override Status StatusCore { get; set; }
    protected override WriteOptions? WriteOptionsCore { get; set; }
    protected override AuthContext AuthContextCore { get; } = new("anonymous", []);
    protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) => throw new NotSupportedException("测试上下文不支持传播令牌。");
    protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --filter UnaryServerHandler_Provides_Grpc_Feature_And_CancellationToken
```

Expected: FAIL because `TwGrpcAopInterceptor` does not exist.

- [ ] **Step 3: Create interceptor**

Implement `TwGrpcAopInterceptor : global::Grpc.Core.Interceptors.Interceptor` so it:

```text
1. Resolves EntryChainCache and interceptor instances from the current service provider.
2. Adds GrpcCallFeature before invoking Entry interceptors.
3. Uses ServerCallContext.CancellationToken as ambient token.
4. Covers UnaryServerHandler, ClientStreamingServerHandler, ServerStreamingServerHandler, and DuplexStreamingServerHandler.
5. Preserves ServerCallContext on every call kind.
6. Uses async stream wrapping for server streaming and duplex streaming without pre-enumerating responses.
```

- [ ] **Step 4: Run interceptor tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --filter FullyQualifiedName~TwGrpcAopInterceptorTests
```

Expected: PASS for unary, client streaming, server streaming, duplex streaming, feature access, and cancellation token propagation.

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Aop/Grpc/TwGrpcAopInterceptor.cs backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Aop/Grpc/TwGrpcAopInterceptorTests.cs
git commit -m "feat: add grpc entry aop adapter"
```

### Task 4: Verify P2 gRPC Acceptance

**Files:**
- Verify: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj`
- Verify: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj`

- [ ] **Step 1: Run focused gRPC tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --filter FullyQualifiedName~Grpc
```

Expected: PASS.

- [ ] **Step 2: Run all BuildingBlocks tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj
```

Expected: PASS.

## Self-Review Checklist

- [ ] `AddGrpcInterceptors()` is idempotent.
- [ ] Unary, client streaming, server streaming, and duplex streaming handlers are covered.
- [ ] `IInvocationContext.GetFeature<IGrpcCallFeature>()` is non-null inside the gRPC adapter and null outside that scenario.
- [ ] `ServerCallContext.CancellationToken` sets ambient token unless a service method CancellationToken argument takes priority.
- [ ] Projects that do not call `AddGrpcInterceptors()` do not enable gRPC Entry AOP.
- [ ] Service AOP through Castle still works for dependencies resolved inside gRPC services.
