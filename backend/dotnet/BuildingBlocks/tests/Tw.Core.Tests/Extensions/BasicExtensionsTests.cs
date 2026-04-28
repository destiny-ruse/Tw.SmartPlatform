using FluentAssertions;
using System.Reflection;
using System.Text;
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
        var nullLeft = () => ((string)null!).Left(3);
        var nullRight = () => ((string)null!).Right(3);

        nullLeft.Should().Throw<ArgumentNullException>().WithParameterName("str");
        nullRight.Should().Throw<ArgumentNullException>().WithParameterName("str");
        ((string?)null).EnsureStartsWith('/').Should().Be("/");
        ((string?)null).EnsureEndsWith('/').Should().Be("/");
        "path".EnsureStartsWith('/').Should().Be("/path");
        "path".EnsureEndsWith('/').Should().Be("path/");
        "path/".EnsureEndsWith('/', StringComparison.OrdinalIgnoreCase).Should().Be("path/");
        "/path".EnsureStartsWith('/', StringComparison.OrdinalIgnoreCase).Should().Be("/path");
        "one,two,,three".Split(",", StringSplitOptions.RemoveEmptyEntries).Should().Equal("one", "two", "three");
        "a\r\nb\nc".SplitToLines().Should().Equal("a", "b", "c");
        "héllo".GetBytes().Should().Equal("héllo".ToBase64().FromBase64().GetBytes());
        "abcabc".NthIndexOf('a', 2).Should().Be(3);
        "HelloWorld".RemovePostFix("World").Should().Be("Hello");
        "HelloWorld".RemovePreFix("Hello").Should().Be("World");
        "HelloWorld".ReplaceFirst("World", replace: "Core").Should().Be("HelloCore");
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
        var nullNthIndex = () => ((string?)null).NthIndexOf('x', 0);

        left.Should().Throw<ArgumentOutOfRangeException>();
        right.Should().Throw<ArgumentOutOfRangeException>();
        truncate.Should().Throw<ArgumentOutOfRangeException>();
        chunk.Should().Throw<ArgumentOutOfRangeException>();
        nullNthIndex.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("n");
    }

    [Fact]
    public void String_Public_Api_Signatures_Match_Plan()
    {
        var methods = typeof(StringExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.DeclaringType == typeof(StringExtensions))
            .ToArray();

        AssertSignature(methods, nameof(StringExtensions.ToPascalCase), typeof(string), ["str"], [typeof(string)]);
        AssertSignature(methods, nameof(StringExtensions.ToCamelCase), typeof(string), ["str"], [typeof(string)]);
        AssertSignature(methods, nameof(StringExtensions.ToSnakeCase), typeof(string), ["str"], [typeof(string)]);
        AssertSignature(methods, nameof(StringExtensions.EnsureEndsWith), typeof(string), ["str", "c", "comparisonType"], [typeof(string), typeof(char), typeof(StringComparison)]);
        AssertSignature(methods, nameof(StringExtensions.EnsureStartsWith), typeof(string), ["str", "c", "comparisonType"], [typeof(string), typeof(char), typeof(StringComparison)]);
        AssertSignature(methods, nameof(StringExtensions.Left), typeof(string), ["str", "len"], [typeof(string), typeof(int)]);
        AssertSignature(methods, nameof(StringExtensions.Right), typeof(string), ["str", "len"], [typeof(string), typeof(int)]);
        AssertSignature(methods, nameof(StringExtensions.NormalizeLineEndings), typeof(string), ["str"], [typeof(string)]);
        AssertSignature(methods, nameof(StringExtensions.NthIndexOf), typeof(int), ["str", "c", "n"], [typeof(string), typeof(char), typeof(int)]);
        AssertSignature(methods, nameof(StringExtensions.ReplaceFirst), typeof(string), ["str", "search", "replace", "comparisonType"], [typeof(string), typeof(string), typeof(string), typeof(StringComparison)]);
        AssertSignature(methods, nameof(StringExtensions.ToBase64), typeof(string), ["value", "encoding"], [typeof(string), typeof(Encoding)]);
        AssertSignature(methods, nameof(StringExtensions.FromBase64), typeof(string), ["value", "encoding"], [typeof(string), typeof(Encoding)]);

        methods.Where(method => method.Name == nameof(StringExtensions.RemovePostFix)).Should().HaveCount(2);
        AssertSignature(methods, nameof(StringExtensions.RemovePostFix), typeof(string), ["str", "postFixes"], [typeof(string), typeof(string[])]);
        AssertSignature(methods, nameof(StringExtensions.RemovePostFix), typeof(string), ["str", "comparisonType", "postFixes"], [typeof(string), typeof(StringComparison), typeof(string[])]);

        methods.Where(method => method.Name == nameof(StringExtensions.RemovePreFix)).Should().HaveCount(2);
        AssertSignature(methods, nameof(StringExtensions.RemovePreFix), typeof(string), ["str", "preFixes"], [typeof(string), typeof(string[])]);
        AssertSignature(methods, nameof(StringExtensions.RemovePreFix), typeof(string), ["str", "comparisonType", "preFixes"], [typeof(string), typeof(StringComparison), typeof(string[])]);

        methods.Where(method => method.Name == nameof(StringExtensions.Split)).Should().HaveCount(2);
        AssertSignature(methods, nameof(StringExtensions.Split), typeof(string[]), ["str", "separator"], [typeof(string), typeof(string)]);
        AssertSignature(methods, nameof(StringExtensions.Split), typeof(string[]), ["str", "separator", "options"], [typeof(string), typeof(string), typeof(StringSplitOptions)]);

        methods.Where(method => method.Name == nameof(StringExtensions.SplitToLines)).Should().HaveCount(2);
        AssertSignature(methods, nameof(StringExtensions.SplitToLines), typeof(string[]), ["str"], [typeof(string)]);
        AssertSignature(methods, nameof(StringExtensions.SplitToLines), typeof(string[]), ["str", "options"], [typeof(string), typeof(StringSplitOptions)]);

        methods.Where(method => method.Name == nameof(StringExtensions.TruncateWithPostfix)).Should().HaveCount(2);
        AssertSignature(methods, nameof(StringExtensions.TruncateWithPostfix), typeof(string), ["str", "maxLength"], [typeof(string), typeof(int)]);
        AssertSignature(methods, nameof(StringExtensions.TruncateWithPostfix), typeof(string), ["str", "maxLength", "postfix"], [typeof(string), typeof(int), typeof(string)]);

        methods.Where(method => method.Name == nameof(StringExtensions.GetBytes)).Should().HaveCount(2);
        AssertSignature(methods, nameof(StringExtensions.GetBytes), typeof(byte[]), ["str"], [typeof(string)]);
        AssertSignature(methods, nameof(StringExtensions.GetBytes), typeof(byte[]), ["str", "encoding"], [typeof(string), typeof(Encoding)]);
        AssertSignature(methods, nameof(StringExtensions.Truncate), typeof(string), ["str", "maxLength"], [typeof(string), typeof(int)]);
        AssertSignature(methods, nameof(StringExtensions.TruncateFromBeginning), typeof(string), ["str", "maxLength"], [typeof(string), typeof(int)]);
        AssertSignature(methods, nameof(StringExtensions.Chunk), typeof(IEnumerable<string>), ["value", "chunkSize"], [typeof(string), typeof(int)]);
        AssertSignature(methods, nameof(StringExtensions.FormatWith), typeof(string), ["template", "args"], [typeof(string), typeof(object[])]);
        AssertSignature(methods, nameof(StringExtensions.RemoveWhiteSpace), typeof(string), ["value"], [typeof(string)]);
        AssertSignature(methods, nameof(StringExtensions.Reverse), typeof(string), ["value"], [typeof(string)]);
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
    public void DateTime_Boundaries_Clamp_Extrema()
    {
        DateTime.MaxValue.EndOfDay().Should().Be(DateTime.MaxValue);
        DateTime.MaxValue.EndOfWeek().Should().Be(DateTime.MaxValue);
        DateTime.MaxValue.EndOfMonth().Should().Be(DateTime.MaxValue);
        DateTime.MaxValue.EndOfYear().Should().Be(DateTime.MaxValue);
        DateTime.MinValue.StartOfWeek().Should().Be(DateTime.MinValue);
    }

    [Fact]
    public void DateTime_Conversions_And_State_Work()
    {
        var utc = new DateTime(2026, 4, 28, 13, 10, 9, DateTimeKind.Utc);
        var timestamp = utc.ToUnixTimestamp();
        var timestampMilliseconds = utc.ToUnixTimestampMilliseconds();

        timestamp.FromUnixTimestamp().Should().Be(utc);
        timestampMilliseconds.FromUnixTimestampMilliseconds().Should().Be(utc);
        utc.StartOfWeek().DayOfWeek.Should().Be(DayOfWeek.Monday);
        utc.EndOfWeek().Should().Be(utc.StartOfWeek().AddDays(7).AddTicks(-1));
        new DateTime(2026, 5, 2).IsWeekend().Should().BeTrue();
        new DateTime(2026, 5, 4).IsWeekday().Should().BeTrue();
        DateTime.Today.AddYears(-25).AddDays(-1).CalculateAge().Should().Be(25);
        DateTime.Today.IsToday().Should().BeTrue();
        DateTime.UtcNow.AddDays(-1).IsPast().Should().BeTrue();
        DateTime.UtcNow.AddDays(1).IsFuture().Should().BeTrue();
        utc.ToFriendlyString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void DateTime_Public_Api_Signatures_Match_Plan()
    {
        var methods = typeof(DateTimeExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.DeclaringType == typeof(DateTimeExtensions))
            .ToArray();

        AssertSignature(methods, nameof(DateTimeExtensions.CalculateAge), typeof(int), ["birthDate"], [typeof(DateTime)]);
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
        0.1234d.ToPercentage(decimals: 1).Should().Be("12.3%");
        0.1234m.ToPercentage(decimals: 1).Should().Be("12.3%");
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
    public void Object_As_Validates_Null_And_Failed_Cast()
    {
        object nullObject = null!;
        object incompatible = 42;

        var nullCast = () => ObjectExtensions.As<string>(nullObject);
        var invalidCast = () => ObjectExtensions.As<string>(incompatible);

        nullCast.Should().Throw<ArgumentNullException>().WithParameterName("obj");
        invalidCast.Should().Throw<InvalidCastException>()
            .WithMessage("*System.Int32*System.String*");
    }

    [Fact]
    public void Object_To_Validates_Null()
    {
        object nullObject = null!;

        var act = () => nullObject.To<int>();

        act.Should().Throw<ArgumentNullException>().WithParameterName("obj");
    }

    [Fact]
    public void Exception_ReThrow_Validates_Null_And_Preserves_Stack()
    {
        Exception exception = null!;
        var nullRethrow = () => exception.ReThrow();
        var captured = CaptureExceptionFromThrowingHelper();

        var rethrow = () => captured.ReThrow();

        nullRethrow.Should().Throw<ArgumentNullException>().WithParameterName(nameof(exception));
        rethrow.Should().Throw<InvalidOperationException>()
            .Which.StackTrace.Should().Contain(nameof(ThrowingHelper));
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

        Type[] baseClasses = typeof(MemoryStream).GetBaseClasses();

        baseClasses.Should().Contain(typeof(Stream));
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
        Type[] baseClassesBeforeStream = typeof(MemoryStream).GetBaseClasses(typeof(Stream));

        baseClassesBeforeStream.Should().NotContain(typeof(Stream));
    }

    private static void AssertSignature(
        MethodInfo[] methods,
        string name,
        Type returnType,
        string[] parameterNames,
        Type[] parameterTypes)
    {
        var match = methods.SingleOrDefault(method =>
            method.Name == name &&
            method.ReturnType == returnType &&
            method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(parameterTypes));

        match.Should().NotBeNull();
        match!.GetParameters().Select(parameter => parameter.Name).Should().Equal(parameterNames);
    }

    private static Exception CaptureExceptionFromThrowingHelper()
    {
        try
        {
            ThrowingHelper();
        }
        catch (Exception exception)
        {
            return exception;
        }

        throw new InvalidOperationException("辅助方法没有抛出异常。");
    }

    private static void ThrowingHelper()
    {
        throw new InvalidOperationException("原始失败。");
    }
}
