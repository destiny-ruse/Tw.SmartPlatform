namespace Tw.Security.DataMasking;

/// <summary>
/// 对敏感数据执行脱敏
/// </summary>
public interface IDataMasker
{
    /// <summary>
    /// 按敏感数据类别返回脱敏后的展示值
    /// </summary>
    /// <param name="value">原始值</param>
    /// <param name="kind">敏感数据类别</param>
    /// <returns>脱敏后的展示值</returns>
    string Mask(string? value, SensitiveDataKind kind);
}
