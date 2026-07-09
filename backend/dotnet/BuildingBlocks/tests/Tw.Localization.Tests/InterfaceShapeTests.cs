using AwesomeAssertions;
using Xunit;

namespace Tw.Localization.Tests;

public class InterfaceShapeTests
{
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
