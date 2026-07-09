# Dotnet Framework P4 Data UOW CAP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement data access contracts, SqlSugar UoW adapter, multi-tenancy, sharding, optimistic concurrency, and CAP event bus integration with Outbox writes fully controlled by the current `Tw.Uow` transaction.

**Architecture:** `Tw.Data` contains data contracts, audit/soft-delete/concurrency interfaces, and repository abstractions. `Tw.Data.SqlSugar` owns SqlSugar connection resolution and UoW binding. Multi-tenancy and sharding packages provide context and resolvers. CAP packages write Outbox records only through the active `Tw.Uow` transaction and reject publishing when the active unit of work cannot cover both business writes and CAP Outbox writes.

**Tech Stack:** .NET 10, SqlSugarCore, DotNetCore.CAP, RabbitMQ adapter, xUnit, AwesomeAssertions, Testcontainers, Respawn

---

## File Structure

- Create: `backend/dotnet/BuildingBlocks/src/Data/Tw.Data`
- Create: `backend/dotnet/BuildingBlocks/src/Data/Tw.Data.SqlSugar`
- Create: `backend/dotnet/BuildingBlocks/src/MultiTenancy/Tw.MultiTenancy.Abstractions`
- Create: `backend/dotnet/BuildingBlocks/src/MultiTenancy/Tw.MultiTenancy`
- Create: `backend/dotnet/BuildingBlocks/src/Sharding/Tw.Sharding.Abstractions`
- Create: `backend/dotnet/BuildingBlocks/src/Sharding/Tw.Sharding`
- Create: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Abstractions`
- Create: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus`
- Create: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap`
- Create matching unit and integration tests
- Modify: `backend/dotnet/Tw.SmartPlatform.slnx`

### Task 1: Implement Multi-Tenant And Shard Context Contracts

**Files:**
- Create: `Tw.MultiTenancy.Abstractions/CurrentTenant.cs`
- Create: `Tw.MultiTenancy.Abstractions/ICurrentTenant.cs`
- Create: `Tw.MultiTenancy/TenantResolver.cs`
- Create: `Tw.Sharding.Abstractions/IShardContext.cs`
- Create: `Tw.Sharding.Abstractions/ShardDescriptor.cs`
- Create: `Tw.Sharding/ShardContext.cs`
- Create: `Tw.MultiTenancy.Tests/TenantResolverTests.cs`
- Create: `Tw.Sharding.Tests/ShardContextTests.cs`

- [ ] **Step 1: Write tenant conflict test**

```csharp
using AwesomeAssertions;
using Tw.MultiTenancy;

namespace Tw.MultiTenancy.Tests;

public sealed class TenantResolverTests
{
    [Fact]
    public void Resolve_RejectsHeaderTenantWhenTokenTenantDiffers()
    {
        var resolver = new TenantResolver();

        var act = () => resolver.Resolve(tokenTenantId: "tenant-a", hintedTenantId: "tenant-b");

        act.Should().Throw<TenantMismatchException>()
            .WithMessage("租户标识与认证票据不一致");
    }
}
```

- [ ] **Step 2: Implement tenant model**

```csharp
namespace Tw.MultiTenancy.Abstractions;

public sealed record CurrentTenant(string Id)
{
    public static CurrentTenant Default { get; } = new("default");
}

public interface ICurrentTenant
{
    CurrentTenant Value { get; }
}
```

- [ ] **Step 3: Implement resolver**

```csharp
using Tw.MultiTenancy.Abstractions;

namespace Tw.MultiTenancy;

public sealed class TenantMismatchException : Exception
{
    public TenantMismatchException() : base("租户标识与认证票据不一致")
    {
    }
}

public sealed class TenantResolver
{
    public CurrentTenant Resolve(string? tokenTenantId, string? hintedTenantId)
    {
        if (!string.IsNullOrWhiteSpace(tokenTenantId) && !string.IsNullOrWhiteSpace(hintedTenantId) && tokenTenantId != hintedTenantId)
        {
            throw new TenantMismatchException();
        }

        return new CurrentTenant(tokenTenantId ?? hintedTenantId ?? CurrentTenant.Default.Id);
    }
}
```

- [ ] **Step 4: Implement shard context**

```csharp
namespace Tw.Sharding.Abstractions;

public sealed record ShardDescriptor(string Strategy, string Key)
{
    public static ShardDescriptor None { get; } = new("none", "default");
}

