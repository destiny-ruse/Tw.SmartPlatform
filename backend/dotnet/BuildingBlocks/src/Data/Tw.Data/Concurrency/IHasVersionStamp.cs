namespace Tw.Data.Concurrency;

/// <summary>定义 IHasVersionStamp 契约</summary>
public interface IHasVersionStamp
{
    /// <summary>表示 VersionStamp 属性</summary>
    long VersionStamp { get; set; }
}
