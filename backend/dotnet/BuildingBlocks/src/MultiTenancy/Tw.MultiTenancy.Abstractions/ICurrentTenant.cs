namespace Tw.MultiTenancy.Abstractions;

/// <summary>
/// 定义Current租户的能力边界
/// </summary>
public interface ICurrentTenant
{
    /// <summary>
    /// 值在当前对象中的业务含义
    /// </summary>
    CurrentTenant Value { get; }
}
