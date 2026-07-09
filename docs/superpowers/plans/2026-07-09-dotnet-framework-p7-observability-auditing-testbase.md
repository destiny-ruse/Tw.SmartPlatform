# Dotnet Framework P7 Observability Auditing TestBase Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement observability, auditing, and test-base packages with explicit sensitive-data protection and test-only dependency boundaries.

**Architecture:** `Tw.Observability` defines shared log, trace, metric, health, and correlation contracts. Sensitive-data masking and write-back protection stay in `Tw.Security`; observability, Serilog, OpenTelemetry, auditing, export, and error responses consume those security contracts. Auditing contracts stay separate from audit collection and storage orchestration. `*TestBase` packages are referenced only by test projects and never by production packages.

**Tech Stack:** .NET 10, Serilog.AspNetCore 10.0.0, Serilog.Sinks.OpenTelemetry 4.2.0, OpenTelemetry 1.16.0, xUnit, AwesomeAssertions, NSubstitute, Testcontainers 4.13.0, Respawn 7.0.0

---

## File Structure

- Create: `backend/dotnet/BuildingBlocks/src/Observability/Tw.Observability`
- Create: `backend/dotnet/BuildingBlocks/src/Observability/Tw.Observability.Serilog`
- Create: `backend/dotnet/BuildingBlocks/src/Observability/Tw.Observability.OpenTelemetry`
- Create: `backend/dotnet/BuildingBlocks/src/Auditing/Tw.Auditing.Contracts`
- Create: `backend/dotnet/BuildingBlocks/src/Auditing/Tw.Auditing`
- Create: `backend/dotnet/BuildingBlocks/src/TestBase/Tw.TestBase`
- Create: `backend/dotnet/BuildingBlocks/src/TestBase/Tw.AspNetCore.TestBase`
- Create: `backend/dotnet/BuildingBlocks/src/TestBase/Tw.Data.SqlSugar.TestBase`
- Create: `backend/dotnet/BuildingBlocks/src/TestBase/Tw.EventBus.Cap.TestBase`
- Create matching test projects under `backend/dotnet/BuildingBlocks/tests`
- Modify: `backend/dotnet/Tw.SmartPlatform.slnx`
- Modify: `backend/dotnet/Build/Packages.ThirdParty.props`
- Modify: `backend/dotnet/Build/Packages.Tests.props`

### Task 1: Create Package Shells And Test-Only Boundaries

**Files:**
- Create all package directories listed above
- Create `.csproj` and `package-charter.yaml` for each package
- Create matching `.Tests.csproj` projects
- Modify: `backend/dotnet/Tw.SmartPlatform.slnx`

- [ ] **Step 1: Add or verify central versions**

```xml
<PackageVersion Include="Serilog.AspNetCore" Version="10.0.0" />
<PackageVersion Include="Serilog.Sinks.OpenTelemetry" Version="4.2.0" />
<PackageVersion Include="OpenTelemetry.Extensions.Hosting" Version="1.16.0" />
<PackageVersion Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.16.0" />
<PackageVersion Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.16.0" />
<PackageVersion Include="OpenTelemetry.Instrumentation.Http" Version="1.16.0" />
<PackageVersion Include="Testcontainers" Version="4.13.0" />
<PackageVersion Include="Respawn" Version="7.0.0" />
```

- [ ] **Step 2: Create project shells**

Runtime packages use `Microsoft.NET.Sdk`, `net10.0`, `Nullable=enable`, `ImplicitUsings=enable`, and `IsPackable=true`. TestBase packages also use `IsPackable=true` but their charter must mark `test_only: true`.

- [ ] **Step 3: Add package charters**

`Tw.Observability` charter lists log context, trace context, metrics fields, health status model, and the boundary for consuming `Tw.Security.DataMasking.IDataMasker`; it does not list masking as an observability-owned capability. `Tw.Auditing.Contracts` charter lists audit event contracts and storage abstractions. Each `*TestBase` charter states that production projects are forbidden from referencing it.

- [ ] **Step 4: Register projects in the solution**

Run:

```powershell
dotnet sln backend/dotnet/Tw.SmartPlatform.slnx add (Get-ChildItem backend/dotnet/BuildingBlocks/src/Observability,backend/dotnet/BuildingBlocks/src/Auditing,backend/dotnet/BuildingBlocks/src/TestBase -Recurse -Filter *.csproj).FullName
dotnet sln backend/dotnet/Tw.SmartPlatform.slnx add (Get-ChildItem backend/dotnet/BuildingBlocks/tests -Recurse -Filter *.csproj | Where-Object FullName -Match 'Observability|Auditing|TestBase').FullName
```

