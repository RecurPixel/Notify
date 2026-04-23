using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the SMTP email adapter.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the SMTP email channel adapter. Delegates to <see cref="SmtpRegistrar"/>.
    /// </summary>
    public static IServiceCollection AddSmtpChannel(
        this IServiceCollection services,
        SmtpOptions options)
    {
        new SmtpRegistrar().Register(services,
            new NotifyOptions { Email = new EmailOptions { Smtp = options } });
        return services;
    }
}
