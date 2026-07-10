using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Tw.DependencyInjection.Abstractions.Configuration;
using Tw.DependencyInjection.Configuration;
using Tw.DependencyInjection.Diagnostics;
using Tw.DependencyInjection.Tests.Fixtures;
using Xunit;

namespace Tw.DependencyInjection.Tests.Configuration;

/// <summary>
/// 覆盖选项绑定Planner的核心行为和边界条件
/// </summary>
public class OptionsBindingPlannerTests
{
    /// <summary>
    /// 验证报告Exposes选项绑定诊断集合
    /// </summary>
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

    /// <summary>
    /// 验证PlanDiscovers选项和Infers路径和名称
    /// </summary>
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

    /// <summary>
    /// 验证Plan抛出异常服务Registration异常当Generic参数DiffersFromSelf
    /// </summary>
    [Fact]
    public void Plan_ThrowsServiceRegistrationException_WhenGenericArgumentDiffersFromSelf()
    {
        var act = () => PlanFor([typeof(InvalidGenericArgumentOptions)]);

        act.Should().Throw<ServiceRegistrationException>()
            .WithMessage("*IConfigurableOptions<TOptions>*");
    }

    /// <summary>
    /// 验证Plan抛出异常服务Registration异常当Same选项类型和名称Repeats
    /// </summary>
    [Fact]
    public void Plan_ThrowsServiceRegistrationException_WhenSameOptionsTypeAndNameRepeats()
    {
        var act = () => PlanFor([typeof(DefaultDuplicateOptions), typeof(DefaultDuplicateOptions)]);

        act.Should().Throw<ServiceRegistrationException>()
            .WithMessage("*命名实例*重复*");
    }

    /// <summary>
    /// 验证Plan抛出异常服务Registration异常当SameSection路径和名称Repeats
    /// </summary>
    [Fact]
    public void Plan_ThrowsServiceRegistrationException_WhenSameSectionPathAndNameRepeats()
    {
        var act = () => PlanFor([typeof(SharedPathOptions), typeof(OtherSharedPathOptions)]);

        act.Should().Throw<ServiceRegistrationException>()
            .WithMessage("*配置路径重复*");
    }

    /// <summary>
    /// 验证Plan抛出异常服务Registration异常当Section路径DiffersOnlyByCase
    /// </summary>
    [Fact]
    public void Plan_ThrowsServiceRegistrationException_WhenSectionPathDiffersOnlyByCase()
    {
        var act = () => PlanFor([typeof(UpperCasePathOptions), typeof(LowerCasePathOptions)]);

        act.Should().Throw<ServiceRegistrationException>()
            .WithMessage("*配置路径重复*");
    }

    /// <summary>
    /// 验证Plan不TreatPipeCharacter作为重复SeparatorInSection路径和名称
    /// </summary>
    [Fact]
    public void Plan_DoesNotTreatPipeCharacterAsDuplicateSeparatorInSectionPathAndName()
    {
        var act = () => PlanFor([typeof(PipeInSectionPathOptions), typeof(PipeInOptionsNameOptions)]);

        act.Should().NotThrow();
    }

    /// <summary>
    /// 验证Plan不TreatPipeCharacter作为重复SeparatorIn选项类型和名称
    /// </summary>
    [Fact]
    public void Plan_DoesNotTreatPipeCharacterAsDuplicateSeparatorInOptionsTypeAndName()
    {
        var act = () => PlanFor([typeof(PipeNameOptions), typeof(OtherPipeNameOptions)]);

        act.Should().NotThrow();
    }

    /// <summary>
    /// 说明PlanFor在当前类型中的职责
    /// </summary>
    /// <param name="optionsTypes">用于提供options类型集合</param>
    /// <returns>匹配当前查询条件的结果集合</returns>
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

    /// <summary>
    /// 覆盖默认重复选项的核心行为和边界条件
    /// </summary>
    private sealed class DefaultDuplicateOptions : IConfigurableOptions
    {
        /// <summary>
        /// 值在当前对象中的业务含义
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// 覆盖非法GenericArgument选项的核心行为和边界条件
    /// </summary>
    private sealed class InvalidGenericArgumentOptions : IConfigurableOptions<DefaultDuplicateOptions>
    {
        /// <summary>
        /// 说明PostConfigure在当前类型中的职责
        /// </summary>
        /// <param name="options">用于配置当前组件行为的选项</param>
        /// <param name="configuration">用于提供configuration</param>
        public void PostConfigure(DefaultDuplicateOptions options, IConfiguration configuration)
        {
        }
    }

    /// <summary>
    /// 覆盖Shared路径选项的核心行为和边界条件
    /// </summary>
    [OptionsSection("Shared")]
    private sealed class SharedPathOptions : IConfigurableOptions
    {
        /// <summary>
        /// 值在当前对象中的业务含义
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// 覆盖OtherShared路径选项的核心行为和边界条件
    /// </summary>
    [OptionsSection("Shared")]
    private sealed class OtherSharedPathOptions : IConfigurableOptions
    {
        /// <summary>
        /// 值在当前对象中的业务含义
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// 覆盖UpperCase路径选项的核心行为和边界条件
    /// </summary>
    [OptionsSection("CasePath")]
    private sealed class UpperCasePathOptions : IConfigurableOptions
    {
        /// <summary>
        /// 值在当前对象中的业务含义
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// 覆盖LowerCase路径选项的核心行为和边界条件
    /// </summary>
    [OptionsSection("casepath")]
    private sealed class LowerCasePathOptions : IConfigurableOptions
    {
        /// <summary>
        /// 值在当前对象中的业务含义
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// 覆盖PipeIn选项名称选项的核心行为和边界条件
    /// </summary>
    [OptionsSection("A")]
    [OptionsName("B|C")]
    private sealed class PipeInOptionsNameOptions : IConfigurableOptions
    {
        /// <summary>
        /// 值在当前对象中的业务含义
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// 覆盖PipeInSection路径选项的核心行为和边界条件
    /// </summary>
    [OptionsSection("A|B")]
    [OptionsName("C")]
    private sealed class PipeInSectionPathOptions : IConfigurableOptions
    {
        /// <summary>
        /// 值在当前对象中的业务含义
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// 覆盖Pipe名称选项的核心行为和边界条件
    /// </summary>
    [OptionsName("pipe|primary")]
    private sealed class PipeNameOptions : IConfigurableOptions
    {
        /// <summary>
        /// 值在当前对象中的业务含义
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// 覆盖OtherPipe名称选项的核心行为和边界条件
    /// </summary>
    [OptionsName("pipe|primary")]
    private sealed class OtherPipeNameOptions : IConfigurableOptions
    {
        /// <summary>
        /// 值在当前对象中的业务含义
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }
}
