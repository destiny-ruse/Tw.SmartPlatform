namespace Tw.Data.SqlSugar.Connection;

/// <summary>定义 ISqlSugarClientFactory 契约</summary>
public interface ISqlSugarClientFactory
{
    /// <summary>执行 CreateClient 操作</summary>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>CreateClient 的执行结果</returns>
    object CreateClient(CancellationToken cancellationToken = default);
}
