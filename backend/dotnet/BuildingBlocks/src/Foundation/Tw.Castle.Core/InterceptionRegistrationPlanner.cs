using System.Reflection;

namespace Tw.Castle.Core;

/// <summary>
/// 描述需要参与拦截承载规划的一次服务注册关系
/// </summary>
/// <param name="ServiceType">服务契约类型</param>
/// <param name="ImplementationType">服务实现类型</param>
public sealed record InterceptionCandidate(Type ServiceType, Type ImplementationType);

/// <summary>
/// 拦截承载规划结果：诊断报告与运行期解析所需的拦截器类型集合
/// </summary>
/// <param name="Report">方法级拦截承载诊断报告</param>
/// <param name="RequiredInterceptorTypes">被 selector 命中、运行期需要按自身类型解析的拦截器类型</param>
public sealed record InterceptionPlan(
    InterceptionReport Report,
    IReadOnlyCollection<Type> RequiredInterceptorTypes);

/// <summary>
/// 根据服务注册候选与拦截器选择器规划 Castle 拦截承载方式
/// </summary>
public static class InterceptionRegistrationPlanner
{
    /// <summary>表示 CastleInterfaceProxy 常量</summary>
    private const string CastleInterfaceProxy = "CastleInterfaceProxy";
    /// <summary>表示 CastleClassProxy 常量</summary>
    private const string CastleClassProxy = "CastleClassProxy";
    /// <summary>表示 Enabled 常量</summary>
    private const string Enabled = "enabled";
    /// <summary>表示 Skipped 常量</summary>
    private const string Skipped = "skipped";

