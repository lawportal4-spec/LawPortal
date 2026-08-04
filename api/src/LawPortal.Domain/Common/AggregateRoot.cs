namespace LawPortal.Domain.Common;

public abstract class AggregateRoot<TId> : Entity<TId>, IAuditable, ISoftDeletable
    where TId : notnull
{
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
}
