namespace Tw.Security.DataMasking;

/// <summary>
/// 默认敏感数据脱敏器
/// </summary>
public sealed class DefaultDataMasker : IDataMasker, ISensitiveValueDetector
{
    /// <summary>
    /// 创建默认敏感数据脱敏器
    /// </summary>
    /// <returns>默认敏感数据脱敏器</returns>
    public static DefaultDataMasker CreateDefault()
    {
        return new DefaultDataMasker();
    }

    /// <inheritdoc />
    public string Mask(string? value, SensitiveDataKind kind)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return kind switch
        {
            SensitiveDataKind.PhoneNumber when value.Length >= 11 =>
                string.Concat(value.AsSpan(0, 3), "****", value.AsSpan(value.Length - 4, 4)),
            SensitiveDataKind.Email => MaskEmail(value),
            SensitiveDataKind.IdentityNumber when value.Length >= 8 =>
                string.Concat(value.AsSpan(0, 3), "********", value.AsSpan(value.Length - 4, 4)),
            _ => "***",
        };
    }

    /// <inheritdoc />
    public bool IsMaskedValue(string? value, SensitiveDataKind kind)
    {
        return !string.IsNullOrWhiteSpace(value) && value.Contains('*', StringComparison.Ordinal);
    }

    /// <summary>
    /// 说明MaskEmail在当前类型中的职责
    /// </summary>
    /// <param name="value">用于转换、回显或断言的输入值</param>
    /// <returns>方法计算得到的文本值</returns>
    private static string MaskEmail(string value)
    {
        var atIndex = value.IndexOf('@', StringComparison.Ordinal);
        return atIndex <= 1 ? "***" : string.Concat(value.AsSpan(0, 1), "***", value.AsSpan(atIndex));
    }
}
