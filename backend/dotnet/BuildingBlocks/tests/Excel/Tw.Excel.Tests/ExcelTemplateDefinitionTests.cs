using AwesomeAssertions;
using Xunit;

namespace Tw.Excel.Tests;

/// <summary>
/// 覆盖Excel模板Definition的核心行为和边界条件
/// </summary>
public sealed class ExcelTemplateDefinitionTests
{
    /// <summary>
    /// 验证创建拒绝动态Column数量OverLimit
    /// </summary>
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
