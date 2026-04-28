using System.Runtime.ExceptionServices;

namespace Tw.Core.Extensions;

/// <summary>提供异常扩展方法</summary>
public static class ExceptionExtensions
{
    /// <summary>在保留原始堆栈跟踪的同时重新抛出异常</summary>
    /// <param name="exception">要重新抛出的异常</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="exception"/> 为 <see langword="null"/> 时抛出</exception>
    public static void ReThrow(this Exception exception)
    {
        ExceptionDispatchInfo.Capture(Check.NotNull(exception)).Throw();
    }
}