public interface IShardContext
{
    ShardDescriptor Current { get; }
}
```

- [ ] **Step 5: Run tests**

Run: `dotnet test backend/dotnet/Tw.SmartPlatform.slnx --filter "FullyQualifiedName~MultiTenancy|FullyQualifiedName~Sharding"`

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/MultiTenancy backend/dotnet/BuildingBlocks/src/Sharding backend/dotnet/BuildingBlocks/tests
git commit -m "feat: add tenant and shard context contracts"
```

### Task 2: Implement Data Contracts And Concurrency Models

**Files:**
- Create: `Tw.Data/Auditing/IAuditedEntity.cs`
- Create: `Tw.Data/SoftDelete/ISoftDelete.cs`
- Create: `Tw.Data/Concurrency/IHasConcurrencyStamp.cs`
- Create: `Tw.Data/Concurrency/IHasVersionStamp.cs`
- Create: `Tw.Data/Concurrency/ConcurrencyConflictException.cs`
- Create: `Tw.Data/Concurrency/IConcurrencyCheckContext.cs`
- Create: `Tw.Data/Repositories/IRepository.cs`
- Create: `Tw.Data.Tests/Concurrency/ConcurrencyConflictExceptionTests.cs`

- [ ] **Step 1: Write concurrency exception test**

```csharp
using AwesomeAssertions;
using Tw.Data.Concurrency;

namespace Tw.Data.Tests.Concurrency;

public sealed class ConcurrencyConflictExceptionTests
{
    [Fact]
    public void Constructor_UsesStableErrorCode()
    {
        var exception = new ConcurrencyConflictException("Order", "order-1");

        exception.Code.Should().Be("DATA:CONFLICT");
        exception.Message.Should().Be("数据已被其他请求修改");
    }
}
```

- [ ] **Step 2: Implement concurrency contracts**

```csharp
namespace Tw.Data.Concurrency;

public interface IHasConcurrencyStamp
{
    string ConcurrencyStamp { get; set; }
}

public interface IHasVersionStamp
{
    long VersionStamp { get; set; }
}

public sealed class ConcurrencyConflictException(string resourceType, string resourceId) : Exception("数据已被其他请求修改")
{
    public string Code { get; } = "DATA:CONFLICT";
    public string ResourceType { get; } = resourceType;
    public string ResourceId { get; } = resourceId;
}
```

- [ ] **Step 3: Implement audit and repository contracts**

```csharp
namespace Tw.Data.Auditing;

public interface IAuditedEntity
{
    DateTimeOffset CreatedAt { get; set; }
    DateTimeOffset UpdatedAt { get; set; }
    string? CreatedBy { get; set; }
    string? UpdatedBy { get; set; }
}
```

```csharp
namespace Tw.Data.Repositories;

public interface IRepository<TEntity, TKey>
{
    Task<TEntity?> FindAsync(TKey id, CancellationToken cancellationToken);
    Task InsertAsync(TEntity entity, CancellationToken cancellationToken);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken);
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken);
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Data.Tests/Tw.Data.Tests.csproj`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Data/Tw.Data backend/dotnet/BuildingBlocks/tests/Tw.Data.Tests
git commit -m "feat: add data and concurrency contracts"
```

### Task 3: Implement SqlSugar UoW Transaction Adapter

**Files:**
- Create: `Tw.Data.SqlSugar/Connection/IConnectionConfigResolver.cs`
- Create: `Tw.Data.SqlSugar/Connection/ISqlSugarClientFactory.cs`
- Create: `Tw.Data.SqlSugar/Uow/SqlSugarUnitOfWork.cs`
- Create: `Tw.Data.SqlSugar/Uow/SqlSugarUnitOfWorkManager.cs`
- Create: `Tw.Data.SqlSugar.Tests/Uow/SqlSugarUnitOfWorkManagerTests.cs`

- [ ] **Step 1: Write UoW commit/rollback test**

```csharp
using AwesomeAssertions;
using Tw.Data.SqlSugar.Connection;
using Tw.Data.SqlSugar.Uow;
using Tw.Uow;

namespace Tw.Data.SqlSugar.Tests.Uow;

public sealed class SqlSugarUnitOfWorkManagerTests
{
    [Fact]
    public async Task BeginAsync_SetsCurrentUnitOfWork()
    {
        var manager = new SqlSugarUnitOfWorkManager(new FakeSqlSugarClientFactory());

        await using var uow = await manager.BeginAsync(UnitOfWorkOptions.Default);

        manager.Current.Should().BeSameAs(uow);
    }