- [ ] **Step 5: Run test-only boundary scan**

Run:

```powershell
rg -n "Tw\.(TestBase|AspNetCore\.TestBase|Data\.SqlSugar\.TestBase|EventBus\.Cap\.TestBase)" backend/dotnet/BuildingBlocks/src
```

Expected: no matches outside the `backend/dotnet/BuildingBlocks/src/TestBase` package source files.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/Build backend/dotnet/Tw.SmartPlatform.slnx backend/dotnet/BuildingBlocks/src/Observability backend/dotnet/BuildingBlocks/src/Auditing backend/dotnet/BuildingBlocks/src/TestBase backend/dotnet/BuildingBlocks/tests
git commit -m "feat: add observability auditing and testbase package shells"
```

### Task 2: Implement Observability Contracts

**Files:**
- Create: `Tw.Observability/CorrelationContext.cs`
- Create: `Tw.Observability/TraceContext.cs`
- Create: `Tw.Observability/MetricTags.cs`
- Create: `Tw.Observability/HealthStatusModel.cs`
- Create: `Tw.Observability/IObservabilityContextAccessor.cs`
- Test: `Tw.Observability.Tests/MetricTagsTests.cs`

- [ ] **Step 1: Write metric tag test**

```csharp
using AwesomeAssertions;
using Tw.Observability;

namespace Tw.Observability.Tests;

public sealed class MetricTagsTests
{
    [Fact]
    public void Create_IncludesServiceTenantShardAndOperation()
    {
        var tags = MetricTags.Create("billing-api", "tenant-a", "shard-01", "CreateOrder");

        tags.Values.Should().ContainKey("service.name");
        tags.Values.Should().ContainKey("tenant.id");
        tags.Values.Should().ContainKey("shard.id");
        tags.Values.Should().ContainKey("operation.name");
    }
}
```

- [ ] **Step 2: Implement contracts**

Use immutable record types for correlation, trace, metric, and health models. `Tw.Observability` must not define a separate masking engine; packages that need redaction reference `Tw.Security` and call `IDataMasker`.

- [ ] **Step 3: Run tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Observability.Tests/Tw.Observability.Tests.csproj --nologo
```

Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Observability/Tw.Observability backend/dotnet/BuildingBlocks/tests/Tw.Observability.Tests
git commit -m "feat: add observability contracts"
```

### Task 3: Implement Serilog And OpenTelemetry Adapters

**Files:**
- Create: `Tw.Observability.Serilog/SerilogBuilderExtensions.cs`
- Create: `Tw.Observability.Serilog/RedactingLogEventEnricher.cs`
- Create: `Tw.Observability.OpenTelemetry/OpenTelemetryBuilderExtensions.cs`
- Create: `Tw.Observability.OpenTelemetry/ActivityTagEnricher.cs`
- Create: `Tw.Observability.OpenTelemetry/AspireDashboardOptions.cs`
- Test: `Tw.Observability.Serilog.Tests/RedactingLogEventEnricherTests.cs`
- Test: `Tw.Observability.OpenTelemetry.Tests/OpenTelemetryBuilderExtensionsTests.cs`

- [ ] **Step 1: Write Serilog redaction test**

```csharp
using AwesomeAssertions;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using Tw.Security.DataMasking;
using Tw.Observability.Serilog;

namespace Tw.Observability.Serilog.Tests;

public sealed class RedactingLogEventEnricherTests
{
    [Fact]
    public void Enrich_RedactsSensitiveScalarProperties()
    {
        var logEvent = new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            exception: null,
            messageTemplate: new MessageTemplateParser().Parse("Password {Password}"),
            properties:
            [
                new LogEventProperty("Password", new ScalarValue("secret"))
            ]);

        new RedactingLogEventEnricher(DefaultDataMasker.CreateDefault()).Enrich(logEvent, new TestLogEventPropertyFactory());

        logEvent.Properties["Password"].ToString().Should().NotContain("secret");
    }

    private sealed class TestLogEventPropertyFactory : ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
        {
            return new LogEventProperty(name, value is LogEventPropertyValue propertyValue ? propertyValue : new ScalarValue(value));
        }
    }
}
```

- [ ] **Step 2: Write OpenTelemetry default dependency test**

```csharp
using AwesomeAssertions;
using Tw.Observability.OpenTelemetry;

