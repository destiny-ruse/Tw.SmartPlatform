namespace Tw.Data.SqlSugar.Connection;

/// <summary>定义 IConnectionConfigResolver 契约</summary>
public interface IConnectionConfigResolver
{
    /// <summary>执行 ResolveAsync 操作</summary>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>ResolveAsync 的执行结果</returns>
    Task<object> ResolveAsync(CancellationToken cancellationToken = default);
}
