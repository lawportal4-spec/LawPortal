using LawPortal.Domain.Common;

namespace LawPortal.Domain.Requests;

public class RequestStatusHistory : Entity<Guid>
{
    public Guid ServiceRequestId { get; set; }
    public RequestStatus FromStatus { get; set; }
    public RequestStatus ToStatus { get; set; }
    public string? Trigger { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
