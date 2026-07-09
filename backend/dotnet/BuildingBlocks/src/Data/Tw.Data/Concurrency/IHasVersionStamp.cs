namespace Tw.Data.Concurrency;

public interface IHasVersionStamp
{
    long VersionStamp { get; set; }
}
