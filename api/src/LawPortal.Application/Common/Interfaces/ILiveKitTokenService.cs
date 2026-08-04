namespace LawPortal.Application.Common.Interfaces;

/// <summary>Mints a LiveKit access token (a JWT with a "video" grant claim, per LiveKit's own
/// token spec) — no official LiveKit .NET SDK exists, so this is built directly against their
/// documented token format rather than a wrapped client library.</summary>
public interface ILiveKitTokenService
{
    string CreateAccessToken(string roomName, string identity, TimeSpan ttl);
}
