using AwesomeAssertions;
using Tw.Reflection;
using Xunit;

namespace Tw.Core.Tests.Reflection;

/// <summary>验证 ReflectionNamespaceTests 相关行为</summary>
public class ReflectionNamespaceTests
{
    /// <summary>验证 TypeFinder_LivesIn_TwReflectionNamespace 场景</summary>
    [Fact]
    public void TypeFinder_LivesIn_TwReflectionNamespace()
    {
        typeof(TypeFinder).Namespace.Should().Be("Tw.Reflection");
    }

    /// <summary>验证 ITypeFinder_LivesIn_TwReflectionNamespace 场景</summary>
    [Fact]
    public void ITypeFinder_LivesIn_TwReflectionNamespace()
    {
        typeof(ITypeFinder).Namespace.Should().Be("Tw.Reflection");
    }
}
