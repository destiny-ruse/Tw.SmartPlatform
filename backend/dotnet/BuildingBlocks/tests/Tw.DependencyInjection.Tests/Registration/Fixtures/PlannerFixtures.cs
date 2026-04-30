using Microsoft.Extensions.DependencyInjection;
using Tw.DependencyInjection.Registration;
using Tw.DependencyInjection.Registration.Attributes;

// Planner-specific fixtures live in a sub-namespace to avoid conflicts with ScannerFixtures
// which already define IOrderService, IHandler, IPaymentService, etc. in the parent namespace.
namespace Tw.DependencyInjection.Tests.Registration.Fixtures.Planner;

// ---- domain service interfaces (planner-specific names to avoid collision) ----

public interface IOrderSvc { }
public interface IInventorySvc { }
public interface INotificationSvc { }
public interface IPaymentProc { }

// ---- fixture implementations ----

/// <summary>基础订单服务，作用域，无任何特殊属性。</summary>
public class BasicOrderService : IOrderSvc, IScopedDependency { }

/// <summary>第一订单服务实现，用于同层冲突测试。</summary>
public class FirstOrderService : IOrderSvc, IScopedDependency { }

/// <summary>第二订单服务实现，用于同层冲突测试。</summary>
public class SecondOrderService : IOrderSvc, IScopedDependency { }

/// <summary>来自上层程序集的叶子节点订单服务实现。</summary>
public class LeafOrderService : IOrderSvc, IScopedDependency { }

/// <summary>第一集合处理器，Order = 1。</summary>
[CollectionService(Order = 1)]
public class FirstCollectionHandler : INotificationSvc, IScopedDependency { }

/// <summary>第二集合处理器，Order = 2。</summary>
[CollectionService(Order = 2)]
public class SecondCollectionHandler : INotificationSvc, IScopedDependency { }

/// <summary>第三集合处理器，Order = 2（与 SecondCollectionHandler 相同，按类型全名排序）。</summary>
[CollectionService(Order = 2)]
public class ThirdCollectionHandler : INotificationSvc, IScopedDependency { }

/// <summary>非集合服务，与集合服务混合注册应抛出异常。</summary>
public class PlainNotificationService : INotificationSvc, IScopedDependency { }

/// <summary>带 [ReplaceService] 的替换服务实现，Order = 5。</summary>
[ReplaceService(Order = 5)]
public class ReplacingOrderService : IOrderSvc, IScopedDependency { }

/// <summary>带键 "stripe" 的支付处理器（无替换属性）。</summary>
public class StripePaymentProcessor : IPaymentProc, IScopedDependency { }

/// <summary>带键 "alipay" 的支付处理器（带替换属性，键不匹配 stripe）。</summary>
[ReplaceService]
public class AlipayPaymentProcessor : IPaymentProc, IScopedDependency { }

// ============================================================
// Descriptor builder helper (for building test descriptors without scanner)
// ============================================================

/// <summary>
/// 用于在测试中构造 <see cref="ServiceScanDescriptor"/> 的静态辅助类，
/// 无需依赖扫描器或真实程序集。
/// </summary>
public static class Descriptor
{
    /// <summary>
    /// 为指定的服务类型和实现类型创建最小描述符，默认 Scoped、无键、非集合、非替换。
    /// </summary>
    public static ServiceScanDescriptor For<TService, TImpl>(
        string? assemblyName = null,
        bool isCollection = false,
        bool isReplacement = false,
        int order = 0,
        object? key = null,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        => new(
            ImplementationType: typeof(TImpl),
            ServiceTypes: [typeof(TService)],
            Lifetime: lifetime,
            Key: key,
            IsCollection: isCollection,
            IsReplacement: isReplacement,
            Order: order,
            IsOpenGenericDefinition: false,
            AssemblyName: assemblyName ?? typeof(TImpl).Assembly.GetName().Name ?? string.Empty);

    /// <summary>
    /// 使用指定程序集名和服务类型创建描述符，适合拓扑测试场景。
    /// </summary>
    public static ServiceScanDescriptor ForAssembly<TService, TImpl>(
        string assemblyName,
        bool isCollection = false,
        bool isReplacement = false,
        int order = 0,
        object? key = null)
        => new(
            ImplementationType: typeof(TImpl),
            ServiceTypes: [typeof(TService)],
            Lifetime: ServiceLifetime.Scoped,
            Key: key,
            IsCollection: isCollection,
            IsReplacement: isReplacement,
            Order: order,
            IsOpenGenericDefinition: false,
            AssemblyName: assemblyName);
}
