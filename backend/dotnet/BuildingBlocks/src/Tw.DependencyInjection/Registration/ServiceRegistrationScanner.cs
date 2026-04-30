using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Tw.Core;
using Tw.Core.Exceptions;
using Tw.Core.Reflection;
using Tw.DependencyInjection.Registration.Attributes;

namespace Tw.DependencyInjection.Registration;

/// <summary>
/// 从 <see cref="ITypeFinder"/> 配置的程序集中扫描所有候选服务类型，
/// 并依据生命周期标记接口与显式元数据属性产出 <see cref="ServiceScanDescriptor"/> 列表。
/// </summary>
public sealed class ServiceRegistrationScanner
{
    private static readonly Type TransientMarker = typeof(ITransientDependency);
    private static readonly Type ScopedMarker = typeof(IScopedDependency);
    private static readonly Type SingletonMarker = typeof(ISingletonDependency);

    private static readonly HashSet<Type> LifecycleMarkers = [TransientMarker, ScopedMarker, SingletonMarker];

    private readonly ITypeFinder _typeFinder;

    /// <summary>
    /// 初始化 <see cref="ServiceRegistrationScanner"/> 类的新实例。
    /// </summary>
    /// <param name="typeFinder">用于发现候选类型的类型查找器。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="typeFinder"/> 为 <see langword="null"/> 时抛出。</exception>
    public ServiceRegistrationScanner(ITypeFinder typeFinder)
    {
        _typeFinder = Check.NotNull(typeFinder);
    }

    /// <summary>
    /// 对所有已配置程序集执行扫描，返回全部服务候选描述符。
    /// </summary>
    /// <returns>按发现顺序排列的 <see cref="ServiceScanDescriptor"/> 列表。</returns>
    public IReadOnlyList<ServiceScanDescriptor> Scan()
    {
        var results = new List<ServiceScanDescriptor>();

        // Concrete types (excludes open generics — TypeFinder uses IsConcrete which checks !ContainsGenericParameters)
        foreach (var type in _typeFinder.FindTypes())
        {
            var descriptors = ScanType(type);
            results.AddRange(descriptors);
        }

        // Open generic type definitions — TypeFinder.FindTypes() skips these; we discover them separately.
        foreach (var assembly in _typeFinder.Assemblies)
        {
            foreach (var type in GetLoadableOpenGenericTypes(assembly))
            {
                var descriptors = ScanType(type);
                results.AddRange(descriptors);
            }
        }

        return results;
    }

    /// <summary>
    /// 对单个类型执行扫描。供测试或专用流程按需调用。
    /// 等同于调用静态方法 <see cref="ScanType"/>。
    /// </summary>
    /// <param name="implementationType">要扫描的实现类型。</param>
    /// <returns>零个或一个 <see cref="ServiceScanDescriptor"/>。</returns>
    public IReadOnlyList<ServiceScanDescriptor> Scan(Type implementationType)
        => ScanType(implementationType);

    /// <summary>
    /// 对单个类型执行扫描的静态入口；测试辅助方法直接调用此重载。
    /// </summary>
    /// <param name="implementationType">要扫描的实现类型。</param>
    /// <returns>零个或一个 <see cref="ServiceScanDescriptor"/>。</returns>
    /// <exception cref="TwConfigurationException">
    /// 类型同时标注多个生命周期标记接口，或开放泛型类型同时标注 <see cref="KeyedServiceAttribute"/> 时抛出。
    /// </exception>
    public static IReadOnlyList<ServiceScanDescriptor> ScanType(Type implementationType)
    {
        // Skip non-concrete types: interfaces, abstract classes, and open generics that
        // are not IsGenericTypeDefinition (i.e. partially constructed generics).
        if (implementationType.IsInterface || implementationType.IsAbstract)
        {
            return [];
        }

        // Allow only proper open generic definitions, not partially constructed ones.
        if (implementationType.ContainsGenericParameters && !implementationType.IsGenericTypeDefinition)
        {
            return [];
        }

        // [DisableAutoRegistration] → skip entirely.
        if (implementationType.IsDefined(typeof(DisableAutoRegistrationAttribute), inherit: true))
        {
            return [];
        }

        // Detect lifecycle markers.
        var allInterfaces = implementationType.GetInterfaces();
        var markers = allInterfaces.Where(i => LifecycleMarkers.Contains(i)).ToArray();

        if (markers.Length == 0)
        {
            // No lifecycle marker → not a candidate.
            return [];
        }

        if (markers.Length > 1)
        {
            throw new TwConfigurationException(
                $"类型 {implementationType.Name} 同时实现了多个生命周期标记接口（{string.Join(", ", markers.Select(m => m.Name))}），每个类型只能声明一个生命周期标记。");
        }

        var lifetime = ResolveLifetime(markers[0]);
        var isOpenGeneric = implementationType.IsGenericTypeDefinition;

        // Validate: open generic + [KeyedService] is unsupported.
        var keyedAttr = (KeyedServiceAttribute?)implementationType.GetCustomAttribute(typeof(KeyedServiceAttribute), inherit: true);
        if (isOpenGeneric && keyedAttr is not null)
        {
            throw new TwConfigurationException(
                $"开放泛型类型 {implementationType.Name} 不支持与 KeyedService 属性同时使用。开放泛型暂不支持命名键注册。");
        }

        var serviceTypes = ResolveServiceTypes(implementationType, allInterfaces);
        var collectionAttr = (CollectionServiceAttribute?)implementationType.GetCustomAttribute(typeof(CollectionServiceAttribute), inherit: true);
        var replaceAttr = (ReplaceServiceAttribute?)implementationType.GetCustomAttribute(typeof(ReplaceServiceAttribute), inherit: true);

        int order = 0;
        if (collectionAttr is not null)
        {
            order = collectionAttr.Order;
        }
        else if (replaceAttr is not null)
        {
            order = replaceAttr.Order;
        }

        var assemblyName = implementationType.Assembly.GetName().Name ?? string.Empty;

        var descriptor = new ServiceScanDescriptor(
            ImplementationType: implementationType,
            ServiceTypes: serviceTypes,
            Lifetime: lifetime,
            Key: keyedAttr?.Key,
            IsCollection: collectionAttr is not null,
            IsReplacement: replaceAttr is not null,
            Order: order,
            IsOpenGenericDefinition: isOpenGeneric,
            AssemblyName: assemblyName);

        return [descriptor];
    }

