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

    [Fact]
    public async Task AddIdentityOpenIddict_KeepsHostProvidedTokenAdapters()
    {
        var services = new ServiceCollection();
        services.AddScoped<IIdentityTokenIssuer, HostProvidedTokenIssuer>();
        services.AddScoped<IIdentityTokenValidator, HostProvidedTokenValidator>();
        services.AddScoped<IIdentitySigningCertificateResolver, HostProvidedSigningCertificateResolver>();

        services.AddIdentityOpenIddict(options =>
        {
            options.Issuer = new Uri("https://identity.smart-platform.local");
            options.Audiences.Add("smart-platform-api");
            options.SigningCertificateName = "smart-platform-token-signing";
        });

        await using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IIdentityTokenIssuer>().Should().BeOfType<HostProvidedTokenIssuer>();
        provider.GetRequiredService<IIdentityTokenValidator>().Should().BeOfType<HostProvidedTokenValidator>();
        provider.GetRequiredService<IIdentitySigningCertificateResolver>().Should().BeOfType<HostProvidedSigningCertificateResolver>();
    }

    [Fact]
    public async Task DefaultTokenAdapters_ThrowHostAdapterRequiredMessage()
    {
        var services = new ServiceCollection();
        services.AddIdentityOpenIddict(options =>
        {
            options.Issuer = new Uri("https://identity.smart-platform.local");
            options.Audiences.Add("smart-platform-api");
            options.SigningCertificateName = "smart-platform-token-signing";
        });

        await using var provider = services.BuildServiceProvider();

        var issue = () => provider.GetRequiredService<IIdentityTokenIssuer>()
            .IssueAsync(new IdentityTokenRequest("user-1", "client-1", new HashSet<string>()), TestContext.Current.CancellationToken);
        var validate = () => provider.GetRequiredService<IIdentityTokenValidator>()
            .ValidateAsync(new IdentityTokenValidationRequest("token", "smart-platform-api"), TestContext.Current.CancellationToken);
        var resolve = () => provider.GetRequiredService<IIdentitySigningCertificateResolver>()
            .ResolveAsync("smart-platform-token-signing", TestContext.Current.CancellationToken);

        await issue.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*宿主*");
        await validate.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*宿主*");
        await resolve.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*宿主*");
    }

    private sealed class HostProvidedTokenIssuer : IIdentityTokenIssuer
    {
        public Task<string> IssueAsync(IdentityTokenRequest request, CancellationToken cancellationToken) =>
            Task.FromResult("host-token");
    }

    private sealed class HostProvidedTokenValidator : IIdentityTokenValidator
    {
        public Task<IdentityTokenValidationResult> ValidateAsync(
            IdentityTokenValidationRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new IdentityTokenValidationResult(true, "user-1", new HashSet<string>(), "SYSTEM:000000"));
    }

    private sealed class HostProvidedSigningCertificateResolver : IIdentitySigningCertificateResolver
    {
        public Task<System.Security.Cryptography.X509Certificates.X509Certificate2> ResolveAsync(
            string certificateName,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("测试不需要真实证书");
    }
}
