using AwesomeAssertions;
using Xunit;

namespace Tw.Excel.Tests;

public sealed class FormulaInjectionProtectorTests
{
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
