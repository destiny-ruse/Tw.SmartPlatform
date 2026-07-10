using AwesomeAssertions;
using Tw.Reflection;
using Xunit;

namespace Tw.Core.Tests.Reflection;

/// <summary>
/// 覆盖ReflectionNamespace的核心行为和边界条件
/// </summary>
public class ReflectionNamespaceTests
{
    /// <summary>
    /// 验证类型FinderLivesInTwReflectionNamespace
    /// </summary>
    [Fact]
    public void TypeFinder_LivesIn_TwReflectionNamespace()
    {
        typeof(TypeFinder).Namespace.Should().Be("Tw.Reflection");
    }

    /// <summary>
    /// 验证类型FinderLivesInTwReflectionNamespace
    /// </summary>
    [Fact]
    public void ITypeFinder_LivesIn_TwReflectionNamespace()
    {
        typeof(ITypeFinder).Namespace.Should().Be("Tw.Reflection");
    }
}
