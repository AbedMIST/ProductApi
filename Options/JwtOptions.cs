using System.ComponentModel.DataAnnotations;

namespace ProductApi.Options;

// Bound from the "Jwt" section of configuration.
public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required] public string Issuer { get; set; } = string.Empty;
    [Required] public string Audience { get; set; } = string.Empty;

    // HMAC-SHA256 needs a key of at least 32 bytes. Keep the real value out of source control.
    [Required, MinLength(32)] public string Key { get; set; } = string.Empty;

    // Access token lifetime.
    [Range(1, 1440)] public int ExpiryMinutes { get; set; } = 60;

    // Refresh token lifetime.
    [Range(1, 90)] public int RefreshTokenExpiryDays { get; set; } = 7;
}
