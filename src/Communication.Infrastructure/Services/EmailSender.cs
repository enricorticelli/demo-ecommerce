using System.Net.Mail;
using Communication.Application.Abstractions.Email;
using Communication.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;

namespace Communication.Infrastructure.Services;

public sealed class SmtpEmailSender(IConfiguration configuration) : IEmailSender
{
    public async Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken)
    {
        var options = CommunicationTechnicalOptions.FromConfiguration(configuration);

        using var message = new MailMessage();
        message.From = new MailAddress(options.FromAddress, options.FromName);
        message.Subject = subject;
        message.Body = body;
        message.IsBodyHtml = false;

        message.To.Add(toEmail);

        using var smtpClient = new SmtpClient(options.SmtpHost, options.SmtpPort);
        smtpClient.EnableSsl = false;

        cancellationToken.ThrowIfCancellationRequested();
        await smtpClient.SendMailAsync(message, cancellationToken);
    }
}
