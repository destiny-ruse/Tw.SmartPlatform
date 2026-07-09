using Tw.DependencyInjection.Diagnostics;

namespace Tw.DependencyInjection.Registration;

/// <summary>
/// 注册规划器（<see cref="ServiceRegistrationPlanner"/>）的规划结果，包含最终选中的候选列表与完整诊断报告
/// </summary>
/// <param name="Registrations">经过单实现仲裁后最终选中的服务候选列表，每项对应一次容器注册</param>
/// <param name="Report">含候选、仲裁、跳过、冲突等完整信息的诊断报告</param>
internal sealed record ServiceRegistrationPlan(
    IReadOnlyList<ServiceCandidate> Registrations,
    ServiceRegistrationReport Report);
