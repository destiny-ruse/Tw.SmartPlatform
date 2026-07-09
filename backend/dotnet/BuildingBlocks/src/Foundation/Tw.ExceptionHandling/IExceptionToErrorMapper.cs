namespace Tw.ExceptionHandling;

/// <summary>
/// 将异常转换为稳定错误描述
/// </summary>
public interface IExceptionToErrorMapper
{
    /// <summary>
    /// 将异常映射为稳定错误描述
    /// </summary>
    /// <param name="exception">要映射的异常</param>
    /// <returns>稳定错误描述</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="exception"/> 为 <see langword="null"/> 时抛出</exception>
    ErrorDescriptor Map(Exception exception);
}
