using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ProductApi.Models;
using ProductApi.Options;

namespace ProductApi.Services;

// Builds and signs the JWT. The same key/issuer/audience are used by the validation side in Program.cs.
public class TokenService(IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions _jwt = options.Value;

    public AccessToken CreateAccessToken(User user)
    {
        var expires = DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            Expires = expires,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key)),
                SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new AccessToken(token, expires);
    }

    // 64 random bytes -> URL-safe Base64. Unguessable, so a fast hash (SHA-256) is enough for storage.
    public string CreateRefreshToken() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));

    public string HashRefreshToken(string refreshToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}
