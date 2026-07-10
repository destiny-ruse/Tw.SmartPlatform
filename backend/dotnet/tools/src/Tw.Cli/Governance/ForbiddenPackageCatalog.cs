namespace Tw.Cli.Governance;

/// <summary>
/// 维护禁止Package目录
/// </summary>
public static class ForbiddenPackageCatalog
{
    /// <summary>
    /// Hash写入在当前对象中的业务含义
    /// </summary>
    public static IReadOnlySet<string> Names { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Tw.Infrastructure",
        "Tw.UnitOfWork",
        "Tw.Data.Abstractions",
        "MassTransit",
        "Tw.ObjectMapping",
        "Tw.ObjectMapping.AutoMapper"
    };
}
