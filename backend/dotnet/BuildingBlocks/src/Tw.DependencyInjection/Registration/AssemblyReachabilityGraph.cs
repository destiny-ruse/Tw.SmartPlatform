namespace Tw.DependencyInjection.Registration;

/// <summary>
/// 程序集可达性图，基于引用关系判断两个程序集之间是否存在传递依赖路径
/// </summary>
internal sealed class AssemblyReachabilityGraph
{
    /// <summary>以程序集名为键的直接引用表</summary>
    private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _referencesByAssemblyName;

    /// <summary>
    /// 使用程序集直接引用表初始化 <see cref="AssemblyReachabilityGraph"/> 的新实例
    /// </summary>
    /// <param name="referencesByAssemblyName">key 为程序集名，value 为其直接引用的程序集名列表</param>
    public AssemblyReachabilityGraph(IReadOnlyDictionary<string, IReadOnlyList<string>> referencesByAssemblyName)
    {
        _referencesByAssemblyName = referencesByAssemblyName;
    }

    /// <summary>
    /// 判断 <paramref name="fromAssemblyName"/> 是否能通过引用关系（含传递）到达 <paramref name="toAssemblyName"/>
    /// </summary>
    /// <param name="fromAssemblyName">起始程序集名</param>
    /// <param name="toAssemblyName">目标程序集名</param>
    /// <returns>存在从 <paramref name="fromAssemblyName"/> 到 <paramref name="toAssemblyName"/> 的传递引用路径时返回 <see langword="true"/>，否则返回 <see langword="false"/></returns>
    /// <remarks>
    /// <para>这是有向可达查询：沿引用链传递判断，即若 A→B→C，则 <c>CanReach("A","C")</c> 为 <see langword="true"/>，但 <c>CanReach("C","A")</c> 为 <see langword="false"/>。</para>
    /// <para>当 <paramref name="fromAssemblyName"/> == <paramref name="toAssemblyName"/> 且该程序集未在引用表中显式引用自身时，返回 <see langword="false"/>。
    /// 这是有意约定：本方法只查询直接或传递引用路径，不隐含任何程序集自身可达。</para>
    /// <para>若 <paramref name="fromAssemblyName"/> 不存在于引用表中（未知程序集名），直接返回 <see langword="false"/>。</para>
    /// <para>内部使用 <c>visited</c> 集合记录已访问节点，确保有环图（如 A→B→A）下不会无限递归。</para>
    /// </remarks>
    public bool CanReach(string fromAssemblyName, string toAssemblyName)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        return Visit(fromAssemblyName);

        bool Visit(string current)
        {
            if (!visited.Add(current))
            {
                return false;
            }

            if (!_referencesByAssemblyName.TryGetValue(current, out var references))
            {
                return false;
            }

            foreach (var reference in references)
            {
                if (string.Equals(reference, toAssemblyName, StringComparison.Ordinal) || Visit(reference))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
