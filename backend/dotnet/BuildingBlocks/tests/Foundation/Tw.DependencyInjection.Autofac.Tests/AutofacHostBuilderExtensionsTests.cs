using Autofac.Extensions.DependencyInjection;
using AwesomeAssertions;
using Microsoft.Extensions.Hosting;
using Tw.DependencyInjection.Autofac;
using Xunit;

namespace Tw.DependencyInjection.Autofac.Tests;

/// <summary>
/// 覆盖Autofac主机构建器Extensions的核心行为和边界条件
/// </summary>
public sealed class AutofacHostBuilderExtensionsTests
{
    /// <summary>
    /// 验证UseAutofacReplaces服务提供器带有Autofac
    /// </summary>
    [Fact]
    public void UseAutofac_ReplacesServiceProvider_WithAutofac()
    {
        var builder = new HostBuilder().UseAutofac();

        using var host = builder.Build();

        host.Services.Should().BeOfType<AutofacServiceProvider>();
    }

    /// <summary>
    /// 验证UseAutofac抛出异常当主机构建器Is空值
    /// </summary>
    [Fact]
    public void UseAutofac_Throws_WhenHostBuilderIsNull()
    {
        IHostBuilder hostBuilder = null!;

        var act = () => hostBuilder.UseAutofac();

        act.Should().Throw<ArgumentNullException>();
    }
}
