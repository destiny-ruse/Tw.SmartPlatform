using AwesomeAssertions;
using Tw.DependencyInjection.Registration;
using Xunit;

namespace Tw.DependencyInjection.Tests.Registration;

/// <summary>
/// 覆盖AssemblyReachabilityGraph的核心行为和边界条件
/// </summary>
public class AssemblyReachabilityGraphTests
{
    // ──────────────────────────────────────────────────────────
    // 基础可达性
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// 验证ReachabilityGraphDetectsTransitive依赖路径
    /// </summary>
    [Fact]
    public void ReachabilityGraph_DetectsTransitiveDependencyPath()
    {
        var graph = new AssemblyReachabilityGraph(new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            ["Tw.App"] = ["Tw.Domain"],
            ["Tw.Domain"] = ["Tw.Core"],
            ["Tw.Core"] = [],
        });

        graph.CanReach("Tw.App", "Tw.Core").Should().BeTrue();
        graph.CanReach("Tw.Core", "Tw.App").Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────
    // 未知 from 名 → false
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// 验证CanReach返回false当FromAssemblyUnknown
    /// </summary>
    [Fact]
    public void CanReach_ReturnsFalse_WhenFromAssemblyUnknown()
    {
        var graph = new AssemblyReachabilityGraph(new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            ["Tw.Core"] = [],
        });

        // "Tw.Unknown" 不在图中，应直接返回 false
        graph.CanReach("Tw.Unknown", "Tw.Core").Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────
    // 有环图：不死循环，且可达判断正确
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// 验证CanReach不InfiniteLoop当GraphHasCycle
    /// </summary>
    [Fact]
    public void CanReach_DoesNotInfiniteLoop_WhenGraphHasCycle()
    {
        // A→B, B→A 形成环
        var graph = new AssemblyReachabilityGraph(new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            ["A"] = ["B"],
            ["B"] = ["A"],
        });

        // 对不存在的目标应正常返回 false，不应死循环
        graph.CanReach("A", "SomethingMissing").Should().BeFalse();

        // A 直接引用 B，应可达
        graph.CanReach("A", "B").Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────
    // from == to 且无自引用 → false（锁定当前约定）
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// 验证CanReach返回false当FromEquals到和NoSelf引用
    /// </summary>
    [Fact]
    public void CanReach_ReturnsFalse_WhenFromEqualsToAndNoSelfReference()
    {
        // A→B，B→[]，均无自引用
        var graph = new AssemblyReachabilityGraph(new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            ["A"] = ["B"],
            ["B"] = [],
        });

        // 当前约定：CanReach 不隐含自身可达，返回 false
        graph.CanReach("A", "A").Should().BeFalse();
    }
}
