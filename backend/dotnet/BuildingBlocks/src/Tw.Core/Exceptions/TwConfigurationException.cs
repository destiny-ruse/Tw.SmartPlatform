namespace Tw.Core.Exceptions;

/// <summary>
/// 表示由无效或缺失配置导致的 Tw.Core 故障
/// </summary>
public class TwConfigurationException : TwException
{
    /// <summary>
    /// 使用错误消息初始化 <see cref="TwConfigurationException"/> 类的新实例
    /// </summary>
    /// <param name="message">描述配置故障的消息</param>
    public TwConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// 使用错误消息和内部异常初始化 <see cref="TwConfigurationException"/> 类的新实例
    /// </summary>
    /// <param name="message">描述配置故障的消息</param>
    /// <param name="innerException">导致当前异常的异常</param>
    public TwConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
