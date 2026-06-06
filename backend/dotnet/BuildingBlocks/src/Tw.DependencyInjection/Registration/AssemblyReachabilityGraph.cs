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
