namespace Tw.Security.DataMasking;

/// <summary>
/// 表示检测到把脱敏展示值写回敏感字段的异常
/// </summary>
public sealed class MaskedValueWriteBackException : Exception
{
    /// <summary>
    /// 创建脱敏值写回异常
    /// </summary>
    public MaskedValueWriteBackException()
        : base("不能把脱敏值写回敏感字段")
    {
    }
}
