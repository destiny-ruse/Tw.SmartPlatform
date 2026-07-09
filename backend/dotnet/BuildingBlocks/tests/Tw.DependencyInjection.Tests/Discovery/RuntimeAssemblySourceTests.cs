using AwesomeAssertions;
using Tw.DependencyInjection.Discovery;
using Xunit;

namespace Tw.DependencyInjection.Tests.Discovery;

public class RuntimeAssemblySourceTests
{
    [Fact]
    public void GetCandidateAssemblies_IncludesLoadedTwAssemblies()
    {
        // 触碰 Tw.Core 类型，确保其程序集已加载
        _ = typeof(Tw.Check).Assembly;
        var source = new RuntimeAssemblySource();

        var names = source.GetCandidateAssemblies().Select(a => a.GetName().Name).ToList();

        names.Should().Contain("Tw.Core");
        names.Should().Contain("Tw.DependencyInjection");
    }
}
