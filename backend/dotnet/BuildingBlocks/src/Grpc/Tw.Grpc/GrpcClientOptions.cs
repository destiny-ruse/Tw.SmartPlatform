namespace Tw.Grpc;

/// <summary>
/// 配置GrpcClient的运行行为
/// </summary>
public sealed record GrpcClientOptions(TimeSpan Deadline);