namespace Tw.Observability.OpenTelemetry.Tests;

public sealed class OpenTelemetryBuilderExtensionsTests
{
    [Fact]
    public void DefaultOptions_DoNotEnableGrpcNetClientInstrumentation()
    {
        var options = OpenTelemetryRegistrationOptions.Default;

        options.EnableGrpcNetClientInstrumentation.Should().BeFalse();
    }
}
```

- [ ] **Step 3: Implement Serilog adapter**

`SerilogBuilderExtensions` registers structured logging, correlation fields, trace id, tenant id, shard id, operation name, and redaction through `Tw.Security.DataMasking.IDataMasker`. It may use `Serilog.Sinks.OpenTelemetry`. It must not write secrets, tokens, full phone numbers, passwords, raw sensitive payloads, or full connection strings.

- [ ] **Step 4: Implement OpenTelemetry adapter**

`OpenTelemetryBuilderExtensions` registers traces, metrics, OTLP exporter, ASP.NET Core instrumentation, HTTP instrumentation, and Aspire Dashboard integration. It must not add `OpenTelemetry.Instrumentation.GrpcNetClient` as a default dependency.

- [ ] **Step 5: Run tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Observability.Serilog.Tests/Tw.Observability.Serilog.Tests.csproj --nologo
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Observability.OpenTelemetry.Tests/Tw.Observability.OpenTelemetry.Tests.csproj --nologo
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Observability/Tw.Observability.Serilog backend/dotnet/BuildingBlocks/src/Observability/Tw.Observability.OpenTelemetry backend/dotnet/BuildingBlocks/tests/Tw.Observability.Serilog.Tests backend/dotnet/BuildingBlocks/tests/Tw.Observability.OpenTelemetry.Tests
git commit -m "feat: add serilog and opentelemetry adapters"
```

### Task 4: Implement Auditing Contracts And Runtime

**Files:**
- Create: `Tw.Auditing.Contracts/AuditEvent.cs`
- Create: `Tw.Auditing.Contracts/AuditActor.cs`
- Create: `Tw.Auditing.Contracts/AuditAction.cs`
- Create: `Tw.Auditing.Contracts/IAuditStore.cs`
- Create: `Tw.Auditing/AuditCollector.cs`
- Create: `Tw.Auditing/AuditScope.cs`
- Create: `Tw.Auditing/AuditRedactionPolicy.cs`
- Test: `Tw.Auditing.Contracts.Tests/AuditEventTests.cs`
- Test: `Tw.Auditing.Tests/AuditCollectorTests.cs`

- [ ] **Step 1: Write audit event test**

```csharp
using AwesomeAssertions;
using Tw.Auditing.Contracts;

namespace Tw.Auditing.Contracts.Tests;

public sealed class AuditEventTests
{
    [Fact]
    public void CreateSecurityDenied_IncludesActorTenantActionAndStableCode()
    {
        var actor = new AuditActor("user-1", "tenant-a", "api");
        var auditEvent = AuditEvent.SecurityDenied(actor, "Order.Delete", "AUTH:FORBIDDEN");

        auditEvent.Actor.Should().Be(actor);
        auditEvent.Action.Name.Should().Be("Order.Delete");
        auditEvent.ErrorCode.Should().Be("AUTH:FORBIDDEN");
    }
}
```

- [ ] **Step 2: Write sensitive payload rejection test**

```csharp
using AwesomeAssertions;
using Tw.Auditing;
using Tw.Auditing.Contracts;

namespace Tw.Auditing.Tests;

public sealed class AuditCollectorTests
{
    [Fact]
    public async Task CollectAsync_RedactsRawSensitivePayload()
    {
        var store = new InMemoryAuditStore();
        var collector = new AuditCollector(store);
        var auditEvent = AuditEvent.ConfigurationChanged(
            new AuditActor("user-1", "tenant-a", "api"),
            key: "ConnectionStrings:Default",
            oldValue: "Password=old",
            newValue: "Password=new");

        await collector.CollectAsync(auditEvent);

        store.Events.Single().Details.Should().NotContain("Password=old");
        store.Events.Single().Details.Should().NotContain("Password=new");
    }

    private sealed class InMemoryAuditStore : IAuditStore
    {
        public List<AuditEvent> Events { get; } = [];

        public Task StoreAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }
}
```

