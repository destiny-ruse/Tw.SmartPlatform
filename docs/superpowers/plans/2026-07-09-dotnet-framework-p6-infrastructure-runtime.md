# Dotnet Framework P6 Infrastructure Runtime Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement configuration governance, caching, distributed locking, idempotency, resilience, background jobs, and gateway runtime packages.

**Architecture:** Infrastructure packages stay below application and protocol packages. Abstraction packages expose contracts only. Runtime packages provide orchestration. Adapter packages bind third-party libraries such as FusionCache, Redis, Quartz, Nacos, and YARP without leaking those libraries into business projects. Gateway routing and header governance remain isolated from data, UoW, CAP, and application packages.

**Tech Stack:** .NET 10, FusionCache 2.6.0, Polly 8.7.0, Microsoft.Extensions.Http.Resilience 10.7.0, Quartz 3.18.2, YARP 2.3.0, Microsoft.Extensions.ServiceDiscovery.Yarp 10.7.0, MediatR 12.5.0, xUnit, AwesomeAssertions, NSubstitute

---

## File Structure

- Create: `backend/dotnet/BuildingBlocks/src/Caching/Tw.Caching`
- Create: `backend/dotnet/BuildingBlocks/src/Caching/Tw.Caching.FusionCache`
- Create: `backend/dotnet/BuildingBlocks/src/DistributedLocking/Tw.DistributedLocking.Abstractions`
- Create: `backend/dotnet/BuildingBlocks/src/DistributedLocking/Tw.DistributedLocking`
- Create: `backend/dotnet/BuildingBlocks/src/DistributedLocking/Tw.DistributedLocking.Redis`
- Create: `backend/dotnet/BuildingBlocks/src/Idempotency/Tw.Idempotency`
- Create: `backend/dotnet/BuildingBlocks/src/Resilience/Tw.Resilience`
- Create: `backend/dotnet/BuildingBlocks/src/BackgroundJobs/Tw.BackgroundJobs.Abstractions`
- Create: `backend/dotnet/BuildingBlocks/src/BackgroundJobs/Tw.BackgroundJobs`
- Create: `backend/dotnet/BuildingBlocks/src/BackgroundJobs/Tw.BackgroundJobs.Quartz`
- Create: `backend/dotnet/BuildingBlocks/src/Configuration/Tw.Configuration`
- Create: `backend/dotnet/BuildingBlocks/src/Configuration/Tw.Configuration.Json`
- Create: `backend/dotnet/BuildingBlocks/src/Configuration/Tw.Configuration.Nacos`
- Create: `backend/dotnet/BuildingBlocks/src/Gateway/Tw.Gateway`
- Create: `backend/dotnet/BuildingBlocks/src/Gateway/Tw.Gateway.Yarp`
- Create matching test projects under `backend/dotnet/BuildingBlocks/tests`
- Modify: `backend/dotnet/Tw.SmartPlatform.slnx`
- Modify: `backend/dotnet/Build/Packages.ThirdParty.props`
- Modify: `backend/dotnet/Build/Packages.Microsoft.props`

### Task 1: Create Package Shells And Dependency Boundaries

**Files:**
- Create every package directory listed in the file structure
- Create one `.csproj` and one `package-charter.yaml` per runtime package
- Create matching `.Tests.csproj` projects
- Modify: `backend/dotnet/Tw.SmartPlatform.slnx`

- [ ] **Step 1: Add central package versions**

Add or verify these central versions:

```xml
<PackageVersion Include="ZiggyCreatures.FusionCache" Version="2.6.0" />
<PackageVersion Include="Polly" Version="8.7.0" />
<PackageVersion Include="Microsoft.Extensions.Http.Resilience" Version="10.7.0" />
<PackageVersion Include="Quartz" Version="3.18.2" />
<PackageVersion Include="Yarp.ReverseProxy" Version="2.3.0" />
<PackageVersion Include="Microsoft.Extensions.ServiceDiscovery.Yarp" Version="10.7.0" />
<PackageVersion Include="MediatR" Version="12.5.0" />
```

- [ ] **Step 2: Create project shells**

Each runtime package uses `Microsoft.NET.Sdk`, `net10.0`, `Nullable=enable`, `ImplicitUsings=enable`, and `IsPackable=true`. Adapter packages reference their runtime abstraction or orchestration package and their third-party package only.

`Tw.Gateway.Yarp` must not reference `Tw.Data.*`, `Tw.Uow`, `Tw.Application`, `Tw.EventBus.*`, `Tw.BackgroundJobs.*`, `Tw.MultiTenancy`, or `Tw.Sharding`.

- [ ] **Step 3: Add charters**

Every `package-charter.yaml` must include `schema_version`, `package`, `owner`, `responsibility`, `in_scope`, `out_of_scope`, `public_capabilities`, and `dependency_rules`. `*Redis`, `*Quartz`, `*Nacos`, `*Yarp`, and `*FusionCache` charters must name the third-party library in `dependency_rules.allow`.

- [ ] **Step 4: Register projects in the solution**

Run:

