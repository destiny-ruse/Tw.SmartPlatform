namespace Tw.DependencyInjection.Interception;

/// <summary>
/// 启动期执行的拦截器自动匹配规则
/// </summary>
public interface IInterceptorMatcher
{
    /// <summary>
    /// 匹配成功后写入拦截器链的运行时拦截器类型
    /// </summary>
    Type InterceptorType { get; }

    /// <summary>
    /// 当前规则参与的拦截作用域
    /// </summary>
    InterceptorScope Scope { get; }

    /// <summary>
    /// 同层排序值，值越小越靠前
    /// </summary>
    int Order => 0;

    /// <summary>
    /// 判断该规则是否匹配服务层注册
    /// </summary>
    /// <param name="serviceType">对外暴露的服务类型</param>
    /// <param name="implementationType">服务实现类型</param>
    bool MatchService(Type serviceType, Type implementationType) => false;

    /// <summary>
    /// 判断该规则是否匹配入口类型
    /// </summary>
    /// <param name="entryType">MVC Controller、gRPC Service 或其他入口类型</param>
    bool MatchEntry(Type entryType) => false;
}
