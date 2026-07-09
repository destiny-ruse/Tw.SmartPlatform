namespace Tw.Data.Concurrency;

/// <summary>表示 ConcurrencyConflictException 类型</summary>
public sealed class ConcurrencyConflictException(string resourceType, string resourceId)
    : Exception("Data has been changed by another request.")
{
    /// <summary>表示 Code 属性</summary>
    public string Code { get; } = "DATA:CONFLICT";

    /// <summary>表示 ResourceType 属性</summary>
    public string ResourceType { get; } = resourceType;

    /// <summary>表示 ResourceId 属性</summary>
    public string ResourceId { get; } = resourceId;
}
