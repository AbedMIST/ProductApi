using Microsoft.AspNetCore.Mvc;
using ProductApi.Dtos;
using ProductApi.Services;

namespace ProductApi.Controllers;

[ApiController]
[Route("api/[controller]")] // -> /api/auth
[Produces("application/json")]
public class AuthController(IAuthService auth) : ControllerBase
{
    // POST /api/auth/register
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        if (!await auth.RegisterAsync(request, ct))
            return Problem("Username is already taken.", statusCode: StatusCodes.Status409Conflict);

        return StatusCode(StatusCodes.Status201Created);
    }

    // POST /api/auth/login  -> returns a JWT to send as "Authorization: Bearer <token>"
    [HttpPost("login")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var token = await auth.LoginAsync(request, ct);
        return token is null
            ? Problem("Invalid username or password.", statusCode: StatusCodes.Status401Unauthorized)
            : Ok(token);
    }

    // POST /api/auth/refresh  -> trade a valid refresh token for a new access + refresh token pair.
    // The submitted refresh token is single-use; reusing it revokes all of the user's sessions.
    [HttpPost("refresh")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> Refresh(RefreshRequest request, CancellationToken ct)
    {
        var token = await auth.RefreshAsync(request, ct);
        return token is null
            ? Problem("Invalid or expired refresh token.", statusCode: StatusCodes.Status401Unauthorized)
            : Ok(token);
    }

    // POST /api/auth/logout  -> revoke the refresh token. Always 204 so it doesn't reveal whether the token existed.
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken ct)
    {
        await auth.LogoutAsync(request, ct);
        return NoContent();
    }
}
