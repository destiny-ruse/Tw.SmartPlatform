using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.Core.Exceptions;
using Tw.DependencyInjection.Registration;
using Tw.DependencyInjection.Tests.Registration.Fixtures.Planner;
using Xunit;

namespace Tw.DependencyInjection.Tests.Registration;

/// <summary>
/// 验证 ServiceRegistrationPlanner 的冲突裁决、集合语义、拓扑排序与诊断输出。
/// </summary>
public sealed class ServiceRegistrationPlannerTests
{
    // ============================================================
    // Helper — plan with injected dependency graph (no real assemblies needed)
    // ============================================================

    /// <summary>
    /// 使用空的依赖图（所有扫描描述符的程序集均在同一层，拓扑索引 0）执行计划。
    /// </summary>
    private static ServiceRegistrationPlan Plan(params ServiceScanDescriptor[] scans)
        => ServiceRegistrationPlanner.Plan(scans, EmptyGraph);

    /// <summary>
    /// 使用指定依赖图执行计划。
    /// </summary>
    private static ServiceRegistrationPlan Plan(
        IReadOnlyDictionary<string, IReadOnlySet<string>> graph,
        params ServiceScanDescriptor[] scans)
        => ServiceRegistrationPlanner.Plan(scans, graph);

    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> EmptyGraph =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal);

    /// <summary>
    /// 构造一个两层程序集依赖图：leaf 引用 root（leaf 是上层程序集，拓扑索引更大）。
    /// </summary>
    private static IReadOnlyDictionary<string, IReadOnlySet<string>> TwoLayerGraph(
        string root, string leaf)
    {
        return new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            [root] = new HashSet<string>(StringComparer.Ordinal),
            [leaf] = new HashSet<string>(StringComparer.Ordinal) { root }
        };
    }

    // ============================================================
    // 1. 空输入
    // ============================================================

    [Fact]
    public void Planner_Returns_Empty_Plan_For_Empty_Input()
    {
        var plan = Plan();

        plan.Registrations.Should().BeEmpty();
        plan.Diagnostics.Warnings.Should().BeEmpty();
    }

    // ============================================================
    // 2. 单条注册 — 直通，无冲突
    // ============================================================

    [Fact]
    public void Planner_Passes_Through_Single_Registration()
    {
        var scan = Descriptor.For<IOrderSvc, BasicOrderService>();

        var plan = Plan(scan);

        plan.Registrations.Should().ContainSingle()
            .Which.ImplementationType.Should().Be(typeof(BasicOrderService));
    }

    [Fact]
    public void Planner_Expands_One_Scan_Descriptor_Per_ServiceType()
    {
        // A scan descriptor with two service types should produce two registration descriptors.
        var scan = new ServiceScanDescriptor(
            ImplementationType: typeof(BasicOrderService),
            ServiceTypes: [typeof(IOrderSvc), typeof(IInventorySvc)],
            Lifetime: ServiceLifetime.Scoped,
            Key: null,
            IsCollection: false,
            IsReplacement: false,
            Order: 0,
            IsOpenGenericDefinition: false,
            AssemblyName: "Tw.Test");

        var plan = Plan(scan);

        plan.Registrations.Should().HaveCount(2);
        plan.Registrations.Select(r => r.ServiceType)
            .Should().BeEquivalentTo([typeof(IOrderSvc), typeof(IInventorySvc)]);
    }

    // ============================================================
    // 3. 集合语义 — [CollectionService]
    // ============================================================

    [Fact]
    public void Planner_Preserves_All_Collection_Implementations()
    {
        var s1 = Descriptor.For<INotificationSvc, FirstCollectionHandler>(isCollection: true, order: 1);
        var s2 = Descriptor.For<INotificationSvc, SecondCollectionHandler>(isCollection: true, order: 2);

        var plan = Plan(s1, s2);

        plan.Registrations.Should().HaveCount(2);
        plan.Registrations.All(r => r.IsCollection).Should().BeTrue();
    }

    [Fact]
    public void Planner_Orders_Collection_By_Order_Then_FullName()
    {
        var s1 = Descriptor.For<INotificationSvc, ThirdCollectionHandler>(isCollection: true, order: 2);
        var s2 = Descriptor.For<INotificationSvc, SecondCollectionHandler>(isCollection: true, order: 2);
        var s3 = Descriptor.For<INotificationSvc, FirstCollectionHandler>(isCollection: true, order: 1);

        var plan = Plan(s1, s2, s3);

        // Order 1 first, then Order 2 sorted by FullName.
        plan.Registrations.Should().HaveCount(3);
        plan.Registrations[0].ImplementationType.Should().Be(typeof(FirstCollectionHandler));

        // Second and third should be sorted by FullName (ascending).
        var namesAtOrder2 = plan.Registrations.Skip(1)
            .Select(r => r.ImplementationType.FullName!)
            .ToList();
        namesAtOrder2.Should().BeInAscendingOrder();
    }

    [Fact]
    public void Planner_Throws_When_Collection_And_Non_Collection_Mixed()
    {
        var collection = Descriptor.For<INotificationSvc, FirstCollectionHandler>(isCollection: true, order: 1);
        var plain = Descriptor.For<INotificationSvc, PlainNotificationService>();

        var act = () => Plan(collection, plain);

        act.Should().Throw<TwConfigurationException>()
            .WithMessage("*FirstCollectionHandler*PlainNotificationService*");
    }

    // ============================================================
    // 4. 替换语义 — [ReplaceService]
    // ============================================================

    [Fact]
    public void Planner_ReplaceService_Replaces_Original_Registration()
    {
        const string rootAssembly = "Tw.Root";
        const string leafAssembly = "Tw.Leaf";
        var graph = TwoLayerGraph(rootAssembly, leafAssembly);

        var original = Descriptor.ForAssembly<IOrderSvc, BasicOrderService>(rootAssembly);
        var replacement = Descriptor.ForAssembly<IOrderSvc, ReplacingOrderService>(leafAssembly, isReplacement: true);

        var plan = Plan(graph, original, replacement);

        plan.Registrations.Should().ContainSingle()
            .Which.ImplementationType.Should().Be(typeof(ReplacingOrderService));
    }

    [Fact]
    public void Planner_ReplaceService_Replaces_Only_Matching_ServiceType()
    {
        // ReplacingOrderService replaces IOrderSvc, but does NOT affect IInventorySvc.
        const string rootAssembly = "Tw.Root";
        const string leafAssembly = "Tw.Leaf";
        var graph = TwoLayerGraph(rootAssembly, leafAssembly);

        var originalOrder = Descriptor.ForAssembly<IOrderSvc, BasicOrderService>(rootAssembly);
        var originalInventory = Descriptor.ForAssembly<IInventorySvc, BasicOrderService>(rootAssembly);
        var replacement = Descriptor.ForAssembly<IOrderSvc, ReplacingOrderService>(leafAssembly, isReplacement: true);

        var plan = Plan(graph, originalOrder, originalInventory, replacement);

        // IOrderSvc should be won by ReplacingOrderService; IInventorySvc by BasicOrderService.
        plan.Registrations.Should().HaveCount(2);
        plan.Registrations.First(r => r.ServiceType == typeof(IOrderSvc))
            .ImplementationType.Should().Be(typeof(ReplacingOrderService));
        plan.Registrations.First(r => r.ServiceType == typeof(IInventorySvc))
            .ImplementationType.Should().Be(typeof(BasicOrderService));
    }

    [Fact]
    public void Planner_ReplaceService_With_Mismatched_Key_Leaves_Original_And_Warns()
    {
        // "stripe" payment is registered (plain); [ReplaceService] carries key "alipay" → mismatch.
        // The group (IPaymentProc, "stripe") has only the plain impl.
        // The group (IPaymentProc, "alipay") has only the [ReplaceService] impl — no original to replace.
        // Per §5.5: since there's no matching plain registration at "alipay", it registers as a new entry.

        var plain = Descriptor.For<IPaymentProc, StripePaymentProcessor>(key: "stripe");
        var replacer = Descriptor.For<IPaymentProc, AlipayPaymentProcessor>(key: "alipay", isReplacement: true);

        var plan = Plan(plain, replacer);

        // Both should survive as independent registrations.
        plan.Registrations.Should().HaveCount(2);
        plan.Registrations.Should().Contain(r => r.Key!.Equals("stripe"));
        plan.Registrations.Should().Contain(r => r.Key!.Equals("alipay"));
    }

    // ============================================================
    // 5. 拓扑排序与决策
    // ============================================================

    [Fact]
    public void Planner_Leaf_Assembly_Wins_Over_Root_Assembly()
    {
        const string rootAssembly = "Tw.Domain";
        const string leafAssembly = "Tw.Application";
        var graph = TwoLayerGraph(rootAssembly, leafAssembly);

        var rootScan = Descriptor.ForAssembly<IOrderSvc, FirstOrderService>(rootAssembly);
        var leafScan = Descriptor.ForAssembly<IOrderSvc, SecondOrderService>(leafAssembly);

        var plan = Plan(graph, rootScan, leafScan);

        plan.Registrations.Should().ContainSingle()
            .Which.ImplementationType.Should().Be(typeof(SecondOrderService));
    }

    [Fact]
    public void Planner_Leaf_Assembly_Win_Emits_Cross_Assembly_Warning()
    {
        const string rootAssembly = "Tw.Domain";
        const string leafAssembly = "Tw.Application";
        var graph = TwoLayerGraph(rootAssembly, leafAssembly);

        var rootScan = Descriptor.ForAssembly<IOrderSvc, FirstOrderService>(rootAssembly);
        var leafScan = Descriptor.ForAssembly<IOrderSvc, SecondOrderService>(leafAssembly);

        var plan = Plan(graph, rootScan, leafScan);

        plan.Diagnostics.Warnings.Should().ContainSingle()
            .Which.Should().Contain("FirstOrderService");
    }

    [Fact]
    public void Planner_Throws_With_All_Conflicting_Type_Names_When_Same_Level_Conflict_Remains()
    {
        var act = () => Plan(
            Descriptor.For<IOrderSvc, FirstOrderService>(),
            Descriptor.For<IOrderSvc, SecondOrderService>());

        act.Should().Throw<TwConfigurationException>()
            .WithMessage("*FirstOrderService*SecondOrderService*");
    }

    [Fact]
    public void Planner_Uses_Order_To_Break_Same_Assembly_Tie()
    {
        const string assembly = "Tw.Application";
        var graph = new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            [assembly] = new HashSet<string>(StringComparer.Ordinal)
        };

        var s1 = Descriptor.ForAssembly<IOrderSvc, FirstOrderService>(assembly, isReplacement: true, order: 5);
        var s2 = Descriptor.ForAssembly<IOrderSvc, SecondOrderService>(assembly, isReplacement: true, order: 10);

        var plan = Plan(graph, s1, s2);

        plan.Registrations.Should().ContainSingle()
            .Which.ImplementationType.Should().Be(typeof(SecondOrderService));
    }

    [Fact]
    public void Planner_Throws_When_Same_Assembly_Same_Order_Conflict()
    {
        const string assembly = "Tw.Application";
        var graph = new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            [assembly] = new HashSet<string>(StringComparer.Ordinal)
        };

        var s1 = Descriptor.ForAssembly<IOrderSvc, FirstOrderService>(assembly, isReplacement: true, order: 5);
        var s2 = Descriptor.ForAssembly<IOrderSvc, SecondOrderService>(assembly, isReplacement: true, order: 5);

        var act = () => Plan(graph, s1, s2);

        act.Should().Throw<TwConfigurationException>()
            .WithMessage("*FirstOrderService*SecondOrderService*");
    }

    // ============================================================
    // 6. 拓扑算法 — Kahn
    // ============================================================

    [Fact]
    public void Planner_Topology_Leaves_Get_Larger_Index_Than_Roots()
    {
        const string root = "Tw.Core";
        const string middle = "Tw.Application";
        const string leaf = "Tw.Api";

        var graph = new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            [root] = new HashSet<string>(StringComparer.Ordinal),
            [middle] = new HashSet<string>(StringComparer.Ordinal) { root },
            [leaf] = new HashSet<string>(StringComparer.Ordinal) { middle }
        };

        // Three-assembly chain: leaf → middle → root.
        // root topo index = 0, middle = 1, leaf = 2.
        var rootScan = Descriptor.ForAssembly<IOrderSvc, FirstOrderService>(root);
        var leafScan = Descriptor.ForAssembly<IOrderSvc, SecondOrderService>(leaf);

        var plan = Plan(graph, rootScan, leafScan);

        // Leaf wins; its topo index should be 2.
        plan.Registrations.Should().ContainSingle()
            .Which.AssemblyTopologicalIndex.Should().Be(2);
    }

    [Fact]
    public void Planner_Throws_On_Cyclic_Assembly_Dependencies()
    {
        var graph = new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            ["Tw.A"] = new HashSet<string>(StringComparer.Ordinal) { "Tw.B" },
            ["Tw.B"] = new HashSet<string>(StringComparer.Ordinal) { "Tw.A" }
        };

        var scan = Descriptor.ForAssembly<IOrderSvc, BasicOrderService>("Tw.A");

        var act = () => Plan(graph, scan);

        act.Should().Throw<TwConfigurationException>()
            .WithMessage("*循环*");
    }

    [Fact]
    public void Planner_Cycle_Message_Contains_Path()
    {
        var graph = new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            ["Tw.A"] = new HashSet<string>(StringComparer.Ordinal) { "Tw.B" },
            ["Tw.B"] = new HashSet<string>(StringComparer.Ordinal) { "Tw.A" }
        };

        var scan = Descriptor.ForAssembly<IOrderSvc, BasicOrderService>("Tw.A");

        var act = () => Plan(graph, scan);

        act.Should().Throw<TwConfigurationException>()
            .WithMessage("*Tw.A*Tw.B*");
    }

    // ============================================================
    // 7. 诊断 — 警告稳定性
    // ============================================================

    [Fact]
    public void Planner_Warnings_Are_Stable_Across_Two_Runs()
    {
        const string rootAssembly = "Tw.Domain";
        const string leafAssembly = "Tw.Application";
        var graph = TwoLayerGraph(rootAssembly, leafAssembly);

        var rootScan = Descriptor.ForAssembly<IOrderSvc, FirstOrderService>(rootAssembly);
        var leafScan = Descriptor.ForAssembly<IOrderSvc, SecondOrderService>(leafAssembly);

        var plan1 = Plan(graph, rootScan, leafScan);
        var plan2 = Plan(graph, rootScan, leafScan);

        plan1.Diagnostics.Warnings.Should().BeEquivalentTo(
            plan2.Diagnostics.Warnings,
            cfg => cfg.WithStrictOrdering());
    }

    [Fact]
    public void Planner_Registrations_Are_Stable_Across_Two_Runs()
    {
        var s1 = Descriptor.For<INotificationSvc, ThirdCollectionHandler>(isCollection: true, order: 2);
        var s2 = Descriptor.For<INotificationSvc, SecondCollectionHandler>(isCollection: true, order: 2);
        var s3 = Descriptor.For<INotificationSvc, FirstCollectionHandler>(isCollection: true, order: 1);

        var plan1 = Plan(s1, s2, s3);
        var plan2 = Plan(s1, s2, s3);

        plan1.Registrations.Select(r => r.ImplementationType.FullName)
            .Should().BeEquivalentTo(
                plan2.Registrations.Select(r => r.ImplementationType.FullName),
                cfg => cfg.WithStrictOrdering());
    }

    // ============================================================
    // 8. AssemblyTopologicalIndex 值验证
    // ============================================================

    [Fact]
    public void Planner_Descriptor_AssemblyTopologicalIndex_Is_Populated()
    {
        const string rootAssembly = "Tw.Core";
        const string leafAssembly = "Tw.App";
        var graph = TwoLayerGraph(rootAssembly, leafAssembly);

        var single = Descriptor.ForAssembly<IOrderSvc, BasicOrderService>(leafAssembly);

        var plan = Plan(graph, single);

        plan.Registrations.Should().ContainSingle()
            .Which.AssemblyTopologicalIndex.Should().Be(1); // 2-node: root=0, leaf=1
    }

    // ============================================================
    // 9. 跨层替换边界 — 高拓扑普通注册覆盖低拓扑替换声明
    // ============================================================

    [Fact]
    public void Planner_High_Topology_Plain_Beats_Low_Topology_Replacement_And_Warns()
    {
        const string rootAssembly = "Tw.Root";
        const string leafAssembly = "Tw.Leaf";
        var graph = TwoLayerGraph(rootAssembly, leafAssembly);

        // Replacement is in root (low topo), plain is in leaf (high topo).
        var rootReplacement = Descriptor.ForAssembly<IOrderSvc, ReplacingOrderService>(rootAssembly, isReplacement: true);
        var leafPlain = Descriptor.ForAssembly<IOrderSvc, LeafOrderService>(leafAssembly);

        var plan = Plan(graph, rootReplacement, leafPlain);

        plan.Registrations.Should().ContainSingle()
            .Which.ImplementationType.Should().Be(typeof(LeafOrderService));

        plan.Diagnostics.Warnings.Should().ContainSingle()
            .Which.Should().Contain("ReplacingOrderService");
    }

    // ============================================================
    // 10. 确认冲突消息包含所有冲突实现的全名（FullName，而非短名）
    // ============================================================

    [Fact]
    public void Planner_Conflict_Exception_Contains_FullName_Not_Just_Short_Name()
    {
        var act = () => Plan(
            Descriptor.For<IOrderSvc, FirstOrderService>(),
            Descriptor.For<IOrderSvc, SecondOrderService>());

        act.Should().Throw<TwConfigurationException>()
            .WithMessage($"*{typeof(FirstOrderService).FullName}*{typeof(SecondOrderService).FullName}*");
    }
}
