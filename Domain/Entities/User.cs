namespace Domain.Entities;

public class User : BaseEntity
{
    private User() { }

    public User(string firstName, string lastName, string email, string passwordHash)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Password = passwordHash;
        Role = "user";
    }

    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string Password { get; private set; } = default!;
    public string Role { get; private set; } = default!; // admin, user
    public string? PasswordResetOtpHash { get; private set; }
    public DateTime? PasswordResetOtpExpiresAtUtc { get; private set; }
    public string? PasswordResetTokenHash { get; private set; }
    public DateTime? PasswordResetTokenExpiresAtUtc { get; private set; }

    public void SetPasswordResetOtp(string otpHash, DateTime expiresAtUtc)
    {
        PasswordResetOtpHash = otpHash;
        PasswordResetOtpExpiresAtUtc = expiresAtUtc;
    }

    public void ChangePassword(string passwordHash)
    {
        Password = passwordHash;
        PasswordResetOtpHash = null;
        PasswordResetOtpExpiresAtUtc = null;
        PasswordResetTokenHash = null;
        PasswordResetTokenExpiresAtUtc = null;
    }

    public void SetPasswordResetToken(string tokenHash, DateTime expiresAtUtc)
    {
        PasswordResetTokenHash = tokenHash;
        PasswordResetTokenExpiresAtUtc = expiresAtUtc;
        PasswordResetOtpHash = null;
        PasswordResetOtpExpiresAtUtc = null;
    }
}
