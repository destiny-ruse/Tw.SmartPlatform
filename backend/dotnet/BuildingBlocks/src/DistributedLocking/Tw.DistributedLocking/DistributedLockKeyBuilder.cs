using Tw.DistributedLocking.Abstractions;

namespace Tw.DistributedLocking;

public static class DistributedLockKeyBuilder
{
    public static DistributedLockKey Build(string tenantId, string shardId, string resourceType, string identifier)
    {
        Validate(tenantId, shardId, resourceType, identifier);
        return new DistributedLockKey($"lock:{tenantId}:{shardId}:{resourceType}:{identifier}");
    }

    private static void Validate(params string[] values)
    {
        foreach (var value in values)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
        }
    }
}
