namespace Tw.Configuration.Abstractions;

/// <summary>
/// 为选项类型指定验证器类型
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class OptionsValidatorAttribute : Attribute
{
    /// <summary>声明验证器类型</summary>
    /// <param name="validatorType">实现 <c>IValidateOptions&lt;TOptions&gt;</c> 的验证器类型</param>
    public OptionsValidatorAttribute(Type validatorType)
    {
        ValidatorType = validatorType;
    }

    /// <summary>验证器类型</summary>
    public Type ValidatorType { get; }
}
