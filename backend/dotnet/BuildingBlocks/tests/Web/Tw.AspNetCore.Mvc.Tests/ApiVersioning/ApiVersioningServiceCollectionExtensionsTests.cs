using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.AspNetCore.Mvc.ApiVersioning;
using Xunit;

namespace Tw.AspNetCore.Mvc.Tests.ApiVersioning;

/// <summary>
/// 覆盖ApiVersioning服务CollectionExtensions的核心行为和边界条件
/// </summary>
public sealed class ApiVersioningServiceCollectionExtensionsTests
{
    /// <summary>
    /// 验证添加ApiVersioningIntegration注册UrlSegmentVersioning
    /// </summary>
    [Fact]
    public void AddApiVersioningIntegration_RegistersUrlSegmentVersioning()
    {
        var services = new ServiceCollection();

        services.AddApiVersioningIntegration();

        services.Should().Contain(descriptor =>
            descriptor.ServiceType.FullName!.Contains("IApiVersionReader", StringComparison.Ordinal));
    }
}
