namespace Tw.DependencyInjection.Discovery;

/// <summary>
/// 封装Assembly过滤器相关的数据和行为
/// </summary>
internal static class AssemblyFilter
{
    /// <summary>
    /// 当前类型内部复用的默认前缀常量值
    /// </summary>
    private const string DefaultPrefix = "Tw.";

    /// <summary>
    /// 说明过滤器在当前类型中的职责
    /// </summary>
    /// <param name="assemblyNames">用于提供assembly名称集合</param>
    /// <param name="options">用于配置当前组件行为的选项</param>
    /// <returns>方法计算得到的文本值</returns>
    public static IReadOnlyList<string> Filter(
        IEnumerable<string> assemblyNames, ServiceRegistrationOptions options)
    {
        ArgumentNullException.ThrowIfNull(assemblyNames);
        ArgumentNullException.ThrowIfNull(options);

        var included = new List<string>();
        foreach (var name in assemblyNames)
        {
            if (IsIncluded(name, options) && !IsExcluded(name, options))
            {
                included.Add(name);
            }
        }

        return included;
    }

    /// <summary>
    /// 判断ncluded是否满足条件
    /// </summary>
    /// <param name="name">待匹配成员或资源的名称</param>
    /// <param name="options">用于配置当前组件行为的选项</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
    private static bool IsIncluded(string name, ServiceRegistrationOptions options)
    {
        if (options.IncludeAssemblies.Contains(name, StringComparer.Ordinal))
        {
            return true;
        }

        if (name.StartsWith(DefaultPrefix, StringComparison.Ordinal))
        {
            return true;
        }

        foreach (var prefix in options.IncludeAssemblyPrefixes)
        {
            if (name.StartsWith(prefix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 判断Excluded是否满足条件
    /// </summary>
    /// <param name="name">待匹配成员或资源的名称</param>
    /// <param name="options">用于配置当前组件行为的选项</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
    private static bool IsExcluded(string name, ServiceRegistrationOptions options)
    {
        if (options.ExcludeAssemblies.Contains(name, StringComparer.Ordinal))
        {
            return true;
        }

        foreach (var prefix in options.ExcludeAssemblyPrefixes)
        {
            if (name.StartsWith(prefix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