```powershell
dotnet sln backend/dotnet/Tw.SmartPlatform.slnx add (Get-ChildItem backend/dotnet/BuildingBlocks/src -Recurse -Filter *.csproj | Where-Object FullName -Match 'Caching|DistributedLocking|Idempotency|Resilience|BackgroundJobs|Configuration|Gateway').FullName
dotnet sln backend/dotnet/Tw.SmartPlatform.slnx add (Get-ChildItem backend/dotnet/BuildingBlocks/tests -Recurse -Filter *.csproj | Where-Object FullName -Match 'Caching|DistributedLocking|Idempotency|Resilience|BackgroundJobs|Configuration|Gateway').FullName
```

- [ ] **Step 5: Run boundary smoke checks**

Run:

```powershell
rg -n "Tw\.Data|Tw\.Uow|Tw\.Application|Tw\.EventBus|Tw\.BackgroundJobs|Tw\.MultiTenancy|Tw\.Sharding" backend/dotnet/BuildingBlocks/src/Gateway/Tw.Gateway.Yarp
```

Expected: no matches.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/Build backend/dotnet/Tw.SmartPlatform.slnx backend/dotnet/BuildingBlocks/src backend/dotnet/BuildingBlocks/tests
git commit -m "feat: add infrastructure runtime package shells"
```

### Task 2: Implement Configuration Governance

**Files:**
- Create: `Tw.Configuration/ConfigurationChangeEvent.cs`
- Create: `Tw.Configuration/IConfigurationGovernance.cs`
- Create: `Tw.Configuration/SensitiveConfigurationKey.cs`
- Create: `Tw.Configuration.Json/JsonConfigurationManifest.cs`
- Create: `Tw.Configuration.Json/JsonConfigurationPathValidator.cs`
- Create: `Tw.Configuration.Json/JsonConfigurationBuilderExtensions.cs`
- Create: `Tw.Configuration.Nacos/NacosConfigurationBridge.cs`
- Test: `Tw.Configuration.Json.Tests/JsonConfigurationPathValidatorTests.cs`
- Test: `Tw.Configuration.Tests/ConfigurationGovernanceTests.cs`

- [ ] **Step 1: Write path validation tests**

```csharp
using AwesomeAssertions;
using Tw.Configuration.Json;

namespace Tw.Configuration.Json.Tests;

public sealed class JsonConfigurationPathValidatorTests
{
    [Fact]
    public void Validate_RejectsPathOutsideAllowedRoots()
    {
        var validator = new JsonConfigurationPathValidator(
            contentRoot: "D:/app",
            allowedRoots: ["D:/app/config"]);

        var act = () => validator.Validate("D:/secrets/appsettings.json");

        act.Should().Throw<ConfigurationPathException>()
            .WithMessage("*outside allowed configuration roots*");
    }
}
```

- [ ] **Step 2: Write user secrets environment test**

```csharp
using AwesomeAssertions;
using Tw.Configuration;

namespace Tw.Configuration.Tests;

public sealed class ConfigurationGovernanceTests
{
    [Fact]
    public void UserSecrets_AreAllowedOnlyInLocalOrDevelopment()
    {
        ConfigurationSourcePolicy.IsUserSecretsAllowed("Development").Should().BeTrue();
        ConfigurationSourcePolicy.IsUserSecretsAllowed("Local").Should().BeTrue();
        ConfigurationSourcePolicy.IsUserSecretsAllowed("Production").Should().BeFalse();
    }
}
```

- [ ] **Step 3: Implement source policy**

Create `ConfigurationSourcePolicy` in `Tw.Configuration`:

```csharp
namespace Tw.Configuration;

public static class ConfigurationSourcePolicy
{
    public static bool IsUserSecretsAllowed(string environmentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentName);

        return string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase)
            || string.Equals(environmentName, "Local", StringComparison.OrdinalIgnoreCase);
    }
}
```

- [ ] **Step 4: Implement JSON manifest and path validator**

`JsonConfigurationManifest` stores an ordered file list. `JsonConfigurationPathValidator` canonicalizes paths with `Path.GetFullPath`, rejects wildcard scanning, rejects traversal outside the content root or explicit allowed roots, and never logs raw secret values.

- [ ] **Step 5: Implement Nacos bridge boundary**

`NacosConfigurationBridge` imports validated configuration keys and service discovery metadata only. It must not implement secret storage and must emit `ConfigurationChangeEvent` for accepted changes.

- [ ] **Step 6: Run tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Configuration.Tests/Tw.Configuration.Tests.csproj --nologo
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Configuration.Json.Tests/Tw.Configuration.Json.Tests.csproj --nologo
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Configuration.Nacos.Tests/Tw.Configuration.Nacos.Tests.csproj --nologo
```

Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Configuration backend/dotnet/BuildingBlocks/tests/Tw.Configuration*
git commit -m "feat: add configuration governance packages"
```

### Task 3: Implement Caching, Distributed Locking, And Idempotency

**Files:**
- Create: `Tw.Caching/CacheKey.cs`
- Create: `Tw.Caching/CacheKeyBuilder.cs`
- Create: `Tw.Caching/ICacheInvalidationPublisher.cs`
- Create: `Tw.Caching.FusionCache/FusionCacheAdapter.cs`
- Create: `Tw.DistributedLocking.Abstractions/DistributedLockKey.cs`
- Create: `Tw.DistributedLocking.Abstractions/IDistributedLock.cs`
- Create: `Tw.DistributedLocking/DistributedLockKeyBuilder.cs`
- Create: `Tw.DistributedLocking.Redis/RedisDistributedLock.cs`
- Create: `Tw.Idempotency/IdempotencyBoundary.cs`
- Create: `Tw.Idempotency/IdempotencyKey.cs`
- Create: `Tw.Idempotency/IIdempotencyStore.cs`
- Create: `Tw.Idempotency/IdempotencyResult.cs`
- Create: `Tw.Idempotency/IdempotencyReservation.cs`
- Create: `Tw.Idempotency/IdempotencyConflictException.cs`
- Create: `Tw.Idempotency/IdempotencyExecutor.cs`
- Create: `Tw.Idempotency/Hosts/HttpIdempotencyContextFactory.cs`
- Create: `Tw.Idempotency/Hosts/GrpcIdempotencyContextFactory.cs`
- Create: `Tw.Idempotency/Hosts/CapIdempotencyContextFactory.cs`
- Create: `Tw.Idempotency/Hosts/BackgroundJobIdempotencyContextFactory.cs`
- Test: `Tw.Caching.Tests/CacheKeyBuilderTests.cs`
- Test: `Tw.DistributedLocking.Tests/DistributedLockKeyBuilderTests.cs`
- Test: `Tw.Idempotency.Tests/IdempotencyExecutorTests.cs`
- Test: `Tw.Idempotency.Tests/IdempotencyHostContextFactoryTests.cs`

- [ ] **Step 1: Write cache key test**

```csharp
using AwesomeAssertions;
using Tw.Caching;

