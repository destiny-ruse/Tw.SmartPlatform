namespace Tw.Castle.Core;

/// <summary>
/// 方法级 AOP 拦截承载诊断项
/// </summary>
/// <param name="ServiceTypeName">服务契约类型全名</param>
/// <param name="ImplementationTypeName">服务实现类型全名</param>
/// <param name="MethodName">被诊断的方法名称</param>
/// <param name="Carrier">拦截承载方式标识，使用稳定字符串区分 Castle 接口代理等代理机制</param>
/// <param name="InterceptorTypeNames">参与该方法拦截的拦截器类型全名列表</param>
/// <param name="Status">拦截状态标识，使用稳定小写字符串表达启用、禁用或跳过等状态</param>
/// <param name="Reason">状态不是启用时的诊断原因；无原因时为 null</param>
public sealed record InterceptionDiagnostic(
    string ServiceTypeName,
    string ImplementationTypeName,
    string MethodName,
    string Carrier,
    IReadOnlyList<string> InterceptorTypeNames,
    string Status,
    string? Reason);
