using Tw.DependencyInjection.Abstractions.Configuration;
using Tw.DependencyInjection.Abstractions;

namespace Tw.DependencyInjection.Registration;

/// <summary>
/// 检查服务类型的注册资格与生命周期声明
/// </summary>
internal static class ServiceTypeInspector
{
    /// <summary>
    /// 判断类型是否应跳过普通服务注册（如接口、抽象类、Options 类型等）
    /// </summary>
    /// <param name="type">待检查的类型</param>
    /// <param name="reason">跳过原因；不跳过时为空字符串</param>
    /// <returns>应跳过时返回 <see langword="true"/></returns>
    public static bool ShouldSkipOrdinaryRegistration(Type type, out string reason)
    {
        if (type.IsInterface)
        {
            reason = "接口不作为普通服务实现注册";
            return true;
        }

        if (type.IsAbstract)
        {
            reason = "抽象类型不作为普通服务实现注册";
            return true;
        }

        // 排除"已部分构造但仍含未绑定泛型参数的类型"；
        // 泛型定义本身（如 Repository<>）由暴露解析按 open generic 处理，不在此跳过。
        if (type.IsGenericType && !type.IsGenericTypeDefinition && type.ContainsGenericParameters)
        {
            reason = "未闭合且非泛型定义的类型不作为普通服务实现注册";
            return true;
        }

        if (typeof(IConfigurableOptions).IsAssignableFrom(type))
        {
            reason = "Options 类型由 P3 Options 装载处理，不作为普通服务注册";
            return true;
        }

        if (type.GetCustomAttributes(typeof(DisableServiceRegistrationAttribute), inherit: false).Length > 0)
        {
            reason = "类型标记 DisableServiceRegistration";
            return true;
        }

        reason = string.Empty;
        return false;
    }

    /// <summary>
    /// 判断类型是否声明参与自动注册（携带标记接口或 <see cref="ServiceRegistrationAttribute"/>）
    /// </summary>
    /// <param name="type">待检查的类型</param>
    /// <returns>参与注册时返回 <see langword="true"/></returns>
    public static bool IsRegistrationParticipant(Type type)
    {
        return type.GetCustomAttributes(typeof(ServiceRegistrationAttribute), inherit: false).Length > 0
            || typeof(ITransientDependency).IsAssignableFrom(type)
            || typeof(IScopedDependency).IsAssignableFrom(type)
            || typeof(ISingletonDependency).IsAssignableFrom(type);
    }

    /// <summary>
    /// 尝试从类型的标记接口或特性中解析服务生命周期
    /// </summary>
    /// <param name="type">待解析的类型</param>
    /// <param name="lifetime">解析成功时的生命周期；失败时为 <see langword="default"/></param>
    /// <param name="failureReason">解析失败时的原因描述；成功时为 <see langword="null"/></param>
    /// <returns>解析成功时返回 <see langword="true"/></returns>
    /// <remarks>
    /// <para>
    /// 本方法以 <see langword="bool"/> 返回值作为成功/失败的唯一协议门控。
    /// 当方法返回 <see langword="false"/> 时，<paramref name="lifetime"/> 被赋值为
    /// <see langword="default"/>（即 <c>default(DependencyLifetime)</c>），
    /// 这仅为满足 C# <see langword="out"/> 参数的语言要求，该值无任何业务语义。
    /// </para>
    /// <para>
    /// 调用方在返回 <see langword="false"/> 时不得读取或依赖 <paramref name="lifetime"/> 的值，
    /// 应仅依据返回值与 <paramref name="failureReason"/> 判断解析结果。
    /// <c>DependencyLifetime</c> 抽象文档禁止将 <c>default(DependencyLifetime)</c> 当作
    /// "未设置"哨兵；本方法未将 <see langword="default"/> 用作语义哨兵。
    /// </para>
    /// </remarks>
    public static bool TryResolveLifetime(
        Type type,
        out DependencyLifetime lifetime,
        out string? failureReason)
    {
        var markerLifetimes = new List<DependencyLifetime>();
        if (typeof(ITransientDependency).IsAssignableFrom(type))
        {
            markerLifetimes.Add(DependencyLifetime.Transient);
        }

        if (typeof(IScopedDependency).IsAssignableFrom(type))
        {
            markerLifetimes.Add(DependencyLifetime.Scoped);
        }

        if (typeof(ISingletonDependency).IsAssignableFrom(type))
        {
            markerLifetimes.Add(DependencyLifetime.Singleton);
        }

        if (markerLifetimes.Count > 1)
        {
            lifetime = default;
            failureReason = "类型声明多个生命周期标记";
            return false;
        }

        var registration = type.GetCustomAttributes(typeof(ServiceRegistrationAttribute), inherit: false)
            .OfType<ServiceRegistrationAttribute>()
            .SingleOrDefault();

        if (registration?.Lifetime is not null)
        {
            lifetime = registration.Lifetime.Value;
            failureReason = null;
            return true;
        }

        if (markerLifetimes.Count == 1)
        {
            lifetime = markerLifetimes[0];
            failureReason = null;
            return true;
        }

        lifetime = default;
        failureReason = "类型未声明生命周期";
        return false;
    }
}
