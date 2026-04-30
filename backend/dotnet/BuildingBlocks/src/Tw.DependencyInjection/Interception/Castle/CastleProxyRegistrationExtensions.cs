using Autofac.Builder;
using Autofac.Extras.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;

namespace Tw.DependencyInjection.Interception.Castle;

internal static class CastleProxyRegistrationExtensions
{
    public static IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle> ApplyTwLifetime<TLimit, TActivatorData, TRegistrationStyle>(
        this IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle> registration,
        ServiceLifetime lifetime)
    {
        return lifetime switch
        {
            ServiceLifetime.Singleton => registration.SingleInstance(),
            ServiceLifetime.Scoped => registration.InstancePerLifetimeScope(),
            _ => registration.InstancePerDependency()
        };
    }

    public static IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle> ApplyTwProxyIfNeeded<TLimit, TActivatorData, TRegistrationStyle>(
        this IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle> registration,
        bool proxyEnabled)
    {
        if (!proxyEnabled)
        {
            return registration;
        }

        return registration
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(CastleAsyncInterceptorAdapter));
    }

    public static IRegistrationBuilder<TLimit, TConcreteReflectionActivatorData, TRegistrationStyle> ApplyTwClassProxyIfNeeded<TLimit, TConcreteReflectionActivatorData, TRegistrationStyle>(
        this IRegistrationBuilder<TLimit, TConcreteReflectionActivatorData, TRegistrationStyle> registration,
        bool proxyEnabled)
        where TConcreteReflectionActivatorData : ConcreteReflectionActivatorData
    {
        if (!proxyEnabled)
        {
            return registration;
        }

        return registration
            .EnableClassInterceptors()
            .InterceptedBy(typeof(CastleAsyncInterceptorAdapter));
    }
}
