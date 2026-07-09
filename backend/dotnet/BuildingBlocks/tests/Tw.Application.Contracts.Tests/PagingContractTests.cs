using AwesomeAssertions;
using Tw.Application.Contracts;
using Xunit;

namespace Tw.Application.Contracts.Tests;

public sealed class PagingContractTests
{
    [Fact]
    public void PagedResult_StoresItemsAndTotalCount()
    {
        var result = new PagedResult<string>(["a", "b"], 10);

        result.Items.Should().Equal("a", "b");
        result.TotalCount.Should().Be(10);
    }
}
