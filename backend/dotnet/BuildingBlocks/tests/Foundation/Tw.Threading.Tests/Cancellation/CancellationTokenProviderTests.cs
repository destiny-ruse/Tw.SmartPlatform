using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.Threading;
using Xunit;

namespace Tw.Threading.Tests.Cancellation;

public sealed class CancellationTokenProviderTests
{
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
