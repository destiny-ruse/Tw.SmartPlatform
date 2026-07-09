namespace Tw.Cli.Governance;

public static class ForbiddenPackageCatalog
{
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