    // ---- private helpers ----

    private static ServiceLifetime ResolveLifetime(Type marker)
    {
        if (marker == TransientMarker) return ServiceLifetime.Transient;
        if (marker == ScopedMarker) return ServiceLifetime.Scoped;
        return ServiceLifetime.Singleton;
    }

    /// <summary>
    /// 按照曝光规则推导服务类型列表。
    /// 规则（按优先级）：
    /// 1. [ExposeServices] 显式声明时使用声明列表（多个属性合并去重）。
    /// 2. 否则取 GetInterfaces() 全集，过滤掉 System.*、Microsoft.* 命名空间及三个生命周期标记接口。
    /// 3. 结果为空时回退到实现类型本身。
    /// 4. IncludeSelf = true 时追加实现类型（去重）。
    /// </summary>
    private static IReadOnlyList<Type> ResolveServiceTypes(Type implementationType, Type[] allInterfaces)
    {
        var exposeAttrs = implementationType
            .GetCustomAttributes(typeof(ExposeServicesAttribute), inherit: true)
            .Cast<ExposeServicesAttribute>()
            .ToArray();

        bool includeSelf = exposeAttrs.Any(a => a.IncludeSelf);

        List<Type> serviceTypes;

        if (exposeAttrs.Length > 0)
        {
            // Explicit list: concatenate with deduplication preserving first occurrence.
            serviceTypes = [];
            var seen = new HashSet<Type>();
            foreach (var attr in exposeAttrs)
            {
                foreach (var t in attr.ServiceTypes)
                {
                    if (seen.Add(t))
                    {
                        serviceTypes.Add(t);
                    }
                }
            }
        }
        else
        {
            // Default: all interfaces minus System.*, Microsoft.*, and lifecycle markers.
            serviceTypes = allInterfaces
                .Where(i => !IsSystemOrMicrosoftNamespace(i) && !LifecycleMarkers.Contains(i))
                .ToList();

            // Fallback: expose implementation type itself when no business interface remains.
            if (serviceTypes.Count == 0)
            {
                serviceTypes.Add(implementationType);
            }
        }

        // IncludeSelf: append implementation type if not already present.
        if (includeSelf && !serviceTypes.Contains(implementationType))
        {
            serviceTypes.Add(implementationType);
        }

        return serviceTypes;
    }

    private static bool IsSystemOrMicrosoftNamespace(Type type)
    {
        var ns = type.Namespace;
        if (ns is null) return false;
        return ns.Equals("System", StringComparison.Ordinal)
            || ns.StartsWith("System.", StringComparison.Ordinal)
            || ns.Equals("Microsoft", StringComparison.Ordinal)
            || ns.StartsWith("Microsoft.", StringComparison.Ordinal);
    }

    private static IEnumerable<Type> GetLoadableOpenGenericTypes(Assembly assembly)
    {
        IEnumerable<Type?> types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t is not null);
        }

        return types
            .Where(t => t is not null && t.IsGenericTypeDefinition && !t.IsAbstract && !t.IsInterface)!;
    }
}
