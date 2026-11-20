using ProductApi.Dtos;

namespace ProductApi.Services;

public interface IAuthService
{
    // Returns false when the username is already taken.
    Task<bool> RegisterAsync(RegisterRequest request, CancellationToken ct);

    // Returns null when the credentials are wrong.
    Task<TokenResponse?> LoginAsync(LoginRequest request, CancellationToken ct);

    // Returns null when the refresh token is unknown, expired, or revoked.
    Task<TokenResponse?> RefreshAsync(RefreshRequest request, CancellationToken ct);

    // Revokes the refresh token if it exists and is active. Idempotent.
    Task LogoutAsync(RefreshRequest request, CancellationToken ct);
}