    private sealed class FakeSqlSugarClientFactory : ISqlSugarClientFactory
    {
        public object CreateClient(CancellationToken cancellationToken)
        {
            return new object();
        }
    }
}
```

- [ ] **Step 2: Implement connection resolver contract**

```csharp
namespace Tw.Data.SqlSugar.Connection;

public interface IConnectionConfigResolver
{
    Task<object> ResolveAsync(CancellationToken cancellationToken);
}

public interface ISqlSugarClientFactory
{
    object CreateClient(CancellationToken cancellationToken);
}
```

- [ ] **Step 3: Implement UoW manager skeleton**

```csharp
using Tw.Data.SqlSugar.Connection;
using Tw.Uow;

namespace Tw.Data.SqlSugar.Uow;

public sealed class SqlSugarUnitOfWorkManager(ISqlSugarClientFactory clientFactory) : IUnitOfWorkManager
{
    private readonly AsyncLocal<IUnitOfWork?> _current = new();

    public IUnitOfWork? Current => _current.Value;

    public Task<IUnitOfWork> BeginAsync(UnitOfWorkOptions options, CancellationToken cancellationToken = default)
    {
        var unitOfWork = new SqlSugarUnitOfWork(clientFactory, () => _current.Value = null, cancellationToken);
        _current.Value = unitOfWork;
        return Task.FromResult<IUnitOfWork>(unitOfWork);
    }
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Data.SqlSugar.Tests/Tw.Data.SqlSugar.Tests.csproj --filter SqlSugarUnitOfWorkManagerTests`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Data/Tw.Data.SqlSugar backend/dotnet/BuildingBlocks/tests/Tw.Data.SqlSugar.Tests
git commit -m "feat: add sqlsugar unit of work adapter"
```

### Task 4: Implement CAP Event Contracts

**Files:**
- Create: `Tw.EventBus.Abstractions/IIntegrationEvent.cs`
- Create: `Tw.EventBus.Abstractions/IEventPublisher.cs`
- Create: `Tw.EventBus.Abstractions/IEventHandler.cs`
- Create: `Tw.EventBus/EventPublisher.cs`
- Create: `Tw.EventBus.Tests/EventPublisherTests.cs`

- [ ] **Step 1: Write publisher test**

```csharp
using AwesomeAssertions;
using Tw.EventBus;
using Tw.EventBus.Abstractions;

namespace Tw.EventBus.Tests;

public sealed class EventPublisherTests
{
    [Fact]
    public async Task PublishAsync_DelegatesToTransport()
    {
        var transport = new RecordingEventTransport();
        var publisher = new EventPublisher(transport);
        var @event = new SampleEvent("event-1");

        await publisher.PublishAsync(@event, CancellationToken.None);

        transport.Published.Should().ContainSingle().Which.Should().Be(@event);
    }

    private sealed record SampleEvent(string EventId) : IIntegrationEvent;

    private sealed class RecordingEventTransport : IEventTransport
    {
        public List<IIntegrationEvent> Published { get; } = [];

        public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            Published.Add(integrationEvent);
            return Task.CompletedTask;
        }
    }
}
```

- [ ] **Step 2: Implement contracts**

```csharp
namespace Tw.EventBus.Abstractions;

public interface IIntegrationEvent
{
    string EventId { get; }
}

public interface IEventPublisher
{
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}

public interface IEventTransport
{
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}
```

- [ ] **Step 3: Implement publisher**

```csharp
using Tw.EventBus.Abstractions;

namespace Tw.EventBus;

public sealed class EventPublisher(IEventTransport transport) : IEventPublisher
{
    public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return transport.PublishAsync(integrationEvent, cancellationToken);
    }
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.EventBus.Tests/Tw.EventBus.Tests.csproj`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus* backend/dotnet/BuildingBlocks/tests/Tw.EventBus.Tests
git commit -m "feat: add event bus contracts"
```

### Task 5: Implement CAP Outbox Binding To Current UoW

**Files:**
- Create: `Tw.EventBus.Cap/CapEventTransport.cs`
- Create: `Tw.EventBus.Cap/Outbox/IOutboxWriter.cs`
- Create: `Tw.EventBus.Cap.Tests/CapEventTransportTests.cs`

- [ ] **Step 1: Write UoW-required test**

```csharp
using AwesomeAssertions;
using Tw.EventBus.Abstractions;
using Tw.EventBus.Cap;
using Tw.EventBus.Cap.Outbox;
using Tw.Uow;

namespace Tw.EventBus.Cap.Tests;

public sealed class CapEventTransportTests
{
    [Fact]
    public async Task PublishAsync_Throws_WhenCurrentUnitOfWorkIsMissing()
    {
        var transport = new CapEventTransport(new NullUnitOfWorkManager(), new RecordingOutboxWriter());

        var act = () => transport.PublishAsync(new SampleEvent("event-1"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CAP Outbox 写入必须处于当前工作单元事务内");
    }

    [Fact]
    public async Task PublishAsync_WritesOutboxThroughCurrentUnitOfWork()
    {
        var unitOfWork = new RecordingUnitOfWork();
        var outboxWriter = new RecordingOutboxWriter();
        var transport = new CapEventTransport(new ActiveUnitOfWorkManager(unitOfWork), outboxWriter);
        var integrationEvent = new SampleEvent("event-2");

        await transport.PublishAsync(integrationEvent, CancellationToken.None);

        outboxWriter.Writes.Should().ContainSingle()
            .Which.Should().Be(new OutboxWrite(unitOfWork, integrationEvent));
    }

    private sealed record SampleEvent(string EventId) : IIntegrationEvent;

    private sealed class NullUnitOfWorkManager : IUnitOfWorkManager
    {
        public IUnitOfWork? Current => null;

        public Task<IUnitOfWork> BeginAsync(UnitOfWorkOptions options, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("The missing-UoW test must not start a new CAP transaction.");
        }
    }

    private sealed class ActiveUnitOfWorkManager(IUnitOfWork current) : IUnitOfWorkManager
    {
        public IUnitOfWork? Current => current;

        public Task<IUnitOfWork> BeginAsync(UnitOfWorkOptions options, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(current);
        }
    }

    private sealed class RecordingUnitOfWork : IUnitOfWork
    {
        public CancellationToken CancellationToken => CancellationToken.None;

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingOutboxWriter : IOutboxWriter
    {
        public List<OutboxWrite> Writes { get; } = [];

        public Task WriteAsync(IUnitOfWork unitOfWork, IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            Writes.Add(new OutboxWrite(unitOfWork, integrationEvent));
            return Task.CompletedTask;
        }
    }

    private sealed record OutboxWrite(IUnitOfWork UnitOfWork, IIntegrationEvent IntegrationEvent);
}
```

- [ ] **Step 2: Implement Outbox writer contract**

```csharp
using Tw.EventBus.Abstractions;
using Tw.Uow;

namespace Tw.EventBus.Cap.Outbox;

public interface IOutboxWriter
{
    Task WriteAsync(IUnitOfWork unitOfWork, IIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}
```

- [ ] **Step 3: Implement transport guard**

```csharp
using Tw.EventBus.Abstractions;
using Tw.EventBus.Cap.Outbox;
using Tw.Uow;

namespace Tw.EventBus.Cap;

public sealed class CapEventTransport(IUnitOfWorkManager unitOfWorkManager, IOutboxWriter outboxWriter) : IEventTransport
{
    public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var current = unitOfWorkManager.Current;
        if (current is null)
        {
            throw new InvalidOperationException("CAP Outbox 写入必须处于当前工作单元事务内");
        }

        return outboxWriter.WriteAsync(current, integrationEvent, cancellationToken);
    }
}
```

- [ ] **Step 4: Confirm current-UoW transaction binding**

The `PublishAsync_WritesOutboxThroughCurrentUnitOfWork` test from Step 1 is the acceptance test for this rule. `CapEventTransport` must write through `unitOfWorkManager.Current` and must not open a separate CAP-owned transaction around the Outbox write.

- [ ] **Step 5: Run CAP tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.EventBus.Cap.Tests/Tw.EventBus.Cap.Tests.csproj`

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap backend/dotnet/BuildingBlocks/tests/Tw.EventBus.Cap.Tests
git commit -m "feat: bind cap outbox writes to current unit of work"
```

### Task 6: Implement CAP RabbitMQ, SqlSugar Storage, Inbox, And Consumer Filter

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap/CapEventBusServiceCollectionExtensions.cs`
- Create: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap/RabbitMq/CapRabbitMqOptions.cs`
- Create: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap/Storage/SqlSugarCapStorageOptions.cs`
- Create: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap/Storage/SqlSugarCapStorageSchema.cs`
- Create: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap/Storage/SqlSugarCapStorageInitializer.cs`
- Create: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap/Storage/SqlSugarCapStorage.cs`
- Create: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap/Inbox/InboxMessage.cs`
- Create: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap/Inbox/IInboxMessageStore.cs`
- Create: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap/Inbox/SqlSugarInboxMessageStore.cs`
- Create: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap/Consumers/CapConsumerContext.cs`
- Create: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap/Consumers/CapConsumerResult.cs`
- Create: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap/Consumers/CapConsumerExecutionFilter.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.EventBus.Cap.Tests/CapEventBusServiceCollectionExtensionsTests.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.EventBus.Cap.Tests/Storage/SqlSugarCapStorageSchemaTests.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.EventBus.Cap.Tests/Consumers/CapConsumerExecutionFilterTests.cs`

- [ ] **Step 1: Write RabbitMQ and SqlSugar registration tests**

```csharp
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.EventBus.Cap;
using Tw.EventBus.Cap.Inbox;
using Tw.EventBus.Cap.RabbitMq;
using Tw.EventBus.Cap.Storage;

namespace Tw.EventBus.Cap.Tests;

public sealed class CapEventBusServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCapEventBus_RejectsMissingRabbitMqHost()
    {
        var services = new ServiceCollection();

        var act = () => services.AddCapEventBus(
            rabbitMq => rabbitMq.UserName = "cap",
            storage => storage.ConnectionName = "Default");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("CAP RabbitMQ host is required");
    }

    [Fact]
    public void AddCapEventBus_RegistersStorageInboxAndConsumerFilter()
    {
        var services = new ServiceCollection();

        services.AddCapEventBus(
            rabbitMq =>
            {
                rabbitMq.HostName = "rabbitmq";
                rabbitMq.UserName = "cap";
                rabbitMq.Password = "secret-from-test-double";
            },
            storage => storage.ConnectionName = "Default");

        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IInboxMessageStore));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(CapConsumerExecutionFilter));
        services.Should().Contain(descriptor => descriptor.ImplementationType == typeof(SqlSugarCapStorage));
    }
}
```

- [ ] **Step 2: Implement RabbitMQ and SqlSugar storage options**

```csharp
namespace Tw.EventBus.Cap.RabbitMq;

