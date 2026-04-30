using Autofac;
using Autofac.Builder;
using System.Reflection;
using Tw.Core;
using Tw.DependencyInjection.Interception;
using Tw.DependencyInjection.Interception.Castle;

namespace Tw.DependencyInjection.Registration;

/// <summary>
/// 将自动注册计划应用到 Autofac 容器的模块
/// </summary>
public sealed class AutoRegistrationModule : Autofac.Module
{
    private readonly IReadOnlyList<InterceptorRegistration> _globalInterceptors;
    private readonly bool _enableClassInterceptors;
    private readonly IReadOnlyList<IInterceptorMatcher> _matchers;
    private readonly ServiceRegistrationPlan _plan;
    private readonly List<string> _warnings = [];
    private ServiceChainCache? _serviceChainCache;

    /// <summary>
    /// 初始化自动注册 Autofac 模块
    /// </summary>
    /// <param name="plan">最终服务注册计划</param>
    /// <param name="globalInterceptors">全局拦截器注册集合</param>
    /// <param name="matchers">自动匹配规则集合</param>
    public AutoRegistrationModule(
        ServiceRegistrationPlan plan,
        IEnumerable<InterceptorRegistration>? globalInterceptors = null,
        IEnumerable<IInterceptorMatcher>? matchers = null,
        bool enableClassInterceptors = false)
    {
        _plan = Check.NotNull(plan);
        _globalInterceptors = [.. globalInterceptors ?? []];
        _matchers = [.. matchers ?? []];
        _enableClassInterceptors = enableClassInterceptors;
    }

    /// <summary>
    /// 模块加载期间产生的非致命警告
    /// </summary>
    public IReadOnlyList<string> Warnings => _warnings;

    /// <inheritdoc />
    protected override void Load(ContainerBuilder builder)
    {
        _serviceChainCache = new ServiceChainCache(_plan.Registrations, _globalInterceptors, _matchers);
        builder.RegisterInstance(_serviceChainCache).SingleInstance();
        builder.RegisterType<CastleAsyncInterceptorAdapter>().AsSelf().InstancePerDependency();

        foreach (var registration in _plan.Registrations)
        {
            RegisterService(builder, registration);
        }
    }

    private void RegisterService(ContainerBuilder builder, ServiceRegistrationDescriptor descriptor)
    {
        var chain = _serviceChainCache!.GetInterceptors(descriptor.ImplementationType);
        var canProxyInterface = chain.Count > 0 && descriptor.ServiceType.IsInterface;
        var canProxyClass = chain.Count > 0 && !descriptor.ServiceType.IsInterface && _enableClassInterceptors;

        if (chain.Count > 0 && !descriptor.ServiceType.IsInterface)
        {
            if (_enableClassInterceptors)
            {
                AddClassProxyWarnings(descriptor.ImplementationType);
            }
            else
            {
                _warnings.Add(
                    $"服务 {descriptor.ImplementationType.FullName ?? descriptor.ImplementationType.Name} 存在 Service 拦截器链，但默认仅为接口服务启用 Castle 代理，已跳过代理注册");
            }
        }

        if (descriptor.IsOpenGenericDefinition)
        {
            RegisterGenericService(builder, descriptor, canProxyInterface);
            return;
        }

        RegisterConcreteService(builder, descriptor, canProxyInterface, canProxyClass);
    }

    private static void RegisterConcreteService(
        ContainerBuilder builder,
        ServiceRegistrationDescriptor descriptor,
        bool canProxyInterface,
        bool canProxyClass)
    {
        var registration = builder.RegisterType(descriptor.ImplementationType);
        ApplyService(registration, descriptor);
        registration.ApplyTwLifetime(descriptor.Lifetime);
        registration.ApplyTwProxyIfNeeded(canProxyInterface);
        registration.ApplyTwClassProxyIfNeeded(canProxyClass);
    }

    private static void RegisterGenericService(
        ContainerBuilder builder,
        ServiceRegistrationDescriptor descriptor,
        bool canProxy)
    {
        var registration = builder.RegisterGeneric(descriptor.ImplementationType);
        ApplyService(registration, descriptor);
        registration.ApplyTwLifetime(descriptor.Lifetime);
        registration.ApplyTwProxyIfNeeded(canProxy);
    }

    private static void ApplyService<TLimit, TActivatorData, TRegistrationStyle>(
        IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle> registration,
        ServiceRegistrationDescriptor descriptor)
    {
        if (descriptor.Key is null)
        {
            registration.As(descriptor.ServiceType);
        }
        else
        {
            registration.Keyed(descriptor.Key, descriptor.ServiceType);
        }
    }

    private void AddClassProxyWarnings(Type implementationType)
    {
        var nonVirtualMethods = implementationType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => m.DeclaringType != typeof(object))
            .Where(m => !m.IsSpecialName)
            .Where(m => !m.IsVirtual || m.IsFinal)
            .OrderBy(m => m.Name, StringComparer.Ordinal);

        foreach (var method in nonVirtualMethods)
        {
            _warnings.Add(
                $"服务 {implementationType.FullName ?? implementationType.Name} 已启用 Castle 类代理，但方法 {method.Name} 不是 virtual，无法被拦截");
        }
    }
}
