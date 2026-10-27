using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace CloudKitchen.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailSender> _logger;
        private readonly IHostEnvironment _env;

        public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger, IHostEnvironment env)
        {
            _configuration = configuration;
            _logger = logger;
            _env = env;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = _configuration.GetValue<int>("Email:SmtpPort", 587);
            var smtpUsername = _configuration["Email:SmtpUsername"];
            var smtpPassword = _configuration["Email:SmtpPassword"]?.Replace(" ", "").Trim();
            var fromEmail = _configuration["Email:FromEmail"] ?? smtpUsername;
            var fromName = _configuration["Email:FromName"] ?? "Cloud Kitchen";

            if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpUsername) || string.IsNullOrWhiteSpace(smtpPassword) || string.IsNullOrWhiteSpace(fromEmail))
            {
                _logger.LogError("Email SMTP configuration is missing or incomplete. SmtpHost: '{Host}', SmtpUsername: '{User}', HasPassword: {HasPass}, FromEmail: '{From}'",
                    smtpHost, smtpUsername, !string.IsNullOrWhiteSpace(smtpPassword), fromEmail);
                throw new System.InvalidOperationException("Email SMTP configuration is missing or incomplete. Please ensure Email:SmtpPassword is set in User Secrets.");
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromName, fromEmail));
                message.To.Add(new MailboxAddress("", email));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlMessage
                };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                client.Timeout = 20000;

                // Bypass SSL/TLS certificate validation in case of local antivirus or corporate proxy inspection
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                _logger.LogInformation("Attempting to send email to '{Email}' via MailKit SMTP {Host}:{Port} (STARTTLS)...", email, smtpHost, smtpPort);

                var secureSocketOptions = smtpPort == 465 
                    ? SecureSocketOptions.SslOnConnect 
                    : SecureSocketOptions.StartTls;

                await client.ConnectAsync(smtpHost, smtpPort, secureSocketOptions);
                await client.AuthenticateAsync(smtpUsername, smtpPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Password reset email sent successfully to '{Email}' via MailKit SMTP.", email);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "SMTP error while sending email to '{Email}': {Message}", email, ex.Message);
                throw;
            }
        }
    }
}
