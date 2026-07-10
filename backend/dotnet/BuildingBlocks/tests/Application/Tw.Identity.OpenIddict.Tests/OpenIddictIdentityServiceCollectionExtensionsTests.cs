using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tw.Identity.OpenIddict.Tests;

/// <summary>
/// 覆盖开放Iddict身份服务CollectionExtensions的核心行为和边界条件
/// </summary>
public sealed class OpenIddictIdentityServiceCollectionExtensionsTests
{
    /// <summary>
    /// 验证添加身份OpenIddict注册签发方Validator和OpenIddictServices
    /// </summary>
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

    /// <summary>
    /// 验证添加身份OpenIddictKeeps主机Provided令牌Adapters
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证默认令牌AdaptersThrow主机Adapter必需消息
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 覆盖主机Provided令牌签发方的核心行为和边界条件
    /// </summary>
    private sealed class HostProvidedTokenIssuer : IIdentityTokenIssuer
    {
        /// <summary>
        /// 判断sue异步是否满足条件
        /// </summary>
        /// <param name="request">用于提供请求</param>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>异步流程完成后产生的string</returns>
        public Task<string> IssueAsync(IdentityTokenRequest request, CancellationToken cancellationToken) =>
            Task.FromResult("host-token");
    }

    /// <summary>
    /// 覆盖主机Provided令牌Validator的核心行为和边界条件
    /// </summary>
    private sealed class HostProvidedTokenValidator : IIdentityTokenValidator
    {
        /// <summary>
        /// 校验异步并在非法时抛出异常
        /// </summary>
        /// <param name="request">用于提供请求</param>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>异步流程完成后产生的身份令牌Validation结果</returns>
        public Task<IdentityTokenValidationResult> ValidateAsync(
            IdentityTokenValidationRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new IdentityTokenValidationResult(true, "user-1", new HashSet<string>(), "SYSTEM:000000"));
    }

    /// <summary>
    /// 覆盖主机ProvidedSigningCertificateResolver的核心行为和边界条件
    /// </summary>
    private sealed class HostProvidedSigningCertificateResolver : IIdentitySigningCertificateResolver
    {
        /// <summary>
        /// 解析测试场景所需的签名证书
        /// </summary>
        /// <param name="certificateName">用于提供certificateName</param>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>异步流程完成后产生的SystemSecurityCryptographyX509CertificatesX509Certificate2</returns>
        public Task<System.Security.Cryptography.X509Certificates.X509Certificate2> ResolveAsync(
            string certificateName,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("测试不需要真实证书");
    }
}
