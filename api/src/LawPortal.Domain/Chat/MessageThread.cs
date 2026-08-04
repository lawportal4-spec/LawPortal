using LawPortal.Domain.Common;
using LawPortal.Domain.Requests;

namespace LawPortal.Domain.Chat;

/// <summary>
/// One thread per paid consultation, opened automatically once payment succeeds (see
/// <c>InitiateCheckoutHandler.FinalizeSuccessfulPaymentAsync</c>) — chat is a consultation
/// feature, not a general request feature, so catalog requests (notarization/trademark) never
/// get one. Both sides are referenced by <see cref="Identity.User.Id"/> rather than
/// <see cref="Identity.ClientProfile.Id"/>/<see cref="Identity.LawyerProfile.Id"/> so a message's
/// <see cref="Message.SenderUserId"/> can be compared directly without a join.
/// </summary>
public class MessageThread : AggregateRoot<Guid>
{
    public Guid ServiceRequestId { get; set; }
    public ServiceRequest? ServiceRequest { get; set; }

    public Guid ClientUserId { get; set; }
    public Guid LawyerUserId { get; set; }

    public DateTime? ClosedAtUtc { get; set; }

    public ICollection<Message> Messages { get; set; } = [];
    public ICollection<ThreadParticipant> Participants { get; set; } = [];
}
