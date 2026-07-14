namespace Tw.Domain.Concurrency;

/// <summary>
/// 标记通过数字版本参与乐观并发控制的领域实体
/// </summary>
public interface IHasVersionStamp
{
    /// <summary>
    /// 持久化适配器用于检测并发写入的数字版本
    /// </summary>
    long VersionStamp { get; set; }
}
