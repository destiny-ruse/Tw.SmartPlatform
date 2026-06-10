using System.Reflection;
using Tw.DependencyInjection.Diagnostics;

namespace Tw.DependencyInjection.DynamicProxy;

/// <summary>
/// 根据服务注册候选与拦截器选择器规划 Castle 拦截承载方式
/// </summary>
internal static class InterceptionRegistrationPlanner
{
    private const string CastleInterfaceProxy = "CastleInterfaceProxy";
    private const string CastleClassProxy = "CastleClassProxy";
    private const string Enabled = "enabled";
    private const string Skipped = "skipped";

    /// <summary>
    /// 为服务注册候选生成方法级拦截承载诊断报告
    /// </summary>
    /// <param name="registrations">服务注册规划阶段选中的候选列表</param>
    /// <param name="selector">拦截器选择器</param>
    /// <returns>方法级拦截承载诊断报告</returns>
    /// <exception cref="ArgumentNullException"><paramref name="registrations"/> 或 <paramref name="selector"/> 为 null 时抛出</exception>
    public static InterceptionReport Plan(
        IReadOnlyList<Registration.ServiceCandidate> registrations,
        IInterceptorSelector selector)
    {
        ArgumentNullException.ThrowIfNull(registrations);
        ArgumentNullException.ThrowIfNull(selector);

        var diagnostics = new List<InterceptionDiagnostic>();
        var implementationsWithInterfaceContracts = registrations
            .Where(registration => registration.ServiceType.IsInterface)
            .Select(registration => registration.ImplementationType)
            .ToHashSet();

        foreach (var registration in registrations)
        {
            foreach (var method in EnumerateCandidateMethods(registration.ServiceType, registration.ImplementationType))
            {
                var interceptors = selector.SelectInterceptors(
                    registration.ImplementationType,
                    registration.ServiceType,
                    method);

                if (interceptors.Count == 0)
                {
                    continue;
                }

                diagnostics.Add(CreateDiagnostic(
                    registration,
                    method,
                    interceptors,
                    implementationsWithInterfaceContracts.Contains(registration.ImplementationType)));
            }
        }

        return new InterceptionReport(diagnostics);
    }

    private static IEnumerable<MethodInfo> EnumerateCandidateMethods(Type serviceType, Type implementationType)
    {
        var inspectedType = serviceType.IsInterface ? serviceType : implementationType;

        return inspectedType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => !method.IsSpecialName)
            .OrderBy(method => method.Name, StringComparer.Ordinal)
            .ThenBy(method => string.Join(",", method.GetParameters().Select(parameter =>
                parameter.ParameterType.FullName ?? parameter.ParameterType.Name)), StringComparer.Ordinal);
    }

    private static InterceptionDiagnostic CreateDiagnostic(
        Registration.ServiceCandidate registration,
        MethodInfo method,
        IReadOnlyList<Type> interceptorTypes,
        bool implementationHasInterfaceContract)
    {
        if (registration.ServiceType.IsInterface)
        {
            return EnabledDiagnostic(registration, method, CastleInterfaceProxy, interceptorTypes);
        }

        if (implementationHasInterfaceContract)
        {
            return SkippedDiagnostic(
                registration,
                method,
                CastleClassProxy,
                interceptorTypes,
                "实现类型已暴露接口契约，拦截通过 Castle interface proxy 承载");
        }

        if (registration.ImplementationType.IsGenericTypeDefinition)
        {
            return SkippedDiagnostic(
                registration,
                method,
                CastleClassProxy,
                interceptorTypes,
                "开放泛型 class-only 服务当前不承载 Castle class proxy");
        }

        if (!IsPublicProxyType(registration.ImplementationType))
        {
            return SkippedDiagnostic(
                registration,
                method,
                CastleClassProxy,
                interceptorTypes,
                "实现类型不是 public，无法使用 Castle class proxy");
        }

        if (registration.ImplementationType.IsSealed)
        {
            return SkippedDiagnostic(
                registration,
                method,
                CastleClassProxy,
                interceptorTypes,
                "实现类型为 sealed，无法使用 Castle class proxy");
        }

        if (!CanUseClassProxy(method))
        {
            return SkippedDiagnostic(
                registration,
                method,
                CastleClassProxy,
                interceptorTypes,
                "目标方法不是可重写 virtual 方法，无法使用 Castle class proxy");
        }

        return EnabledDiagnostic(registration, method, CastleClassProxy, interceptorTypes);
    }

    private static bool CanUseClassProxy(MethodInfo method) =>
        method.IsVirtual && !method.IsFinal && !method.IsPrivate;

    private static bool IsPublicProxyType(Type type) =>
        type.IsPublic || type.IsNestedPublic;

    private static InterceptionDiagnostic EnabledDiagnostic(
        Registration.ServiceCandidate registration,
        MethodInfo method,
        string carrier,
        IReadOnlyList<Type> interceptorTypes) =>
        new(
            ServiceTypeName: TypeName(registration.ServiceType),
            ImplementationTypeName: TypeName(registration.ImplementationType),
            MethodName: method.Name,
            Carrier: carrier,
            InterceptorTypeNames: InterceptorTypeNames(interceptorTypes),
            Status: Enabled,
            Reason: null);

    private static InterceptionDiagnostic SkippedDiagnostic(
        Registration.ServiceCandidate registration,
        MethodInfo method,
        string carrier,
        IReadOnlyList<Type> interceptorTypes,
        string reason) =>
        new(
            ServiceTypeName: TypeName(registration.ServiceType),
            ImplementationTypeName: TypeName(registration.ImplementationType),
            MethodName: method.Name,
            Carrier: carrier,
            InterceptorTypeNames: InterceptorTypeNames(interceptorTypes),
            Status: Skipped,
            Reason: reason);

    private static IReadOnlyList<string> InterceptorTypeNames(IReadOnlyList<Type> interceptorTypes) =>
        interceptorTypes
            .Select(TypeName)
            .ToList();

    private static string TypeName(Type type) => type.FullName ?? type.Name;
}
