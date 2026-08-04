namespace LawPortal.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}
