using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.Threading;
using Xunit;

namespace Tw.Threading.Tests.Cancellation;

/// <summary>验证 CancellationTokenProviderTests 相关行为</summary>
public sealed class CancellationTokenProviderTests
{
    /// <summary>验证 AddCancellationTokenProvider_RegistersDefaultProvider 场景</summary>
    [Fact]
    public void AddCancellationTokenProvider_RegistersDefaultProvider()
    {
        var services = new ServiceCollection();

        services.AddCancellationTokenProvider();

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ICancellationTokenProvider>()
            .Token
            .Should()
            .Be(CancellationToken.None);
    }
}
