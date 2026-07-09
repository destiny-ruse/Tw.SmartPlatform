using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tw.EventBus.Abstractions;
using Tw.EventBus.Cap.Consumers;
using Tw.EventBus.Cap.Inbox;
using Tw.EventBus.Cap.Outbox;
using Tw.EventBus.Cap.RabbitMq;
using Tw.EventBus.Cap.Storage;

namespace Tw.EventBus.Cap;

/// <summary>表示 CapEventBusServiceCollectionExtensions 类型</summary>
public static class CapEventBusServiceCollectionExtensions
{
    /// <summary>执行 AddCapEventBus 操作</summary>
    /// <param name="services">services 参数</param>
    /// <param name="configureRabbitMq">configureRabbitMq 参数</param>
    /// <param name="configureStorage">configureStorage 参数</param>
    /// <returns>AddCapEventBus 的执行结果</returns>
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
