namespace Tw.MultiTenancy;

/// <summary>
/// 提供当前调用链已经解析的租户身份
/// </summary>
public interface ICurrentTenant
{
    /// <summary>
    /// 当前调用链使用的租户身份
    /// </summary>
    CurrentTenant Value { get; }
}
