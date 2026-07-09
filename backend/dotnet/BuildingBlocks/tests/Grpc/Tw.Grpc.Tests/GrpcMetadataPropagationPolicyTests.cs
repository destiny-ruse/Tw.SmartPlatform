using AwesomeAssertions;
using Tw.Grpc;
using Xunit;

namespace Tw.Grpc.Tests;

/// <summary>验证 GrpcMetadataPropagationPolicyTests 相关行为</summary>
public sealed class GrpcMetadataPropagationPolicyTests
{
    /// <summary>验证 AllowedMetadata_IncludesTraceTenantCultureAndAuthorization 场景</summary>
    [Fact]
    public void AllowedMetadata_IncludesTraceTenantCultureAndAuthorization()
    {
        GrpcMetadataPropagationPolicy.AllowedMetadata
            .Should()
            .BeEquivalentTo("traceparent", "tracestate", "correlation-id", "tenant-id", "culture", "authorization");
    }
}
