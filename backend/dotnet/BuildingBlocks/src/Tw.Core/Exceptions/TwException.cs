namespace Tw.Core.Exceptions;

/// <summary>
/// Serves as the base exception type for Tw.Core failures that callers may handle consistently.
/// </summary>
public class TwException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TwException"/> class.
    /// </summary>
    public TwException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TwException"/> class with an error message.
    /// </summary>
    /// <param name="message">The message that describes the failure.</param>
    public TwException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TwException"/> class with an error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the failure.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public TwException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
