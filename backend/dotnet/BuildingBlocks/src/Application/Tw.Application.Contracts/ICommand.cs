namespace Tw.Application.Contracts;

/// <summary>
/// 表示不返回业务结果的应用命令
/// </summary>
public interface ICommand;

/// <summary>
/// 表示返回业务结果的应用命令
/// </summary>
/// <typeparam name="TResult">命令执行后的业务结果类型</typeparam>
public interface ICommand<out TResult>;
