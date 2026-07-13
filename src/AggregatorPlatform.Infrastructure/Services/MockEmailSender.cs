using AggregatorPlatform.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AggregatorPlatform.Infrastructure.Services;

/// <summary>
/// Implementation MOCK : n'envoie rien reellement, log l'email en mode INFO.
/// Remplacer par un vrai transport (SendGrid, MailKit, SES...) le moment venu.
/// </summary>
public class MockEmailSender : IEmailSender
{
    private readonly ILogger<MockEmailSender> _logger;

    public MockEmailSender(ILogger<MockEmailSender> logger) => _logger = logger;

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[MOCK Email] to={To} subject={Subject} body={Body}", to, subject, body);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Implementation MOCK : n'envoie rien reellement, log le SMS en mode INFO.
/// </summary>
public class MockSmsSender : ISmsSender
{
    private readonly ILogger<MockSmsSender> _logger;

    public MockSmsSender(ILogger<MockSmsSender> logger) => _logger = logger;

    public Task SendAsync(string toPhoneNumber, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[MOCK SMS] to={To} body={Body}", toPhoneNumber, body);
        return Task.CompletedTask;
    }
}
