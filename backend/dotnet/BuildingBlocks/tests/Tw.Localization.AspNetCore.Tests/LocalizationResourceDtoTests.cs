using FluentAssertions;
using Xunit;

namespace Tw.Localization.AspNetCore.Tests;

public class LocalizationResourceDtoTests
{
    [Fact]
    public void ResourceDto_HoldsTexts()
    {
        var dto = new LocalizationResourceDto(
            "App",
            "zh-Hans",
            [new LocalizationTextDto("Menu", "菜单", false)]);

        dto.ResourceName.Should().Be("App");
        dto.Texts.Should().ContainSingle(x => x.Name == "Menu" && x.Value == "菜单");
    }
}
