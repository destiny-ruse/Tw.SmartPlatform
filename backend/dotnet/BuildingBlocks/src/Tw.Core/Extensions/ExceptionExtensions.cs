using System.Runtime.ExceptionServices;

namespace Tw.Core.Extensions;

/// <summary>Provides extension methods for exceptions.</summary>
public static class ExceptionExtensions
{
    /// <summary>Rethrows an exception while preserving its original stack trace.</summary>
    /// <param name="exception">The exception to rethrow.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is <see langword="null"/>.</exception>
    public static void ReThrow(this Exception exception)
    {
        ExceptionDispatchInfo.Capture(Check.NotNull(exception)).Throw();
    }
}
