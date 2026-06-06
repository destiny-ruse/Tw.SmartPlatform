namespace Tw.DependencyInjection;

/// <summary>
/// 控制程序集发现与注册规划的引擎选项，对应配置节 <c>Tw:DependencyInjection</c>
/// </summary>
public sealed class ServiceRegistrationOptions
{
    /// <summary>额外纳入扫描的程序集名（精确匹配），在内置 <c>Tw.</c> 前缀之外补充白名单</summary>
    public IList<string> IncludeAssemblies { get; } = new List<string>();

    /// <summary>排除出扫描的程序集名（精确匹配），优先于任何白名单</summary>
    public IList<string> ExcludeAssemblies { get; } = new List<string>();

    /// <summary>额外纳入扫描的程序集名前缀，叠加在内置 <c>Tw.</c> 前缀之上</summary>
    public IList<string> IncludeAssemblyPrefixes { get; } = new List<string>();

    /// <summary>排除出扫描的程序集名前缀，优先于任何白名单</summary>
    public IList<string> ExcludeAssemblyPrefixes { get; } = new List<string>();
}