namespace Tw.Caching.Tests;

public sealed class CacheKeyBuilderTests
{
    [Fact]
    public void Build_IncludesTenantShardResourceAndVersion()
    {
        var key = CacheKeyBuilder.Build("tenant-a", "orders-2026", "Order", "42", "v3");

        key.Value.Should().Be("tenant-a:orders-2026:Order:42:v3");
    }
}
```

- [ ] **Step 2: Write lock key test**

```csharp
using AwesomeAssertions;
using Tw.DistributedLocking;

namespace Tw.DistributedLocking.Tests;

public sealed class DistributedLockKeyBuilderTests
{
    [Fact]
    public void Build_IncludesTenantShardResourceAndIdentifier()
    {
        var key = DistributedLockKeyBuilder.Build("tenant-a", "shard-01", "Invoice", "inv-100");

        key.Value.Should().Be("lock:tenant-a:shard-01:Invoice:inv-100");
    }
}
```

- [ ] **Step 3: Write idempotency executor tests**

```csharp
using AwesomeAssertions;
using Tw.Idempotency;

namespace Tw.Idempotency.Tests;

public sealed class IdempotencyExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsFirstResultForDuplicateRequest()
    {
        var store = new InMemoryIdempotencyStore();
        var executor = new IdempotencyExecutor(store);
        var key = new IdempotencyKey(IdempotencyBoundary.Http, "tenant-a", "Order", "Create", "request-1");

        var first = await executor.ExecuteAsync(key, "body-hash-1", () => Task.FromResult(IdempotencyResult.Success(201, "created")));
        var duplicate = await executor.ExecuteAsync(key, "body-hash-1", () => Task.FromResult(IdempotencyResult.Success(201, "duplicate-created")));

        first.Should().Be(IdempotencyResult.Success(201, "created"));
        duplicate.Should().Be(IdempotencyResult.Success(201, "created"));
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsStableConflictCode_WhenSameKeyHasDifferentFingerprint()
    {
        var store = new InMemoryIdempotencyStore();
        var executor = new IdempotencyExecutor(store);
        var key = new IdempotencyKey(IdempotencyBoundary.Http, "tenant-a", "Order", "Create", "request-1");

        await executor.ExecuteAsync(key, "body-hash-1", () => Task.FromResult(IdempotencyResult.Success(201, "created")));

        var act = () => executor.ExecuteAsync(key, "body-hash-2", () => Task.FromResult(IdempotencyResult.Success(201, "created")));

        await act.Should().ThrowAsync<IdempotencyConflictException>()
            .Where(exception => exception.Code == "IDEMPOTENCY:000409");
    }
}
```

- [ ] **Step 4: Write idempotency host-boundary tests**

```csharp
using AwesomeAssertions;
using Tw.Idempotency;
using Tw.Idempotency.Hosts;

namespace Tw.Idempotency.Tests;

public sealed class IdempotencyHostContextFactoryTests
{
    [Fact]
    public void HttpFactory_BuildsTenantScopedRequestKey()
    {
        var key = HttpIdempotencyContextFactory.Create(
            tenantId: "tenant-a",
            resourceType: "Order",
            operation: "Create",
            idempotencyHeader: "request-1");

        key.Should().Be(new IdempotencyKey(IdempotencyBoundary.Http, "tenant-a", "Order", "Create", "request-1"));
    }

    [Fact]
    public void CapFactory_BuildsMessageDedupeKey()
    {
        var key = CapIdempotencyContextFactory.Create("tenant-a", "OrderCreated", "cap-message-1");

        key.Should().Be(new IdempotencyKey(IdempotencyBoundary.Cap, "tenant-a", "OrderCreated", "Consume", "cap-message-1"));
    }

