using AwesomeAssertions;
using Xunit;

namespace Tw.Excel.Tests;

public sealed class ExcelTemplateDefinitionTests
{
    [Fact]
    public void Create_RejectsDynamicColumnCountOverLimit()
    {
        var columns = Enumerable.Range(0, 101)
            .Select(index => new ExcelColumnDefinition($"dynamic_{index}", $"动态列 {index}", "string", IsDynamic: true))
            .ToArray();

        var act = () => ExcelTemplateDefinition.Create("invoice", columns, maxDynamicColumns: 100);

        act.Should().Throw<ExcelTemplateException>()
            .WithMessage("动态列数量超过配置上限");
    }
}
