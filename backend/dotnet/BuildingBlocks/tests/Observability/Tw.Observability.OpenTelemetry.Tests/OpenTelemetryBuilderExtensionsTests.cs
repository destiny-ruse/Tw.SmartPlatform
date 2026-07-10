using AwesomeAssertions;
using Tw.Observability.OpenTelemetry;
using Xunit;

namespace Tw.Observability.OpenTelemetry.Tests;

/// <summary>
/// 覆盖开放Telemetry构建器Extensions的核心行为和边界条件
/// </summary>
public sealed class OpenTelemetryBuilderExtensionsTests
{
    /// <summary>
    /// 验证默认选项Do不EnableGrpc.NETClientInstrumentation
    /// </summary>
    [Fact]
    public void DefaultOptions_DoNotEnableGrpcNetClientInstrumentation()
    {
        var options = OpenTelemetryRegistrationOptions.Default;

        options.EnableGrpcNetClientInstrumentation.Should().BeFalse();
    }
}
