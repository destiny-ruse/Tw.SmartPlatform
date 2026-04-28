namespace Tw.Core.Exceptions;

public class TwException : Exception
{
    public TwException()
    {
    }

    public TwException(string message)
        : base(message)
    {
    }

    public TwException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