    [Fact]
    public void BackgroundJobFactory_BuildsJobFireKey()
    {
        var key = BackgroundJobIdempotencyContextFactory.Create("tenant-a", "MonthlyBillingJob", "fire-1");

        key.Should().Be(new IdempotencyKey(IdempotencyBoundary.BackgroundJob, "tenant-a", "MonthlyBillingJob", "Execute", "fire-1"));
    }
}
```

- [ ] **Step 5: Implement models and builders**

Use immutable record types for `CacheKey`, `DistributedLockKey`, and `IdempotencyKey`. Builders must reject empty tenant, shard, resource type, operation, boundary, and identifier values. Non-SaaS callers pass `tenantId = "default"`. No-sharding callers pass `shardStrategy = "none"` and `shardKey = "default"`.

- [ ] **Step 6: Implement invalidation and idempotency boundaries**

```csharp
namespace Tw.Idempotency;

public enum IdempotencyBoundary
{
    Http = 1,
    Grpc = 2,
    Cap = 3,
    BackgroundJob = 4
}

public sealed record IdempotencyKey(
    IdempotencyBoundary Boundary,
    string TenantId,
    string ResourceType,
    string Operation,
    string BusinessKey);

public sealed record IdempotencyResult(int StatusCode, string Body, string Code)
{
    public static IdempotencyResult Success(int statusCode, string body) => new(statusCode, body, "SYSTEM:000000");

    public static IdempotencyResult Conflict(string code) => new(409, string.Empty, code);
}

public enum IdempotencyReservationStatus
{
    Started = 1,
    Duplicate = 2,
    Conflict = 3
}

public sealed record IdempotencyReservation(IdempotencyReservationStatus Status, IdempotencyResult? ExistingResult);

public sealed class IdempotencyConflictException(IdempotencyKey key)
    : Exception("幂等键已被不同请求内容使用")
{
    public string Code { get; } = "IDEMPOTENCY:000409";

    public IdempotencyKey Key { get; } = key;
}

public interface IIdempotencyStore
{
    Task<IdempotencyReservation> TryBeginAsync(IdempotencyKey key, string fingerprint, CancellationToken cancellationToken = default);

    Task<IdempotencyResult?> GetAsync(IdempotencyKey key, CancellationToken cancellationToken = default);

    Task CompleteAsync(IdempotencyKey key, IdempotencyResult result, CancellationToken cancellationToken = default);
}
```

```csharp
namespace Tw.Idempotency;

public sealed class IdempotencyExecutor(IIdempotencyStore store)
{
    public async Task<IdempotencyResult> ExecuteAsync(
        IdempotencyKey key,
        string fingerprint,
        Func<Task<IdempotencyResult>> operation,
        CancellationToken cancellationToken = default)
    {
        var reservation = await store.TryBeginAsync(key, fingerprint, cancellationToken);
        if (reservation.Status == IdempotencyReservationStatus.Duplicate)
        {
            return reservation.ExistingResult ?? await store.GetAsync(key, cancellationToken) ?? IdempotencyResult.Conflict("IDEMPOTENCY:000409");
        }

        if (reservation.Status == IdempotencyReservationStatus.Conflict)
        {
            throw new IdempotencyConflictException(key);
        }

        var result = await operation();
        await store.CompleteAsync(key, result, cancellationToken);
        return result;
    }
}
```

`ICacheInvalidationPublisher` publishes invalidation messages after UoW commit only. `IIdempotencyStore` stores request state, first response, conflict response, expiration, and fingerprint. SQL persistence for idempotency remains in `Tw.Data.SqlSugar`, not in `Tw.Idempotency`.

- [ ] **Step 7: Implement host context factories**

`HttpIdempotencyContextFactory`, `GrpcIdempotencyContextFactory`, `CapIdempotencyContextFactory`, and `BackgroundJobIdempotencyContextFactory` create `IdempotencyKey` values at the trusted boundary. HTTP and gRPC use caller-supplied idempotency metadata only after tenant and operation are known. CAP uses message id or event id for message dedupe. Background jobs use scheduler fire id plus job name for task dedupe. Every factory must reject missing tenant id, resource type, operation, or dedupe key.

- [ ] **Step 8: Run tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Caching.Tests/Tw.Caching.Tests.csproj --nologo
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DistributedLocking.Tests/Tw.DistributedLocking.Tests.csproj --nologo
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Idempotency.Tests/Tw.Idempotency.Tests.csproj --nologo
```

Expected: PASS.

- [ ] **Step 9: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Caching backend/dotnet/BuildingBlocks/src/DistributedLocking backend/dotnet/BuildingBlocks/src/Idempotency backend/dotnet/BuildingBlocks/tests/Tw.Caching* backend/dotnet/BuildingBlocks/tests/Tw.DistributedLocking* backend/dotnet/BuildingBlocks/tests/Tw.Idempotency*
git commit -m "feat: add caching locking and idempotency packages"
```

### Task 4: Implement Resilience Policies

**Files:**
- Create: `Tw.Resilience/OperationKind.cs`
- Create: `Tw.Resilience/ResiliencePolicyDescriptor.cs`
- Create: `Tw.Resilience/ResiliencePolicyBuilder.cs`
- Create: `Tw.Resilience/HttpResilienceServiceCollectionExtensions.cs`
- Test: `Tw.Resilience.Tests/ResiliencePolicyBuilderTests.cs`

- [ ] **Step 1: Write no-retry test for non-idempotent writes**

```csharp
using AwesomeAssertions;
using Tw.Resilience;

