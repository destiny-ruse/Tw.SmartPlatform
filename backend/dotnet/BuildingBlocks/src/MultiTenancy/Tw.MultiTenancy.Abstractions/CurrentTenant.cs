namespace Tw.MultiTenancy.Abstractions;

public sealed record CurrentTenant(string Id)
{
    public static CurrentTenant Default { get; } = new("default");
}