public sealed class CapRabbitMqOptions
{
    public string? HostName { get; set; }
    public string VirtualHost { get; set; } = "/";
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string ExchangeName { get; set; } = "tw.smart-platform";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(HostName))
        {
            throw new InvalidOperationException("CAP RabbitMQ host is required");
        }

        if (string.IsNullOrWhiteSpace(UserName))
        {
            throw new InvalidOperationException("CAP RabbitMQ user is required");
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            throw new InvalidOperationException("CAP RabbitMQ password is required");
        }
    }
}
```

```csharp
namespace Tw.EventBus.Cap.Storage;

public sealed class SqlSugarCapStorageOptions
{
    public string? ConnectionName { get; set; }
    public string Schema { get; set; } = "cap";
    public string PublishedTable { get; set; } = "published";
    public string ReceivedTable { get; set; } = "received";
    public string LockTable { get; set; } = "locks";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionName))
        {
            throw new InvalidOperationException("CAP SqlSugar connection name is required");
        }
    }
}
```

- [ ] **Step 3: Write CAP storage schema tests**

```csharp
using AwesomeAssertions;
using Tw.EventBus.Cap.Storage;

namespace Tw.EventBus.Cap.Tests.Storage;

public sealed class SqlSugarCapStorageSchemaTests
{
    [Fact]
    public void DefaultSchema_UsesDedicatedCapTables()
    {
        var schema = SqlSugarCapStorageSchema.FromOptions(new SqlSugarCapStorageOptions
        {
            ConnectionName = "Default"
        });

        schema.RequiredTables.Should().Equal("cap.published", "cap.received", "cap.locks");
        schema.IsTenantSharded.Should().BeFalse();
    }
}
```

- [ ] **Step 4: Implement custom SqlSugar CAP storage**

```csharp
namespace Tw.EventBus.Cap.Storage;

