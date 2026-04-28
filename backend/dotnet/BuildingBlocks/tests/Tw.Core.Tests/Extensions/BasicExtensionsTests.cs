using FluentAssertions;
using Tw.Core.Extensions;
using Xunit;

namespace Tw.Core.Tests.Extensions;

public class BasicExtensionsTests
{
    [Fact]
    public void ByteArray_ToHexString_Returns_Lowercase_By_Default()
    {
        new byte[] { 0x0a, 0xff }.ToHexString().Should().Be("0aff");
    }

    [Fact]
    public void ByteArray_ToHexString_Supports_Uppercase_And_Validates_Null()
    {
        new byte[] { 0x0a, 0xff }.ToHexString(useUpperCase: true).Should().Be("0AFF");

        byte[] bytes = null!;

        var act = () => bytes.ToHexString();

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(bytes));
    }

    [Fact]
    public void String_Casing_And_Truncation_Work()
    {
        "hello".ToPascalCase().Should().Be("Hello");
        "Hello".ToCamelCase().Should().Be("hello");
        "HelloWorld".ToSnakeCase().Should().Be("hello_world");
        "abcdef".Left(3).Should().Be("abc");
        "abcdef".Right(2).Should().Be("ef");
        "abcdef".Truncate(3).Should().Be("abc");
    }

    [Fact]
    public void String_Edge_Cases_Work()
    {
        ((string?)null).Left(3).Should().BeNull();
        ((string?)null).Right(3).Should().BeNull();
        ((string?)null).EnsureStartsWith('/').Should().Be("/");
        ((string?)null).EnsureEndsWith('/').Should().Be("/");
        "path".EnsureStartsWith('/').Should().Be("/path");
        "path".EnsureEndsWith('/').Should().Be("path/");
        "one,two,,three".Split(",", StringSplitOptions.RemoveEmptyEntries).Should().Equal("one", "two", "three");
        "a\r\nb\nc".SplitToLines().Should().Equal("a", "b", "c");
        "héllo".GetBytes().Should().Equal("héllo".ToBase64().FromBase64().GetBytes());
        "abcabc".NthIndexOf("abc", 2).Should().Be(3);
        "HelloWorld".RemovePostFix("World").Should().Be("Hello");
        "HelloWorld".RemovePreFix("Hello").Should().Be("World");
        "one two\tthree".RemoveWhiteSpace().Should().Be("onetwothree");
        "abc".Reverse().Should().Be("cba");
        "abcdef".TruncateFromBeginning(3).Should().Be("def");
        "abcdef".TruncateWithPostfix(5, "..").Should().Be("abc..");
        "abcdef".Chunk(2).Should().Equal("ab", "cd", "ef");
        "{0}-{1}".FormatWith("a", 1).Should().Be("a-1");
    }

    [Fact]
    public void String_Invalid_Lengths_Throw()
    {
        var left = () => "abc".Left(-1);
        var right = () => "abc".Right(-1);
        var truncate = () => "abc".Truncate(-1);
        var chunk = () => "abc".Chunk(0).ToArray();

        left.Should().Throw<ArgumentOutOfRangeException>();
        right.Should().Throw<ArgumentOutOfRangeException>();
        truncate.Should().Throw<ArgumentOutOfRangeException>();
        chunk.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void DateTime_Boundaries_Work()
    {
        var value = new DateTime(2026, 4, 28, 13, 10, 9);

        value.StartOfDay().Should().Be(new DateTime(2026, 4, 28));
        value.StartOfMonth().Should().Be(new DateTime(2026, 4, 1));
        value.StartOfYear().Should().Be(new DateTime(2026, 1, 1));
        value.EndOfDay().Should().Be(new DateTime(2026, 4, 28).AddDays(1).AddTicks(-1));
        value.EndOfMonth().Should().Be(new DateTime(2026, 5, 1).AddTicks(-1));
        value.EndOfYear().Should().Be(new DateTime(2027, 1, 1).AddTicks(-1));
    }

    [Fact]
    public void DateTime_Conversions_And_State_Work()
    {
        var utc = new DateTime(2026, 4, 28, 13, 10, 9, DateTimeKind.Utc);
        var timestamp = utc.ToUnixTimestamp();
        var timestampMilliseconds = utc.ToUnixTimestampMilliseconds();

        DateTimeExtensions.FromUnixTimestamp(timestamp).Should().Be(utc);
        DateTimeExtensions.FromUnixTimestampMilliseconds(timestampMilliseconds).Should().Be(utc);
        utc.StartOfWeek().DayOfWeek.Should().Be(DayOfWeek.Monday);
        utc.EndOfWeek().Should().Be(utc.StartOfWeek().AddDays(7).AddTicks(-1));
        new DateTime(2026, 5, 2).IsWeekend().Should().BeTrue();
        new DateTime(2026, 5, 4).IsWeekday().Should().BeTrue();
        new DateTime(2000, 12, 31).CalculateAge(new DateTime(2026, 4, 28)).Should().Be(25);
        DateTime.Today.IsToday().Should().BeTrue();
        DateTime.UtcNow.AddDays(-1).IsPast().Should().BeTrue();
        DateTime.UtcNow.AddDays(1).IsFuture().Should().BeTrue();
        utc.ToFriendlyString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Number_And_Guid_Extensions_Work()
    {
        2.IsEven().Should().BeTrue();
        3.IsOdd().Should().BeTrue();
        2L.IsEven().Should().BeTrue();
        3L.IsOdd().Should().BeTrue();
        10.Clamp(0, 5).Should().Be(5);
        10L.Clamp(0, 5).Should().Be(5);
        10.5d.Clamp(0, 5).Should().Be(5);
        10.5m.Clamp(0, 5).Should().Be(5);
        ((Guid?)Guid.Empty).IsNullOrEmpty().Should().BeTrue();
        Guid.Parse("00112233-4455-6677-8899-aabbccddeeff").ToNString().Should().Be("00112233445566778899aabbccddeeff");
    }

    [Fact]
    public void Number_Invalid_Ranges_Throw()
    {
        var intClamp = () => 1.Clamp(2, 1);
        var longClamp = () => 1L.Clamp(2, 1);
        var doubleClamp = () => 1d.Clamp(2, 1);
        var decimalClamp = () => 1m.Clamp(2, 1);

        intClamp.Should().Throw<ArgumentException>().WithParameterName("max");
        longClamp.Should().Throw<ArgumentException>().WithParameterName("max");
        doubleClamp.Should().Throw<ArgumentException>().WithParameterName("max");
        decimalClamp.Should().Throw<ArgumentException>().WithParameterName("max");
    }

    [Fact]
    public void Number_Formatting_Work()
    {
        1536L.ToFileSize().Should().Be("1.50 KB");
        1.234d.Round(2).Should().Be(1.23);
        1.235m.Round(2).Should().Be(1.24m);
        0.1234d.ToPercentage(1).Should().Be("12.3%");
        0.1234m.ToPercentage(1).Should().Be("12.3%");
    }

    [Fact]
    public void Object_Extensions_Work()
    {
        object text = "value";
        object number = "42";
        var invoked = false;

        ObjectExtensions.As<string>(text).Should().Be("value");
        number.To<int>().Should().Be(42);
        2.IsIn(1, 2, 3).Should().BeTrue();
        2.IsIn(new[] { 1, 2, 3 }).Should().BeTrue();
        2.If(true, x => x + 1).Should().Be(3);
        "value".If(true, _ => invoked = true).Should().Be("value");
        invoked.Should().BeTrue();
    }

    [Fact]
    public void Comparable_IsBetween_Works()
    {
        2.IsBetween(1, 3).Should().BeTrue();
        4.IsBetween(1, 3).Should().BeFalse();
    }

    [Fact]
    public void Type_Extensions_Return_Assignability_And_Base_Classes()
    {
        typeof(MemoryStream).IsAssignableTo<Stream>().Should().BeTrue();
        typeof(MemoryStream).GetBaseClasses().Should().Contain(typeof(Stream));
    }

    [Fact]
    public void Type_Extensions_Validate_Null_And_Stop_Before_StoppingType()
    {
        Type type = null!;
        Type targetType = null!;

        var assignable = () => type.IsAssignableTo<Stream>();
        var assignableToType = () => global::Tw.Core.Extensions.TypeExtensions.IsAssignableTo(typeof(MemoryStream), targetType);
        var baseClasses = () => type.GetBaseClasses().ToArray();

        assignable.Should().Throw<ArgumentNullException>().WithParameterName(nameof(type));
        assignableToType.Should().Throw<ArgumentNullException>().WithParameterName(nameof(targetType));
        baseClasses.Should().Throw<ArgumentNullException>().WithParameterName(nameof(type));
        typeof(MemoryStream).GetBaseClasses(typeof(Stream)).Should().NotContain(typeof(Stream));
    }
}
