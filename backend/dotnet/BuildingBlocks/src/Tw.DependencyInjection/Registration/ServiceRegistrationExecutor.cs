using Microsoft.Extensions.DependencyInjection;
using Tw.DependencyInjection.Abstractions;

namespace Tw.DependencyInjection.Registration;

/// <summary>
/// 将 <see cref="ServiceRegistrationPlan"/> 中的候选项执行写入 <see cref="IServiceCollection"/>。
/// 非 keyed 候选写入前会移除同契约的既有非 keyed 描述符（单实现语义）；
/// keyed 候选除注册带 key 的实现外，还会额外登记可枚举的 <see cref="KeyedServiceEntry{TService}"/> 条目。
/// </summary>
internal static class ServiceRegistrationExecutor
{
    /// <summary>
    /// 将规划结果应用到服务集合
    /// </summary>
    /// <param name="services">目标服务集合</param>
    /// <param name="plan">由 <see cref="ServiceRegistrationPlanner"/> 产出的注册规划</param>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> 或 <paramref name="plan"/> 为 null 时抛出</exception>
    public static void Apply(IServiceCollection services, ServiceRegistrationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(plan);

        foreach (var registration in plan.Registrations)
        {
            if (registration.Key is null)
            {
                // 非 keyed 单实现语义：写入前移除既有同契约非 keyed 描述符，保证容器只保留最终选中的实现
                RemoveExistingNonKeyedDescriptors(services, registration.ServiceType);
                AddNonKeyed(services, registration);
            }
            else
            {
                AddKeyed(services, registration);
                // 为 keyed 契约登记可枚举的 KeyedServiceEntry<TService> 条目，供调用方通过 GetServices<KeyedServiceEntry<T>> 枚举所有 key
                AddKeyedEntry(services, registration);
            }
        }
    }

    /// <summary>注册非 keyed 服务描述符</summary>
    private static void AddNonKeyed(IServiceCollection services, ServiceCandidate registration)
    {
        services.Add(ServiceDescriptor.Describe(
            registration.ServiceType,
            registration.ImplementationType,
            DependencyLifetimeMapper.Map(registration.Lifetime)));
    }

    /// <summary>注册带 key 的 keyed 服务描述符</summary>
    private static void AddKeyed(IServiceCollection services, ServiceCandidate registration)
    {
        var lifetime = DependencyLifetimeMapper.Map(registration.Lifetime);
        services.Add(ServiceDescriptor.DescribeKeyed(
            registration.ServiceType,
            registration.Key,
            registration.ImplementationType,
            lifetime));
    }

    /// <summary>
    /// 为 keyed 契约登记可枚举的 <see cref="KeyedServiceEntry{TService}"/> 条目。
    /// 工厂通过 <see cref="ServiceProviderKeyedServiceExtensions.GetRequiredKeyedService(IServiceProvider, Type, object?)"/>
    /// 取得实际实例，再构造泛型条目；生命周期与对应 keyed 实现保持一致。
    /// </summary>
    private static void AddKeyedEntry(IServiceCollection services, ServiceCandidate registration)
    {
        var entryType = typeof(KeyedServiceEntry<>).MakeGenericType(registration.ServiceType);
        services.Add(ServiceDescriptor.Describe(
            entryType,
            provider =>
            {
                var service = provider.GetRequiredKeyedService(registration.ServiceType, registration.Key);
                return Activator.CreateInstance(entryType, registration.Key!, service)!;
            },
            DependencyLifetimeMapper.Map(registration.Lifetime)));
    }

    /// <summary>
    /// 移除 <paramref name="services"/> 中所有与 <paramref name="serviceType"/> 契约匹配的非 keyed 描述符。
    /// 非 keyed 单实现语义要求写入前清空旧注册，避免多实现并存导致容器返回错误实例。
    /// </summary>
    private static void RemoveExistingNonKeyedDescriptors(IServiceCollection services, Type serviceType)
    {
        for (var index = services.Count - 1; index >= 0; index--)
        {
            if (services[index].ServiceType == serviceType && services[index].ServiceKey is null)
            {
                services.RemoveAt(index);
            }
        }
    }
}
