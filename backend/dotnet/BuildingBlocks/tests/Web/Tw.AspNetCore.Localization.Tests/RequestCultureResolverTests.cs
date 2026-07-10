using AwesomeAssertions;
using Tw.Localization;
using Xunit;

namespace Tw.AspNetCore.Localization.Tests;

/// <summary>
/// 覆盖请求文化Resolver的核心行为和边界条件
/// </summary>
public class RequestCultureResolverTests
{
    /// <summary>
    /// 构造测试所需的本地化选项
    /// </summary>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    private static LocalizationOptions Options()
    {
        return new LocalizationOptions
        {
            DefaultCulture = "en-US",
            SupportedCultures = { "en-US", "zh-Hans" },
        };
    }

    /// <summary>
    /// 验证ResolveUsesRoute前置处理Query
    /// </summary>
    [Fact]
    public void Resolve_UsesRouteBeforeQuery()
    {
        var result = RequestCultureResolver.Resolve(
            routeCulture: "zh-Hans",
            queryCulture: "en-US",
            cookieCulture: null,
            acceptLanguageHeader: null,
            Options());

        result.CultureName.Should().Be("zh-Hans");
        result.IsExplicitSwitch.Should().BeTrue();
    }

    /// <summary>
    /// 验证ResolveUses默认针对Unsupported文化
    /// </summary>
    [Fact]
    public void Resolve_UsesDefaultForUnsupportedCulture()
    {
        var result = RequestCultureResolver.Resolve(
            routeCulture: null,
            queryCulture: "fr-FR",
            cookieCulture: null,
            acceptLanguageHeader: null,
            Options());

        result.CultureName.Should().Be("en-US");
        result.IsExplicitSwitch.Should().BeFalse();
    }

    /// <summary>
    /// 验证ResolveUsesCookie文化当Route和QueryAre空值
    /// </summary>
    [Fact]
    public void Resolve_UsesCookieCulture_WhenRouteAndQueryAreNull()
    {
        var result = RequestCultureResolver.Resolve(
            routeCulture: null,
            queryCulture: null,
            cookieCulture: "zh-Hans",
            acceptLanguageHeader: null,
            Options());

        result.CultureName.Should().Be("zh-Hans");
        result.IsExplicitSwitch.Should().BeFalse();
    }

    /// <summary>
    /// 验证ResolveUses第一个支持AcceptLanguage当RouteQueryCookieAre空值
    /// </summary>
    [Fact]
    public void Resolve_UsesFirstSupportedAcceptLanguage_WhenRouteQueryCookieAreNull()
    {
        // fr-FR 不在支持列表中，zh-Hans 在支持列表中；应跳过 fr-FR，使用 zh-Hans
        var result = RequestCultureResolver.Resolve(
            routeCulture: null,
            queryCulture: null,
            cookieCulture: null,
            acceptLanguageHeader: "fr-FR,zh-Hans",
            Options());

        result.CultureName.Should().Be("zh-Hans");
        result.IsExplicitSwitch.Should().BeFalse();
    }

    /// <summary>
    /// 验证ResolveStripQWeightFromAcceptLanguage
    /// </summary>
    [Fact]
    public void Resolve_StripQWeightFromAcceptLanguage()
    {
        // Accept-Language 值携带 q-weight 后缀，应剥离后匹配
        var result = RequestCultureResolver.Resolve(
            routeCulture: null,
            queryCulture: null,
            cookieCulture: null,
            acceptLanguageHeader: "zh-Hans;q=0.9",
            Options());

        result.CultureName.Should().Be("zh-Hans");
        result.IsExplicitSwitch.Should().BeFalse();
    }

    /// <summary>
    /// 验证ResolveCaseInsensitiveMatch返回CanonicalCasing
    /// </summary>
    [Fact]
    public void Resolve_CaseInsensitiveMatch_ReturnsCanonicalCasing()
    {
        // 路由传入大写 "ZH-HANS"，应返回配置中的规范大小写 "zh-Hans"
        var result = RequestCultureResolver.Resolve(
            routeCulture: "ZH-HANS",
            queryCulture: null,
            cookieCulture: null,
            acceptLanguageHeader: null,
            Options());

        result.CultureName.Should().Be("zh-Hans");
        result.IsExplicitSwitch.Should().BeTrue();
    }

    /// <summary>
    /// 验证Resolve抛出异常参数空值异常当选项Is空值
    /// </summary>
    [Fact]
    public void Resolve_ThrowsArgumentNullException_WhenOptionsIsNull()
    {
        var act = () => RequestCultureResolver.Resolve(
            routeCulture: null,
            queryCulture: null,
            cookieCulture: null,
            acceptLanguageHeader: null,
            options: null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
