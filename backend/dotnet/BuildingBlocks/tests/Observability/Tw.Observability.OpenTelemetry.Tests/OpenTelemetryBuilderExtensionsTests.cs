using AwesomeAssertions;
using Tw.Observability.OpenTelemetry;
using Xunit;

namespace Tw.Observability.OpenTelemetry.Tests;

/// <summary>验证 OpenTelemetryBuilderExtensionsTests 相关行为</summary>
public sealed class OpenTelemetryBuilderExtensionsTests
{
    /// <summary>验证 DefaultOptions_DoNotEnableGrpcNetClientInstrumentation 场景</summary>
    [Fact]
    public void DefaultOptions_DoNotEnableGrpcNetClientInstrumentation()
    {
        var options = OpenTelemetryRegistrationOptions.Default;

        options.EnableGrpcNetClientInstrumentation.Should().BeFalse();
    }
}
