namespace Tw.DependencyInjection.Abstractions.Configuration;

/// <summary>
/// 为选项类型指定验证器类型
/// </summary>
/// <remarks>
/// 本特性与 <c>Microsoft.Extensions.Options.OptionsValidatorAttribute</c>（源生成器特性）同名，
/// 命名空间不同，不构成冲突。同一文件同时引用两者时，请使用命名空间限定或 <c>using alias</c> 区分。
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class OptionsValidatorAttribute : Attribute
{
    /// <summary>
    /// 声明验证器类型
    /// </summary>
    /// <param name="validatorType">实现 <c>IValidateOptions&lt;TOptions&gt;</c> 的验证器类型</param>
    public OptionsValidatorAttribute(Type validatorType)
    {
        ValidatorType = validatorType;
    }

    /// <summary>
    /// 验证器类型
    /// </summary>
    public Type ValidatorType { get; }
}
