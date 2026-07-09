using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tw.EventBus.Abstractions;
using Tw.EventBus.Cap.Consumers;
using Tw.EventBus.Cap.Inbox;
using Tw.EventBus.Cap.Outbox;
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
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureRabbitMq);
        ArgumentNullException.ThrowIfNull(configureStorage);

        var rabbitMqOptions = new CapRabbitMqOptions();
        configureRabbitMq(rabbitMqOptions);
        rabbitMqOptions.Validate();

        var storageOptions = new SqlSugarCapStorageOptions();
        configureStorage(storageOptions);
        storageOptions.Validate();

        services.TryAddSingleton(rabbitMqOptions);
        services.TryAddSingleton(SqlSugarCapStorageSchema.FromOptions(storageOptions));
        services.TryAddSingleton<SqlSugarCapStorage>();
        services.TryAddScoped<IInboxMessageStore, SqlSugarInboxMessageStore>();
        services.TryAddScoped<IOutboxWriter, CapOutboxWriter>();
        services.TryAddScoped<IEventTransport, CapEventTransport>();
        services.TryAddScoped<CapConsumerExecutionFilter>();

        return services;
    }
}
