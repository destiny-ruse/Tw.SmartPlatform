using FluentAssertions;
using Tw.Localization;
using Tw.Localization.Requests;
using Xunit;

namespace Tw.Localization.Tests;

public sealed class LocalizationModelsTests
{
    [Fact]
    public void LanguageInfo_DefaultsUiCultureToCulture()
    {
        var language = new LanguageInfo("zh-Hans") { DisplayName = "简体中文" };

        language.UiCultureName.Should().Be("zh-Hans");
        language.IsEnabled.Should().BeTrue();
        language.SortOrder.Should().Be(0);
    }

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

    [Fact]
    public void EntityTranslationKey_UsesValueEquality()
    {
        var left = new EntityTranslationKey("Product", "42", "Name");
        var right = new EntityTranslationKey("Product", "42", "Name");

        left.Should().Be(right);
    }

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
