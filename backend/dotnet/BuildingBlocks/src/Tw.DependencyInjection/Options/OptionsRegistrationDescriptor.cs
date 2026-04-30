using System.Reflection;

namespace Tw.DependencyInjection.Options;

/// <summary>
/// 描述单次选项注册所需的全部元数据。由扫描阶段产出，供注册阶段消费。
/// </summary>
/// <param name="OptionsType">选项类型。</param>
/// <param name="SectionName">配置节路径，如 <c>Redis</c> 或 <c>Redis:Primary</c>。</param>
/// <param name="OptionsName">Microsoft Options 命名实例名称；为 <see langword="null"/> 时表示默认实例。</param>
/// <param name="ValidateOnStart">是否在主机启动时验证；为 <see langword="null"/> 时由全局策略决定（默认 <see langword="true"/>）。</param>
/// <param name="DirectInject">是否将选项类型本身注册为可直接解析的服务。</param>
/// <param name="AssemblyName">选项类型所在程序集的简单名称。</param>
public sealed record OptionsRegistrationDescriptor(
    Type OptionsType,
    string SectionName,
    string? OptionsName,
    bool? ValidateOnStart,
    bool DirectInject,
    string AssemblyName);
