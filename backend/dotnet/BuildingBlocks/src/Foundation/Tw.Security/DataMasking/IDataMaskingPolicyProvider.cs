namespace Tw.Security.DataMasking;

/// <summary>
/// 提供敏感数据脱敏规则集合
/// </summary>
public interface IDataMaskingPolicyProvider
{
    /// <summary>
    /// 获取脱敏规则集合
    /// </summary>
    /// <returns>脱敏规则集合</returns>
    IReadOnlyList<IDataMaskingRule> GetRules();
}
