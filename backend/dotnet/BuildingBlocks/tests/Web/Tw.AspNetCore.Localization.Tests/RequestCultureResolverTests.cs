using AwesomeAssertions;
using Tw.Localization;
using Xunit;

namespace Tw.AspNetCore.Localization.Tests;

/// <summary>验证 RequestCultureResolverTests 相关行为</summary>
public class RequestCultureResolverTests
{
    /// <summary>验证 Options 场景</summary>
    /// <returns>Options 的执行结果</returns>
    private static LocalizationOptions Options()
    {
        return new LocalizationOptions
        {
            DefaultCulture = "en-US",
            SupportedCultures = { "en-US", "zh-Hans" },
        };
    }

    /// <summary>验证 Resolve_UsesRouteBeforeQuery 场景</summary>
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

    /// <summary>验证 Resolve_UsesDefaultForUnsupportedCulture 场景</summary>
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

    /// <summary>验证 Resolve_UsesCookieCulture_WhenRouteAndQueryAreNull 场景</summary>
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

    /// <summary>验证 Resolve_UsesFirstSupportedAcceptLanguage_WhenRouteQueryCookieAreNull 场景</summary>
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

    /// <summary>验证 Resolve_StripQWeightFromAcceptLanguage 场景</summary>
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

    /// <summary>验证 Resolve_CaseInsensitiveMatch_ReturnsCanonicalCasing 场景</summary>
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

    /// <summary>验证 Resolve_ThrowsArgumentNullException_WhenOptionsIsNull 场景</summary>
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
