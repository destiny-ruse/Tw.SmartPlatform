using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Tw.Core;
using Tw.Core.Exceptions;
using Tw.Core.Reflection;
using Tw.DependencyInjection.Interception;

namespace Tw.DependencyInjection.Registration;

/// <summary>
/// 提供自动注册的 Autofac ContainerBuilder 入口
/// </summary>
public static class AutoRegistrationContainerBuilderExtensions
{
    /// <summary>
    /// 将已配置的自动注册计划应用到 Autofac 容器
    /// </summary>
    /// <param name="builder">Autofac 容器构建器</param>
    /// <param name="services">已调用 AddAutoRegistration 的服务集合</param>
    public static ContainerBuilder UseAutoRegistration(
        this ContainerBuilder builder,
        IServiceCollection services)
    {
        Check.NotNull(builder);
        Check.NotNull(services);

        var state = services
            .LastOrDefault(d => d.ServiceType == typeof(AutoRegistrationState))
            ?.ImplementationInstance as AutoRegistrationState;

        if (state is null)
        {
            throw new TwConfigurationException(
                "调用 UseAutoRegistration 前必须先调用 AddAutoRegistration。");
        }

        var scans = new ServiceRegistrationScanner(new TypeFinder(state.Options.Assemblies)).Scan();
        var plan = ServiceRegistrationPlanner.Plan(scans, state.Options.Assemblies);
        var matchers = state.Options.MatcherTypes.Select(CreateMatcher).ToArray();

        builder.RegisterModule(new AutoRegistrationModule(
            plan,
            state.Options.GlobalInterceptors,
            matchers));

        return builder;
    }

    private static IInterceptorMatcher CreateMatcher(Type matcherType)
    {
        try
        {
            return (IInterceptorMatcher)Activator.CreateInstance(matcherType)!;
        }
        catch (Exception ex)
        {
            throw new TwConfigurationException(
                $"无法创建拦截器匹配器 {matcherType.FullName ?? matcherType.Name}，请提供公开无参构造函数。",
                ex);
        }
    }
}
