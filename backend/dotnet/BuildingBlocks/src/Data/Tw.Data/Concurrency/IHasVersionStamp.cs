namespace Tw.Data.Concurrency;

/// <summary>
/// 定义HasVersionStamp的能力边界
/// </summary>
public interface IHasVersionStamp
{
    /// <summary>
    /// VersionStamp在当前对象中的业务含义
    /// </summary>
    long VersionStamp { get; set; }
}
