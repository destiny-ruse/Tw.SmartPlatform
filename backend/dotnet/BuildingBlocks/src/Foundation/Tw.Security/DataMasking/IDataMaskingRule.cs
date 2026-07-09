namespace Tw.Security.DataMasking;

/// <summary>
/// 单个敏感数据类别的脱敏规则
/// </summary>
public interface IDataMaskingRule
{
    /// <summary>
    /// 判断当前规则是否支持指定敏感数据类别
    /// </summary>
    /// <param name="kind">敏感数据类别</param>
    /// <returns>支持该类别时返回 true</returns>
    bool CanMask(SensitiveDataKind kind);

    /// <summary>
    /// 执行脱敏
    /// </summary>
    /// <param name="value">原始值</param>
    /// <returns>脱敏后的展示值</returns>
    string Mask(string? value);
}
