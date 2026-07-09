namespace Tw.DependencyInjection.Abstractions.Configuration;

/// <summary>
/// 标记配置整类或单个属性为敏感，诊断报告不输出其值
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class SensitiveConfigurationAttribute : Attribute;
