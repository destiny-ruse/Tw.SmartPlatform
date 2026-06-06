using System.Reflection;
using Tw.DependencyInjection.Abstractions;

namespace Tw.DependencyInjection.Registration;

/// <summary>
/// 解析服务类型与程序集的注册优先级，并合成最终排序权重
/// </summary>
internal static class ServicePriorityResolver
{
    /// <summary>拓扑层级权重步长：每层间距 1_000_000，确保层间隔离</summary>
    public const int TopologyLevelStep = 1_000_000;

    /// <summary>显式优先级下限（含），超出范围时抛出 <see cref="Tw.DependencyInjection.ServiceRegistrationException"/></summary>
    public const int ExplicitPriorityMin = -100_000;

    /// <summary>显式优先级上限（含），超出范围时抛出 <see cref="Tw.DependencyInjection.ServiceRegistrationException"/></summary>
    public const int ExplicitPriorityMax = 100_000;

    /// <summary>
    /// 解析指定程序集的程序集级优先级：配置项优先于 <see cref="TwAssemblyPriorityAttribute"/>，缺省为 0
    /// </summary>
    /// <param name="assembly">目标程序集</param>
    /// <param name="options">包含 <c>AssemblyPriorities</c> 配置的选项对象</param>
    /// <returns>程序集优先级数值</returns>
    /// <exception cref="Tw.DependencyInjection.ServiceRegistrationException">程序集名称为空或优先级超出允许范围时抛出</exception>
    public static int ResolveAssemblyPriority(Assembly assembly, Tw.DependencyInjection.ServiceRegistrationOptions options)
    {
        var assemblyName = assembly.GetName().Name
            ?? throw new Tw.DependencyInjection.ServiceRegistrationException("程序集名称为空，无法解析程序集优先级");

        if (options.AssemblyPriorities.TryGetValue(assemblyName, out var configuredPriority))
        {
            ValidateExplicitPriority(configuredPriority, "程序集优先级");
            return configuredPriority;
        }

        var attribute = assembly
            .GetCustomAttributes(typeof(TwAssemblyPriorityAttribute))
            .OfType<TwAssemblyPriorityAttribute>()
            .SingleOrDefault();

        var priority = attribute?.Priority ?? 0;
        ValidateExplicitPriority(priority, "程序集优先级");
        return priority;
    }

    /// <summary>
    /// 解析指定类型的类型级优先级：<see cref="ServicePriorityAttribute"/> 与 <see cref="ServiceRegistrationAttribute.Priority"/> 必须一致，缺省为 0
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <returns>类型优先级数值</returns>
    /// <exception cref="Tw.DependencyInjection.ServiceRegistrationException">两个特性声明值不一致或优先级超出允许范围时抛出</exception>
    public static int ResolveTypePriority(Type type)
    {
        var servicePriority = type
            .GetCustomAttributes(typeof(ServicePriorityAttribute), inherit: false)
            .OfType<ServicePriorityAttribute>()
            .SingleOrDefault();

        var registrationPriority = type
            .GetCustomAttributes(typeof(ServiceRegistrationAttribute), inherit: false)
            .OfType<ServiceRegistrationAttribute>()
            .SingleOrDefault()
            ?.Priority;

        if (servicePriority is not null && registrationPriority is not null && servicePriority.Priority != registrationPriority.Value)
        {
            throw new Tw.DependencyInjection.ServiceRegistrationException($"类型优先级声明不一致: {type.FullName}");
        }

        var priority = servicePriority?.Priority ?? registrationPriority ?? 0;
        ValidateExplicitPriority(priority, "类型优先级");
        return priority;
    }

    /// <summary>
    /// 合成最终注册排序权重：拓扑层级 × <see cref="TopologyLevelStep"/> + 程序集优先级 + 类型优先级
    /// </summary>
    /// <param name="topologyLevel">程序集在拓扑排序中所处层级（0 为最底层）</param>
    /// <param name="assemblyPriority">程序集优先级，由 <see cref="ResolveAssemblyPriority"/> 得出</param>
    /// <param name="typePriority">类型优先级，由 <see cref="ResolveTypePriority"/> 得出</param>
    /// <returns>最终排序权重，值越大优先级越高</returns>
    /// <exception cref="Tw.DependencyInjection.ServiceRegistrationException">优先级超出允许范围时抛出</exception>
    public static long CalculateFinalPriority(int topologyLevel, int assemblyPriority, int typePriority)
    {
        ValidateExplicitPriority(assemblyPriority, "程序集优先级");
        ValidateExplicitPriority(typePriority, "类型优先级");
        return (long)topologyLevel * TopologyLevelStep + assemblyPriority + typePriority;
    }

    /// <summary>验证显式优先级在允许范围内，超出则抛出异常</summary>
    private static void ValidateExplicitPriority(int priority, string label)
    {
        if (priority is < ExplicitPriorityMin or > ExplicitPriorityMax)
        {
            throw new Tw.DependencyInjection.ServiceRegistrationException(
                $"{label}超出允许范围 {ExplicitPriorityMin}..{ExplicitPriorityMax}: {priority}");
        }
    }
}
