namespace Tw.EventBus.Cap.RabbitMq;

/// <summary>
/// 配置CapRabbitMq的运行行为
/// </summary>
public sealed class CapRabbitMqOptions
{
    /// <summary>
    /// 主机名称在当前对象中的业务含义
    /// </summary>
    public string? HostName { get; set; }

    /// <summary>
    /// Virtual主机在当前对象中的业务含义
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// 用户名称在当前对象中的业务含义
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Password在当前对象中的业务含义
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Exchange名称在当前对象中的业务含义
    /// </summary>
    public string ExchangeName { get; set; } = "tw.smart-platform";

    /// <summary>
    /// 校验当前配置或输入约束，并在非法时抛出异常
    /// </summary>
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