namespace Tw.Resilience.Tests;

public sealed class ResiliencePolicyBuilderTests
{
    [Fact]
    public void Build_DisablesRetryForNonIdempotentWrite()
    {
        var descriptor = ResiliencePolicyDescriptor.ForHttp(
            operationName: "CreateOrder",
            operationKind: OperationKind.NonIdempotentWrite,
            timeout: TimeSpan.FromSeconds(3));

        var policy = ResiliencePolicyBuilder.Build(descriptor);

        policy.RetryEnabled.Should().BeFalse();
        policy.Timeout.Should().Be(TimeSpan.FromSeconds(3));
    }
}
```

- [ ] **Step 2: Implement descriptors**

`ResiliencePolicyDescriptor` must include operation name, operation kind, timeout, retry count, circuit breaker flag, rate limiter flag, concurrency limiter flag, and fallback flag. Constructor and factory methods reject missing timeout.

- [ ] **Step 3: Implement policy builder**

The builder creates Polly 8 pipeline definitions. It must not retry non-idempotent writes, input validation errors, permission errors, or contract errors. All HTTP, database, cache, message, and file operations require a timeout or deadline.

- [ ] **Step 4: Run tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Resilience.Tests/Tw.Resilience.Tests.csproj --nologo
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Resilience backend/dotnet/BuildingBlocks/tests/Tw.Resilience.Tests
git commit -m "feat: add resilience policy package"
```

### Task 5: Implement Background Jobs And Quartz Adapter

**Files:**
- Create: `Tw.BackgroundJobs.Abstractions/BackgroundJobDefinition.cs`
- Create: `Tw.BackgroundJobs.Abstractions/IBackgroundJob.cs`
- Create: `Tw.BackgroundJobs.Abstractions/BackgroundJobContext.cs`
- Create: `Tw.BackgroundJobs.Abstractions/BackgroundJobControlAction.cs`
- Create: `Tw.BackgroundJobs.Abstractions/BackgroundJobControlCommand.cs`
- Create: `Tw.BackgroundJobs.Abstractions/IBackgroundJobControlService.cs`
- Create: `Tw.BackgroundJobs.Abstractions/IBackgroundJobStateStore.cs`
- Create: `Tw.BackgroundJobs/BackgroundJobPipeline.cs`
- Create: `Tw.BackgroundJobs/BackgroundJobCommand.cs`
- Create: `Tw.BackgroundJobs/IBackgroundJobAuditSink.cs`
- Create: `Tw.BackgroundJobs/IBackgroundJobTraceSink.cs`
- Create: `Tw.BackgroundJobs/IBackgroundJobMetricSink.cs`
- Create: `Tw.BackgroundJobs/BackgroundJobAuditEvent.cs`
- Create: `Tw.BackgroundJobs/BackgroundJobTraceEvent.cs`
- Create: `Tw.BackgroundJobs/BackgroundJobMetricEvent.cs`
- Create: `Tw.BackgroundJobs.Quartz/QuartzBackgroundJobScheduler.cs`
- Create: `Tw.BackgroundJobs.Quartz/QuartzJobAdapter.cs`
- Create: `Tw.BackgroundJobs.Quartz/QuartzBackgroundJobControlService.cs`
- Create: `Tw.BackgroundJobs.Quartz/QuartzSchedulerStoreOptions.cs`
- Create: `Tw.BackgroundJobs.Quartz/CronExpressionValidator.cs`
- Test: `Tw.BackgroundJobs.Tests/BackgroundJobPipelineTests.cs`
- Test: `Tw.BackgroundJobs.Tests/BackgroundJobControlContractTests.cs`
- Test: `Tw.BackgroundJobs.Quartz.Tests/CronExpressionValidatorTests.cs`
- Test: `Tw.BackgroundJobs.Quartz.Tests/QuartzBackgroundJobControlServiceTests.cs`
- Test: `Tw.BackgroundJobs.Quartz.Tests/QuartzJobAdapterTests.cs`

- [ ] **Step 1: Write pipeline test that enters application through ISender**

