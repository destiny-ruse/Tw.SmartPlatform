using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.EventBus.Cap;
using Tw.EventBus.Cap.Consumers;
using Tw.EventBus.Cap.Inbox;
using Tw.EventBus.Cap.Storage;
using Xunit;

namespace Tw.EventBus.Cap.Tests;

/// <summary>验证 CapEventBusServiceCollectionExtensionsTests 相关行为</summary>
public sealed class CapEventBusServiceCollectionExtensionsTests
{
    /// <summary>验证 AddCapEventBus_RejectsMissingRabbitMqHost 场景</summary>
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

    /// <summary>验证 AddCapEventBus_RegistersStorageInboxAndConsumerFilter 场景</summary>
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
