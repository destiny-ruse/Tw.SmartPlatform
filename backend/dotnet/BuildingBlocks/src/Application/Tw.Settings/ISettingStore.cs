namespace Tw.Settings;

/// <summary>
/// Setting 值存储读取边界
/// </summary>
public interface ISettingStore
{
    /// <summary>
    /// 查找指定作用域下的 Setting 值
    /// </summary>
    /// <param name="name">Setting 名称</param>
    /// <param name="scope">Setting 作用域</param>
    /// <param name="scopeKey">作用域键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>匹配的 Setting 值，不存在时返回 null</returns>
    Task<SettingValue?> FindAsync(
        string name,
        SettingScope scope,
        string scopeKey,
        CancellationToken cancellationToken);
}
