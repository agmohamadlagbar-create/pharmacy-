using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OnlineStore.Api.Services;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string toEmail, string code);
    Task SendPasswordResetEmailAsync(string toEmail, string code);
}

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _env;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, IHostEnvironment env, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _env = env;
        _logger = logger;
    }

    public Task SendVerificationEmailAsync(string toEmail, string code)
    {
        return SendEmailAsync(toEmail,
            "Verify your email – Online Store",
            "Email Verification",
            "Welcome! 👋",
            "Enter the code below in the app to verify your email address and get started.",
            code);
    }

    public Task SendPasswordResetEmailAsync(string toEmail, string code)
    {
        return SendEmailAsync(toEmail,
            "Reset your password – Online Store",
            "Password Reset",
            "Password reset requested",
            "Enter the code below in the app to reset your password. If you didn't request this, you can safely ignore this email.",
            code);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string subtitle, string heading, string body, string code)
    {
        var smtp = _config.GetSection("Smtp");
        var host = smtp["Host"] ?? "smtp.gmail.com";
        var port = int.TryParse(smtp["Port"], out var p) ? p : 587;
        var user = smtp["User"] ?? "";
        var password = smtp["Password"] ?? "";
        var fromEmail = smtp["FromEmail"] ?? user;
        var fromName = smtp["FromName"] ?? "Online Store";

        if (_env.IsDevelopment())
        {
            _logger.LogInformation("📧 [DEV EMAIL] To: {Email} | {Subtitle} Code: {Code}", toEmail, subtitle, code);
        }

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(user, password),
            EnableSsl = true
        };

        var message = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = subject,
            IsBodyHtml = true,
            Body = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""margin:0; padding:0; background-color:#f0f2f5; font-family: 'Segoe UI', Roboto, Arial, sans-serif;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f0f2f5; padding:40px 20px;"">
    <tr>
      <td align=""center"">
        <table width=""480"" cellpadding=""0"" cellspacing=""0"" style=""background:#ffffff; border-radius:16px; box-shadow:0 4px 24px rgba(0,0,0,0.08); overflow:hidden;"">
          <tr>
            <td style=""background: linear-gradient(135deg, #4f46e5, #7c3aed); padding:32px 40px 24px; text-align:center;"">
              <h1 style=""margin:0; color:#ffffff; font-size:24px; font-weight:700; letter-spacing:-0.5px;"">
                🛍️ Online Store
              </h1>
              <p style=""margin:8px 0 0; color:rgba(255,255,255,0.85); font-size:14px;"">
                {subtitle}
              </p>
            </td>
          </tr>
          <tr>
            <td style=""padding:32px 40px;"">
              <p style=""margin:0 0 8px; color:#1e293b; font-size:16px; font-weight:600;"">{heading}</p>
              <p style=""margin:0 0 24px; color:#64748b; font-size:14px; line-height:1.6;"">{body}</p>
              <div style=""text-align:center; margin:0 0 24px;"">
                <div style=""display:inline-block; background:#f8fafc; border:2px dashed #cbd5e1; border-radius:12px; padding:20px 40px;"">
                  <span style=""font-size:36px; font-weight:800; letter-spacing:8px; color:#4f46e5; font-family:'Courier New',monospace;"">{code}</span>
                </div>
              </div>
              <p style=""margin:0; color:#94a3b8; font-size:12px; text-align:center; line-height:1.5;"">
                This code is valid for <strong>24 hours</strong>.
              </p>
            </td>
          </tr>
          <tr>
            <td style=""background:#f8fafc; padding:20px 40px; text-align:center; border-top:1px solid #e2e8f0;"">
              <p style=""margin:0; color:#94a3b8; font-size:11px;"">
                © Online Store · This is an automated message, please do not reply.
              </p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>"
        };

        message.To.Add(toEmail);
        try
        {
            await client.SendMailAsync(message);
        }
        catch (Exception ex) when (_env.IsDevelopment())
        {
            _logger.LogWarning(ex, "⚠️ [DEV EMAIL] SMTP send failed for {Email}; use the console code logged above ({Code}).", toEmail, code);
        }
    }
}