public sealed record SqlSugarCapStorageSchema(IReadOnlyList<string> RequiredTables, bool IsTenantSharded)
{
    public static SqlSugarCapStorageSchema FromOptions(SqlSugarCapStorageOptions options)
    {
        options.Validate();

        return new SqlSugarCapStorageSchema(
            [
                $"{options.Schema}.{options.PublishedTable}",
                $"{options.Schema}.{options.ReceivedTable}",
                $"{options.Schema}.{options.LockTable}"
            ],
            IsTenantSharded: false);
    }
}
```

`SqlSugarCapStorage` implements the CAP storage interfaces with SqlSugar clients resolved from `Tw.Data.SqlSugar`. CAP storage tables are infrastructure tables and are not tenant-sharded. Business tenant context stays in message headers and Inbox records.

- [ ] **Step 5: Write Inbox and consumer filter tests**

```csharp
using AwesomeAssertions;
using Tw.EventBus.Cap.Consumers;
using Tw.EventBus.Cap.Inbox;

namespace Tw.EventBus.Cap.Tests.Consumers;

public sealed class CapConsumerExecutionFilterTests
{
    [Fact]
    public async Task ExecuteAsync_RejectsMissingTenantShardOrCultureHeaders()
    {
        var filter = new CapConsumerExecutionFilter(new InMemoryInboxMessageStore());
        var context = new CapConsumerContext("message-1", TenantId: "", ShardId: "default", Culture: "zh-CN");

        var act = () => filter.ExecuteAsync(context, _ => Task.CompletedTask, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CAP 消费消息缺少租户、分片或区域性上下文");
    }

    [Fact]
    public async Task ExecuteAsync_DispatchesCommandOnceForDuplicateMessage()
    {
        var inbox = new InMemoryInboxMessageStore();
        var dispatchCount = 0;
        var filter = new CapConsumerExecutionFilter(inbox);
        var context = new CapConsumerContext("message-1", "tenant-a", "orders-2026", "zh-CN");

        await filter.ExecuteAsync(context, _ =>
        {
            dispatchCount++;
            return Task.CompletedTask;
        }, CancellationToken.None);
        var duplicate = await filter.ExecuteAsync(context, _ =>
        {
            dispatchCount++;
            return Task.CompletedTask;
        }, CancellationToken.None);

        duplicate.Status.Should().Be(CapConsumerStatus.Duplicate);
        dispatchCount.Should().Be(1);
    }
}
```

- [ ] **Step 6: Implement Inbox and consumer filter**

```csharp
using Tw.EventBus.Cap.Inbox;

namespace Tw.EventBus.Cap.Consumers;

public sealed record CapConsumerContext(string MessageId, string TenantId, string ShardId, string Culture);

public enum CapConsumerStatus
{
    Succeeded = 1,
    Duplicate = 2
}

public sealed record CapConsumerResult(CapConsumerStatus Status);
```

```csharp
namespace Tw.EventBus.Cap.Inbox;

public sealed record InboxMessage(string MessageId, string TenantId, string ShardId, string Culture, DateTimeOffset ReceivedAt);

public interface IInboxMessageStore
{
    Task<bool> TryBeginAsync(InboxMessage message, CancellationToken cancellationToken);
    Task CompleteAsync(string messageId, CancellationToken cancellationToken);
    Task FailAsync(string messageId, Exception exception, CancellationToken cancellationToken);
}
```

```csharp
using Tw.EventBus.Cap.Inbox;

namespace Tw.EventBus.Cap.Consumers;

public sealed class CapConsumerExecutionFilter(IInboxMessageStore inboxStore)
{
    public async Task<CapConsumerResult> ExecuteAsync(
        CapConsumerContext context,
        Func<CancellationToken, Task> dispatch,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(context.TenantId)
            || string.IsNullOrWhiteSpace(context.ShardId)
            || string.IsNullOrWhiteSpace(context.Culture))
        {
            throw new InvalidOperationException("CAP 消费消息缺少租户、分片或区域性上下文");
        }

