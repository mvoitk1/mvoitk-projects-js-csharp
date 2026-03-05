using System.Net;
using System.Net.Mail;

namespace VenuePlatform.Web.Services;

/// <summary>
/// Simple SMTP email sender. KISS - no templates, no background jobs.
/// </summary>
public sealed class EmailSender
{
    private readonly IConfiguration _config;

    public EmailSender(IConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Sends an email via SMTP.
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="body">Plain text email body</param>
    /// <exception cref="InvalidOperationException">Thrown when required email configuration is missing</exception>
    public Task SendAsync(string toEmail, string subject, string body)
    {
        return SendAsync(toEmail, subject, body, attachments: null);
    }

    /// <summary>
    /// Sends an email via SMTP with attachments.
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="body">Plain text email body</param>
    /// <param name="attachments">List of attachments (filename, content bytes, content type)</param>
    /// <exception cref="InvalidOperationException">Thrown when required email configuration is missing</exception>
    public Task SendAsync(
        string toEmail,
        string subject,
        string body,
        IReadOnlyList<(string FileName, byte[] Content, string ContentType)>? attachments)
    {
        // Validate required config
        var host = _config.GetSection("Email")["Host"];
        var portStr = _config.GetSection("Email")["Port"];
        var fromAddress = _config.GetSection("Email")["FromAddress"];

        if (string.IsNullOrWhiteSpace(host))
        {
            throw new InvalidOperationException("Email configuration 'Host' is required.");
        }

        if (string.IsNullOrWhiteSpace(portStr) || !int.TryParse(portStr, out var port))
        {
            throw new InvalidOperationException("Email configuration 'Port' is required and must be a valid number.");
        }

        if (string.IsNullOrWhiteSpace(fromAddress))
        {
            throw new InvalidOperationException("Email configuration 'FromAddress' is required.");
        }

        // Optional credentials
        var username = _config.GetSection("Email")["Username"];
        var password = _config.GetSection("Email")["Password"];

        // Enable SSL for non-standard SMTP ports (not 25)
        var enableSsl = port != 25;

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            UseDefaultCredentials = string.IsNullOrWhiteSpace(username)
        };

        // Set credentials only if provided
        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            client.Credentials = new NetworkCredential(username, password);
        }

        using var message = new MailMessage(fromAddress, toEmail, subject, body)
        {
            IsBodyHtml = false
        };

        // Add attachments if provided
        if (attachments is not null)
        {
            foreach (var (fileName, content, contentType) in attachments)
            {
                var stream = new MemoryStream(content);
                var attachment = new Attachment(stream, fileName, contentType);
                message.Attachments.Add(attachment);
            }
        }

        return client.SendMailAsync(message);
    }
}