- [ ] **Step 3: Implement contracts**

```csharp
namespace Tw.Auditing.Contracts;

public interface IAuditStore
{
    Task StoreAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
}
```

`AuditEvent` covers login, logout, permission change, configuration change, data export, sensitive data access, production data repair, batch operation, security denial, CAP cleanup, background job execution, and key rotation. Events include actor, tenant, shard, action, resource, result, error code, correlation id, trace id, timestamp, and redacted details.

- [ ] **Step 4: Implement runtime collector**

`AuditCollector` redacts sensitive details before storage, enriches events from observability context, writes to `IAuditStore`, and never throws raw storage exceptions to callers. Storage-specific implementations are outside this plan unless provided by another package.

- [ ] **Step 5: Run tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Auditing.Contracts.Tests/Tw.Auditing.Contracts.Tests.csproj --nologo
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Auditing.Tests/Tw.Auditing.Tests.csproj --nologo
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Auditing backend/dotnet/BuildingBlocks/tests/Tw.Auditing*
git commit -m "feat: add auditing contracts and collector"
```

### Task 5: Implement TestBase Packages

**Files:**
- Create: `Tw.TestBase/TestClock.cs`
- Create: `Tw.TestBase/TestCurrentUser.cs`
- Create: `Tw.TestBase/TestCurrentTenant.cs`
- Create: `Tw.TestBase/ContractTestJsonOptions.cs`
- Create: `Tw.AspNetCore.TestBase/AuthenticatedWebApplicationFactory.cs`
- Create: `Tw.AspNetCore.TestBase/TestAuthenticationHandler.cs`
- Create: `Tw.Data.SqlSugar.TestBase/SqlSugarDatabaseFixture.cs`
- Create: `Tw.Data.SqlSugar.TestBase/RespawnDatabaseResetter.cs`
- Create: `Tw.EventBus.Cap.TestBase/CapRabbitMqFixture.cs`
- Create: `Tw.EventBus.Cap.TestBase/OutboxInboxAssertionExtensions.cs`
- Test: `Tw.TestBase.Tests/TestClockTests.cs`
- Test: `Tw.AspNetCore.TestBase.Tests/TestAuthenticationHandlerTests.cs`
- Test: `Tw.Data.SqlSugar.TestBase.Tests/RespawnDatabaseResetterTests.cs`
- Test: `Tw.EventBus.Cap.TestBase.Tests/OutboxInboxAssertionExtensionsTests.cs`

- [ ] **Step 1: Write base test clock test**

```csharp
using AwesomeAssertions;
using Tw.TestBase;

namespace Tw.TestBase.Tests;

public sealed class TestClockTests
{
    [Fact]
    public void AdvanceBy_MovesUtcNowForward()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 7, 9, 0, 0, 0, TimeSpan.Zero));

        clock.AdvanceBy(TimeSpan.FromMinutes(5));

        clock.UtcNow.Should().Be(new DateTimeOffset(2026, 7, 9, 0, 5, 0, TimeSpan.Zero));
    }
}
```

- [ ] **Step 2: Implement common test helpers**

`Tw.TestBase` provides test clock, test id generation, test current user, test current tenant, test culture, JSON contract options, stable correlation ids, and assertion helpers for stable error codes.

- [ ] **Step 3: Implement ASP.NET Core test base**

`Tw.AspNetCore.TestBase` provides `WebApplicationFactory` setup, test authentication, HTTP contract test helpers, OpenAPI assertion helpers, and response error model assertion helpers. It must not be referenced by production packages.

- [ ] **Step 4: Implement data and CAP test bases**

`Tw.Data.SqlSugar.TestBase` provides Testcontainers database fixtures and Respawn reset. `Tw.EventBus.Cap.TestBase` provides CAP, RabbitMQ, Outbox/Inbox, consumer idempotency, and retry classification assertions.

- [ ] **Step 5: Run tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.TestBase.Tests/Tw.TestBase.Tests.csproj --nologo
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.TestBase.Tests/Tw.AspNetCore.TestBase.Tests.csproj --nologo
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Data.SqlSugar.TestBase.Tests/Tw.Data.SqlSugar.TestBase.Tests.csproj --nologo
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.EventBus.Cap.TestBase.Tests/Tw.EventBus.Cap.TestBase.Tests.csproj --nologo
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/TestBase backend/dotnet/BuildingBlocks/tests/Tw.*TestBase.Tests
git commit -m "feat: add framework testbase packages"
```

