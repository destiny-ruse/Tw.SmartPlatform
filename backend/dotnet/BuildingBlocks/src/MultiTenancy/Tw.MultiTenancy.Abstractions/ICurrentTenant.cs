namespace Tw.MultiTenancy.Abstractions;

public interface ICurrentTenant
{
    CurrentTenant Value { get; }
}
