namespace Tw.Data.Concurrency;

/// <summary>定义 IHasConcurrencyStamp 契约</summary>
public interface IHasConcurrencyStamp
{
    /// <summary>表示 ConcurrencyStamp 属性</summary>
    string ConcurrencyStamp { get; set; }
}