    /// <summary>
    /// 为服务注册候选生成方法级拦截承载诊断报告
    /// </summary>
    /// <param name="registrations">服务注册规划阶段选中的候选列表</param>
    /// <param name="selector">拦截器选择器</param>
    /// <returns>拦截承载规划结果，含诊断报告与所需拦截器类型集合</returns>
    /// <exception cref="ArgumentNullException"><paramref name="registrations"/> 或 <paramref name="selector"/> 为 null 时抛出</exception>
    public static InterceptionPlan Plan(
        IReadOnlyList<InterceptionCandidate> registrations,
        IInterceptorSelector selector)
    {
        ArgumentNullException.ThrowIfNull(registrations);
        ArgumentNullException.ThrowIfNull(selector);

        var diagnostics = new List<InterceptionDiagnostic>();
        var requiredInterceptorTypes = new HashSet<Type>();
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

                requiredInterceptorTypes.UnionWith(interceptors);

                diagnostics.Add(CreateDiagnostic(
                    registration,
                    method,
                    interceptors,
                    implementationsWithInterfaceContracts.Contains(registration.ImplementationType)));
            }
        }

        return new InterceptionPlan(new InterceptionReport(diagnostics), requiredInterceptorTypes);
    }

    /// <summary>执行 EnumerateCandidateMethods 操作</summary>
    /// <param name="serviceType">serviceType 参数</param>
    /// <param name="implementationType">implementationType 参数</param>
    /// <returns>EnumerateCandidateMethods 的执行结果</returns>
    private static IEnumerable<MethodInfo> EnumerateCandidateMethods(Type serviceType, Type implementationType)
    {
        var inspectedMethods = serviceType.IsInterface
            ? EnumerateInterfaceMethods(serviceType)
            : implementationType.GetMethods(BindingFlags.Instance | BindingFlags.Public);

        return inspectedMethods
            .Where(method => !method.IsSpecialName)
            .Where(method => method.DeclaringType != typeof(object))
            .OrderBy(method => method.Name, StringComparer.Ordinal)
            .ThenBy(method => method.DeclaringType?.FullName ?? method.DeclaringType?.Name ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(method => string.Join(",", method.GetParameters().Select(parameter =>
                parameter.ParameterType.FullName ?? parameter.ParameterType.Name)), StringComparer.Ordinal);
    }

    /// <summary>执行 EnumerateInterfaceMethods 操作</summary>
    /// <param name="serviceType">serviceType 参数</param>
    /// <returns>EnumerateInterfaceMethods 的执行结果</returns>
    private static IEnumerable<MethodInfo> EnumerateInterfaceMethods(Type serviceType)
    {
        var methods = new List<MethodInfo>();
        AddMethods(methods, serviceType);

        foreach (var inheritedInterface in serviceType.GetInterfaces())
        {
            AddMethods(methods, inheritedInterface);
        }

        return methods;
    }

    /// <summary>执行 AddMethods 操作</summary>
    /// <param name="methods">methods 参数</param>
    /// <param name="type">type 参数</param>
    private static void AddMethods(ICollection<MethodInfo> methods, Type type)
    {
        foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public))
        {
            if (methods.Any(existingMethod => IsSameMethod(existingMethod, method)))
            {
                continue;
            }

            methods.Add(method);
        }
    }

    /// <summary>执行 IsSameMethod 操作</summary>
    /// <param name="left">left 参数</param>
    /// <param name="right">right 参数</param>
    /// <returns>IsSameMethod 的执行结果</returns>
    private static bool IsSameMethod(MethodInfo left, MethodInfo right) =>
        Equals(left, right) || (left.Module == right.Module && left.MetadataToken == right.MetadataToken);

    /// <summary>执行 CreateDiagnostic 操作</summary>
    /// <param name="registration">registration 参数</param>
    /// <param name="method">method 参数</param>
    /// <param name="interceptorTypes">interceptorTypes 参数</param>
    /// <param name="implementationHasInterfaceContract">implementationHasInterfaceContract 参数</param>
    /// <returns>CreateDiagnostic 的执行结果</returns>
    private static InterceptionDiagnostic CreateDiagnostic(
        InterceptionCandidate registration,
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

    /// <summary>执行 CanUseClassProxy 操作</summary>
    /// <param name="method">method 参数</param>
    /// <returns>CanUseClassProxy 的执行结果</returns>
    private static bool CanUseClassProxy(MethodInfo method) =>
        method.IsVirtual && !method.IsFinal && !method.IsPrivate;

    /// <summary>执行 IsPublicProxyType 操作</summary>
    /// <param name="type">type 参数</param>
    /// <returns>IsPublicProxyType 的执行结果</returns>
    private static bool IsPublicProxyType(Type type) =>
        type.IsVisible;

    /// <summary>执行 EnabledDiagnostic 操作</summary>
    /// <param name="registration">registration 参数</param>
    /// <param name="method">method 参数</param>
    /// <param name="carrier">carrier 参数</param>
    /// <param name="interceptorTypes">interceptorTypes 参数</param>
    /// <returns>EnabledDiagnostic 的执行结果</returns>
    private static InterceptionDiagnostic EnabledDiagnostic(
        InterceptionCandidate registration,
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

    /// <summary>执行 SkippedDiagnostic 操作</summary>
    /// <param name="registration">registration 参数</param>
    /// <param name="method">method 参数</param>
    /// <param name="carrier">carrier 参数</param>
    /// <param name="interceptorTypes">interceptorTypes 参数</param>
    /// <param name="reason">reason 参数</param>
    /// <returns>SkippedDiagnostic 的执行结果</returns>
    private static InterceptionDiagnostic SkippedDiagnostic(
        InterceptionCandidate registration,
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

    /// <summary>执行 InterceptorTypeNames 操作</summary>
    /// <param name="interceptorTypes">interceptorTypes 参数</param>
    /// <returns>InterceptorTypeNames 的执行结果</returns>
    private static IReadOnlyList<string> InterceptorTypeNames(IReadOnlyList<Type> interceptorTypes) =>
        interceptorTypes
            .Select(TypeName)
            .ToList();

    /// <summary>执行 TypeName 操作</summary>
    /// <param name="type">type 参数</param>
    /// <returns>TypeName 的执行结果</returns>
    private static string TypeName(Type type) => type.FullName ?? type.Name;
}
