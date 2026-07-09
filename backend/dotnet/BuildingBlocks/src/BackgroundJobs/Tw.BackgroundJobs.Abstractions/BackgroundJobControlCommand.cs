namespace Tw.BackgroundJobs.Abstractions;

/// <summary>表示 BackgroundJobControlCommand 声明</summary>
public sealed record BackgroundJobControlCommand(
    string JobName,
    BackgroundJobControlAction Action,
    BackgroundJobDefinition? Definition = null);
