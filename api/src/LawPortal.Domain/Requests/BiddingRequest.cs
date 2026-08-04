namespace LawPortal.Domain.Requests;

public enum BidSendMethod
{
    Broadcast = 1,
    Targeted = 2,
}

/// <summary>
/// Judiciary & execution, contracts & agreements, and "other services" — the pricing model
/// Baynah's own documentation stops covering entirely ("This is where Baynah's documentation
/// stops entirely — all of it is our design," per the plan). No fixed price and no fixed lawyer
/// exist until <see cref="AwardedLawyerProfileId"/> is set by accepting an <see cref="Offer"/>;
/// until then <see cref="ServiceRequest.Subtotal"/> stays null, same as a details-only catalog
/// request before a variant is chosen.
/// </summary>
public class BiddingRequest : ServiceRequest
{
    public BidSendMethod SendMethod { get; set; }

    public Guid? AwardedLawyerProfileId { get; set; }
    public Guid? AwardedOfferId { get; set; }

    public ICollection<RequestInvitation> Invitations { get; set; } = [];
    public ICollection<Offer> Offers { get; set; } = [];
}
