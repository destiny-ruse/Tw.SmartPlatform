namespace Tw.DependencyInjection.Registration;

/// <summary>
/// 描述一次服务暴露关系，即实现类型以哪个契约类型（以及可选 key）注册到容器
/// </summary>
/// <param name="ServiceType">要注册的服务契约类型；开放泛型时为泛型定义</param>
/// <param name="Key">keyed service 注册 key；普通注册时为 <c>null</c></param>
internal sealed record ServiceExposure(Type ServiceType, object? Key);
