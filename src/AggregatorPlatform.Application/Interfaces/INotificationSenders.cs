namespace AggregatorPlatform.Application.Interfaces;

/// <summary>Envoi d'email (implementation MOCK par defaut : log uniquement).</summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}

/// <summary>Envoi de SMS (implementation MOCK par defaut : log uniquement).</summary>
public interface ISmsSender
{
    Task SendAsync(string toPhoneNumber, string body, CancellationToken cancellationToken = default);
}
