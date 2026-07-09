using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tw.Identity.OpenIddict.Tests;

/// <summary>验证 OpenIddictIdentityServiceCollectionExtensionsTests 相关行为</summary>
public sealed class OpenIddictIdentityServiceCollectionExtensionsTests
{
    /// <summary>验证 AddIdentityOpenIddict_RegistersIssuerValidatorAndOpenIddictServices 场景</summary>
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

    /// <summary>验证 AddIdentityOpenIddict_KeepsHostProvidedTokenAdapters 场景</summary>
    /// <returns>AddIdentityOpenIddict_KeepsHostProvidedTokenAdapters 的执行结果</returns>
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

    /// <summary>验证 DefaultTokenAdapters_ThrowHostAdapterRequiredMessage 场景</summary>
    /// <returns>DefaultTokenAdapters_ThrowHostAdapterRequiredMessage 的执行结果</returns>
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

    /// <summary>验证 HostProvidedTokenIssuer 相关行为</summary>
    private sealed class HostProvidedTokenIssuer : IIdentityTokenIssuer
    {
        /// <summary>验证 IssueAsync 场景</summary>
        /// <param name="request">request 参数</param>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>IssueAsync 的执行结果</returns>
        public Task<string> IssueAsync(IdentityTokenRequest request, CancellationToken cancellationToken) =>
            Task.FromResult("host-token");
    }

    /// <summary>验证 HostProvidedTokenValidator 相关行为</summary>
    private sealed class HostProvidedTokenValidator : IIdentityTokenValidator
    {
        /// <summary>验证 ValidateAsync 场景</summary>
        /// <param name="request">request 参数</param>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>ValidateAsync 的执行结果</returns>
        public Task<IdentityTokenValidationResult> ValidateAsync(
            IdentityTokenValidationRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new IdentityTokenValidationResult(true, "user-1", new HashSet<string>(), "SYSTEM:000000"));
    }

    /// <summary>验证 HostProvidedSigningCertificateResolver 相关行为</summary>
    private sealed class HostProvidedSigningCertificateResolver : IIdentitySigningCertificateResolver
    {
        /// <summary>验证 ResolveAsync 场景</summary>
        /// <param name="certificateName">certificateName 参数</param>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>ResolveAsync 的执行结果</returns>
        public Task<System.Security.Cryptography.X509Certificates.X509Certificate2> ResolveAsync(
            string certificateName,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("测试不需要真实证书");
    }
}
