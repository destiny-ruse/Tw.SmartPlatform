using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
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
}
