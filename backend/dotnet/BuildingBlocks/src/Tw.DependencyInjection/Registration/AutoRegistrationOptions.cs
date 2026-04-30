using System.Reflection;
using Tw.Core;
using Tw.DependencyInjection.Interception;

namespace Tw.DependencyInjection.Registration;

/// <summary>
/// 自动注册公共入口的配置对象
/// </summary>
public sealed class AutoRegistrationOptions
{
    private readonly List<Assembly> _assemblies = [];
    private readonly List<InterceptorRegistration> _globalInterceptors = [];
    private readonly List<Type> _matcherTypes = [];

    /// <summary>
    /// 参与服务、选项和匹配器扫描的程序集列表
    /// </summary>
    public IReadOnlyList<Assembly> Assemblies => _assemblies;

    /// <summary>
    /// 全局拦截器注册列表
    /// </summary>
    public IReadOnlyList<InterceptorRegistration> GlobalInterceptors => _globalInterceptors;

    /// <summary>
    /// 是否启用 Options 自动注册
    /// </summary>
    public bool OptionsAutoRegistrationEnabled { get; private set; }

    internal IReadOnlyList<Type> MatcherTypes => _matcherTypes;

    /// <summary>
    /// 添加参与扫描的程序集
    /// </summary>
    /// <param name="assembly">参与扫描的程序集</param>
    public AutoRegistrationOptions AddAssembly(Assembly assembly)
    {
        var value = Check.NotNull(assembly);
        if (_assemblies.All(a => a.FullName != value.FullName))
        {
            _assemblies.Add(value);
        }

        return this;
    }

    /// <summary>
    /// 添加包含指定类型的程序集
    /// </summary>
    /// <typeparam name="T">用于定位程序集的类型</typeparam>
    public AutoRegistrationOptions AddAssemblyOf<T>()
        => AddAssembly(typeof(T).Assembly);

    /// <summary>
    /// 启用或禁用 Options 自动注册
    /// </summary>
    /// <param name="enabled">是否启用 Options 自动注册</param>
    public AutoRegistrationOptions EnableOptionsAutoRegistration(bool enabled = true)
    {
        OptionsAutoRegistrationEnabled = enabled;
        return this;
    }

    /// <summary>
    /// 添加指定作用域的全局拦截器
    /// </summary>
    /// <typeparam name="TInterceptor">运行时拦截器类型</typeparam>
    /// <param name="scope">拦截器作用域</param>
    /// <param name="order">同层排序值</param>
    public AutoRegistrationOptions AddGlobalInterceptor<TInterceptor>(
        InterceptorScope scope,
        int order = 0)
        where TInterceptor : InterceptorBase
    {
        _globalInterceptors.Add(new InterceptorRegistration(typeof(TInterceptor), scope, order));
        return this;
    }

    internal void AddMatcherType(Type matcherType)
    {
        var value = Check.AssignableTo<IInterceptorMatcher>(matcherType);
        if (_matcherTypes.All(t => t != value))
        {
            _matcherTypes.Add(value);
        }
    }
}
