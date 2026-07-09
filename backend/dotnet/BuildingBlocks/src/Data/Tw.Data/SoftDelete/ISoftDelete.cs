namespace Tw.Data.SoftDelete;

public interface ISoftDelete
{
    bool IsDeleted { get; set; }

    DateTimeOffset? DeletedAt { get; set; }

    string? DeletedBy { get; set; }
}