```csharp
using AwesomeAssertions;
using MediatR;
using NSubstitute;
using Tw.BackgroundJobs;
using Tw.BackgroundJobs.Abstractions;

namespace Tw.BackgroundJobs.Tests;

public sealed class BackgroundJobPipelineTests
{
    [Fact]
    public async Task ExecuteAsync_SendsCommandAndRecordsAuditTraceAndMetrics()
    {
        var sender = Substitute.For<ISender>();
        var auditSink = new RecordingJobAuditSink();
        var traceSink = new RecordingJobTraceSink();
        var metricSink = new RecordingJobMetricSink();
        var pipeline = new BackgroundJobPipeline(sender, auditSink, traceSink, metricSink);
        var context = new BackgroundJobContext("tenant-a", "default", "job-1", DateTimeOffset.UtcNow);
        var request = new SampleCommand("order-1");

        await pipeline.ExecuteAsync(new BackgroundJobCommand(request, context), CancellationToken.None);

        await sender.Received(1).Send(request, Arg.Any<CancellationToken>());
        auditSink.Events.Should().Contain(e => e.TenantId == "tenant-a" && e.JobId == "job-1");
        traceSink.Events.Should().Contain(e => e.JobId == "job-1" && e.EventName == "background_job.started");
        metricSink.Events.Should().Contain(e => e.JobId == "job-1" && e.MetricName == "background_job.succeeded");
    }

    private sealed record SampleCommand(string OrderId) : IRequest;

    private sealed class RecordingJobAuditSink : IBackgroundJobAuditSink
    {
        public List<BackgroundJobAuditEvent> Events { get; } = [];

        public Task RecordAsync(BackgroundJobAuditEvent auditEvent, CancellationToken cancellationToken)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingJobTraceSink : IBackgroundJobTraceSink
    {
        public List<BackgroundJobTraceEvent> Events { get; } = [];

        public Task RecordAsync(BackgroundJobTraceEvent traceEvent, CancellationToken cancellationToken)
        {
            Events.Add(traceEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingJobMetricSink : IBackgroundJobMetricSink
    {
        public List<BackgroundJobMetricEvent> Events { get; } = [];

        public Task RecordAsync(BackgroundJobMetricEvent metricEvent, CancellationToken cancellationToken)
        {
            Events.Add(metricEvent);
            return Task.CompletedTask;
        }
    }
}
```

- [ ] **Step 2: Write scheduler control and Cron validation tests**

```csharp
using AwesomeAssertions;
using Tw.BackgroundJobs.Abstractions;
using Tw.BackgroundJobs.Quartz;

namespace Tw.BackgroundJobs.Quartz.Tests;

public sealed class CronExpressionValidatorTests
{
    [Fact]
    public void Validate_RejectsInvalidCron()
    {
        var act = () => CronExpressionValidator.Validate("not-a-cron");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("后台任务 Cron 表达式无效");
    }
}

public sealed class QuartzBackgroundJobControlServiceTests
{
    [Fact]
    public async Task ExecuteAsync_ForwardsPauseResumeTriggerAndStopToScheduler()
    {
        var scheduler = new RecordingBackgroundJobScheduler();
        var service = new QuartzBackgroundJobControlService(scheduler, new InMemoryBackgroundJobStateStore());

        await service.ExecuteAsync(new BackgroundJobControlCommand("billing.monthly", BackgroundJobControlAction.Pause), CancellationToken.None);
        await service.ExecuteAsync(new BackgroundJobControlCommand("billing.monthly", BackgroundJobControlAction.Resume), CancellationToken.None);
        await service.ExecuteAsync(new BackgroundJobControlCommand("billing.monthly", BackgroundJobControlAction.Trigger), CancellationToken.None);
        await service.ExecuteAsync(new BackgroundJobControlCommand("billing.monthly", BackgroundJobControlAction.Stop), CancellationToken.None);

        scheduler.Actions.Should().Equal("Pause", "Resume", "Trigger", "Stop");
    }
}
```

- [ ] **Step 3: Implement abstractions**

```csharp
namespace Tw.BackgroundJobs.Abstractions;

public sealed record BackgroundJobContext(string TenantId, string ShardId, string JobId, DateTimeOffset StartedAt);

public interface IBackgroundJob<TArgs>
{
    Task ExecuteAsync(TArgs args, BackgroundJobContext context, CancellationToken cancellationToken);
}

public sealed record BackgroundJobDefinition(
    string Name,
    Type ArgumentType,
    string Schedule,
    string TenantBehavior,
    TimeSpan Timeout,
    string RetryPolicyName,
    string AuditCategory,
    bool IsClustered,
    string SchedulerDatabaseKey);

public enum BackgroundJobControlAction
{
    Create = 1,
    Pause = 2,
    Resume = 3,
    Trigger = 4,
    Stop = 5
}

public sealed record BackgroundJobControlCommand(
    string JobName,
    BackgroundJobControlAction Action,
    BackgroundJobDefinition? Definition = null);

public interface IBackgroundJobControlService
{
    Task ExecuteAsync(BackgroundJobControlCommand command, CancellationToken cancellationToken);
}

public interface IBackgroundJobStateStore
{
    Task SaveAsync(BackgroundJobDefinition definition, CancellationToken cancellationToken);
    Task MarkPausedAsync(string jobName, CancellationToken cancellationToken);
    Task MarkRunningAsync(string jobName, CancellationToken cancellationToken);
    Task MarkStoppedAsync(string jobName, CancellationToken cancellationToken);
}
```

- [ ] **Step 4: Implement runtime pipeline**

