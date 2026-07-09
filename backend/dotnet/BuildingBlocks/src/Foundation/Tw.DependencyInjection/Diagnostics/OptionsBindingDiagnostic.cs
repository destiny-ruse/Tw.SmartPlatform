namespace Tw.DependencyInjection.Diagnostics;

/// <summary>
/// 单个 Options 类型的绑定诊断项
/// </summary>
/// <param name="OptionsTypeName">Options 类型全名</param>
/// <param name="SectionPath">绑定的配置节路径</param>
/// <param name="Name">Options 命名实例名称</param>
/// <param name="SectionExists">配置节是否存在</param>
/// <param name="BindingStatus">绑定状态</param>
/// <param name="ValidationStatus">校验状态</param>
/// <param name="IsSensitive">是否带有敏感配置标记</param>
public sealed record OptionsBindingDiagnostic(
    string OptionsTypeName,
    string SectionPath,
    string Name,
    bool SectionExists,
    string BindingStatus,
    string ValidationStatus,
    bool IsSensitive);
