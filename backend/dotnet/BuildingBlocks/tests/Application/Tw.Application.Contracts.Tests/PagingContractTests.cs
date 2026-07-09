using AwesomeAssertions;
using Tw.Application.Contracts;
using Xunit;

namespace Tw.Application.Contracts.Tests;

/// <summary>验证 PagingContractTests 相关行为</summary>
public sealed class PagingContractTests
{
    /// <summary>验证 PagedResult_StoresItemsAndTotalCount 场景</summary>
    [Fact]
    public void PagedResult_StoresItemsAndTotalCount()
    {
        var result = new PagedResult<string>(["a", "b"], 10);

        result.Items.Should().Equal("a", "b");
        result.TotalCount.Should().Be(10);
    }
}
