using AwesomeAssertions;
using Tw.Application.Contracts;
using Xunit;

namespace Tw.Application.Contracts.Tests;

/// <summary>
/// 覆盖PagingContract的核心行为和边界条件
/// </summary>
public sealed class PagingContractTests
{
    /// <summary>
    /// 验证Paged结果StoresItems和Total数量
    /// </summary>
    [Fact]
    public void PagedResult_StoresItemsAndTotalCount()
    {
        var result = new PagedResult<string>(["a", "b"], 10);

        result.Items.Should().Equal("a", "b");
        result.TotalCount.Should().Be(10);
    }
}
