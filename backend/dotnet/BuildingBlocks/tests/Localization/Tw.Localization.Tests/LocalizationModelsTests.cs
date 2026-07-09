using AwesomeAssertions;
using Tw.Localization;
using Tw.Localization.Requests;
using Xunit;

namespace Tw.Localization.Tests;

/// <summary>验证 LocalizationModelsTests 相关行为</summary>
public sealed class LocalizationModelsTests
{
    /// <summary>验证 LanguageInfo_DefaultsUiCultureToCulture 场景</summary>
    [Fact]
    public void LanguageInfo_DefaultsUiCultureToCulture()
    {
        var language = new LanguageInfo("zh-Hans") { DisplayName = "简体中文" };

        language.UiCultureName.Should().Be("zh-Hans");
        language.IsEnabled.Should().BeTrue();
        language.SortOrder.Should().Be(0);
    }

    /// <summary>验证 LocalizedText_NotFound_ReturnsKeyAsValue 场景</summary>
    [Fact]
    public void LocalizedText_NotFound_ReturnsKeyAsValue()
    {
        var text = LocalizedText.NotFound("App", "Menu.Home", "zh-Hans");

        text.ResourceName.Should().Be("App");
        text.Name.Should().Be("Menu.Home");
        text.Value.Should().Be("Menu.Home");
        text.CultureName.Should().Be("zh-Hans");
        text.ResourceNotFound.Should().BeTrue();
        text.Source.Should().Be(LocalizedTextSource.NotFound);
    }

    /// <summary>验证 EntityTranslationKey_UsesValueEquality 场景</summary>
    [Fact]
    public void EntityTranslationKey_UsesValueEquality()
    {
        var left = new EntityTranslationKey("Product", "42", "Name");
        var right = new EntityTranslationKey("Product", "42", "Name");

        left.Should().Be(right);
    }

    /// <summary>验证 BatchQuery_ReusesContext 场景</summary>
    [Fact]
    public void BatchQuery_ReusesContext()
    {
        var context = new LocalizationContext("zh-Hans") { TenantId = "tenant-a" };
        var query = new EntityTranslationBatchQuery(
            [new EntityTranslationKey("Product", "42", "Name")],
            context);

        query.Context.TenantId.Should().Be("tenant-a");
        query.Keys.Should().ContainSingle();
    }
}
