using Microsoft.Extensions.Configuration;

namespace Tw.Configuration.Abstractions;

/// <summary>
/// 支持后置配置的强类型选项契约
/// </summary>
/// <typeparam name="TOptions">选项自身类型，必须等于实现类型</typeparam>
/// <remarks>
/// <typeparamref name="TOptions"/> 在运行期必须等于实现类自身类型（即 <c>class MyOptions : IConfigurableOptions&lt;MyOptions&gt;</c>）；
/// 绑定引擎在扫描阶段若发现不等，将抛出 <see cref="InvalidOperationException"/> 并中止启动。
/// </remarks>
public interface IConfigurableOptions<TOptions> : IConfigurableOptions
    where TOptions : class, IConfigurableOptions
{
    /// <summary>
    /// 在绑定后补默认值、组合校验或派生非敏感字段
    /// </summary>
    /// <param name="options">已绑定的选项实例</param>
    /// <param name="configuration">该选项绑定的配置节</param>
    /// <remarks>不得在此解析服务或使用 Service Locator。</remarks>
    void PostConfigure(TOptions options, IConfiguration configuration);
}
