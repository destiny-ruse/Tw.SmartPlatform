using Tw.DependencyInjection.Abstractions.Configuration;
using Tw.DependencyInjection.Abstractions;

namespace Tw.DependencyInjection.Registration;

/// <summary>
/// 根据实现类型上的标记接口与特性解析该类型应暴露的全部服务契约
/// </summary>
/// <remarks>
/// 解析策略分三种路径：
/// <list type="number">
/// <item>若类型标注了 <see cref="ExposeServicesAttribute"/>，则以显式声明为准，<see cref="ExposeServicesAttribute.IncludeSelf"/> 控制是否附加自身类型。</item>
/// <item>若无显式声明，则默认暴露自身类型与 ABP 风格命名匹配的接口（实现类名以"接口名去掉前导 <c>I</c>"结尾，如 <c>DefaultPaymentProvider</c> 匹配 <c>IPaymentProvider</c>）；开放泛型实现同样仅暴露命名匹配的泛型接口定义（如 <c>Repository&lt;&gt;</c> → <c>IRepository&lt;&gt;</c>），不暴露实现的其它接口，以符合"默认规则不暴露所有接口"的设计原则。</item>
/// <item>无论哪种路径，<see cref="ExposeKeyedServiceAttribute"/> 均独立追加 keyed service 暴露。去重基于 <see cref="ServiceExposure"/> 记录值相等；<see cref="ServiceExposure.Key"/> 为 <see cref="string"/> 等值类型时按值去重，复杂对象 key 需自身实现 <see cref="object.Equals(object)"/>/<see cref="object.GetHashCode"/>（当前 keyed key 仅来自 attribute 常量参数）。</item>
/// </list>
/// </remarks>
internal static class ServiceExposureResolver
{
    /// <summary>
    /// 解析指定实现类型应暴露的全部服务契约列表
    /// </summary>
    /// <param name="implementationType">实现类型；可为开放泛型定义</param>
    /// <returns>去重后的 <see cref="ServiceExposure"/> 只读列表</returns>
    /// <exception cref="ArgumentNullException"><paramref name="implementationType"/> 为 <c>null</c> 时抛出</exception>
    /// <remarks>
    /// 默认路径仅暴露自身与命名匹配的接口，不暴露实现类型的全部接口。
    /// </remarks>
    public static IReadOnlyList<ServiceExposure> Resolve(Type implementationType)
    {
        ArgumentNullException.ThrowIfNull(implementationType);

        var exposures = new List<ServiceExposure>();

        // 读取显式 ExposeServicesAttribute 声明
        var explicitExposes = implementationType
            .GetCustomAttributes(typeof(ExposeServicesAttribute), inherit: false)
            .OfType<ExposeServicesAttribute>()
            .ToList();

        if (explicitExposes.Count > 0)
        {
            // 显式声明路径：以 Attribute 声明的契约类型为准
            foreach (var expose in explicitExposes)
            {
                exposures.AddRange(expose.ServiceTypes.Select(serviceType =>
                    new ServiceExposure(NormalizeGenericServiceType(serviceType), null)));

                if (expose.IncludeSelf)
                {
                    exposures.Add(new ServiceExposure(implementationType, null));
                }
            }
        }
        else
        {
            // 默认路径：暴露自身 + 名称匹配的接口
            exposures.Add(new ServiceExposure(implementationType, null));
            exposures.AddRange(DefaultInterfaceExposures(implementationType));
        }

        // keyed service 独立追加，不受显式/默认路径影响
        foreach (var keyed in implementationType
            .GetCustomAttributes(typeof(ExposeKeyedServiceAttribute), inherit: false)
            .OfType<ExposeKeyedServiceAttribute>())
        {
            ValidateKeyedService(implementationType, keyed);
            exposures.Add(new ServiceExposure(NormalizeGenericServiceType(keyed.ServiceType), keyed.Key));
        }

        return exposures
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// 校验Keyed服务并在非法时抛出异常
    /// </summary>
    /// <param name="implementationType">服务注册中使用的实现类型</param>
    /// <param name="keyed">用于提供keyed</param>
    private static void ValidateKeyedService(Type implementationType, ExposeKeyedServiceAttribute keyed)
    {
        if (keyed.Key is null || keyed.Key is string stringKey && string.IsNullOrWhiteSpace(stringKey))
        {
            throw new Tw.DependencyInjection.ServiceRegistrationException(
                $"Keyed service 的 key 不能为空: {implementationType.FullName ?? implementationType.Name} / " +
                $"{keyed.ServiceType.FullName ?? keyed.ServiceType.Name}");
        }
    }

    /// <summary>
    /// 按默认命名匹配策略枚举实现类型对外暴露的接口契约
    /// </summary>
    /// <remarks>
    /// 仅暴露 ABP 风格命名匹配的接口——即实现类名以"接口名去掉前导 <c>I</c>"结尾的接口，不暴露实现类型实现的其它全部接口。
    /// 对于开放泛型实现，同样仅暴露命名匹配的泛型接口定义（<c>Repository&lt;&gt;</c> → <c>IRepository&lt;&gt;</c>）；
    /// 其余泛型接口（如 <c>IEnumerable&lt;&gt;</c>）因名称不匹配而被排除。
    /// </remarks>
    private static IEnumerable<ServiceExposure> DefaultInterfaceExposures(Type implementationType)
    {
        foreach (var interfaceType in implementationType.GetInterfaces())
        {
            // 跳过框架标记接口与基础设施接口
            if (IsFrameworkOrMarkerInterface(interfaceType))
            {
                continue;
            }

            var normalized = NormalizeGenericServiceType(interfaceType);

            if (IsNameMatchedInterface(implementationType, normalized))
            {
                yield return new ServiceExposure(normalized, null);
            }
        }
    }

    /// <summary>
    /// 将封闭泛型类型归一化为其泛型定义；非泛型类型原样返回
    /// </summary>
    private static Type NormalizeGenericServiceType(Type serviceType)
    {
        return serviceType.IsGenericType ? serviceType.GetGenericTypeDefinition() : serviceType;
    }

    /// <summary>
    /// 判断接口名称是否与实现类型名称匹配（ABP 风格：实现类名以"接口名去掉前导 <c>I</c>"结尾）
    /// </summary>
    /// <param name="implementationType">实现类型；可为开放泛型定义</param>
    /// <param name="serviceType">
    /// 候选服务接口类型；调用前应已通过 <see cref="NormalizeGenericServiceType"/> 归一化为泛型定义。
    /// 名称比较时将截去泛型参数数量后缀（<c>`1</c>、<c>`2</c> 等），仅比较基础名称部分。
    /// </param>
    /// <returns>
    /// 实现类基础名称以"接口基础名称去掉前导 <c>I</c>"结尾时返回 <see langword="true"/>；否则返回 <see langword="false"/>。
    /// 例如 <c>DefaultPaymentProvider</c> 实现 <c>IPaymentProvider</c>：
    /// 候选后缀为 <c>PaymentProvider</c>，<c>"DefaultPaymentProvider".EndsWith("PaymentProvider")</c> 成立，视为命名匹配。
    /// </returns>
    private static bool IsNameMatchedInterface(Type implementationType, Type serviceType)
    {
        if (!serviceType.IsInterface)
        {
            return false;
        }

        // 去除泛型参数数量后缀（`1、`2 等）再比较
        var implName = implementationType.IsGenericType
            ? implementationType.Name[..implementationType.Name.IndexOf('`')]
            : implementationType.Name;

        var serviceName = serviceType.IsGenericType
            ? serviceType.Name[..serviceType.Name.IndexOf('`')]
            : serviceType.Name;

        // ABP 风格结尾匹配：去掉接口名前导 I，判断实现类名是否以剩余部分结尾
        var expected = serviceName.Length > 1 && serviceName[0] == 'I'
            ? serviceName[1..]
            : serviceName;

        return implName.EndsWith(expected, StringComparison.Ordinal);
    }

    /// <summary>
    /// 判断接口是否为应被跳过的框架标记接口或基础设施接口
    /// </summary>
    private static bool IsFrameworkOrMarkerInterface(Type interfaceType)
    {
        var normalized = NormalizeGenericServiceType(interfaceType);
        return normalized == typeof(ITransientDependency)
            || normalized == typeof(IScopedDependency)
            || normalized == typeof(ISingletonDependency)
            || normalized == typeof(IConfigurableOptions)
            || normalized == typeof(IDisposable)
            || normalized == typeof(IAsyncDisposable);
    }
}
