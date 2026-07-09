namespace Tw.Security.DataMasking;

/// <summary>
/// 标记字段、属性或参数承载敏感数据
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class SensitiveDataAttribute : Attribute
{
    /// <summary>
    /// 创建敏感数据标记
    /// </summary>
    /// <param name="kind">敏感数据类别</param>
    public SensitiveDataAttribute(SensitiveDataKind kind)
    {
        Kind = kind;
    }

    /// <summary>
    /// 敏感数据类别
    /// </summary>
    public SensitiveDataKind Kind { get; }
}
