namespace Tw.DependencyInjection.Interception;

/// <summary>
/// 拦截器适用的调用边界
/// </summary>
public enum InterceptorScope
{
    /// <summary>
    /// 通过容器解析的服务方法调用
    /// </summary>
    Service = 0,

    /// <summary>
    /// MVC、gRPC 等入口边界调用
    /// </summary>
    Entry = 1
}
