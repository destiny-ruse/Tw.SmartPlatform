namespace Tw.Core.Exceptions;

/// <summary>
/// 作为调用方可统一处理的 Tw.Core 故障基异常类型
/// </summary>
public class TwException : Exception
{
    /// <summary>
    /// 初始化 <see cref="TwException"/> 类的新实例
    /// </summary>
    public TwException()
    {
    }

    /// <summary>
    /// 使用错误消息初始化 <see cref="TwException"/> 类的新实例
    /// </summary>
    /// <param name="message">描述故障的消息</param>
    public TwException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// 使用错误消息和内部异常初始化 <see cref="TwException"/> 类的新实例
    /// </summary>
    /// <param name="message">描述故障的消息</param>
    /// <param name="innerException">导致当前异常的异常</param>
    public TwException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
