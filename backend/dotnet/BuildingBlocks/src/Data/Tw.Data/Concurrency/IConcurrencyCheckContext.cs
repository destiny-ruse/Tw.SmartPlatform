namespace Tw.Data.Concurrency;

/// <summary>定义 IConcurrencyCheckContext 契约</summary>
public interface IConcurrencyCheckContext
{
    /// <summary>表示 ResourceType 属性</summary>
    string ResourceType { get; }

    /// <summary>表示 ResourceId 属性</summary>
    string ResourceId { get; }

    /// <summary>表示 ExpectedConcurrencyStamp 属性</summary>
    string? ExpectedConcurrencyStamp { get; }

    /// <summary>表示 ExpectedVersionStamp 属性</summary>
    long? ExpectedVersionStamp { get; }
}
