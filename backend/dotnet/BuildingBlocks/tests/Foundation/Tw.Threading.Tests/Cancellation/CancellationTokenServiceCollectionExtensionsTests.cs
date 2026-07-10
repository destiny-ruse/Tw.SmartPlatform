using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.Threading;
using Xunit;

namespace Tw.Threading.Tests.Cancellation;

/// <summary>
/// 覆盖Cancellation令牌服务CollectionExtensions的核心行为和边界条件
/// </summary>
public class CancellationTokenServiceCollectionExtensionsTests
{
    /// <summary>
    /// 验证添加Cancellation令牌提供器注册空值提供器作为默认
    /// </summary>
    [Fact]
    public void AddCancellationTokenProvider_RegistersNullProvider_AsDefault()
    {
        var services = new ServiceCollection();

        services.AddCancellationTokenProvider();

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ICancellationTokenProvider>()
            .Should().BeOfType<NullCancellationTokenProvider>();
    }

    /// <summary>
    /// 验证添加Cancellation令牌提供器注册作用域提供器作为Singleton
    /// </summary>
    [Fact]
    public void AddCancellationTokenProvider_RegistersScopeProvider_AsSingleton()
    {
        var services = new ServiceCollection();

        services.AddCancellationTokenProvider();

        using var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<AsyncLocalCancellationTokenScopeProvider>();
        var second = provider.GetRequiredService<AsyncLocalCancellationTokenScopeProvider>();
        first.Should().BeSameAs(second);
    }

    /// <summary>
    /// 验证添加Cancellation令牌提供器不OverrideExisting提供器
    /// </summary>
    [Fact]
    public void AddCancellationTokenProvider_DoesNotOverride_ExistingProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICancellationTokenProvider>(
            new NullCancellationTokenProvider(new AsyncLocalCancellationTokenScopeProvider()));
        var sentinel = services.Single(d => d.ServiceType == typeof(ICancellationTokenProvider));

        services.AddCancellationTokenProvider();

        services.Single(d => d.ServiceType == typeof(ICancellationTokenProvider))
            .Should().BeSameAs(sentinel);
    }
}