        var inboxMessage = new InboxMessage(
            context.MessageId,
            context.TenantId,
            context.ShardId,
            context.Culture,
            DateTimeOffset.UtcNow);

        if (!await inboxStore.TryBeginAsync(inboxMessage, cancellationToken))
        {
            return new CapConsumerResult(CapConsumerStatus.Duplicate);
        }

        try
        {
            await dispatch(cancellationToken);
            await inboxStore.CompleteAsync(context.MessageId, cancellationToken);
            return new CapConsumerResult(CapConsumerStatus.Succeeded);
        }
        catch (Exception exception)
        {
            await inboxStore.FailAsync(context.MessageId, exception, cancellationToken);
            throw;
        }
    }
}
```

- [ ] **Step 7: Implement service registration**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tw.EventBus.Cap.Consumers;
using Tw.EventBus.Cap.Inbox;
using Tw.EventBus.Cap.RabbitMq;
using Tw.EventBus.Cap.Storage;

namespace Tw.EventBus.Cap;

public static class CapEventBusServiceCollectionExtensions
{
    public static IServiceCollection AddCapEventBus(
        this IServiceCollection services,
        Action<CapRabbitMqOptions> configureRabbitMq,
        Action<SqlSugarCapStorageOptions> configureStorage)
    {
        var rabbitMqOptions = new CapRabbitMqOptions();
        configureRabbitMq(rabbitMqOptions);
        rabbitMqOptions.Validate();

        var storageOptions = new SqlSugarCapStorageOptions();
        configureStorage(storageOptions);
        storageOptions.Validate();

        services.AddCap(cap =>
        {
            cap.UseRabbitMQ(rabbitMq =>
            {
                rabbitMq.HostName = rabbitMqOptions.HostName!;
                rabbitMq.VirtualHost = rabbitMqOptions.VirtualHost;
                rabbitMq.UserName = rabbitMqOptions.UserName!;
                rabbitMq.Password = rabbitMqOptions.Password!;
                rabbitMq.ExchangeName = rabbitMqOptions.ExchangeName;
            });
            cap.UseStorage<SqlSugarCapStorage>();
        });

        services.TryAddScoped<IInboxMessageStore, SqlSugarInboxMessageStore>();
        services.TryAddScoped<CapConsumerExecutionFilter>();
        services.TryAddSingleton(SqlSugarCapStorageSchema.FromOptions(storageOptions));

        return services;
    }
}
```