```csharp
using MediatR;
using Tw.BackgroundJobs.Abstractions;

namespace Tw.BackgroundJobs;

public sealed record BackgroundJobCommand(IRequest Request, BackgroundJobContext Context);

public interface IBackgroundJobAuditSink
{
    Task RecordAsync(BackgroundJobAuditEvent auditEvent, CancellationToken cancellationToken);
}

public interface IBackgroundJobTraceSink
{
    Task RecordAsync(BackgroundJobTraceEvent traceEvent, CancellationToken cancellationToken);
}

public interface IBackgroundJobMetricSink
{
    Task RecordAsync(BackgroundJobMetricEvent metricEvent, CancellationToken cancellationToken);
}

public sealed record BackgroundJobAuditEvent(string TenantId, string ShardId, string JobId, DateTimeOffset StartedAt);

public sealed record BackgroundJobTraceEvent(string TenantId, string ShardId, string JobId, string EventName, DateTimeOffset OccurredAt);

public sealed record BackgroundJobMetricEvent(string TenantId, string ShardId, string JobId, string MetricName, double Value);

public sealed class BackgroundJobPipeline(
    ISender sender,
    IBackgroundJobAuditSink auditSink,
    IBackgroundJobTraceSink traceSink,
    IBackgroundJobMetricSink metricSink)
{
    public async Task ExecuteAsync(BackgroundJobCommand command, CancellationToken cancellationToken = default)
    {
        var context = command.Context;
        await traceSink.RecordAsync(new BackgroundJobTraceEvent(context.TenantId, context.ShardId, context.JobId, "background_job.started", DateTimeOffset.UtcNow), cancellationToken);

        try
        {
            await sender.Send(command.Request, cancellationToken);
            await auditSink.RecordAsync(new BackgroundJobAuditEvent(context.TenantId, context.ShardId, context.JobId, context.StartedAt), cancellationToken);
            await metricSink.RecordAsync(new BackgroundJobMetricEvent(context.TenantId, context.ShardId, context.JobId, "background_job.succeeded", 1), cancellationToken);
        }
        catch
        {
            await metricSink.RecordAsync(new BackgroundJobMetricEvent(context.TenantId, context.ShardId, context.JobId, "background_job.failed", 1), cancellationToken);
            throw;
        }
    }
}
```

`BackgroundJobPipeline` resolves tenant, shard, culture, idempotency, authorization, audit, trace, metric, and UoW behavior before invoking `ISender.Send(...)`. Background jobs must not execute business use cases through direct service calls that bypass the application pipeline.

- [ ] **Step 5: Implement scheduler control service and static Scheduler DB options**

```csharp
namespace Tw.BackgroundJobs.Quartz;

public sealed class QuartzSchedulerStoreOptions
{
    public string SchedulerDatabaseKey { get; set; } = "Scheduler";

    public bool Clustered { get; set; } = true;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SchedulerDatabaseKey))
        {
            throw new InvalidOperationException("后台任务 Scheduler DB 标识不能为空");
        }
    }
}

public static class CronExpressionValidator
{
    public static void Validate(string cronExpression)
    {
        if (!Quartz.CronExpression.IsValidExpression(cronExpression))
        {
            throw new InvalidOperationException("后台任务 Cron 表达式无效");
        }
    }
}
```

`QuartzBackgroundJobControlService` maps `Create`, `Pause`, `Resume`, `Trigger`, and `Stop` to the scheduler adapter and writes state transitions through `IBackgroundJobStateStore`. `Create` must validate the Cron expression before persisting state or registering the trigger. `QuartzSchedulerStoreOptions.SchedulerDatabaseKey` identifies the static scheduler database; tenant and shard routing must not change the scheduler metadata database.

- [ ] **Step 6: Implement Quartz adapter**

`QuartzBackgroundJobScheduler` maps framework job definitions to Quartz job details and triggers. Clustered jobs must use Quartz persistent store configuration with `QuartzSchedulerStoreOptions.Clustered = true`. `QuartzJobAdapter` creates the framework `BackgroundJobContext` from Quartz execution data and rejects missing tenant or shard metadata for tenant-scoped jobs. The adapter then creates `BackgroundJobCommand` and calls `BackgroundJobPipeline.ExecuteAsync(...)`.

- [ ] **Step 7: Run tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.BackgroundJobs.Tests/Tw.BackgroundJobs.Tests.csproj --nologo
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.BackgroundJobs.Quartz.Tests/Tw.BackgroundJobs.Quartz.Tests.csproj --nologo
```

Expected: PASS.

- [ ] **Step 8: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/BackgroundJobs backend/dotnet/BuildingBlocks/tests/Tw.BackgroundJobs*
git commit -m "feat: add background jobs and quartz adapter"
```

### Task 6: Implement Gateway Governance And YARP Adapter

**Files:**
- Create: `Tw.Gateway/GatewayRoute.cs`
- Create: `Tw.Gateway/GatewayHeaderPolicy.cs`
- Create: `Tw.Gateway/GatewayHeaderSanitizer.cs`
- Create: `Tw.Gateway/GatewayRateLimitPolicy.cs`
- Create: `Tw.Gateway.Yarp/YarpGatewayBuilderExtensions.cs`
- Create: `Tw.Gateway.Yarp/YarpHeaderTransformFactory.cs`
- Create: `Tw.Gateway.Yarp/YarpRouteValidation.cs`
- Test: `Tw.Gateway.Tests/GatewayHeaderSanitizerTests.cs`
- Test: `Tw.Gateway.Yarp.Tests/YarpRouteValidationTests.cs`

- [ ] **Step 1: Write header sanitizer test**

