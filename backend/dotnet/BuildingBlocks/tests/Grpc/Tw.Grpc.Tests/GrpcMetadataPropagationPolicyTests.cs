using AwesomeAssertions;
using Tw.Grpc;
using Xunit;

namespace Tw.Grpc.Tests;

/// <summary>
/// 覆盖GrpcMetadataPropagation策略的核心行为和边界条件
/// </summary>
public sealed class GrpcMetadataPropagationPolicyTests
{
    /// <summary>
    /// 验证AllowedMetadataIncludesTrace租户文化和授权
    /// </summary>
    [Fact]
    public void AllowedMetadata_IncludesTraceTenantCultureAndAuthorization()
    {
        GrpcMetadataPropagationPolicy.AllowedMetadata
            .Should()
            .BeEquivalentTo("traceparent", "tracestate", "correlation-id", "tenant-id", "culture", "authorization");
    }
}
