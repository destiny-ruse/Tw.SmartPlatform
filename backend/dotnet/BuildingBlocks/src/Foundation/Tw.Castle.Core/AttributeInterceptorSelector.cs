using System.Reflection;
using Tw.Castle.Core.Abstractions;

namespace Tw.Castle.Core;

/// <summary>
/// 基于 <see cref="InterceptAttribute"/> 与 <see cref="DisableInterceptionAttribute"/> 选择拦截器类型
/// </summary>
/// <remarks>
/// 当服务契约为接口时，通过 <see cref="Type.GetInterfaceMap(Type)"/> 在接口方法与实现方法之间双向映射，包含显式接口实现方法。
/// </remarks>
public sealed class AttributeInterceptorSelector : IInterceptorSelector
{
    /// <summary>
    /// 从服务契约、实现类型以及相关接口/实现方法读取拦截声明
    /// </summary>
    /// <param name="implementationType">服务实现类型</param>
    /// <param name="serviceType">服务契约类型</param>
    /// <param name="method">被调用的方法，可能来自服务契约或实现类型</param>
    /// <returns>按 <see cref="InterceptorOrderAttribute.Order"/> 和类型全名排序后的拦截器类型列表；方法或类型禁用拦截时返回空列表</returns>
    /// <exception cref="ArgumentNullException">implementationType、serviceType 或 method 为 null 时抛出</exception>
    public IReadOnlyList<Type> SelectInterceptors(Type implementationType, Type serviceType, MethodInfo method)
    {
        ArgumentNullException.ThrowIfNull(implementationType);
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(method);

        var relatedMethods = ResolveRelatedMethods(implementationType, serviceType, method);
        if (IsInterceptionDisabled(implementationType)
            || IsInterceptionDisabled(serviceType)
            || relatedMethods.Any(IsInterceptionDisabled))
        {
            return [];
        }

        var interceptorTypes = new List<Type>();
        AddInterceptorTypes(serviceType, interceptorTypes);
        AddInterceptorTypes(implementationType, interceptorTypes);
        foreach (var relatedMethod in relatedMethods)
        {
            AddInterceptorTypes(relatedMethod, interceptorTypes);
        }

        return interceptorTypes
            .Distinct()
            .OrderBy(GetInterceptorOrder)
            .ThenBy(type => type.FullName ?? type.Name, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// 注册nterceptor类型集合所需服务
    /// </summary>
    /// <param name="member">用于提供member</param>
    /// <param name="interceptorTypes">需要注册或选择的拦截器类型集合</param>
    private static void AddInterceptorTypes(MemberInfo member, ICollection<Type> interceptorTypes)
    {
        foreach (var attribute in member.GetCustomAttributes<InterceptAttribute>(inherit: true))
        {
            interceptorTypes.Add(attribute.InterceptorType);
        }
    }

    /// <summary>
    /// 判断nterceptionDisabled是否满足条件
    /// </summary>
    /// <param name="member">用于提供member</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
    private static bool IsInterceptionDisabled(MemberInfo member) =>
        member.IsDefined(typeof(DisableInterceptionAttribute), inherit: true);

    /// <summary>
    /// 说明读取拦截器顺序在当前类型中的职责
    /// </summary>
    /// <param name="interceptorType">用于提供nterceptor类型</param>
    /// <returns>读取到的拦截器Order</returns>
    private static int GetInterceptorOrder(Type interceptorType) =>
        interceptorType.GetCustomAttribute<InterceptorOrderAttribute>(inherit: false)?.Order ?? 0;

    /// <summary>
    /// 说明解析RelatedMethods在当前类型中的职责
    /// </summary>
    /// <param name="implementationType">服务注册中使用的实现类型</param>
    /// <param name="serviceType">服务注册中暴露的服务类型</param>
    /// <param name="method">用于构造测试场景的方法元数据</param>
    /// <returns>匹配当前查询条件的结果集合</returns>
    private static IReadOnlyList<MethodInfo> ResolveRelatedMethods(
        Type implementationType,
        Type serviceType,
        MethodInfo method)
    {
        var methods = new List<MethodInfo>();
        AddMethod(methods, method);

        foreach (var interfaceType in EnumerateServiceInterfaces(serviceType))
        {
            InterfaceMapping interfaceMap;
            try
            {
                interfaceMap = implementationType.GetInterfaceMap(interfaceType);
            }
            catch (ArgumentException)
            {
                continue;
            }

            for (var index = 0; index < interfaceMap.InterfaceMethods.Length; index++)
            {
                var interfaceMethod = interfaceMap.InterfaceMethods[index];
                var targetMethod = interfaceMap.TargetMethods[index];
                if (IsSameMethod(interfaceMethod, method) || IsSameMethod(targetMethod, method))
                {
                    AddMethod(methods, interfaceMethod);
                    AddMethod(methods, targetMethod);
                }
            }
        }

        return methods;
    }

    /// <summary>
    /// 说明Enumerate服务Interfaces在当前类型中的职责
    /// </summary>
    /// <param name="serviceType">服务注册中暴露的服务类型</param>
    /// <returns>匹配当前查询条件的结果集合</returns>
    private static IEnumerable<Type> EnumerateServiceInterfaces(Type serviceType)
    {
        if (!serviceType.IsInterface)
        {
            yield break;
        }

        yield return serviceType;

        foreach (var inheritedInterface in serviceType.GetInterfaces())
        {
            yield return inheritedInterface;
        }
    }

    /// <summary>
    /// 注册方法所需服务
    /// </summary>
    /// <param name="methods">用于提供methods</param>
    /// <param name="method">用于构造测试场景的方法元数据</param>
    private static void AddMethod(ICollection<MethodInfo> methods, MethodInfo method)
    {
        if (methods.Any(existingMethod => IsSameMethod(existingMethod, method)))
        {
            return;
        }

        methods.Add(method);
    }

    /// <summary>
    /// 判断Same方法是否满足条件
    /// </summary>
    /// <param name="left">用于提供left</param>
    /// <param name="right">用于提供right</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
    private static bool IsSameMethod(MethodInfo left, MethodInfo right) =>
        Equals(left, right) || (left.Module == right.Module && left.MetadataToken == right.MetadataToken);
}