```csharp
using AwesomeAssertions;
using Tw.Gateway;

namespace Tw.Gateway.Tests;

public sealed class GatewayHeaderSanitizerTests
{
    [Fact]
    public void Sanitize_RemovesCallerSuppliedIdentityTenantPermissionAndRoleHeaders()
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Authorization"] = "Bearer token",
            ["X-Tenant-Id"] = "forged",
            ["X-User-Id"] = "forged",
            ["X-Permissions"] = "forged",
            ["X-Roles"] = "forged"
        };

        var sanitized = GatewayHeaderSanitizer.Sanitize(headers);

        sanitized.Should().ContainKey("Authorization");
        sanitized.Should().NotContainKey("X-Tenant-Id");
        sanitized.Should().NotContainKey("X-User-Id");
        sanitized.Should().NotContainKey("X-Permissions");
        sanitized.Should().NotContainKey("X-Roles");
    }
}
```

- [ ] **Step 2: Implement gateway model**

`GatewayRoute` includes route id, cluster id, path, methods, destination, service discovery name, weight, timeout, retry policy, rate limit policy, WebSocket/SSE/gRPC pass-through flags, and trusted tenant source.

- [ ] **Step 3: Implement header policy**

Gateway forwards the original `Authorization` header for user-agent requests. Gateway removes caller-supplied identity, tenant, permission, and role headers. Gateway may set `X-Tenant-Id` only from a verified JWT, controlled route, controlled subdomain, or server-side configuration.

- [ ] **Step 4: Implement YARP adapter**

`YarpGatewayBuilderExtensions` registers YARP, service discovery, transforms, and route validation. `YarpRouteValidation` rejects routes that combine strict global rate limiting with gateway-local rate limiting; strict global limits stay at Edge, WAF, API Management, or load balancer layers.

- [ ] **Step 5: Run tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Gateway.Tests/Tw.Gateway.Tests.csproj --nologo
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Gateway.Yarp.Tests/Tw.Gateway.Yarp.Tests.csproj --nologo
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Gateway backend/dotnet/BuildingBlocks/tests/Tw.Gateway*
git commit -m "feat: add gateway governance and yarp adapter"
```

### Task 7: Documentation And Full Verification

**Files:**
- Create: `docs/shared-packages/dotnet/Tw.Caching/README.md`
- Create: `docs/shared-packages/dotnet/Tw.DistributedLocking/README.md`
- Create: `docs/shared-packages/dotnet/Tw.Idempotency/README.md`
- Create: `docs/shared-packages/dotnet/Tw.Resilience/README.md`
- Create: `docs/shared-packages/dotnet/Tw.BackgroundJobs/README.md`
- Create: `docs/shared-packages/dotnet/Tw.Configuration/README.md`
- Create: `docs/shared-packages/dotnet/Tw.Gateway/README.md`
- Modify: `docs/shared-packages/dotnet/README.md`

- [ ] **Step 1: Add shared package docs**

Each README must include package responsibility, public entry points, dependency boundary, and one minimal registration or usage example. `Tw.Gateway.Yarp` docs must restate that it does not depend on data, UoW, application, CAP, background jobs, tenancy runtime, or sharding runtime packages.

- [ ] **Step 2: Update shared package index**

Add entries for caching, distributed locking, idempotency, resilience, background jobs, configuration, and gateway packages.

- [ ] **Step 3: Run package family tests**

Run:

```powershell
dotnet test backend/dotnet/Tw.SmartPlatform.slnx --filter "FullyQualifiedName~Caching|FullyQualifiedName~DistributedLocking|FullyQualifiedName~Idempotency|FullyQualifiedName~Resilience|FullyQualifiedName~BackgroundJobs|FullyQualifiedName~Configuration|FullyQualifiedName~Gateway" --nologo
```

Expected: PASS.

- [ ] **Step 4: Run boundary scan**

Run:

```powershell
rg -n "Tw\.Data|Tw\.Uow|Tw\.Application|Tw\.EventBus|Tw\.BackgroundJobs|Tw\.MultiTenancy|Tw\.Sharding" backend/dotnet/BuildingBlocks/src/Gateway/Tw.Gateway.Yarp
rg -n "UserSecrets|AddUserSecrets" backend/dotnet/BuildingBlocks/src/Configuration
```

Expected: first command has no matches; second command matches policy code and tests only, with no production path enabling User Secrets outside Local or Development.

- [ ] **Step 5: Commit**

```bash
git add docs/shared-packages backend/dotnet/BuildingBlocks/src backend/dotnet/BuildingBlocks/tests
git commit -m "docs: document infrastructure runtime packages"
```

## Plan Self-Review

- Spec coverage: configuration precedence and secret boundary, caching invalidation after UoW, tenant and shard aware lock keys, HTTP/gRPC/CAP/background-job idempotency boundaries, duplicate request/message/task handling, stable idempotency conflict code, Polly resilience, Quartz scheduler center controls, Cron validation, clustered jobs with static Scheduler DB, background job ISender entry, job audit/trace/metric telemetry, and YARP gateway governance are covered.
- Package boundary coverage: adapter packages carry third-party dependencies; gateway adapter forbidden dependencies are explicitly scanned.
- CAP transaction constraint: no CAP behavior is introduced here; CAP remains in P4 and continues to rely on the current `Tw.Uow` transaction.
- Placeholder scan: no placeholder tokens are present.
- Verification: each package family has targeted tests and a final solution-filtered test command.
