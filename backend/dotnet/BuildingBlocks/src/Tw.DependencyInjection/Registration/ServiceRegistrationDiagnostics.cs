namespace Tw.DependencyInjection.Registration;

/// <summary>
/// 计划阶段产出的诊断信息集合，包含拓扑覆盖、键控不匹配等非致命警告。
/// </summary>
public sealed class ServiceRegistrationDiagnostics
{
    private readonly List<string> _warnings;

    internal ServiceRegistrationDiagnostics(IEnumerable<string> warnings)
    {
        _warnings = [.. warnings];
    }

    /// <summary>
    /// 计划过程中产生的警告消息只读列表，按产生顺序排列。
    /// </summary>
    public IReadOnlyList<string> Warnings => _warnings;
}
