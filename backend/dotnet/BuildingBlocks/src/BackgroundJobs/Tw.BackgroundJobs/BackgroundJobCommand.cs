using MediatR;
using Tw.BackgroundJobs.Abstractions;

namespace Tw.BackgroundJobs;

public sealed record BackgroundJobCommand(IRequest Request, BackgroundJobContext Context);
