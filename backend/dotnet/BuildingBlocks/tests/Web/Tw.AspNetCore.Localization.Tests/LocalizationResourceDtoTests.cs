using AwesomeAssertions;
using Xunit;

namespace Tw.AspNetCore.Localization.Tests;

/// <summary>
/// 覆盖本地化资源Dto的核心行为和边界条件
/// </summary>
public class LocalizationResourceDtoTests
{
    /// <summary>
    /// 验证资源DtoHoldsTexts
    /// </summary>
    [Fact]
    public void ResourceDto_HoldsTexts()
    {
        var dto = new LocalizationResourceDto(
            "App",
            "zh-Hans",
            [new LocalizationTextDto("Menu", "菜单", false)]);

        dto.ResourceName.Should().Be("App");
        dto.Texts.Should().ContainSingle(x => x.Name == "Menu" && x.Value == "菜单");
        dto.Texts.Should().ContainSingle(x => x.ResourceNotFound == false);
    }

    /// <summary>
    /// 验证文本Dto资源不FoundIstrue当缺少
    /// </summary>
    [Fact]
    public void TextDto_ResourceNotFound_IsTrue_WhenMissing()
    {
        var text = new LocalizationTextDto("Missing", "Missing", true);

        text.Name.Should().Be("Missing");
        text.Value.Should().Be("Missing");
        text.ResourceNotFound.Should().BeTrue();
    }
}
