namespace Tw.DependencyInjection.Configuration;

/// <summary>
/// Options 自动装载候选项
/// </summary>
/// <param name="OptionsType">Options 类型</param>
/// <param name="SectionPath">配置节路径</param>
/// <param name="Name">Options 命名实例名称</param>
/// <param name="SectionExists">配置节是否存在</param>
/// <param name="IsSensitive">是否带有敏感配置标记</param>
/// <param name="ValidatorType">显式校验器类型</param>
internal sealed record OptionsBindingCandidate(
    Type OptionsType,
    string SectionPath,
    string Name,
    bool SectionExists,
    bool IsSensitive,
    Type? ValidatorType);
