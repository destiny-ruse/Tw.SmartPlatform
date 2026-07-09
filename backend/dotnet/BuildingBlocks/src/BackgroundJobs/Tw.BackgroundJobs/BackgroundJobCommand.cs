using MediatR;
using Tw.BackgroundJobs.Abstractions;

namespace Tw.BackgroundJobs;

/// <summary>表示 BackgroundJobCommand 声明</summary>
public sealed record BackgroundJobCommand(IRequest Request, BackgroundJobContext Context);
