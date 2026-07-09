namespace Tw.EventBus.Cap.RabbitMq;

public sealed class CapRabbitMqOptions
{
    public string? HostName { get; set; }

    public string VirtualHost { get; set; } = "/";

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public string ExchangeName { get; set; } = "tw.smart-platform";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(HostName))
        {
            throw new InvalidOperationException("CAP RabbitMQ host is required");
        }

        if (string.IsNullOrWhiteSpace(UserName))
        {
            throw new InvalidOperationException("CAP RabbitMQ user is required");
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            throw new InvalidOperationException("CAP RabbitMQ password is required");
        }
    }
}
