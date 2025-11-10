using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace RTMultiTenant.Api.Services;

public interface IEmailSender
{
    Task<bool> SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken);
}

public class SmtpSettings
{
    public const string SectionName = "Smtp";
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string? User { get; set; }
    public string? Password { get; set; }
    public string From { get; set; } = "no-reply@example.com";
}

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpSettings _settings;

    public SmtpEmailSender(IOptions<SmtpSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<bool> SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.Host))
        {
            Console.WriteLine($"[EmailSender] SMTP not configured. To: {to}, Subject: {subject}\n{htmlBody}");
            return false;
        }

        using var client = new SmtpClient(_settings.Host!, _settings.Port)
        {
            EnableSsl = _settings.EnableSsl,
            Credentials = string.IsNullOrWhiteSpace(_settings.User)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(_settings.User, _settings.Password)
        };

        using var msg = new MailMessage(_settings.From, to)
        {
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        await client.SendMailAsync(msg, cancellationToken);
        return true;
    }
}

