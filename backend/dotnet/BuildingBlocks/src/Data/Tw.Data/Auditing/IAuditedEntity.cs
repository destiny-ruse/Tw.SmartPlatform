namespace Tw.Data.Auditing;

public interface IAuditedEntity
{
    DateTimeOffset CreatedAt { get; set; }

    DateTimeOffset UpdatedAt { get; set; }

    string? CreatedBy { get; set; }

    string? UpdatedBy { get; set; }
}
