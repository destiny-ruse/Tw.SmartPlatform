using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tw.Configuration.Abstractions;
using Tw.DependencyInjection.Abstractions;

namespace Tw.DependencyInjection.Tests.Fixtures;

/// <summary>集成测试用订单服务契约</summary>
public interface IOrderService;

/// <summary>订单服务默认实现，按命名匹配暴露 <see cref="IOrderService"/></summary>
public sealed class OrderService : IOrderService, IScopedDependency;

/// <summary>集成测试用支付提供方契约</summary>
public interface IPaymentProvider;

/// <summary>微信支付提供方，按 key "wechat" 注册为 keyed service</summary>
[ExposeKeyedService(typeof(IPaymentProvider), "wechat")]
public sealed class WechatPaymentProvider : IPaymentProvider, IScopedDependency;

/// <summary>结算服务，通过 [FromKeyedServices] 注入指定 key 的支付提供方</summary>
public sealed class CheckoutService : IScopedDependency
{
    /// <summary>初始化结算服务</summary>
    /// <param name="provider">key 为 "wechat" 的支付提供方</param>
    public CheckoutService([FromKeyedServices("wechat")] IPaymentProvider provider)
    {
        Provider = provider;
    }

    /// <summary>注入的支付提供方</summary>
    public IPaymentProvider Provider { get; }
}

/// <summary>集成测试用开放泛型仓储契约</summary>
/// <typeparam name="TEntity">实体类型</typeparam>
public interface IRepository<TEntity>;

/// <summary>开放泛型仓储实现，按命名匹配暴露 <see cref="IRepository{TEntity}"/></summary>
/// <typeparam name="TEntity">实体类型</typeparam>
public sealed class Repository<TEntity> : IRepository<TEntity>, IScopedDependency;

/// <summary>集成测试用订单实体（泛型参数占位，不参与注册）</summary>
public sealed class OrderEntity;

/// <summary>集成测试用缓存配置</summary>
public sealed class IntegrationCacheOptions : IConfigurableOptions<IntegrationCacheOptions>
{
    /// <summary>缓存端点</summary>
    [Required]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>后置配置生成的有效端点</summary>
    public string EffectiveEndpoint { get; set; } = string.Empty;

    /// <inheritdoc />
    public void PostConfigure(IntegrationCacheOptions options, IConfiguration configuration)
    {
        options.EffectiveEndpoint = configuration["Endpoint"] ?? options.Endpoint;
    }
}

/// <summary>集成测试用命名 Redis 配置</summary>
[OptionsSection("Tw:Redis")]
[OptionsName("primary")]
public sealed class NamedRedisOptions : IConfigurableOptions
{
    /// <summary>Redis 端点</summary>
    [Required]
    public string Endpoint { get; set; } = string.Empty;
}

/// <summary>禁用自动绑定的测试配置</summary>
[DisableOptionsBinding]
public sealed class DisabledOptions : IConfigurableOptions
{
    /// <summary>被禁用的值</summary>
    public string Value { get; set; } = string.Empty;
}
