using Autofac;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
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

        var total = Stopwatch.StartNew();
        var diagnostics = AutoRegistrationDiagnostics.Create(state.Configuration, state.Options);

        var scanStopwatch = Stopwatch.StartNew();
        var scans = new ServiceRegistrationScanner(new TypeFinder(state.Options.Assemblies)).Scan();
        scanStopwatch.Stop();
        diagnostics.Stage(
            "scan",
            scanStopwatch.Elapsed,
            $"assemblyCount={state.Options.Assemblies.Count}; descriptorCount={scans.Count}");

        var sortStopwatch = Stopwatch.StartNew();
        var plan = ServiceRegistrationPlanner.Plan(scans, state.Options.Assemblies);
        sortStopwatch.Stop();
        diagnostics.Stage(
            "sort",
            sortStopwatch.Elapsed,
            $"serviceCount={plan.Registrations.Count}; warningCount={plan.Diagnostics.Warnings.Count}");
        foreach (var warning in plan.Diagnostics.Warnings)
        {
            diagnostics.Warning(warning);
        }

        var aopStopwatch = Stopwatch.StartNew();
        var matchers = state.Options.MatcherTypes.Select(CreateMatcher).ToArray();
        var chainCache = new ServiceChainCache(plan.Registrations, state.Options.GlobalInterceptors, matchers);
        var interceptedCount = plan.Registrations.Count(
            registration => chainCache.GetInterceptors(registration.ImplementationType).Count > 0);
        aopStopwatch.Stop();
        diagnostics.Stage(
            "AOP",
            aopStopwatch.Elapsed,
            $"globalInterceptorCount={state.Options.GlobalInterceptors.Count}; matcherCount={matchers.Length}; interceptedCount={interceptedCount}");

        var module = new AutoRegistrationModule(
            plan,
            state.Options.GlobalInterceptors,
            matchers);
        module.UseDiagnostics(diagnostics);
        builder.RegisterModule(module);

        total.Stop();
        diagnostics.Stage(
            "total",
            total.Elapsed,
            $"serviceCount={plan.Registrations.Count}; interceptedCount={interceptedCount}; warningCount={plan.Diagnostics.Warnings.Count}");

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
