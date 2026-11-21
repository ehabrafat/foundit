using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Globalization;
using System.Text;
using Application.Authentication;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Services;

public sealed class AuthService(ApiDbContext dbContext, IConfiguration configuration, IEmailSender emailSender) : IAuthService
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is null || !VerifyPassword(request.Password, user.Password))
            return null;

        return CreateToken(user);
    }

    public async Task<bool> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        if (await dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken))
            return false;

        var user = new User(request.FirstName.Trim(), request.LastName.Trim(), email, HashPassword(request.Password));
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task RequestPasswordResetAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is null) return;

        var otp = RandomNumberGenerator.GetInt32(0, 10_000).ToString("D4", CultureInfo.InvariantCulture);
        var otpHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(otp)));
        user.SetPasswordResetOtp(otpHash, DateTime.UtcNow.AddMinutes(10));
        await dbContext.SaveChangesAsync(cancellationToken);
        await emailSender.SendAsync(user.Email, "Password reset code", $"Your password reset code is {otp}. It expires in 10 minutes.", cancellationToken);
    }

    public async Task<PasswordResetTokenResponse?> VerifyPasswordResetOtpAsync(VerifyPasswordResetOtpRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Otp.Length != 4 || request.Otp.Any(character => character is < '0' or > '9'))
            return null;
        var email = NormalizeEmail(request.Email);
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user?.PasswordResetOtpHash is null || user.PasswordResetOtpExpiresAtUtc <= DateTime.UtcNow)
            return null;

        var providedHash = SHA256.HashData(Encoding.UTF8.GetBytes(request.Otp));
        var storedHash = Convert.FromHexString(user.PasswordResetOtpHash);
        if (!CryptographicOperations.FixedTimeEquals(providedHash, storedHash)) return null;

        var resetToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var expiresAt = DateTime.UtcNow.AddMinutes(15);
        user.SetPasswordResetToken(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(resetToken))), expiresAt);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new PasswordResetTokenResponse(resetToken, expiresAt);
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var resetTokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.ResetToken)));
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.PasswordResetTokenHash == resetTokenHash, cancellationToken);
        if (user is null || user.PasswordResetTokenExpiresAtUtc <= DateTime.UtcNow)
            return false;

        user.ChangePassword(HashPassword(request.NewPassword));
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private AuthResponse CreateToken(User user)
    {
        var secret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT secret is missing from configuration.");
        var expiresAt = DateTime.UtcNow.AddHours(1);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(claims: claims, expires: expiresAt, signingCredentials: credentials);

        return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithm, HashSize);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
            return false;

        try
        {
            var salt = Convert.FromBase64String(parts[1]);
            var expectedHash = Convert.FromBase64String(parts[2]);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithm, expectedHash.Length);
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
