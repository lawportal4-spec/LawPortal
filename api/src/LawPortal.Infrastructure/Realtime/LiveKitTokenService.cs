using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using LawPortal.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace LawPortal.Infrastructure.Realtime;

/// <summary>
/// Builds a LiveKit access token exactly per their documented spec: a JWT whose standard "sub"
/// claim is the participant identity and whose "video" claim is a grant object (room name,
/// join/publish/subscribe permissions), signed HS256 with the API secret and issued as the API
/// key. No official LiveKit .NET SDK exists to wrap this — verified correct by confirming
/// LiveKit's own server accepts the minted token (see the P8 progress log).
/// </summary>
public class LiveKitTokenService(IConfiguration configuration) : ILiveKitTokenService
{
    public string CreateAccessToken(string roomName, string identity, TimeSpan ttl)
    {
        var section = configuration.GetSection("LiveKit");
        var apiKey = section["ApiKey"] ?? throw new InvalidOperationException("LiveKit:ApiKey is not configured.");
        var apiSecret = section["ApiSecret"] ?? throw new InvalidOperationException("LiveKit:ApiSecret is not configured.");

        var videoGrant = JsonSerializer.Serialize(new
        {
            room = roomName,
            roomJoin = true,
            canPublish = true,
            canSubscribe = true,
            canPublishData = true,
        });

        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, identity),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("video", videoGrant, "JSON"),
        };

        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(apiSecret)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer: apiKey, claims: claims, notBefore: now, expires: now.Add(ttl), signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
