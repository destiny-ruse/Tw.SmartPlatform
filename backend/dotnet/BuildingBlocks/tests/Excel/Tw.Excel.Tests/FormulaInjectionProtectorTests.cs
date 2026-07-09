using AwesomeAssertions;
using Xunit;

namespace Tw.Excel.Tests;

/// <summary>验证 FormulaInjectionProtectorTests 相关行为</summary>
public sealed class FormulaInjectionProtectorTests
{
    /// <summary>验证 Protect_PrefixesFormulaLikeUserText 场景</summary>
    /// <param name="value">value 参数</param>
    [Theory]
    [InlineData("=cmd|'/C calc'!A0")]
    [InlineData("+SUM(A1:A2)")]
    [InlineData("-10+20")]
    [InlineData("@user")]
    public void Protect_PrefixesFormulaLikeUserText(string value)
    {
        FormulaInjectionProtector.Protect(value).Should().Be("'" + value);
    }
}
