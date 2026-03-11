using MailKit.Net.Smtp;
using MimeKit;

namespace imp_api.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<string> SendPasswordResetEmailAsync(string? email, string token)
    {
        var frontendBaseUrl = _config["ResetPassword:FrontendBaseUrl"] ?? "http://localhost:3000";
        var resetLink = $"{frontendBaseUrl}/reset-password?token={token}";

        var smtpHost = _config["Smtp:Host"];
        if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("SMTP not configured or no email. Reset link for {Email}: {ResetLink}", email, resetLink);
            return resetLink;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                _config["Smtp:FromName"] ?? "IMP System",
                _config["Smtp:FromEmail"] ?? "noreply@example.com"));
            message.To.Add(MailboxAddress.Parse(email));
            message.Subject = "รีเซ็ตรหัสผ่าน — IMP System";

            message.Body = new TextPart("html")
            {
                Text = $@"
                <h2>รีเซ็ตรหัสผ่าน</h2>
                <p>คุณได้ร้องขอการรีเซ็ตรหัสผ่าน คลิกลิงก์ด้านล่างเพื่อตั้งรหัสผ่านใหม่:</p>
                <p><a href='{resetLink}'>รีเซ็ตรหัสผ่าน</a></p>
                <p>ลิงก์นี้จะหมดอายุภายใน {_config.GetValue<int>("ResetPassword:TokenExpirationHours", 1)} ชั่วโมง</p>
                <p>หากคุณไม่ได้ร้องขอ กรุณาเพิกเฉยอีเมลนี้</p>"
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(
                smtpHost,
                _config.GetValue<int>("Smtp:Port", 587),
                _config.GetValue<bool>("Smtp:UseSsl", true));
            await client.AuthenticateAsync(
                _config["Smtp:UserName"],
                _config["Smtp:Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Password reset email sent to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", email);
        }

        return resetLink;
    }
}
