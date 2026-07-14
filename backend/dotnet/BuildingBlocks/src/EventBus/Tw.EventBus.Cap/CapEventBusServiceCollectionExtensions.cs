using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tw.EventBus;
using Tw.EventBus.Cap.Consumers;
using Tw.EventBus.Cap.Inbox;
using Tw.EventBus.Cap.Outbox;
using Tw.EventBus.Cap.RabbitMq;
using Tw.EventBus.Cap.Storage;

namespace Tw.EventBus.Cap;

/// <summary>
/// 封装Cap事件Bus服务CollectionExtensions相关的数据和行为
/// </summary>
public static class CapEventBusServiceCollectionExtensions
{
    /// <summary>
    /// 注册Cap事件Bus所需服务
    /// </summary>
    /// <param name="services">需要注册组件依赖的服务集合</param>
    /// <param name="configureRabbitMq">用于提供configureRabbitMq</param>
    /// <param name="configureStorage">用于提供configureStorage</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
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
