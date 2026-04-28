namespace Tw.Core.Exceptions;

public class TwConfigurationException : TwException
{
    public TwConfigurationException(string message)
        : base(message)
    {
    }

    public TwConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
