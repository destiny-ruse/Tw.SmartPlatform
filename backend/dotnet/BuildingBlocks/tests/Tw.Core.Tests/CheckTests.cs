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
        var value = " ";

        var act = () => Check.NotNullOrWhiteSpace(value);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(value));
    }

    [Fact]
    public void NotNullOrWhiteSpace_Throws_For_Null()
    {
        string? value = null;

        var act = () => Check.NotNullOrWhiteSpace(value);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(value));
    }

    [Fact]
    public void NotNullOrEmpty_String_Throws_For_Null()
    {
        string? value = null;

        var act = () => Check.NotNullOrEmpty(value);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(value));
    }

    [Fact]
    public void NotNullOrEmpty_String_Throws_For_Empty()
    {
        var value = string.Empty;

        var act = () => Check.NotNullOrEmpty(value);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(value));
    }

    [Fact]
    public void NotNullOrEmpty_String_Returns_Value()
    {
        var value = "value";

        var result = Check.NotNullOrEmpty(value);

        result.Should().BeSameAs(value);
    }

    [Fact]
    public void NotNullOrEmpty_Enumerable_Throws_For_Null()
    {
        IEnumerable<int>? value = null;

        var act = () => Check.NotNullOrEmpty(value);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(value));
    }

    [Fact]
    public void NotNullOrEmpty_Enumerable_Throws_For_Empty()
    {
        IEnumerable<int> value = [];

        var act = () => Check.NotNullOrEmpty(value);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(value));
    }

    [Fact]
    public void NotNullOrEmpty_Enumerable_Returns_Same_Reference()
    {
        IEnumerable<int> value = new[] { 1 };

        var result = Check.NotNullOrEmpty(value);

        result.Should().BeSameAs(value);
    }

    [Fact]
    public void NotNullOrEmpty_Collection_Throws_For_Empty()
    {
        ICollection<int> value = [];

        var act = () => Check.NotNullOrEmpty(value);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(value));
    }

    [Fact]
    public void NotNullOrEmpty_Collection_Returns_Same_Reference()
    {
        ICollection<int> value = [1];

        var result = Check.NotNullOrEmpty(value);

        result.Should().BeSameAs(value);
    }

    [Fact]
    public void Positive_Throws_For_Zero()
    {
        var act = () => Check.Positive(0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Positive_Long_Returns_Value()
    {
        const long value = 1L;

        var result = Check.Positive(value);

        result.Should().Be(value);
    }

    [Fact]
    public void Positive_Long_Throws_For_Zero()
    {
        const long value = 0L;

        var act = () => Check.Positive(value);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(value));
    }

    [Fact]
    public void NonNegative_Returns_Zero()
    {
        const int value = 0;

        var result = Check.NonNegative(value);

        result.Should().Be(value);
    }

    [Fact]
    public void NonNegative_Throws_For_Negative()
    {
        const int value = -1;

        var act = () => Check.NonNegative(value);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(value));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void InRange_Returns_Inclusive_Bounds(int value)
    {
        var result = Check.InRange(value, 1, 3);

        result.Should().Be(value);
    }

    [Fact]
    public void InRange_Throws_For_Out_Of_Range()
    {
        const int value = 4;

        var act = () => Check.InRange(value, 1, 3);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(value));
    }

    [Fact]
    public void AssignableTo_Returns_Input_Type()
    {
        var type = typeof(MemoryStream);

        var result = Check.AssignableTo<IDisposable>(type);

        result.Should().BeSameAs(type);
    }

    [Fact]
    public void AssignableTo_Throws_For_Unassignable_Type()
    {
        var type = typeof(string);

        var act = () => Check.AssignableTo<IDisposable>(type);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(type));
    }

    [Fact]
    public void AssignableTo_Type_Throws_For_Null_Type()
    {
        Type? type = null;

        var act = () => Check.AssignableTo(type, typeof(IDisposable));

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(type));
    }

    [Fact]
    public void AssignableTo_Type_Throws_For_Null_BaseType()
    {
        var type = typeof(string);
        Type baseType = null!;

        var act = () => Check.AssignableTo(type, baseType);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(baseType));
    }

    [Fact]
    public void AssignableTo_Type_Throws_For_Unassignable_Type()
    {
        var type = typeof(string);
        var baseType = typeof(IDisposable);

        var act = () => Check.AssignableTo(type, baseType);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(type));
    }
}
