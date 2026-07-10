namespace Tw.Data.SqlSugar.Connection;

/// <summary>
/// 定义SqlSugarClientFactory的能力边界
/// </summary>
public interface ISqlSugarClientFactory
{
    /// <summary>
    /// 创建Client测试对象
    /// </summary>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    object CreateClient(CancellationToken cancellationToken = default);
}