- [ ] **Step 8: Run CAP integration tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.EventBus.Cap.Tests/Tw.EventBus.Cap.Tests.csproj --filter "CapEventBus|SqlSugarCapStorage|CapConsumer"`

Expected: PASS.

- [ ] **Step 9: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap backend/dotnet/BuildingBlocks/tests/Tw.EventBus.Cap.Tests
git commit -m "feat: add cap rabbitmq storage inbox and consumer filter"
```

### Task 7: Implement CAP Cleanup Job Contract

**Files:**
- Create: `Tw.EventBus.Cap/Cleanup/CapMessageCleanupOptions.cs`
- Create: `Tw.EventBus.Cap/Cleanup/ICapMessageCleanupJob.cs`
- Create: `Tw.EventBus.Cap/Cleanup/CapMessageCleanupJob.cs`
- Create: `Tw.EventBus.Cap.Tests/Cleanup/CapMessageCleanupJobTests.cs`

- [ ] **Step 1: Write cleanup safety test**

```csharp
using AwesomeAssertions;
using Tw.EventBus.Cap.Cleanup;

namespace Tw.EventBus.Cap.Tests.Cleanup;

public sealed class CapMessageCleanupJobTests
{
    [Fact]
    public void Options_Defaults_DoNotDeleteFailedMessages()
    {
        var options = CapMessageCleanupOptions.Default;

        options.DeleteFailedMessages.Should().BeFalse();
        options.BatchSize.Should().Be(500);
    }
}
```

- [ ] **Step 2: Implement options**

```csharp
namespace Tw.EventBus.Cap.Cleanup;

public sealed record CapMessageCleanupOptions(int BatchSize, TimeSpan Retention, bool DeleteFailedMessages)
{
    public static CapMessageCleanupOptions Default { get; } = new(500, TimeSpan.FromDays(7), false);
}
```

- [ ] **Step 3: Run tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.EventBus.Cap.Tests/Tw.EventBus.Cap.Tests.csproj --filter Cleanup`

Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap backend/dotnet/BuildingBlocks/tests/Tw.EventBus.Cap.Tests
git commit -m "feat: add cap cleanup job contract"
```

