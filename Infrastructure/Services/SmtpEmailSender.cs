using System.Net;
using System.Net.Mail;
using Application.Authentication;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services;

public sealed class SmtpEmailSender(IConfiguration configuration) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var host = configuration["Smtp:Host"] ?? throw new InvalidOperationException("SMTP host is missing.");
        var port = int.TryParse(configuration["Smtp:Port"], out var parsedPort) ? parsedPort : 587;
        var username = configuration["Smtp:Username"];
        var password = configuration["Smtp:Password"];
        var from = configuration["Smtp:From"] ?? username ?? throw new InvalidOperationException("SMTP sender address is missing.");
        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            Credentials = string.IsNullOrWhiteSpace(username) ? CredentialCache.DefaultNetworkCredentials : new NetworkCredential(username, password)
        };
        using var message = new MailMessage(from, to, subject, body);
        await client.SendMailAsync(message, cancellationToken);
    }
}
