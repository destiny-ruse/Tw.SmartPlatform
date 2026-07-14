using System.Collections;
using AwesomeAssertions;
using Tw.Http.HeaderPropagation;
using Xunit;

namespace Tw.Http.Tests;

/// <summary>
/// 验证出站请求头传播的信任边界与不可变多值语义
/// </summary>
public sealed class HeaderPropagationPolicyTests
{
    /// <summary>
    /// 仅返回调用方配置且平台允许传播的请求头
    /// </summary>
    [Fact]
    public void SelectHeaders_ReturnsOnlyConfiguredSafeHeaders()
    {
        var headers = new Dictionary<string, IReadOnlyList<string>>
        {
            ["X-Correlation-Id"] = ["correlation-1"],
            ["X-Custom-Header"] = ["custom-value"]
        };

        var selectedHeaders = HeaderPropagationPolicy.SelectHeaders(
            headers,
            CreateOptions("X-Correlation-Id", "X-Custom-Header"),
            HeaderTrustLevel.Verified);

        selectedHeaders.Should().ContainSingle();
        selectedHeaders["X-Correlation-Id"].Should().Equal("correlation-1");
    }

    /// <summary>
    /// 请求头名称匹配不区分大小写
    /// </summary>
    [Fact]
    public void SelectHeaders_MatchesHeaderNamesWithoutCaseSensitivity()
    {
        var headers = new Dictionary<string, IReadOnlyList<string>>
        {
            ["TRACEPARENT"] = ["00-trace-parent"]
        };

        var selectedHeaders = HeaderPropagationPolicy.SelectHeaders(
            headers,
            CreateOptions("traceparent"),
            HeaderTrustLevel.ClientSupplied);

        selectedHeaders.Should().ContainKey("traceparent");
        selectedHeaders["traceparent"].Should().Equal("00-trace-parent");
    }

    /// <summary>
    /// 配置中存在但输入中缺失的请求头不会出现在结果中
    /// </summary>
    [Fact]
    public void SelectHeaders_OmitsConfiguredHeadersThatAreAbsent()
    {
        var headers = new Dictionary<string, IReadOnlyList<string>>
        {
            ["X-Culture"] = ["zh-CN"]
        };

        var selectedHeaders = HeaderPropagationPolicy.SelectHeaders(
            headers,
            CreateOptions("X-Correlation-Id"),
            HeaderTrustLevel.Verified);

        selectedHeaders.Should().BeEmpty();
    }

    /// <summary>
    /// 选择请求头不会修改调用方传入的字典和值列表
    /// </summary>
    [Fact]
    public void SelectHeaders_DoesNotMutateSourceHeaders()
    {
        var correlationValues = new List<string> { "correlation-1" };
        var headers = new Dictionary<string, IReadOnlyList<string>>
        {
            ["X-Correlation-Id"] = correlationValues,
            ["X-Private"] = ["private-value"]
        };
        var originalKeys = headers.Keys.ToArray();
        var originalValues = correlationValues.ToArray();

        _ = HeaderPropagationPolicy.SelectHeaders(
            headers,
            CreateOptions("X-Correlation-Id"),
            HeaderTrustLevel.Verified);

        headers.Keys.Should().Equal(originalKeys);
        correlationValues.Should().Equal(originalValues);
    }

    /// <summary>
    /// 多值 tracestate 保留全部值、重复项与原始顺序
    /// </summary>
    [Fact]
    public void SelectHeaders_PreservesMultipleValuesAndOrder()
    {
        var headers = new Dictionary<string, IReadOnlyList<string>>
        {
            ["tracestate"] = ["vendor-a=1", "vendor-b=2", "vendor-a=1"]
        };

        var selectedHeaders = HeaderPropagationPolicy.SelectHeaders(
            headers,
            CreateOptions("tracestate"),
            HeaderTrustLevel.Verified);

        selectedHeaders["tracestate"].Should().Equal("vendor-a=1", "vendor-b=2", "vendor-a=1");
    }

