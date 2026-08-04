using LawPortal.Domain.Common;

namespace LawPortal.Domain.FreeMinutes;

/// <summary>
/// "A free-minutes entitlement per user" (the plan's own reading of Baynah's docs settles the
/// open question of per-user vs per-lawyer vs per-consultation-type) — granted once, at client
/// account creation. <see cref="ConsumedSeconds"/> has nothing to decrement it yet: metering
/// against actual call duration is P8's job, once real voice/video sessions exist. This pass
/// only grants the entitlement and exposes the balance.
/// </summary>
public class FreeMinutesEntitlement : Entity<Guid>
{
    public const int DefaultGrantSeconds = 7 * 60;

    public Guid ClientId { get; set; }
    public int GrantedSeconds { get; set; } = DefaultGrantSeconds;
    public int ConsumedSeconds { get; set; }
    public DateTime GrantedAtUtc { get; set; } = DateTime.UtcNow;
}
