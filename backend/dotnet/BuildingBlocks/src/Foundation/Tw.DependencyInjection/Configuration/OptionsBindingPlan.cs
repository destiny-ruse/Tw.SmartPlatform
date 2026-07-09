using Tw.DependencyInjection.Diagnostics;

namespace Tw.DependencyInjection.Configuration;

/// <summary>
/// Options 自动装载规划结果
/// </summary>
/// <param name="Candidates">Options 绑定候选</param>
/// <param name="Report">Options 绑定诊断报告</param>
internal sealed record OptionsBindingPlan(
    IReadOnlyList<OptionsBindingCandidate> Candidates,
    OptionsBindingReport Report);
