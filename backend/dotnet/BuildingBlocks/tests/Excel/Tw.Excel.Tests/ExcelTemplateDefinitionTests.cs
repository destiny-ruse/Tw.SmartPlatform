using AwesomeAssertions;
using Xunit;

namespace Tw.Excel.Tests;

/// <summary>验证 ExcelTemplateDefinitionTests 相关行为</summary>
public sealed class ExcelTemplateDefinitionTests
{
    /// <summary>验证 Create_RejectsDynamicColumnCountOverLimit 场景</summary>
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
