using AwesomeAssertions;
using Xunit;

namespace Tw.Excel.Tests;

/// <summary>
/// 覆盖Formula注入Protector的核心行为和边界条件
/// </summary>
public sealed class FormulaInjectionProtectorTests
{
    /// <summary>
    /// 验证ProtectPrefixesFormulaLike用户文本
    /// </summary>
    /// <param name="value">用于转换、回显或断言的输入值</param>
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
