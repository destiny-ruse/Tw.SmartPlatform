using MediatR;
using Tw.BackgroundJobs.Abstractions;

namespace Tw.BackgroundJobs;

/// <summary>
/// 提供 CLI 中后台作业命令的入口描述
/// </summary>
public sealed record BackgroundJobCommand(IRequest Request, BackgroundJobContext Context);
