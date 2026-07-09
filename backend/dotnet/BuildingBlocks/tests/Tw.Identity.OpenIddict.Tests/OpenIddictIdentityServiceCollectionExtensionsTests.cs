using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tw.Identity.OpenIddict.Tests;

public sealed class OpenIddictIdentityServiceCollectionExtensionsTests
{
    [Fact]
    public void AddIdentityOpenIddict_RegistersIssuerValidatorAndOpenIddictServices()
    {
        var services = new ServiceCollection();

        services.AddIdentityOpenIddict(options =>
        {
            options.Issuer = new Uri("https://identity.smart-platform.local");
            options.Audiences.Add("smart-platform-api");
            options.SigningCertificateName = "smart-platform-token-signing";
        });

        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IIdentityTokenIssuer));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IIdentityTokenValidator));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IIdentitySigningCertificateResolver));
    }
}
