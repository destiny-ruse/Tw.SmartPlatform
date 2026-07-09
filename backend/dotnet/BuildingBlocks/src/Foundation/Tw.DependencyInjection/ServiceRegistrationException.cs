namespace Tw.DependencyInjection;

/// <summary>
/// 服务注册规划在启动期失败时抛出，例如程序集拓扑存在循环依赖
/// </summary>
public sealed class ServiceRegistrationException : Exception
{
    /// <summary>使用错误消息初始化 <see cref="ServiceRegistrationException"/> 的新实例</summary>
    /// <param name="message">描述失败原因的消息</param>
    public ServiceRegistrationException(string message)
        : base(message)
    {
    }
}
