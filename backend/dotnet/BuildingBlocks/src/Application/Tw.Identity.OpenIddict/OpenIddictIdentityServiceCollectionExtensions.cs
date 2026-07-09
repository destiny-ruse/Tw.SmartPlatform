using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Tw.Identity.OpenIddict;

/// <summary>
/// OpenIddict 身份中心服务注册扩展
/// </summary>
public static class OpenIddictIdentityServiceCollectionExtensions
{
    /// <summary>
    /// 注册 OpenIddict 身份中心边界服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">配置委托</param>
    /// <returns>服务集合</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> 或 <paramref name="configure"/> 为 null 时抛出</exception>
    /// <exception cref="InvalidOperationException">OpenIddict 配置不满足身份中心边界要求时抛出</exception>
    public static IServiceCollection AddIdentityOpenIddict(
        this IServiceCollection services,
        Action<OpenIddictIdentityOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var identityOptions = new OpenIddictIdentityOptions();
        configure(identityOptions);
        identityOptions.Validate();

        services.AddOptions<OpenIddictIdentityOptions>()
            .Configure(configure)
            .Validate(options =>
            {
                options.Validate();
                return true;
            })
            .ValidateOnStart();

        services.AddOpenIddict()
            .AddServer(server =>
            {
                server.SetIssuer(identityOptions.Issuer!);
                server.AllowAuthorizationCodeFlow();
                server.AllowClientCredentialsFlow();
                server.AllowRefreshTokenFlow();

                if (identityOptions.RequireProofKey)
                {
                    server.RequireProofKeyForCodeExchange();
                }

                server.UseAspNetCore();
            })
            .AddValidation(validation =>
            {
                validation.SetIssuer(identityOptions.Issuer!);
                foreach (var audience in identityOptions.Audiences)
                {
                    validation.AddAudiences(audience);
                }

                validation.UseLocalServer();
                validation.UseAspNetCore();
            });

        services.TryAddScoped<IIdentitySigningCertificateResolver, StoreIdentitySigningCertificateResolver>();
        services.TryAddScoped<IIdentityTokenIssuer, OpenIddictIdentityTokenIssuer>();
        services.TryAddScoped<IIdentityTokenValidator, OpenIddictIdentityTokenValidator>();

        return services;
    }
}
