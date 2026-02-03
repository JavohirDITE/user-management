using System.Net;
using System.Net.Mail;

namespace UserManagement.API.Services;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string email, string token);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendVerificationEmailAsync(string email, string token)
    {
        var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") 
            ?? _config["FrontendUrl"] 
            ?? "http://localhost:5173";
        var verificationLink = $"{frontendUrl}/verify/{token}";

        var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? _config["Email:SmtpHost"];
        var smtpPort = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT") ?? _config["Email:SmtpPort"], out var port) ? port : 587;
        var smtpUser = Environment.GetEnvironmentVariable("SMTP_USER") ?? _config["Email:SmtpUser"];
        var smtpPass = Environment.GetEnvironmentVariable("SMTP_PASS") ?? _config["Email:SmtpPass"];

        if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPass))
        {
            _logger.LogWarning("SMTP not configured. Verification link: {Link}", verificationLink);
            return;
        }

        try
        {
            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress(smtpUser, "User Management"),
                Subject = "Verify your email",
                Body = $"Please verify your email by clicking this link: {verificationLink}",
                IsBodyHtml = false
            };
            message.To.Add(email);

            await client.SendMailAsync(message);
            _logger.LogInformation("Verification email sent to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email to {Email}", email);
        }
    }
}
