namespace Tw.Security.DataMasking;

/// <summary>
/// 识别输入值是否已经是脱敏展示值
/// </summary>
public interface ISensitiveValueDetector
{
    /// <summary>
    /// 判断值是否为指定敏感数据类别的脱敏展示值
    /// </summary>
    /// <param name="value">待检查值</param>
    /// <param name="kind">敏感数据类别</param>
    /// <returns>值已经脱敏时返回 true</returns>
    bool IsMaskedValue(string? value, SensitiveDataKind kind);
}
