namespace Tw.EventBus.Cap.Consumers;

public enum CapConsumerStatus
{
    Succeeded = 1,
    Duplicate = 2
}

public sealed record CapConsumerResult(CapConsumerStatus Status);
