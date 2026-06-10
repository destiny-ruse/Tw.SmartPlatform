using Microsoft.Extensions.DependencyInjection;

namespace Tw.DependencyInjection.Registration;

/// <summary>
/// 构造函数 keyed 服务依赖校验器：在注册计划执行前验证
/// 所有带 <see cref="FromKeyedServicesAttribute"/> 参数的构造函数所依赖的 keyed service 均已包含在注册计划中
/// </summary>
internal static class ConstructorKeyedServiceValidator
{
    /// <summary>
    /// 校验注册列表中所有实现类型的构造函数，确保
    /// <see cref="FromKeyedServicesAttribute"/> 标注的参数所引用的 keyed service 已在计划中注册
    /// </summary>
    /// <param name="registrations">注册规划产出的候选列表</param>
    /// <exception cref="ArgumentNullException"><paramref name="registrations"/> 为 null 时抛出</exception>
    /// <exception cref="Tw.DependencyInjection.ServiceRegistrationException">
    /// 某构造函数参数通过 <see cref="FromKeyedServicesAttribute"/> 引用了未在注册计划中出现的 keyed service 时抛出
    /// </exception>
    /// <remarks>
    /// 本校验遍历实现类型的<b>全部公共构造函数</b>的 <see cref="FromKeyedServicesAttribute"/> 参数，
    /// 并要求其 key 已在计划中注册，属保守校验。
    /// 当类型存在多个公共构造函数、而容器仅选用其一时，
    /// 对未被选用构造函数中指向缺失 key 的参数也会报错（保守拒绝，倾向启动期暴露问题）。
    /// </remarks>
    public static void Validate(IReadOnlyList<ServiceCandidate> registrations)
    {
        ArgumentNullException.ThrowIfNull(registrations);

        var keyedContracts = registrations
            .Where(r => r.Key is not null)
            .Select(r => (r.ServiceType, r.Key))
            .ToHashSet();

        foreach (var registration in registrations)
        {
            foreach (var constructor in registration.ImplementationType.GetConstructors())
            {
                foreach (var parameter in constructor.GetParameters())
                {
                    var keyed = parameter
                        .GetCustomAttributes(typeof(FromKeyedServicesAttribute), inherit: false)
                        .OfType<FromKeyedServicesAttribute>()
                        .SingleOrDefault();

                    if (keyed is null)
                    {
                        continue;
                    }

                    if (!keyedContracts.Contains((parameter.ParameterType, keyed.Key)))
                    {
                        throw new ServiceRegistrationException(
                            $"构造函数参数 {registration.ImplementationType.FullName}.{parameter.Name} " +
                            $"指向未注册 keyed service: {parameter.ParameterType.FullName} / {keyed.Key}");
                    }
                }
            }
        }
    }
}
