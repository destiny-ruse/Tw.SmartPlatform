using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.Threading;
using Xunit;

namespace Tw.Threading.Tests.Cancellation;

/// <summary>
/// 覆盖Cancellation令牌提供器的核心行为和边界条件
/// </summary>
public sealed class CancellationTokenProviderTests
{
    /// <summary>
    /// 验证添加Cancellation令牌提供器注册默认提供器
    /// </summary>
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
