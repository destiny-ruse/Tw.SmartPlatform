using FluentAssertions;
using Tw.Core.Utilities;
using Xunit;

namespace Tw.Core.Tests;

public class SecureRandomGeneratorTests
{
    private const string SpecialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";

    [Fact]
    public void GetInt_Returns_Value_In_Range()
    {
        var result = SecureRandomGenerator.GetInt(10, 20);

        result.Should().BeInRange(10, 19);
    }

    [Fact]
    public void GetInt_Throws_When_Min_Is_Not_Less_Than_Max()
    {
        var act = () => SecureRandomGenerator.GetInt(5, 5);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("maxValue");
    }

    [Fact]
    public void GetInt_Max_Returns_Value_In_Range()
    {
        var result = SecureRandomGenerator.GetInt(5);

        result.Should().BeInRange(0, 4);
    }

    [Fact]
    public void GetInt_Max_Throws_When_Max_Is_Not_Positive()
    {
        var act = () => SecureRandomGenerator.GetInt(0);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("maxValue");
    }

    [Fact]
    public void GetLong_Returns_Value_In_Range()
    {
        var result = SecureRandomGenerator.GetLong(long.MaxValue - 100, long.MaxValue);

        result.Should().BeInRange(long.MaxValue - 100, long.MaxValue - 1);
    }

    [Fact]
    public void GetLong_Returns_Value_In_Full_Long_Range()
    {
        var result = SecureRandomGenerator.GetLong(long.MinValue, long.MaxValue);

        result.Should().BeGreaterThanOrEqualTo(long.MinValue)
            .And.BeLessThan(long.MaxValue);
    }

    [Fact]
    public void GetLong_Returns_Value_In_Wide_Range()
    {
        var result = SecureRandomGenerator.GetLong(-9_000_000_000_000_000_000L, 9_000_000_000_000_000_000L);

        result.Should().BeGreaterThanOrEqualTo(-9_000_000_000_000_000_000L)
            .And.BeLessThan(9_000_000_000_000_000_000L);
    }

    [Fact]
    public void GetLong_Throws_When_Min_Is_Not_Less_Than_Max()
    {
        var act = () => SecureRandomGenerator.GetLong(8, 8);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("maxValue");
    }

    [Fact]
    public void GetDouble_Returns_Value_In_Range()
    {
        var result = SecureRandomGenerator.GetDouble();

        result.Should().BeGreaterThanOrEqualTo(0.0)
            .And.BeLessThan(1.0);
    }

    [Fact]
    public void GetDouble_Range_Returns_Value_In_Range()
    {
        var result = SecureRandomGenerator.GetDouble(1.5, 2.5);

        result.Should().BeGreaterThanOrEqualTo(1.5)
            .And.BeLessThan(2.5);
    }

    [Fact]
    public void GetDouble_Range_Stays_Half_Open_For_Tiny_Range()
    {
        var result = SecureRandomGenerator.GetDouble(0.0, double.Epsilon);

        result.Should().BeGreaterThanOrEqualTo(0.0)
            .And.BeLessThan(double.Epsilon);
    }

    [Theory]
    [InlineData(double.NaN, 1.0, "minValue")]
    [InlineData(1.0, double.PositiveInfinity, "maxValue")]
    [InlineData(2.0, 2.0, "maxValue")]
    public void GetDouble_Range_Validates_Bounds(double minValue, double maxValue, string parameterName)
    {
        var act = () => SecureRandomGenerator.GetDouble(minValue, maxValue);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(parameterName);
    }

    [Fact]
    public void GetDouble_Range_Rejects_Overflowing_Difference()
    {
        var act = () => SecureRandomGenerator.GetDouble(double.MinValue, double.MaxValue);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("maxValue");
    }

    [Fact]
    public void GetBool_Returns_Boolean()
    {
        var result = SecureRandomGenerator.GetBool();

        (result == true || result == false).Should().BeTrue();
    }

