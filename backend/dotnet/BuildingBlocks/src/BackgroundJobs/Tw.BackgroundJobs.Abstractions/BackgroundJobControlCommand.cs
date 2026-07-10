namespace Tw.BackgroundJobs.Abstractions;

/// <summary>
/// 提供 CLI 中后台作业Control命令的入口描述
/// </summary>
public sealed record BackgroundJobControlCommand(
    string JobName,
    BackgroundJobControlAction Action,
    BackgroundJobDefinition? Definition = null);
