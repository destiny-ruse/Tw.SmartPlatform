using AwesomeAssertions;
using Tw.Security.Cryptography;
using Xunit;

namespace Tw.Security.Tests.Cryptography;

/// <summary>
/// 覆盖密码学安全随机值生成器的范围和输入契约
/// </summary>
public sealed class SecureRandomGeneratorTests
{
    /// <summary>
    /// 验证生成的随机整数始终位于请求的半开区间内
    /// </summary>
    [Fact]
    public void GetInt_ReturnsValuesWithinRequestedRange()
    {
        var values = Enumerable.Range(0, 64)
            .Select(_ => SecureRandomGenerator.GetInt(-10, 10));

        values.Should().OnlyContain(value => value >= -10 && value < 10);
    }

    /// <summary>
    /// 验证生成的随机长整数始终位于请求的半开区间内
    /// </summary>
    [Fact]
    public void GetLong_ReturnsValuesWithinRequestedRange()
    {
        var values = Enumerable.Range(0, 64)
            .Select(_ => SecureRandomGenerator.GetLong(-1_000_000_000_000, 1_000_000_000_000));

        values.Should().OnlyContain(value => value >= -1_000_000_000_000 && value < 1_000_000_000_000);
    }

    /// <summary>
    /// 验证有限浮点边界生成的值保持有限且位于半开区间
    /// </summary>
    [Fact]
    public void GetDouble_WithFiniteBounds_ReturnsFiniteValuesWithinRequestedRange()
    {
        var values = Enumerable.Range(0, 64)
            .Select(_ => SecureRandomGenerator.GetDouble(-123.5, 456.75));

        values.Should().OnlyContain(value => double.IsFinite(value) && value >= -123.5 && value < 456.75);
    }

