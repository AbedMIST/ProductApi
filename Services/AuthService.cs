using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProductApi.Data;
using ProductApi.Dtos;
using ProductApi.Models;
using ProductApi.Options;

namespace ProductApi.Services;

public class AuthService(AppDbContext db, ITokenService tokens, IOptions<JwtOptions> jwtOptions) : IAuthService
{
    // Salted, slow hash (PBKDF2) from ASP.NET Core; the User object is only used as a type tag.
    private readonly PasswordHasher<User> _hasher = new();
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<bool> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var username = request.Username.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Username == username, ct)) return false;

        var user = new User { Username = username, CreatedAt = DateTime.UtcNow };
        user.PasswordHash = _hasher.HashPassword(user, request.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<TokenResponse?> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var username = request.Username.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username, ct);

        // Same null result for "no such user" and "wrong password" so the API doesn't reveal which usernames exist.
        if (user is null) return null;

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed) return null;

        var response = IssueTokens(user, out _);
        await db.SaveChangesAsync(ct);
        return response;
    }

    public async Task<TokenResponse?> RefreshAsync(RefreshRequest request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var hash = tokens.HashRefreshToken(request.RefreshToken);
        var stored = await db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is null) return null;

        // Reuse detection: a token that was already rotated/revoked is being presented again,
        // which suggests it was stolen. Kill every active session for this user.
        if (stored.RevokedAt is not null)
        {
            await RevokeAllForUserAsync(stored.UserId, now, ct);
            await db.SaveChangesAsync(ct);
            return null;
        }

        if (stored.ExpiresAt <= now) return null;

        // Rotation: the old token is single-use and is replaced by a new one.
        var response = IssueTokens(stored.User, out var newHash);
        stored.RevokedAt = now;
        stored.ReplacedByTokenHash = newHash;

        await db.SaveChangesAsync(ct);
        return response;
    }

    public async Task LogoutAsync(RefreshRequest request, CancellationToken ct)
    {
        var hash = tokens.HashRefreshToken(request.RefreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (stored is null || stored.RevokedAt is not null) return;

        stored.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    // Creates a new access token + refresh token pair and stages the refresh token row (caller saves).
    private TokenResponse IssueTokens(User user, out string refreshTokenHash)
    {
        var now = DateTime.UtcNow;
        var access = tokens.CreateAccessToken(user);
        var refresh = tokens.CreateRefreshToken();
        refreshTokenHash = tokens.HashRefreshToken(refresh);

        var refreshExpires = now.AddDays(_jwt.RefreshTokenExpiryDays);
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            CreatedAt = now,
            ExpiresAt = refreshExpires
        });

        return new TokenResponse(access.Token, "Bearer", access.ExpiresAtUtc, refresh, refreshExpires);
    }

    private async Task RevokeAllForUserAsync(int userId, DateTime now, CancellationToken ct)
    {
        var active = await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var t in active) t.RevokedAt = now;
    }
}
