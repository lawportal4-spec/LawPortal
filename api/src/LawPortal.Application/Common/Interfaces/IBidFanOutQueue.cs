namespace LawPortal.Application.Common.Interfaces;

/// <summary>
/// Publishes a "fan this bidding request out to matching lawyers" message onto a durable queue —
/// the plan's own architecture rule verbatim: "bid fan-out to ~1,000 lawyers goes through a
/// queue, never a loop." The HTTP request that submits a broadcast bidding request only ever
/// publishes one small message here; a background consumer (see
/// LawPortal.Infrastructure.Messaging.BidFanOutConsumer) does the actual lawyer-matching and
/// bulk-insert of <see cref="Domain.Requests.RequestInvitation"/> rows out of band.
/// </summary>
public interface IBidFanOutQueue
{
    Task EnqueueAsync(Guid biddingRequestId, CancellationToken cancellationToken);
}
