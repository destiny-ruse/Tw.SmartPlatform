namespace Tw.Core.Exceptions;

/// <summary>
/// Represents a Tw.Core failure caused by invalid or missing configuration.
/// </summary>
public class TwConfigurationException : TwException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TwConfigurationException"/> class with an error message.
    /// </summary>
    /// <param name="message">The message that describes the configuration failure.</param>
    public TwConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TwConfigurationException"/> class with an error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the configuration failure.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public TwConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