    /// <summary>
    /// 验证拒绝虽为有限边界但区间跨度溢出为无穷大的请求
    /// </summary>
    [Fact]
    public void GetDouble_WithFiniteBoundsWhoseSpanOverflows_ThrowsArgumentException()
    {
        var act = () => SecureRandomGenerator.GetDouble(double.MinValue, double.MaxValue);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// 验证拒绝非有限的浮点边界
    /// </summary>
    /// <param name="minValue">请求的下界</param>
    /// <param name="maxValue">请求的上界</param>
    [Theory]
    [InlineData(double.NaN, 1.0)]
    [InlineData(0.0, double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity, 0.0)]
    public void GetDouble_WithNonFiniteBound_ThrowsArgumentException(double minValue, double maxValue)
    {
        var act = () => SecureRandomGenerator.GetDouble(minValue, maxValue);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// 验证生成的随机字节数组具有请求长度
    /// </summary>
    [Fact]
    public void GetBytes_ReturnsRequestedLength()
    {
        var bytes = SecureRandomGenerator.GetBytes(32);

        bytes.Should().HaveCount(32);
    }

    /// <summary>
    /// 验证零长度字节和字符串请求返回空值
    /// </summary>
    [Fact]
    public void GetBytesAndString_WithZeroLength_ReturnEmptyValues()
    {
        SecureRandomGenerator.GetBytes(0).Should().BeEmpty();
        SecureRandomGenerator.GetString(0).Should().BeEmpty();
        SecureRandomGenerator.GetNumericString(0).Should().BeEmpty();
        SecureRandomGenerator.GetAlphaString(0).Should().BeEmpty();
        SecureRandomGenerator.GetAlphanumericString(0).Should().BeEmpty();
    }

    /// <summary>
    /// 验证生成随机字节时拒绝负长度
    /// </summary>
    [Fact]
    public void GetBytes_WithNegativeLength_ThrowsArgumentOutOfRangeException()
    {
        var act = () => SecureRandomGenerator.GetBytes(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// 验证生成随机字符串时拒绝负长度
    /// </summary>
    [Fact]
    public void StringGenerators_WithNegativeLength_ThrowArgumentOutOfRangeException()
    {
        var getString = () => SecureRandomGenerator.GetString(-1);
        var getNumericString = () => SecureRandomGenerator.GetNumericString(-1);
        var getAlphaString = () => SecureRandomGenerator.GetAlphaString(-1);
        var getAlphanumericString = () => SecureRandomGenerator.GetAlphanumericString(-1);

        getString.Should().Throw<ArgumentOutOfRangeException>();
        getNumericString.Should().Throw<ArgumentOutOfRangeException>();
        getAlphaString.Should().Throw<ArgumentOutOfRangeException>();
        getAlphanumericString.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// 验证生成随机字符串时拒绝空字符源
    /// </summary>
    [Fact]
    public void GetString_WithEmptyCharacterSource_ThrowsArgumentException()
    {
        var act = () => SecureRandomGenerator.GetString(1, string.Empty);

        act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("chars");
    }

    /// <summary>
    /// 验证强密码包含请求的长度和每个必需字符类别
    /// </summary>
    [Fact]
    public void GetStrongPassword_WithSpecialCharacters_ContainsRequiredCharacterCategories()
    {
        var password = SecureRandomGenerator.GetStrongPassword(length: 16, includeSpecialChars: true);

        password.Should().HaveLength(16);
        password.Any(char.IsLower).Should().BeTrue();
        password.Any(char.IsUpper).Should().BeTrue();
        password.Any(char.IsDigit).Should().BeTrue();
        password.Any(character => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(character)).Should().BeTrue();
    }

    /// <summary>
    /// 验证不含特殊字符的强密码仅使用字母数字并保留必需类别
    /// </summary>
    [Fact]
    public void GetStrongPassword_WithoutSpecialCharacters_ContainsOnlyRequiredAlphanumericCategories()
    {
        var password = SecureRandomGenerator.GetStrongPassword(length: 12, includeSpecialChars: false);

        password.Should().HaveLength(12);
        password.Any(char.IsLower).Should().BeTrue();
        password.Any(char.IsUpper).Should().BeTrue();
        password.Any(char.IsDigit).Should().BeTrue();
        password.All(char.IsLetterOrDigit).Should().BeTrue();
    }

    /// <summary>
    /// 验证强密码接受刚好容纳必需字符类别的最小长度
    /// </summary>
    /// <param name="length">请求的最小密码长度</param>
    /// <param name="includeSpecialChars">是否要求特殊字符</param>
    [Theory]
    [InlineData(4, true)]
    [InlineData(3, false)]
    public void GetStrongPassword_AtMinimumLength_ReturnsRequestedLength(
        int length,
        bool includeSpecialChars)
    {
        var password = SecureRandomGenerator.GetStrongPassword(length, includeSpecialChars);

        password.Should().HaveLength(length);
    }

    /// <summary>
    /// 验证强密码长度不能少于启用模式要求的字符类别数
    /// </summary>
    /// <param name="length">请求的密码长度</param>
    /// <param name="includeSpecialChars">是否要求特殊字符</param>
    [Theory]
    [InlineData(3, true)]
    [InlineData(2, false)]
    public void GetStrongPassword_ShorterThanRequiredCategories_ThrowsArgumentOutOfRangeException(
        int length,
        bool includeSpecialChars)
    {
        var act = () => SecureRandomGenerator.GetStrongPassword(length, includeSpecialChars);

        act.Should().Throw<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("length");
    }

    /// <summary>
    /// 验证随机元素和集合操作拒绝空集合
    /// </summary>
    [Fact]
    public void CollectionOperations_WithEmptyCollection_ThrowArgumentException()
    {
        var empty = Array.Empty<int>();
        var getElement = () => SecureRandomGenerator.GetRandomElement(empty);
        var getElements = () => SecureRandomGenerator.GetRandomElements(empty, 0);
        var shuffle = () => SecureRandomGenerator.Shuffle(empty);

        getElement.Should().Throw<ArgumentException>();
        getElements.Should().Throw<ArgumentException>();
        shuffle.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// 验证随机选择的数量按不重复元素计算
    /// </summary>
    [Fact]
    public void GetRandomElements_WithDuplicateSourceValues_ReturnsRequestedDistinctCount()
    {
        int[] source = [1, 1, 2, 2, 3];

        var selected = SecureRandomGenerator.GetRandomElements(source, 3);

        selected.Should().HaveCount(3);
        selected.Should().OnlyHaveUniqueItems();
        selected.Should().OnlyContain(value => source.Contains(value));
    }

    /// <summary>
    /// 验证请求数量超过不重复元素数时失败
    /// </summary>
    [Fact]
    public void GetRandomElements_WithCountExceedingDistinctValues_ThrowsArgumentOutOfRangeException()
    {
        int[] source = [1, 1, 2];
        var act = () => SecureRandomGenerator.GetRandomElements(source, 3);

        act.Should().Throw<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("count");
    }

    /// <summary>
    /// 验证随机选择和打乱操作不修改输入集合
    /// </summary>
    [Fact]
    public void CollectionOperations_DoNotModifyInputCollection()
    {
        int[] source = [1, 2, 3, 4, 5];
        var original = source.ToArray();

        _ = SecureRandomGenerator.GetRandomElements(source, 3);
        _ = SecureRandomGenerator.Shuffle(source);

        source.Should().Equal(original);
    }
}
