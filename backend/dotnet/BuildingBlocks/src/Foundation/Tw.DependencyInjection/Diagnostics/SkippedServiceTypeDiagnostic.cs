namespace Tw.DependencyInjection.Diagnostics;

/// <summary>
/// 扫描到但未参与普通服务注册的类型诊断项
/// </summary>
/// <param name="TypeName">被跳过的类型全名</param>
/// <param name="Reason">跳过原因描述</param>
public sealed record SkippedServiceTypeDiagnostic(
    string TypeName,
    string Reason);
