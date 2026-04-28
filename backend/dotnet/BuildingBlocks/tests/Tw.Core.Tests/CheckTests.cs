using FluentAssertions;
using Tw.Core;
using Xunit;

namespace Tw.Core.Tests;

public class CheckTests
{
    [Fact]
    public void NotNull_Returns_Value()
    {
        var value = new object();

        var result = Check.NotNull(value);

        result.Should().BeSameAs(value);
    }

    [Fact]
    public void NotNull_Throws_For_Null()
    {
        object? value = null;

        var act = () => Check.NotNull(value);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(value));
    }

    [Fact]
    public void NotNullOrWhiteSpace_Throws_For_Whitespace()
    {
        var act = () => Check.NotNullOrWhiteSpace(" ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Positive_Throws_For_Zero()
    {
        var act = () => Check.Positive(0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AssignableTo_Throws_For_Unassignable_Type()
    {
        var act = () => Check.AssignableTo<IDisposable>(typeof(string));

        act.Should().Throw<ArgumentException>();
    }
}
