using ProductApi.Models;

namespace ProductApi.Services;

public record AccessToken(string Token, DateTime ExpiresAtUtc);

public interface ITokenService
{
    // Signed, short-lived JWT.
    AccessToken CreateAccessToken(User user);

    // Opaque, cryptographically random string; returned to the client once.
    string CreateRefreshToken();

    // Deterministic hash used to store and look up refresh tokens.
    string HashRefreshToken(string refreshToken);
}