### Task 6: Add Quality Gates And Sensitive Data Checks

**Files:**
- Create: `backend/dotnet/Build/QualityGates/TestBaseReferenceGuard.ps1`
- Create: `backend/dotnet/Build/QualityGates/SensitiveOutputGuard.ps1`
- Modify: `backend/dotnet/Build/Build.cs`
- Modify: `backend/dotnet/Directory.Build.props`

- [ ] **Step 1: Add production reference guard**

`TestBaseReferenceGuard.ps1` scans production `.csproj` files under `backend/dotnet/BuildingBlocks/src` and fails when a project references any `*TestBase` package or project.

- [ ] **Step 2: Add sensitive output guard**

`SensitiveOutputGuard.ps1` scans test logs, generated OpenAPI samples, and contract response fixtures for known secret key names, token patterns, full phone numbers, and connection string fragments.

- [ ] **Step 3: Wire gates into NUKE build**

`Build.cs` adds targets `ValidateTestOnlyPackages` and `ValidateSensitiveOutput`, and includes both in the normal verification pipeline before packing.

- [ ] **Step 4: Run quality gates**

Run:

```powershell
pwsh backend/dotnet/Build/QualityGates/TestBaseReferenceGuard.ps1
pwsh backend/dotnet/Build/QualityGates/SensitiveOutputGuard.ps1
dotnet test backend/dotnet/Tw.SmartPlatform.slnx --filter "FullyQualifiedName~Observability|FullyQualifiedName~Auditing|FullyQualifiedName~TestBase" --nologo
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/Build backend/dotnet/Directory.Build.props
git commit -m "build: add observability audit and testbase quality gates"
```

### Task 7: Documentation And Final Verification

**Files:**
- Create: `docs/shared-packages/dotnet/Tw.Observability/README.md`
- Create: `docs/shared-packages/dotnet/Tw.Observability.Serilog/README.md`
- Create: `docs/shared-packages/dotnet/Tw.Observability.OpenTelemetry/README.md`
- Create: `docs/shared-packages/dotnet/Tw.Auditing.Contracts/README.md`
- Create: `docs/shared-packages/dotnet/Tw.Auditing/README.md`
- Create: `docs/shared-packages/dotnet/Tw.TestBase/README.md`
- Create: `docs/shared-packages/dotnet/Tw.AspNetCore.TestBase/README.md`
- Create: `docs/shared-packages/dotnet/Tw.Data.SqlSugar.TestBase/README.md`
- Create: `docs/shared-packages/dotnet/Tw.EventBus.Cap.TestBase/README.md`
- Modify: `docs/shared-packages/dotnet/README.md`

- [ ] **Step 1: Add package docs**

Each README includes package responsibility, public entry points, dependency boundary, and one minimal usage example. TestBase README files state that production projects must not reference the package.

- [ ] **Step 2: Update shared package index**

Add Observability, Auditing, and TestBase package entries to `docs/shared-packages/dotnet/README.md`.

- [ ] **Step 3: Run final verification**

Run:

```powershell
dotnet test backend/dotnet/Tw.SmartPlatform.slnx --filter "FullyQualifiedName~Observability|FullyQualifiedName~Auditing|FullyQualifiedName~TestBase" --nologo
pwsh backend/dotnet/Build/QualityGates/TestBaseReferenceGuard.ps1
pwsh backend/dotnet/Build/QualityGates/SensitiveOutputGuard.ps1
rg -n "OpenTelemetry\.Instrumentation\.GrpcNetClient" backend/dotnet/BuildingBlocks/src/Observability
```

Expected: tests and scripts PASS; `rg` has no matches.

- [ ] **Step 4: Commit**

```bash
git add docs/shared-packages backend/dotnet/BuildingBlocks/src backend/dotnet/BuildingBlocks/tests backend/dotnet/Build
git commit -m "docs: document observability auditing and testbase packages"
```

## Plan Self-Review

- Spec coverage: log redaction, trace and metric fields, OTLP and Aspire Dashboard, auditing categories, sensitive operation auditing, and test-only package boundaries are covered.
- Sensitive data: logs, audit events, error outputs, and contract fixtures receive explicit redaction tests or build guards.
- Test-only governance: every `*TestBase` package has a production-reference scan.
- Placeholder scan: no placeholder tokens are present.
- Verification: targeted tests, boundary scans, and quality gate scripts are included.
