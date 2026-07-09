namespace Tw.Data.Concurrency;

public interface IHasConcurrencyStamp
{
    string ConcurrencyStamp { get; set; }
}
