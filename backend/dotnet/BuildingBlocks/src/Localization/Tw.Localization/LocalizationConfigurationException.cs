using Tw.Exceptions;

namespace Tw.Localization;

/// <summary>
/// 表示由无效或缺失本地化配置导致的故障
/// </summary>
public class LocalizationConfigurationException : TwException
{
    /// <summary>
    /// 使用错误消息初始化 <see cref="LocalizationConfigurationException"/> 类的新实例
    /// </summary>
    /// <param name="message">描述配置故障的消息</param>
    public LocalizationConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// 使用错误消息和内部异常初始化 <see cref="LocalizationConfigurationException"/> 类的新实例
    /// </summary>
    /// <param name="message">描述配置故障的消息</param>
    /// <param name="innerException">导致当前异常的异常</param>
    public LocalizationConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
