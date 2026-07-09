namespace Tw.EventBus.Cap.Storage;

public sealed class SqlSugarCapStorageOptions
{
    public string? ConnectionName { get; set; }

    public string Schema { get; set; } = "cap";

    public string PublishedTable { get; set; } = "published";

    public string ReceivedTable { get; set; } = "received";

    public string LockTable { get; set; } = "locks";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionName))
        {
            throw new InvalidOperationException("CAP SqlSugar connection name is required");
        }
    }
}
