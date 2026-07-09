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
    /// <remarks>
    /// <para><strong>生命周期设计依据（防止 captive dependency）</strong></para>
    /// <para>
    /// <see cref="KeyedServiceEntry{TService}"/> 条目的注册生命周期刻意等同于所包裹 keyed 服务的生命周期。
    /// 这样可确保条目内的 <see cref="KeyedServiceEntry{TService}.Service"/> 尊重对应 keyed 注册的生命周期、
    /// 在当前 scope 内解析、不缓存为单例。若条目被设为更长的生命周期（如 Singleton）去包裹更短生命周期
    /// （如 Scoped / Transient）的服务，则会造成 captive dependency 问题：Singleton 条目被首次解析后缓存，
    /// 后续 scope 无法获得新的 Scoped 实例，导致生命周期泄漏。因此此处必须保持两者一致。
    /// </para>
    /// <para><strong>工厂内无自依赖风险</strong></para>
    /// <para>
    /// 工厂委托内 <see cref="ServiceProviderKeyedServiceExtensions.GetRequiredKeyedService(IServiceProvider, Type, object?)"/>
    /// 解析的是 keyed 服务类型（即 <c>registration.ServiceType</c>，如 <c>IPaymentProvider</c>），
    /// 与正在注册的 <c>KeyedServiceEntry&lt;TService&gt;</c> 类型完全不同，不存在自依赖或循环依赖。
    /// </para>
    /// <para><strong>反射开销说明</strong></para>
    /// <para>
    /// <see cref="Activator.CreateInstance"/> 在解析期构造条目：Singleton 生命周期下仅触发一次反射，
    /// Transient 生命周期下每次解析都触发一次反射。条目的用途是枚举 key 元数据，通常不在热路径，
    /// 此处反射开销可接受。
    /// </para>
    /// </remarks>
    private static void AddKeyedEntry(IServiceCollection services, ServiceCandidate registration)
    {
        // keyed 开放泛型契约无法构造具体 KeyedServiceEntry<TService> 枚举条目，跳过登记，避免 MakeGenericType 失败。
        // keyed 服务本身已由 AddKeyed 完成注册，此处仅负责可枚举条目，对开放泛型无意义。
        if (registration.ServiceType.IsGenericTypeDefinition)
        {
            return;
        }

        var entryType = typeof(KeyedServiceEntry<>).MakeGenericType(registration.ServiceType);
        services.Add(ServiceDescriptor.Describe(
            entryType,
            provider =>
            {
                // 工厂捕获当前迭代 registration 的快照（foreach 迭代变量每轮独立，安全）
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