    /// <summary>
    /// 输出不会受到输入字典和值列表后续修改影响
    /// </summary>
    [Fact]
    public void SelectHeaders_CopiesDictionaryAndValueLists()
    {
        var traceStateValues = new List<string> { "vendor-a=1", "vendor-b=2" };
        var headers = new Dictionary<string, IReadOnlyList<string>>
        {
            ["tracestate"] = traceStateValues
        };

        var selectedHeaders = HeaderPropagationPolicy.SelectHeaders(
            headers,
            CreateOptions("tracestate"),
            HeaderTrustLevel.Verified);

        traceStateValues.Add("vendor-c=3");
        headers["tracestate"] = ["replacement=1"];

        selectedHeaders["tracestate"].Should().Equal("vendor-a=1", "vendor-b=2");
        selectedHeaders.Should().NotBeAssignableTo<Dictionary<string, IReadOnlyList<string>>>();
        selectedHeaders["tracestate"].Should().NotBeAssignableTo<string[]>();
        var listSurface = selectedHeaders["tracestate"] as IList<string>;
        listSurface.Should().NotBeNull();
        listSurface!.IsReadOnly.Should().BeTrue();
        var mutation = () => listSurface.Add("forbidden");
        mutation.Should().Throw<NotSupportedException>();
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
        var headers = new Dictionary<string, IReadOnlyList<string>>
        {
            [headerName] = ["sensitive-value"]
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
        var headers = new Dictionary<string, IReadOnlyList<string>>
        {
            ["X-Tenant-Id"] = ["tenant-1"]
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
        var headers = new Dictionary<string, IReadOnlyList<string>>
        {
            ["X-Tenant-Id"] = ["tenant-1"]
        };

        var selectedHeaders = HeaderPropagationPolicy.SelectHeaders(
            headers,
            CreateOptions("X-Tenant-Id"),
            HeaderTrustLevel.Verified);

        selectedHeaders["X-Tenant-Id"].Should().Equal("tenant-1");
    }

    /// <summary>
    /// 传播选项复制输入并按不区分大小写的规则折叠重复名称
    /// </summary>
    [Fact]
    public void Constructor_CopiesAndNormalizesAllowedHeaders()
    {
        var allowedHeaders = new List<string> { "traceparent", "TRACEPARENT" };

        var options = new HeaderPropagationOptions(allowedHeaders);
        allowedHeaders.Add("X-Correlation-Id");

        options.AllowedHeaders.Should().ContainSingle();
        options.AllowedHeaders.Should().Contain("TraceParent");
        options.AllowedHeaders.Should().NotContain("X-Correlation-Id");
    }

    /// <summary>
    /// 传播选项只枚举输入集合一次
    /// </summary>
    [Fact]
    public void Constructor_EnumeratesAllowedHeadersOnlyOnce()
    {
        var allowedHeaders = new SingleUseEnumerable<string>(["traceparent", "tracestate"]);

        var options = new HeaderPropagationOptions(allowedHeaders);

        options.AllowedHeaders.Should().BeEquivalentTo("traceparent", "tracestate");
        allowedHeaders.EnumerationCount.Should().Be(1);
    }

    /// <summary>
    /// 空白请求头名称无法形成传播配置
    /// </summary>
    [Fact]
    public void Constructor_RejectsBlankAllowedHeaderName()
    {
        var action = () => new HeaderPropagationOptions([" "]);

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
        var headers = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            ["traceparent"] = ["first-value"],
            ["TraceParent"] = ["second-value"]
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
    /// 平台策略拒绝未知信任级别
    /// </summary>
    [Fact]
    public void ShouldPropagate_RejectsUnknownTrustLevel()
    {
        var action = () => HeaderPropagationPolicy.ShouldPropagate(
            "traceparent",
            (HeaderTrustLevel)999);

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("trustLevel")
            .WithMessage("*请求头信任级别不受支持*");
    }

    /// <summary>
    /// 多值选择入口拒绝未知信任级别
    /// </summary>
    [Fact]
    public void SelectHeaders_RejectsUnknownTrustLevel()
    {
        var headers = new Dictionary<string, IReadOnlyList<string>>
        {
            ["traceparent"] = ["00-trace-parent"]
        };

        var action = () => HeaderPropagationPolicy.SelectHeaders(
            headers,
            CreateOptions("traceparent"),
            (HeaderTrustLevel)999);

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("trustLevel")
            .WithMessage("*请求头信任级别不受支持*");
    }

    /// <summary>
    /// null 请求头值列表无法形成不可变输出
    /// </summary>
    [Fact]
    public void SelectHeaders_RejectsNullValueList()
    {
        var headers = new Dictionary<string, IReadOnlyList<string>>
        {
            ["traceparent"] = null!
        };

        var action = () => HeaderPropagationPolicy.SelectHeaders(
            headers,
            CreateOptions("traceparent"),
            HeaderTrustLevel.Verified);

        action.Should().Throw<ArgumentException>()
            .WithParameterName("headers")
            .WithMessage("*请求头值集合不能为空*");
    }

    /// <summary>
    /// 值列表中的 null 元素无法形成出站请求头
    /// </summary>
    [Fact]
    public void SelectHeaders_RejectsNullHeaderValue()
    {
        var headers = new Dictionary<string, IReadOnlyList<string>>
        {
            ["traceparent"] = new string[] { "00-trace-parent", null! }
        };

        var action = () => HeaderPropagationPolicy.SelectHeaders(
            headers,
            CreateOptions("traceparent"),
            HeaderTrustLevel.Verified);

        action.Should().Throw<ArgumentException>()
            .WithParameterName("headers")
            .WithMessage("*请求头值不能为空*");
    }

    /// <summary>
    /// 创建使用不区分大小写匹配规则的传播选项
    /// </summary>
    /// <param name="headerNames">调用方明确允许传播的请求头名称</param>
    /// <returns>复制输入集合后的传播选项</returns>
    private static HeaderPropagationOptions CreateOptions(params string[] headerNames)
    {
        return new HeaderPropagationOptions(headerNames);
    }

    /// <summary>
    /// 提供第二次枚举即失败的输入以验证单次物化契约
    /// </summary>
    /// <typeparam name="T">集合元素类型</typeparam>
    private sealed class SingleUseEnumerable<T> : IEnumerable<T>
    {
        /// <summary>
        /// 首次枚举时返回的元素
        /// </summary>
        private readonly IEnumerable<T> _items;

        /// <summary>
        /// 创建仅允许枚举一次的测试输入
        /// </summary>
        /// <param name="items">首次枚举时返回的元素</param>
        public SingleUseEnumerable(IEnumerable<T> items)
        {
            _items = items;
        }

        /// <summary>
        /// 已请求枚举器的次数
        /// </summary>
        public int EnumerationCount { get; private set; }

        /// <summary>
        /// 首次调用返回底层枚举器，后续调用抛出异常
        /// </summary>
        /// <returns>首次遍历输入元素的枚举器</returns>
        /// <exception cref="InvalidOperationException">输入被重复枚举时抛出</exception>
        public IEnumerator<T> GetEnumerator()
        {
            EnumerationCount++;
            if (EnumerationCount > 1)
            {
                throw new InvalidOperationException("测试输入不得重复枚举");
            }

            return _items.GetEnumerator();
        }

        /// <summary>
        /// 通过泛型枚举入口遍历输入元素
        /// </summary>
        /// <returns>首次遍历输入元素的枚举器</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
