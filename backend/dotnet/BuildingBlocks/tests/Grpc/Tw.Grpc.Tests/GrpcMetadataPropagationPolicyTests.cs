using AwesomeAssertions;
using Tw.Grpc;
using Xunit;

namespace Tw.Grpc.Tests;

public sealed class GrpcMetadataPropagationPolicyTests
{
    [Fact]
    public void AllowedMetadata_IncludesTraceTenantCultureAndAuthorization()
    {
        GrpcMetadataPropagationPolicy.AllowedMetadata
            .Should()
            .BeEquivalentTo("traceparent", "tracestate", "correlation-id", "tenant-id", "culture", "authorization");
    }
}
