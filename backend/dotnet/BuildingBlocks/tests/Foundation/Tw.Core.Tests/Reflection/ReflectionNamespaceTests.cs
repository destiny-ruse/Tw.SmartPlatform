using AwesomeAssertions;
using Tw.Reflection;
using Xunit;

namespace Tw.Core.Tests.Reflection;

public class ReflectionNamespaceTests
{
    [Fact]
    public void TypeFinder_LivesIn_TwReflectionNamespace()
    {
        typeof(TypeFinder).Namespace.Should().Be("Tw.Reflection");
    }

    [Fact]
    public void ITypeFinder_LivesIn_TwReflectionNamespace()
    {
        typeof(ITypeFinder).Namespace.Should().Be("Tw.Reflection");
    }
}
