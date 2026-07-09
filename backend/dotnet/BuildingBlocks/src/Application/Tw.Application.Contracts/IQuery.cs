namespace Tw.Application.Contracts;

/// <summary>
/// 表示返回业务结果的应用查询
/// </summary>
/// <typeparam name="TResult">查询返回的业务结果类型</typeparam>
public interface IQuery<out TResult>;
