namespace Tw.Data.Concurrency;

/// <summary>
/// 定义HasConcurrencyStamp的能力边界
/// </summary>
public interface IHasConcurrencyStamp
{
    /// <summary>
    /// ConcurrencyStamp在当前对象中的业务含义
    /// </summary>
    string ConcurrencyStamp { get; set; }
}
