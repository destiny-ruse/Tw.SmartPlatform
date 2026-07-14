namespace Tw.Domain.Concurrency;

/// <summary>
/// 标记通过不透明并发戳参与乐观并发控制的领域实体
/// </summary>
public interface IHasConcurrencyStamp
{
    /// <summary>
    /// 持久化适配器用于检测并发写入的不透明标识
    /// </summary>
    string ConcurrencyStamp { get; set; }
}
