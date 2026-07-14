using AwesomeAssertions;
using Tw.DependencyInjection;
using Tw.DependencyInjection.Discovery;
using Xunit;

namespace Tw.DependencyInjection.Tests.Discovery;

/// <summary>
/// 覆盖Assembly过滤器的核心行为和边界条件
/// </summary>
public class AssemblyFilterTests
{
    /// <summary>
    /// 验证选项Defaults到空Lists
    /// </summary>
    [Fact]
    public void Options_DefaultsToEmptyLists()
    {
        var options = new ServiceRegistrationOptions();

        options.IncludeAssemblies.Should().BeEmpty();
        options.ExcludeAssemblies.Should().BeEmpty();
        options.IncludeAssemblyPrefixes.Should().BeEmpty();
        options.ExcludeAssemblyPrefixes.Should().BeEmpty();
    }

    /// <summary>
    /// 未配置显式规则时保留使用默认程序集前缀的候选项
    /// </summary>
    [Fact]
    public void Filter_KeepsDefaultAssemblyPrefix_ByDefault()
    {
        var result = AssemblyFilter.Filter(
            ["Tw.Core", "Tw.Order.Application", "System.Text.Json", "Newtonsoft.Json"],
            new ServiceRegistrationOptions());

        result.Should().BeEquivalentTo("Tw.Core", "Tw.Order.Application");
    }

    /// <summary>
    /// 显式包含规则允许没有默认程序集前缀的候选项
    /// </summary>
    [Fact]
    public void Filter_IncludesExplicitAssembly_WithoutDefaultAssemblyPrefix()
    {
        var options = new ServiceRegistrationOptions();
        options.IncludeAssemblies.Add("Acme.Payments");

        var result = AssemblyFilter.Filter(["Acme.Payments", "Contoso.Crm"], options);

        result.Should().BeEquivalentTo("Acme.Payments");
    }

    /// <summary>
    /// 自定义前缀与默认程序集前缀共同参与包含判断
    /// </summary>
    [Fact]
    public void Filter_IncludesCustomPrefix_InAdditionToDefaultAssemblyPrefix()
    {
        var options = new ServiceRegistrationOptions();
        options.IncludeAssemblyPrefixes.Add("Acme.");

        var result = AssemblyFilter.Filter(["Tw.Core", "Acme.Payments", "Contoso.Crm"], options);

        result.Should().BeEquivalentTo("Tw.Core", "Acme.Payments");
    }

    /// <summary>
    /// 名称排除规则优先于默认程序集前缀
    /// </summary>
    [Fact]
    public void Filter_ExcludesByName_EvenWhenDefaultAssemblyPrefix()
    {
        var options = new ServiceRegistrationOptions();
        options.ExcludeAssemblies.Add("Tw.Legacy");

        var result = AssemblyFilter.Filter(["Tw.Core", "Tw.Legacy"], options);

        result.Should().BeEquivalentTo("Tw.Core");
    }

    /// <summary>
    /// 前缀排除规则优先于默认程序集前缀
    /// </summary>
    [Fact]
    public void Filter_ExcludesByPrefix_EvenWhenDefaultAssemblyPrefix()
    {
        var options = new ServiceRegistrationOptions();
        options.ExcludeAssemblyPrefixes.Add("Tw.Test");

        var result = AssemblyFilter.Filter(["Tw.Core", "Tw.TestKit"], options);

        result.Should().BeEquivalentTo("Tw.Core");
    }

    /// <summary>
    /// 验证过滤器BlacklistWins当名称BothIncluded和Excluded
    /// </summary>
    [Fact]
    public void Filter_BlacklistWins_WhenNameBothIncludedAndExcluded()
    {
        var options = new ServiceRegistrationOptions();
        options.IncludeAssemblies.Add("Tw.Order");
        options.ExcludeAssemblies.Add("Tw.Order");

        var result = AssemblyFilter.Filter(["Tw.Order"], options);

        result.Should().BeEmpty();
    }

    /// <summary>
    /// 验证选项DefaultsAssemblyPriorities到空Dictionary
    /// </summary>
    [Fact]
    public void Options_DefaultsAssemblyPrioritiesToEmptyDictionary()
    {
        var options = new ServiceRegistrationOptions();

        options.AssemblyPriorities.Should().BeEmpty();
    }
}
