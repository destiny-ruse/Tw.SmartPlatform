namespace Tw.Security.DataMasking;

/// <summary>
/// 防止脱敏展示值被写回敏感字段
/// </summary>
public sealed class MaskWriteBackGuard
{
    /// <summary>
    /// 保存当前类型处理流程依赖的detector
    /// </summary>
    private readonly ISensitiveValueDetector _detector;

    /// <summary>
    /// 创建脱敏写回保护器
    /// </summary>
    /// <param name="detector">脱敏值检测器</param>
    /// <exception cref="ArgumentNullException">detector 为 null 时抛出</exception>
    public MaskWriteBackGuard(ISensitiveValueDetector detector)
    {
        _detector = detector ?? throw new ArgumentNullException(nameof(detector));
    }

    /// <summary>
    /// 确保输入值不是脱敏展示值
    /// </summary>
    /// <param name="value">待写回值</param>
    /// <param name="kind">敏感数据类别</param>
    /// <exception cref="MaskedValueWriteBackException">value 是脱敏展示值时抛出</exception>
    public void EnsureNotMaskedValue(string? value, SensitiveDataKind kind)
    {
        if (_detector.IsMaskedValue(value, kind))
        {
            throw new MaskedValueWriteBackException();
        }
    }
}
