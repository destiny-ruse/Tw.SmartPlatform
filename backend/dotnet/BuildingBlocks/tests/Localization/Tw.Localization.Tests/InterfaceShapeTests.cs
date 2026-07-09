using AwesomeAssertions;
using Xunit;

namespace Tw.Localization.Tests;

/// <summary>验证 InterfaceShapeTests 相关行为</summary>
public class InterfaceShapeTests
{
    /// <summary>验证 PublicInterfaces_LiveInTwLocalizationNamespace 场景</summary>
    [Fact]
    public void PublicInterfaces_LiveInTwLocalizationNamespace()
    {
        typeof(ITextLocalizer).Namespace.Should().Be("Tw.Localization");
        typeof(ITextResourceContributor).Namespace.Should().Be("Tw.Localization");
        typeof(IDynamicTextStore).Namespace.Should().Be("Tw.Localization");
        typeof(IEntityTranslationStore).Namespace.Should().Be("Tw.Localization");
        typeof(IEntityTranslationService).Namespace.Should().Be("Tw.Localization");
        typeof(IStaticTextSnapshot).Namespace.Should().Be("Tw.Localization");
    }
}
