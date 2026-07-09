using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Tw.DependencyInjection.Abstractions.Configuration;

namespace Tw.DependencyInjection.Configuration;

/// <summary>
/// 将 <see cref="IConfigurableOptions{TOptions}.PostConfigure"/> 适配到 Options 后置配置管线
/// </summary>
/// <typeparam name="TOptions">Options 类型</typeparam>
internal sealed class ConfigurableOptionsPostConfigure<TOptions> : IPostConfigureOptions<TOptions>
    where TOptions : class, IConfigurableOptions<TOptions>
{
    private readonly string _name;
    private readonly IConfiguration _section;

    /// <summary>
    /// 初始化后置配置适配器
    /// </summary>
    /// <param name="name">Options 命名实例</param>
    /// <param name="section">绑定配置节</param>
    public ConfigurableOptionsPostConfigure(string name, IConfiguration section)
    {
        _name = name;
        _section = section;
    }

    /// <inheritdoc />
    public void PostConfigure(string? name, TOptions options)
    {
        if (name == _name)
        {
            options.PostConfigure(options, _section);
        }
    }
}
