using AwesomeAssertions;
using Tw.Observability.OpenTelemetry;
using Xunit;

namespace Tw.Observability.OpenTelemetry.Tests;

public sealed class OpenTelemetryBuilderExtensionsTests
{
    [Fact]
    public void DefaultOptions_DoNotEnableGrpcNetClientInstrumentation()
    {
        var options = OpenTelemetryRegistrationOptions.Default;

        options.EnableGrpcNetClientInstrumentation.Should().BeFalse();
    }
}
