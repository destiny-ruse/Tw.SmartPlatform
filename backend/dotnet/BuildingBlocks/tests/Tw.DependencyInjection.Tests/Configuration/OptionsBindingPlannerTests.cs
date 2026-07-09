using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Tw.Configuration.Abstractions;
using Tw.DependencyInjection.Configuration;
using Tw.DependencyInjection.Diagnostics;
using Tw.DependencyInjection.Tests.Fixtures;
using Xunit;

namespace Tw.DependencyInjection.Tests.Configuration;

public class OptionsBindingPlannerTests
{
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

    [Fact]
    public void Plan_ThrowsServiceRegistrationException_WhenGenericArgumentDiffersFromSelf()
    {
        var act = () => PlanFor([typeof(InvalidGenericArgumentOptions)]);

        act.Should().Throw<ServiceRegistrationException>()
            .WithMessage("*IConfigurableOptions<TOptions>*");
    }

    [Fact]
    public void Plan_ThrowsServiceRegistrationException_WhenSameOptionsTypeAndNameRepeats()
    {
        var act = () => PlanFor([typeof(DefaultDuplicateOptions), typeof(DefaultDuplicateOptions)]);

        act.Should().Throw<ServiceRegistrationException>()
            .WithMessage("*命名实例*重复*");
    }

    [Fact]
    public void Plan_ThrowsServiceRegistrationException_WhenSameSectionPathAndNameRepeats()
    {
        var act = () => PlanFor([typeof(SharedPathOptions), typeof(OtherSharedPathOptions)]);

        act.Should().Throw<ServiceRegistrationException>()
            .WithMessage("*配置路径重复*");
    }

    [Fact]
    public void Plan_ThrowsServiceRegistrationException_WhenSectionPathDiffersOnlyByCase()
    {
        var act = () => PlanFor([typeof(UpperCasePathOptions), typeof(LowerCasePathOptions)]);

        act.Should().Throw<ServiceRegistrationException>()
            .WithMessage("*配置路径重复*");
    }

    [Fact]
    public void Plan_DoesNotTreatPipeCharacterAsDuplicateSeparatorInSectionPathAndName()
    {
        var act = () => PlanFor([typeof(PipeInSectionPathOptions), typeof(PipeInOptionsNameOptions)]);

        act.Should().NotThrow();
    }

    [Fact]
    public void Plan_DoesNotTreatPipeCharacterAsDuplicateSeparatorInOptionsTypeAndName()
    {
        var act = () => PlanFor([typeof(PipeNameOptions), typeof(OtherPipeNameOptions)]);

        act.Should().NotThrow();
    }

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

    private sealed class DefaultDuplicateOptions : IConfigurableOptions
    {
        public string Value { get; set; } = string.Empty;
    }

    private sealed class InvalidGenericArgumentOptions : IConfigurableOptions<DefaultDuplicateOptions>
    {
        public void PostConfigure(DefaultDuplicateOptions options, IConfiguration configuration)
        {
        }
    }

    [OptionsSection("Shared")]
    private sealed class SharedPathOptions : IConfigurableOptions
    {
        public string Value { get; set; } = string.Empty;
    }

    [OptionsSection("Shared")]
    private sealed class OtherSharedPathOptions : IConfigurableOptions
    {
        public string Value { get; set; } = string.Empty;
    }

    [OptionsSection("CasePath")]
    private sealed class UpperCasePathOptions : IConfigurableOptions
    {
        public string Value { get; set; } = string.Empty;
    }

    [OptionsSection("casepath")]
    private sealed class LowerCasePathOptions : IConfigurableOptions
    {
        public string Value { get; set; } = string.Empty;
    }

    [OptionsSection("A")]
    [OptionsName("B|C")]
    private sealed class PipeInOptionsNameOptions : IConfigurableOptions
    {
        public string Value { get; set; } = string.Empty;
    }

    [OptionsSection("A|B")]
    [OptionsName("C")]
    private sealed class PipeInSectionPathOptions : IConfigurableOptions
    {
        public string Value { get; set; } = string.Empty;
    }

    [OptionsName("pipe|primary")]
    private sealed class PipeNameOptions : IConfigurableOptions
    {
        public string Value { get; set; } = string.Empty;
    }

    [OptionsName("pipe|primary")]
    private sealed class OtherPipeNameOptions : IConfigurableOptions
    {
        public string Value { get; set; } = string.Empty;
    }
}
