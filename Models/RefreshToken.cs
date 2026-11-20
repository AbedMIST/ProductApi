namespace ProductApi.Models;

// One row per issued refresh token. Only the SHA-256 hash is stored, never the raw token.
public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public required string TokenHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    // Set when the token is used (rotated) or revoked (logout / reuse detected).
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public bool IsActive(DateTime nowUtc) => RevokedAt is null && ExpiresAt > nowUtc;
}
