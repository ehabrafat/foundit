using System.ComponentModel.DataAnnotations;

namespace Application.Authentication;

public sealed record RegisterRequest(string FirstName, string LastName, string Email, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record AuthResponse(string Token, DateTime ExpiresAtUtc);
public sealed record ForgotPasswordRequest([Required, EmailAddress] string Email);
public sealed record VerifyPasswordResetOtpRequest([Required, EmailAddress] string Email, [Required, StringLength(4, MinimumLength = 4)] string Otp);
public sealed record PasswordResetTokenResponse(string ResetToken, DateTime ExpiresAtUtc);
public sealed record ResetPasswordRequest([Required] string ResetToken, [Required, MinLength(8)] string NewPassword);

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<bool> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task RequestPasswordResetAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task<PasswordResetTokenResponse?> VerifyPasswordResetOtpAsync(VerifyPasswordResetOtpRequest request, CancellationToken cancellationToken = default);
    Task<bool> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}
