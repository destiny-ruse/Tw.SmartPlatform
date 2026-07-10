namespace Tw.DistributedLocking.Abstractions;

/// <summary>
/// 封装DistributedLock键相关的数据和行为
/// </summary>
public sealed record DistributedLockKey(string Value);
