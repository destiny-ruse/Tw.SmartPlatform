using AwesomeAssertions;
using Tw.Http.HeaderPropagation;
using Xunit;

namespace Tw.Http.Tests;

/// <summary>
/// 验证出站请求头传播的信任边界与集合语义
/// </summary>
public sealed class HeaderPropagationPolicyTests
{
    /// <summary>
    /// 仅返回调用方配置且平台允许传播的请求头
    /// </summary>
    [Fact]
    public void SelectHeaders_ReturnsOnlyConfiguredSafeHeaders()
    {
        var headers = new Dictionary<string, string>
        {
            ["X-Correlation-Id"] = "correlation-1",
            ["X-Custom-Header"] = "custom-value"
        };
        var options = CreateOptions("X-Correlation-Id", "X-Custom-Header");

        var selectedHeaders = HeaderPropagationPolicy.SelectHeaders(
            headers,
            options,
            HeaderTrustLevel.Verified);

        selectedHeaders.Should().ContainSingle()
            .Which.Should().Be(new KeyValuePair<string, string>("X-Correlation-Id", "correlation-1"));
    }

    /// <summary>
    /// 请求头名称匹配不区分大小写
    /// </summary>
    [Fact]
    public void SelectHeaders_MatchesHeaderNamesWithoutCaseSensitivity()
    {
        var headers = new Dictionary<string, string>
        {
            ["TRACEPARENT"] = "00-trace-parent"
        };
        var options = CreateOptions("traceparent");

        var selectedHeaders = HeaderPropagationPolicy.SelectHeaders(
            headers,
            options,
            HeaderTrustLevel.ClientSupplied);

        selectedHeaders.Should().ContainKey("traceparent")
            .WhoseValue.Should().Be("00-trace-parent");
    }

    /// <summary>
    /// 配置中存在但输入中缺失的请求头不会出现在结果中
    /// </summary>
    [Fact]
    public void SelectHeaders_OmitsConfiguredHeadersThatAreAbsent()
    {
        var headers = new Dictionary<string, string>
        {
            ["X-Culture"] = "zh-CN"
        };
        var options = CreateOptions("X-Correlation-Id");

        var selectedHeaders = HeaderPropagationPolicy.SelectHeaders(
            headers,
            options,
            HeaderTrustLevel.Verified);

        selectedHeaders.Should().BeEmpty();
    }

    /// <summary>
    /// 选择请求头不会修改调用方传入的集合
    /// </summary>
    [Fact]
    public void SelectHeaders_DoesNotMutateSourceHeaders()
    {
        var headers = new Dictionary<string, string>
        {
            ["X-Correlation-Id"] = "correlation-1",
            ["X-Private"] = "private-value"
        };
        var originalHeaders = headers.ToArray();

        _ = HeaderPropagationPolicy.SelectHeaders(
            headers,
            CreateOptions("X-Correlation-Id"),
            HeaderTrustLevel.Verified);

        headers.Should().Equal(originalHeaders);
    }

    /// <summary>
    /// 身份凭据与 Cookie 请求头即使被配置也不会传播
    /// </summary>
    /// <param name="headerName">不得跨出站边界复制的敏感请求头名称</param>
    [Theory]
    [InlineData("Authorization")]
    [InlineData("Cookie")]
    [InlineData("Set-Cookie")]
    [InlineData("Proxy-Authorization")]
    public void SelectHeaders_RejectsSensitiveHeaders(string headerName)
    {
        var headers = new Dictionary<string, string>
        {
            [headerName] = "sensitive-value"
        };

        var selectedHeaders = HeaderPropagationPolicy.SelectHeaders(
            headers,
            CreateOptions(headerName),
            HeaderTrustLevel.Verified);

        HeaderPropagationPolicy.ShouldPropagate(headerName, HeaderTrustLevel.Verified).Should().BeFalse();
        selectedHeaders.Should().BeEmpty();
    }

    /// <summary>
    /// 未验证租户标识不会从调用方输入传播
    /// </summary>
    [Fact]
    public void SelectHeaders_RejectsClientSuppliedTenantHeader()
    {
        var headers = new Dictionary<string, string>
        {
            ["X-Tenant-Id"] = "tenant-1"
        };

        var selectedHeaders = HeaderPropagationPolicy.SelectHeaders(
            headers,
            CreateOptions("X-Tenant-Id"),
            HeaderTrustLevel.ClientSupplied);

        selectedHeaders.Should().BeEmpty();
    }

    /// <summary>
    /// 已验证租户标识可以按配置传播
    /// </summary>
    [Fact]
    public void SelectHeaders_AllowsVerifiedTenantHeader()
    {
        var headers = new Dictionary<string, string>
        {
            ["X-Tenant-Id"] = "tenant-1"
        };

        var selectedHeaders = HeaderPropagationPolicy.SelectHeaders(
            headers,
            CreateOptions("X-Tenant-Id"),
            HeaderTrustLevel.Verified);

        selectedHeaders.Should().ContainKey("X-Tenant-Id")
            .WhoseValue.Should().Be("tenant-1");
    }

    /// <summary>
    /// 传播选项复制输入并按不区分大小写的规则折叠重复名称
    /// </summary>
    [Fact]
    public void Constructor_CopiesAndNormalizesAllowedHeaders()
    {
        var allowedHeaders = new HashSet<string>(StringComparer.Ordinal)
        {
            "traceparent",
            "TRACEPARENT"
        };

        var options = new HeaderPropagationOptions(allowedHeaders);
        allowedHeaders.Add("X-Correlation-Id");

        options.AllowedHeaders.Should().ContainSingle();
        options.AllowedHeaders.Should().Contain("TraceParent");
        options.AllowedHeaders.Should().NotContain("X-Correlation-Id");
    }

    /// <summary>
    /// 空白请求头名称无法形成传播配置
    /// </summary>
    [Fact]
    public void Constructor_RejectsBlankAllowedHeaderName()
    {
        var allowedHeaders = new HashSet<string> { " " };

        var action = () => new HeaderPropagationOptions(allowedHeaders);

        action.Should().Throw<ArgumentException>()
            .WithParameterName("allowedHeaders")
            .WithMessage("*允许传播的请求头名称不能为空*");
    }

    /// <summary>
    /// 输入集合包含大小写不同的同名请求头时拒绝产生歧义结果
    /// </summary>
    [Fact]
    public void SelectHeaders_RejectsCaseVariantDuplicates()
    {
        var headers = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["traceparent"] = "first-value",
            ["TraceParent"] = "second-value"
        };

        var action = () => HeaderPropagationPolicy.SelectHeaders(
            headers,
            CreateOptions("traceparent"),
            HeaderTrustLevel.Verified);

        action.Should().Throw<ArgumentException>()
            .WithParameterName("headers")
            .WithMessage("*请求头名称不得仅因大小写不同而重复*");
    }

    /// <summary>
    /// 创建使用不区分大小写匹配规则的传播选项
    /// </summary>
    /// <param name="headerNames">调用方明确允许传播的请求头名称</param>
    /// <returns>复制输入集合后的传播选项</returns>
    private static HeaderPropagationOptions CreateOptions(params string[] headerNames)
    {
        return new HeaderPropagationOptions(headerNames.ToHashSet(StringComparer.Ordinal));
    }
}