### Task 8: Add Data, Tenant, Sharding, And CAP Charters

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Data/Tw.Data/package-charter.yaml`
- Create: `backend/dotnet/BuildingBlocks/src/Data/Tw.Data.SqlSugar/package-charter.yaml`
- Create: `backend/dotnet/BuildingBlocks/src/MultiTenancy/Tw.MultiTenancy.Abstractions/package-charter.yaml`
- Create: `backend/dotnet/BuildingBlocks/src/MultiTenancy/Tw.MultiTenancy/package-charter.yaml`
- Create: `backend/dotnet/BuildingBlocks/src/Sharding/Tw.Sharding.Abstractions/package-charter.yaml`
- Create: `backend/dotnet/BuildingBlocks/src/Sharding/Tw.Sharding/package-charter.yaml`
- Create: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Abstractions/package-charter.yaml`
- Create: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus/package-charter.yaml`
- Create: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap/package-charter.yaml`
- Create: `docs/shared-packages/dotnet/Tw.Data/README.md`
- Create: `docs/shared-packages/dotnet/Tw.Data.SqlSugar/README.md`
- Create: `docs/shared-packages/dotnet/Tw.MultiTenancy/README.md`
- Create: `docs/shared-packages/dotnet/Tw.Sharding/README.md`
- Create: `docs/shared-packages/dotnet/Tw.EventBus.Cap/README.md`
- Modify: `docs/shared-packages/dotnet/README.md`

- [ ] **Step 1: Add charter template for contracts packages**

Use this shape for `Tw.Data`, `Tw.MultiTenancy.Abstractions`, `Tw.Sharding.Abstractions`, and `Tw.EventBus.Abstractions`, changing scope per package:

```yaml
schema_version: "1.0.0"
package: Tw.Data
owner: platform-team
stability: experimental
compatibility: "experimental 阶段不承诺兼容"
responsibility: >
  数据源描述、连接解析、仓储基础抽象、审计字段、软删除和并发契约。
in_scope:
  - 数据源描述
  - 仓储基础抽象
  - 审计字段契约
  - 软删除契约
  - 并发契约
out_of_scope:
  - SqlSugar 连接工厂
  - CAP Outbox 存储
  - 业务实体映射
public_capabilities:
  - Tw.Data
dependency_rules:
  forbid:
    - "SqlSugar*"
    - "DotNetCore.CAP*"
    - "Microsoft.AspNetCore.*"
```

- [ ] **Step 2: Add SqlSugar and CAP adapter charters**

`Tw.Data.SqlSugar/package-charter.yaml` must allow `SqlSugarCore`, `Tw.Data`, `Tw.Uow`, and `Tw.Core`, and must forbid `DotNetCore.CAP*`, ASP.NET Core, Quartz, and Gateway packages.

`Tw.EventBus.Cap/package-charter.yaml` must allow `DotNetCore.CAP`, `DotNetCore.CAP.RabbitMQ`, `Tw.EventBus`, `Tw.EventBus.Abstractions`, `Tw.Uow`, and `Tw.Data.SqlSugar`. Its `responsibility` must state that CAP Outbox writes are bound to the active `Tw.Uow` transaction and publishing is rejected when the active unit of work cannot cover business writes and Outbox writes. Its `in_scope` must include RabbitMQ transport binding, custom SqlSugar CAP storage, Inbox persistence, CAP consumer filter, and cleanup.

- [ ] **Step 3: Add shared-package docs**

Each README must include responsibility, public capabilities, dependency boundary, and a minimal usage example. `Tw.EventBus.Cap/README.md` must include this rule:

```markdown
CAP Outbox writes are valid only inside the active `Tw.Uow` transaction. The package does not create a separate Outbox transaction outside the current unit of work. CAP consumption uses Inbox records to deduplicate delivered messages. Host CAP consumers call `ISender.Send(...)` inside the dispatch delegate after tenant, shard, and culture headers are validated.
```

- [ ] **Step 4: Update index**

`docs/shared-packages/dotnet/README.md` must link to `Tw.Data`, `Tw.Data.SqlSugar`, `Tw.MultiTenancy`, `Tw.Sharding`, `Tw.EventBus`, and `Tw.EventBus.Cap`.

- [ ] **Step 5: Run architecture tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj --filter PackageCharterTests`

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Data backend/dotnet/BuildingBlocks/src/MultiTenancy backend/dotnet/BuildingBlocks/src/Sharding backend/dotnet/BuildingBlocks/src/EventBus docs/shared-packages/dotnet
git commit -m "docs: add data tenant sharding and cap package charters"
```

## Plan Self-Review

- Spec coverage: multi-tenancy, sharding, data contracts, SqlSugar UoW, concurrency, CAP contracts, RabbitMQ transport, custom SqlSugar CAP storage, Outbox transaction dependency, Inbox consumer idempotency, consumer context filter, cleanup job, package charters, and shared-package docs are covered.
- Placeholder scan: no placeholder tokens are present.
- Type consistency: CAP transport uses `IUnitOfWorkManager.Current` defined in P1.
