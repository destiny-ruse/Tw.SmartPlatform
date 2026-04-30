namespace Tw.DependencyInjection.Registration;

/// <summary>
/// 计划阶段的最终产出，包含已决策的服务注册列表与诊断警告信息。
/// </summary>
public sealed class ServiceRegistrationPlan
{
    internal ServiceRegistrationPlan(
        IEnumerable<ServiceRegistrationDescriptor> registrations,
        ServiceRegistrationDiagnostics diagnostics)
    {
        Registrations = [.. registrations];
        Diagnostics = diagnostics;
    }

    /// <summary>
    /// 经过冲突裁决后最终确定的服务注册描述符只读列表。
    /// </summary>
    public IReadOnlyList<ServiceRegistrationDescriptor> Registrations { get; }

    /// <summary>
    /// 计划阶段产生的诊断信息，包含非致命警告。
    /// </summary>
    public ServiceRegistrationDiagnostics Diagnostics { get; }
}
