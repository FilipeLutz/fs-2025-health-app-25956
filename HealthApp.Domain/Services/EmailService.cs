using HealthApp.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace HealthApp.Domain.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string email, string subject, string message)
    {
        _logger.LogInformation($"Email to {email}: {subject} - {message}");
        return Task.CompletedTask;
    }
}