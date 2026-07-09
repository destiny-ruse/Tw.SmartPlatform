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

    /// <summary>执行 MaskEmail 操作</summary>
    /// <param name="value">value 参数</param>
    /// <returns>MaskEmail 的执行结果</returns>
    private static string MaskEmail(string value)
    {
        var atIndex = value.IndexOf('@', StringComparison.Ordinal);
        return atIndex <= 1 ? "***" : string.Concat(value.AsSpan(0, 1), "***", value.AsSpan(atIndex));
    }
}
