namespace Tw.EventBus.Cap.RabbitMq;

/// <summary>表示 CapRabbitMqOptions 类型</summary>
public sealed class CapRabbitMqOptions
{
    /// <summary>表示 HostName 属性</summary>
    public string? HostName { get; set; }

    /// <summary>表示 VirtualHost 属性</summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>表示 UserName 属性</summary>
    public string? UserName { get; set; }

    /// <summary>表示 Password 属性</summary>
    public string? Password { get; set; }

    /// <summary>表示 ExchangeName 属性</summary>
    public string ExchangeName { get; set; } = "tw.smart-platform";

    /// <summary>执行 Validate 操作</summary>
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
