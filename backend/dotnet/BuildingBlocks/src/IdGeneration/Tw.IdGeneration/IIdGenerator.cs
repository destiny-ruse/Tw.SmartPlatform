namespace Tw.IdGeneration;

/// <summary>
/// 生成全局唯一长整型标识
/// </summary>
public interface IIdGenerator
{
    /// <summary>
    /// 生成新的长整型标识
    /// </summary>
    /// <returns>新的长整型标识</returns>
    long NewId();
}
