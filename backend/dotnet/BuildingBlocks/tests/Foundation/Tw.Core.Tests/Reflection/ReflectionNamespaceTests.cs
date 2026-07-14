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
    /// 类型查找实现位于约定的反射功能命名空间
    /// </summary>
    [Fact]
    public void TypeFinder_LivesIn_ReflectionNamespace()
    {
        typeof(TypeFinder).Namespace.Should().Be("Tw.Reflection");
    }

    /// <summary>
    /// 类型查找契约位于约定的反射功能命名空间
    /// </summary>
    [Fact]
    public void ITypeFinder_LivesIn_ReflectionNamespace()
    {
        typeof(ITypeFinder).Namespace.Should().Be("Tw.Reflection");
    }
}
