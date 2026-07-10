namespace Tw.Data.SqlSugar.Connection;

/// <summary>
/// 定义ConnectionConfigResolver的能力边界
/// </summary>
public interface IConnectionConfigResolver
{
    /// <summary>
    /// 解析测试场景所需的签名证书
    /// </summary>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的object</returns>
    Task<object> ResolveAsync(CancellationToken cancellationToken = default);
}
