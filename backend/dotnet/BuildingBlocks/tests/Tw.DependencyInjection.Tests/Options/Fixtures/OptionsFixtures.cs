using System.ComponentModel.DataAnnotations;
using Tw.Core.Configuration;

namespace Tw.DependencyInjection.Tests.Options.Fixtures;

// -----------------------------------------------------------------------
// 类库约定测试夹具：必须有 IConfigurableOptions 或 [ConfigurationSection]
// -----------------------------------------------------------------------

/// <summary>同时有 IConfigurableOptions 和 [ConfigurationSection] 标注的合法类库选项。</summary>
[ConfigurationSection("Redis")]
public sealed class RedisOptions : IConfigurableOptions
{
    public string? Host { get; init; }
}

/// <summary>仅有 [ConfigurationSection] 标注、无 IConfigurableOptions 的合法选项。</summary>
[ConfigurationSection("Payment", DirectInject = true)]
public sealed class PaymentOptions
{
    public string? MerchantId { get; init; }
}

/// <summary>只有 IConfigurableOptions 而无 [ConfigurationSection]，默认节名由类名去后缀计算。</summary>
public sealed class CacheOptions : IConfigurableOptions
{
    public int? Ttl { get; init; }
}

/// <summary>
/// 同时有 IConfigurableOptions，但不显式标注 ValidateOnStart（即 null，由全局策略决定）。
/// 用于测试 ValidateOnStart 默认值（null → true）的行为。
/// 注意：C# 属性参数不支持 bool?，ValidateOnStart = false 只能通过 OptionsRegistrationDescriptor 直接构造测试。
/// </summary>
[ConfigurationSection("Auth")]
public sealed class AuthOptions : IConfigurableOptions
{
    [Required]
    public string? Issuer { get; init; }
}

/// <summary>多属性命名选项：同一类型对应多个配置节。</summary>
[ConfigurationSection("Redis:Primary", OptionsName = "Primary")]
[ConfigurationSection("Redis:Replica", OptionsName = "Replica")]
public sealed class RedisInstanceOptions : IConfigurableOptions
{
    public string? Host { get; init; }
}

// -----------------------------------------------------------------------
// 重复声明检测夹具（用于异常断言）
// -----------------------------------------------------------------------

/// <summary>同一类型上两个 OptionsName = null（默认），应触发重复声明错误。</summary>
[ConfigurationSection("DupA")]
[ConfigurationSection("DupB")]
public sealed class DuplicateDefaultOptions : IConfigurableOptions
{
    public string? Value { get; init; }
}

/// <summary>同一类型上两个相同 OptionsName = "X"，应触发重复声明错误。</summary>
[ConfigurationSection("DupA", OptionsName = "X")]
[ConfigurationSection("DupB", OptionsName = "X")]
public sealed class DuplicateNamedOptions : IConfigurableOptions
{
    public string? Value { get; init; }
}

// -----------------------------------------------------------------------
// 入口约定测试夹具：仅靠名称后缀被发现（无 marker，无 attribute）
// -----------------------------------------------------------------------

/// <summary>无任何标记，仅靠 "Options" 后缀在入口模式下被发现。</summary>
public sealed class SuffixOnlyOptions
{
    public int? Ttl { get; init; }
}

/// <summary>无任何标记，仅靠 "Settings" 后缀在入口模式下被发现。</summary>
public sealed class FeatureSettings
{
    public bool Enabled { get; init; }
}

// -----------------------------------------------------------------------
// 应被排除的类型
// -----------------------------------------------------------------------

/// <summary>普通 DTO，无任何标记，在类库模式下不应被注册。</summary>
public sealed class JustADto
{
    public string? Foo { get; init; }
}

/// <summary>无公共 set/init 属性，不应作为选项候选（ConfigurationBinder 无法绑定）。</summary>
public sealed class NoBindablePropertyOptions : IConfigurableOptions
{
    // 仅 getter，没有 setter
    public string? ReadOnly => "constant";
}

/// <summary>无无参构造函数，不应作为选项候选。</summary>
public sealed class NoDefaultCtorOptions : IConfigurableOptions
{
    public string? Value { get; init; }

    public NoDefaultCtorOptions(string value)
    {
        Value = value;
    }
}
