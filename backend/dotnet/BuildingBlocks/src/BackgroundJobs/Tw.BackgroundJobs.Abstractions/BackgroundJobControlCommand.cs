namespace Tw.BackgroundJobs.Abstractions;

public sealed record BackgroundJobControlCommand(
    string JobName,
    BackgroundJobControlAction Action,
    BackgroundJobDefinition? Definition = null);