    [Fact]
    public void GetBytes_Returns_Requested_Length()
    {
        var result = SecureRandomGenerator.GetBytes(16);

        result.Should().HaveCount(16);
    }

    [Fact]
    public void GetBytes_Zero_Returns_Empty()
    {
        var result = SecureRandomGenerator.GetBytes(0);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetBytes_Throws_For_Negative_Length()
    {
        var act = () => SecureRandomGenerator.GetBytes(-1);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("length");
    }

    [Fact]
    public void GetString_Returns_Requested_Length_From_Default_Source()
    {
        var result = SecureRandomGenerator.GetString(12);

        result.Should().HaveLength(12);
        result.All(char.IsLetterOrDigit).Should().BeTrue();
    }

    [Fact]
    public void GetString_Returns_Requested_Length_From_Custom_Source()
    {
        var result = SecureRandomGenerator.GetString(8, "ab");

        result.Should().HaveLength(8);
        result.All(c => c is 'a' or 'b').Should().BeTrue();
    }

    [Fact]
    public void GetString_Validates_Length_And_Character_Source()
    {
        var negativeLength = () => SecureRandomGenerator.GetString(-1);
        var emptyChars = () => SecureRandomGenerator.GetString(1, string.Empty);

        negativeLength.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("length");
        emptyChars.Should().Throw<ArgumentException>()
            .WithParameterName("chars");
    }

    [Fact]
    public void String_Generators_Return_Requested_Lengths()
    {
        var numeric = SecureRandomGenerator.GetNumericString(6);
        var upperAlpha = SecureRandomGenerator.GetAlphaString(6);
        var lowerAlpha = SecureRandomGenerator.GetAlphaString(6, upperCase: false);
        var alphanumeric = SecureRandomGenerator.GetAlphanumericString(6);

        numeric.Should().HaveLength(6);
        numeric.All(char.IsDigit).Should().BeTrue();
        upperAlpha.Should().HaveLength(6);
        upperAlpha.All(char.IsUpper).Should().BeTrue();
        lowerAlpha.Should().HaveLength(6);
        lowerAlpha.All(char.IsLower).Should().BeTrue();
        alphanumeric.Should().HaveLength(6);
        alphanumeric.All(char.IsLetterOrDigit).Should().BeTrue();
    }

    [Fact]
    public void GetStrongPassword_Contains_Required_Categories()
    {
        var result = SecureRandomGenerator.GetStrongPassword(24);

        result.Should().HaveLength(24);
        result.Any(char.IsLower).Should().BeTrue();
        result.Any(char.IsUpper).Should().BeTrue();
        result.Any(char.IsDigit).Should().BeTrue();
        result.Any(c => SpecialChars.Contains(c)).Should().BeTrue();
    }

    [Fact]
    public void GetStrongPassword_Rejects_Too_Short_Length()
    {
        var withSpecialChars = () => SecureRandomGenerator.GetStrongPassword(3);
        var withoutSpecialChars = () => SecureRandomGenerator.GetStrongPassword(2, includeSpecialChars: false);

        withSpecialChars.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("length");
        withoutSpecialChars.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("length");
    }

    [Fact]
    public void GetStrongPassword_Without_Special_Chars_Contains_Required_Categories()
    {
        var result = SecureRandomGenerator.GetStrongPassword(24, includeSpecialChars: false);

        result.Should().HaveLength(24);
        result.Any(char.IsLower).Should().BeTrue();
        result.Any(char.IsUpper).Should().BeTrue();
        result.Any(char.IsDigit).Should().BeTrue();
        result.All(char.IsLetterOrDigit).Should().BeTrue();
    }

    [Fact]
    public void GetRandomElement_Returns_Source_Value()
    {
        var source = new[] { 1, 2, 3 };

        var result = SecureRandomGenerator.GetRandomElement(source);

        result.Should().BeOneOf(source);
    }

    [Fact]
    public void GetRandomElement_Rejects_Null_Or_Empty_List()
    {
        IList<int> nullList = null!;
        var emptyList = Array.Empty<int>();

        var nullAct = () => SecureRandomGenerator.GetRandomElement(nullList);
        var emptyAct = () => SecureRandomGenerator.GetRandomElement(emptyList);

        nullAct.Should().Throw<ArgumentNullException>()
            .WithParameterName("collection");
        emptyAct.Should().Throw<ArgumentException>()
            .WithParameterName("collection");
    }

    [Fact]
    public void GetRandomElements_Returns_Unique_Selection()
    {
        var source = Enumerable.Range(1, 10).ToArray();

        var result = SecureRandomGenerator.GetRandomElements(source, 4);

        result.Should().HaveCount(4)
            .And.OnlyHaveUniqueItems()
            .And.OnlyContain(item => source.Contains(item));
    }

    [Fact]
    public void GetRandomElements_Returns_Unique_Values_From_Duplicate_Source()
    {
        var source = new[] { 1, 1, 2, 2, 3, 3 };

        var result = SecureRandomGenerator.GetRandomElements(source, 3);

        result.Should().HaveCount(3)
            .And.OnlyHaveUniqueItems()
            .And.BeEquivalentTo([1, 2, 3]);
    }

    [Fact]
    public void GetRandomElements_Throws_When_Count_Exceeds_Distinct_Source_Count()
    {
        var source = new[] { 1, 1, 2 };

        var act = () => SecureRandomGenerator.GetRandomElements(source, 3);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("count");
    }

    [Fact]
    public void GetRandomElements_Validates_Arguments()
    {
        IList<int> nullList = null!;
        var source = new[] { 1, 2, 3 };

        var nullAct = () => SecureRandomGenerator.GetRandomElements(nullList, 1);
        var negativeAct = () => SecureRandomGenerator.GetRandomElements(source, -1);
        var tooManyAct = () => SecureRandomGenerator.GetRandomElements(source, 4);

        nullAct.Should().Throw<ArgumentNullException>()
            .WithParameterName("collection");
        negativeAct.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("count");
        tooManyAct.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("count");
    }

    [Fact]
    public void GetRandomElements_Rejects_Empty_Collection_When_Count_Is_Zero()
    {
        var source = Array.Empty<int>();

        var act = () => SecureRandomGenerator.GetRandomElements(source, 0);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("collection");
    }

    [Fact]
    public void GetRandomElements_Returns_New_List_And_Does_Not_Mutate_Source()
    {
        var source = new List<int> { 1, 2, 3, 4 };

        var result = SecureRandomGenerator.GetRandomElements(source, 0);

        result.Should().NotBeSameAs(source)
            .And.BeEmpty();
        source.Should().Equal(1, 2, 3, 4);
    }

    [Fact]
    public void Shuffle_List_Returns_New_List_With_Source_Content()
    {
        var source = new List<int> { 1, 2, 3, 4 };

        var result = SecureRandomGenerator.Shuffle(source);

        result.Should().NotBeSameAs(source)
            .And.BeEquivalentTo(source);
        source.Should().Equal(1, 2, 3, 4);
    }

    [Fact]
    public void Shuffle_List_Rejects_Empty_Collection()
    {
        var source = Array.Empty<int>();

        var act = () => SecureRandomGenerator.Shuffle(source);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("collection");
    }

    [Fact]
    public void Shuffle_String_Returns_Source_Content()
    {
        const string value = "aabbcc";

        var result = SecureRandomGenerator.Shuffle(value);

        result.Should().HaveLength(value.Length);
        result.Order().Should().Equal(value.Order());
    }

    [Fact]
    public void Shuffle_String_Returns_Empty_For_Empty_String()
    {
        var result = SecureRandomGenerator.Shuffle(string.Empty);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Shuffle_Rejects_Null_String()
    {
        string value = null!;

        var act = () => SecureRandomGenerator.Shuffle(value);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(value));
    }
}
