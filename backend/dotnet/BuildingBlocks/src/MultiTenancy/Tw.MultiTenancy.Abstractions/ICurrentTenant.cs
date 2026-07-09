namespace Tw.MultiTenancy.Abstractions;

/// <summary>定义 ICurrentTenant 契约</summary>
public interface ICurrentTenant
{
    /// <summary>表示 Value 属性</summary>
    CurrentTenant Value { get; }
}
