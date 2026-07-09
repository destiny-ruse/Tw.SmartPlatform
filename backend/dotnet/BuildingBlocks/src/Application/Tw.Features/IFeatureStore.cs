namespace Tw.Features;

/// <summary>
/// Feature 值存储读取边界
/// </summary>
public interface IFeatureStore
{
    /// <summary>
    /// 查找指定作用域下的 Feature 值
    /// </summary>
    /// <param name="name">Feature 名称</param>
    /// <param name="scope">Feature 值作用域</param>
    /// <param name="scopeKey">作用域键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Feature 值；不存在时返回 null</returns>
    Task<FeatureValue?> FindAsync(
        string name,
        FeatureScope scope,
        string scopeKey,
        CancellationToken cancellationToken);
}
