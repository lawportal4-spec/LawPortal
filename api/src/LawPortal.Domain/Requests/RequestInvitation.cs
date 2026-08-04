using LawPortal.Domain.Common;

namespace LawPortal.Domain.Requests;

/// <summary>
/// Which lawyers can see and bid on a given <see cref="BiddingRequest"/> — one row per invited
/// lawyer, whether they got there via <see cref="BidSendMethod.Targeted"/> (the client picked
/// them) or <see cref="BidSendMethod.Broadcast"/> (a queue-driven fan-out matched them by
/// specialty — see <c>BidFanOutConsumer</c> — never a synchronous loop over hundreds of rows).
/// </summary>
public class RequestInvitation : Entity<Guid>
{
    public Guid ServiceRequestId { get; set; }
    public Guid LawyerProfileId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ViewedAtUtc { get; set; }
}
