namespace Tw.Cli.Governance;

/// <summary>表示 ForbiddenPackageCatalog 类型</summary>
public static class ForbiddenPackageCatalog
{
    /// <summary>表示 Names 属性</summary>
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
