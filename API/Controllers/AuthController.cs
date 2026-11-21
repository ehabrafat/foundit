using Application.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace FounditAi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var registered = await authService.RegisterAsync(request, cancellationToken);
        if (!registered)
            return Conflict(new { message = "An account with this email already exists." });

        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, cancellationToken);
        return response is null ? Unauthorized() : Ok(response);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.RequestPasswordResetAsync(request, cancellationToken);
        return Ok(new { message = "If an account exists for that email, a reset code has been sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var reset = await authService.ResetPasswordAsync(request, cancellationToken);
        return reset ? NoContent() : BadRequest(new { message = "The reset code is invalid or expired." });
    }

    [HttpPost("verify-reset-code")]
    public async Task<ActionResult<PasswordResetTokenResponse>> VerifyResetCode(VerifyPasswordResetOtpRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.VerifyPasswordResetOtpAsync(request, cancellationToken);
        return result is null ? BadRequest(new { message = "The reset code is invalid or expired." }) : Ok(result);
    }
}
