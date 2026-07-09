using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Tw.DependencyInjection.Abstractions.Configuration;
using Tw.DependencyInjection.Configuration;
using Tw.DependencyInjection.Diagnostics;
using Tw.DependencyInjection.Tests.Fixtures;
using Xunit;

namespace Tw.DependencyInjection.Tests.Configuration;

/// <summary>验证 OptionsBindingPlannerTests 相关行为</summary>
public class OptionsBindingPlannerTests
{
    /// <summary>验证 Report_ExposesOptionsBindingDiagnostics 场景</summary>
    [Fact]
    public void Report_ExposesOptionsBindingDiagnostics()
    {
        var item = new OptionsBindingDiagnostic(
            OptionsTypeName: "Sample.CacheOptions",
            SectionPath: "Cache",
            Name: Options.DefaultName,
            SectionExists: true,
            BindingStatus: "bound",
            ValidationStatus: "enabled",
            IsSensitive: false);

        var report = new OptionsBindingReport([item]);

        report.Items.Should().ContainSingle().Which.SectionPath.Should().Be("Cache");
    }

    /// <summary>验证 Plan_DiscoversOptionsAndInfersPathAndName 场景</summary>
    [Fact]
    public void Plan_DiscoversOptionsAndInfersPathAndName()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["IntegrationCache:Endpoint"] = "localhost",
                ["Tw:Redis:Endpoint"] = "redis",
                ["Disabled:Value"] = "ignored",
            })
            .Build();
        var assembly = typeof(IntegrationCacheOptions).Assembly;

        var plan = OptionsBindingPlanner.Plan(
            assemblies: [assembly],
            typesByAssemblyName: new Dictionary<string, IReadOnlyList<Type>>(StringComparer.Ordinal)
            {
                [assembly.GetName().Name!] =
                [
                    typeof(IntegrationCacheOptions),
                    typeof(NamedRedisOptions),
                    typeof(DisabledOptions),
                ],
            },
            configuration);

        plan.Candidates.Should().Contain(candidate =>
            candidate.OptionsType == typeof(IntegrationCacheOptions) &&
            candidate.SectionPath == "IntegrationCache" &&
            candidate.Name == Options.DefaultName);
        plan.Candidates.Should().Contain(candidate =>
            candidate.OptionsType == typeof(NamedRedisOptions) &&
            candidate.SectionPath == "Tw:Redis" &&
            candidate.Name == "primary");
        plan.Candidates.Should().NotContain(candidate => candidate.OptionsType == typeof(DisabledOptions));
        plan.Report.Items.Should().OnlyContain(item => item.BindingStatus == "bound");
    }

    /// <summary>验证 Plan_ThrowsServiceRegistrationException_WhenGenericArgumentDiffersFromSelf 场景</summary>
    [Fact]
    public void Plan_ThrowsServiceRegistrationException_WhenGenericArgumentDiffersFromSelf()
    {
        var act = () => PlanFor([typeof(InvalidGenericArgumentOptions)]);

        act.Should().Throw<ServiceRegistrationException>()
            .WithMessage("*IConfigurableOptions<TOptions>*");
    }

    /// <summary>验证 Plan_ThrowsServiceRegistrationException_WhenSameOptionsTypeAndNameRepeats 场景</summary>
    [Fact]
    public void Plan_ThrowsServiceRegistrationException_WhenSameOptionsTypeAndNameRepeats()
    {
        var act = () => PlanFor([typeof(DefaultDuplicateOptions), typeof(DefaultDuplicateOptions)]);

        act.Should().Throw<ServiceRegistrationException>()
            .WithMessage("*命名实例*重复*");
    }

    /// <summary>验证 Plan_ThrowsServiceRegistrationException_WhenSameSectionPathAndNameRepeats 场景</summary>
    [Fact]
    public void Plan_ThrowsServiceRegistrationException_WhenSameSectionPathAndNameRepeats()
    {
        var act = () => PlanFor([typeof(SharedPathOptions), typeof(OtherSharedPathOptions)]);

        act.Should().Throw<ServiceRegistrationException>()
            .WithMessage("*配置路径重复*");
    }

    /// <summary>验证 Plan_ThrowsServiceRegistrationException_WhenSectionPathDiffersOnlyByCase 场景</summary>
    [Fact]
    public void Plan_ThrowsServiceRegistrationException_WhenSectionPathDiffersOnlyByCase()
    {
        var act = () => PlanFor([typeof(UpperCasePathOptions), typeof(LowerCasePathOptions)]);

        act.Should().Throw<ServiceRegistrationException>()
            .WithMessage("*配置路径重复*");
    }

    /// <summary>验证 Plan_DoesNotTreatPipeCharacterAsDuplicateSeparatorInSectionPathAndName 场景</summary>
    [Fact]
    public void Plan_DoesNotTreatPipeCharacterAsDuplicateSeparatorInSectionPathAndName()
    {
        var act = () => PlanFor([typeof(PipeInSectionPathOptions), typeof(PipeInOptionsNameOptions)]);

        act.Should().NotThrow();
    }

    /// <summary>验证 Plan_DoesNotTreatPipeCharacterAsDuplicateSeparatorInOptionsTypeAndName 场景</summary>
    [Fact]
    public void Plan_DoesNotTreatPipeCharacterAsDuplicateSeparatorInOptionsTypeAndName()
    {
        var act = () => PlanFor([typeof(PipeNameOptions), typeof(OtherPipeNameOptions)]);

        act.Should().NotThrow();
    }

    /// <summary>验证 PlanFor 场景</summary>
    /// <param name="optionsTypes">optionsTypes 参数</param>
    /// <returns>PlanFor 的执行结果</returns>
    private static OptionsBindingPlan PlanFor(IReadOnlyList<Type> optionsTypes)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DefaultDuplicate:Value"] = "value",
                ["InvalidGenericArgument:Value"] = "value",
                ["Shared:Value"] = "value",
                ["CasePath:Value"] = "value",
                ["A:Value"] = "value",
                ["A:B:Value"] = "value",
                ["PipeName:Value"] = "value",
                ["OtherPipeName:Value"] = "value",
            })
            .Build();
        var assembly = typeof(OptionsBindingPlannerTests).Assembly;

        return OptionsBindingPlanner.Plan(
            assemblies: [assembly],
            typesByAssemblyName: new Dictionary<string, IReadOnlyList<Type>>(StringComparer.Ordinal)
            {
                [assembly.GetName().Name!] = optionsTypes,
            },
            configuration);
    }

    /// <summary>验证 DefaultDuplicateOptions 相关行为</summary>
    private sealed class DefaultDuplicateOptions : IConfigurableOptions
    {
        /// <summary>表示 Value 属性</summary>
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>验证 InvalidGenericArgumentOptions 相关行为</summary>
    private sealed class InvalidGenericArgumentOptions : IConfigurableOptions<DefaultDuplicateOptions>
    {
        /// <summary>验证 PostConfigure 场景</summary>
        /// <param name="options">options 参数</param>
        /// <param name="configuration">configuration 参数</param>
        public void PostConfigure(DefaultDuplicateOptions options, IConfiguration configuration)
        {
        }
    }

    /// <summary>验证 SharedPathOptions 相关行为</summary>
    [OptionsSection("Shared")]
    private sealed class SharedPathOptions : IConfigurableOptions
    {
        /// <summary>表示 Value 属性</summary>
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>验证 OtherSharedPathOptions 相关行为</summary>
    [OptionsSection("Shared")]
    private sealed class OtherSharedPathOptions : IConfigurableOptions
    {
        /// <summary>表示 Value 属性</summary>
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>验证 UpperCasePathOptions 相关行为</summary>
    [OptionsSection("CasePath")]
    private sealed class UpperCasePathOptions : IConfigurableOptions
    {
        /// <summary>表示 Value 属性</summary>
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>验证 LowerCasePathOptions 相关行为</summary>
    [OptionsSection("casepath")]
    private sealed class LowerCasePathOptions : IConfigurableOptions
    {
        /// <summary>表示 Value 属性</summary>
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>验证 PipeInOptionsNameOptions 相关行为</summary>
    [OptionsSection("A")]
    [OptionsName("B|C")]
    private sealed class PipeInOptionsNameOptions : IConfigurableOptions
    {
        /// <summary>表示 Value 属性</summary>
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>验证 PipeInSectionPathOptions 相关行为</summary>
    [OptionsSection("A|B")]
    [OptionsName("C")]
    private sealed class PipeInSectionPathOptions : IConfigurableOptions
    {
        /// <summary>表示 Value 属性</summary>
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>验证 PipeNameOptions 相关行为</summary>
    [OptionsName("pipe|primary")]
    private sealed class PipeNameOptions : IConfigurableOptions
    {
        /// <summary>表示 Value 属性</summary>
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>验证 OtherPipeNameOptions 相关行为</summary>
    [OptionsName("pipe|primary")]
    private sealed class OtherPipeNameOptions : IConfigurableOptions
    {
        /// <summary>表示 Value 属性</summary>
        public string Value { get; set; } = string.Empty;
    }
}
