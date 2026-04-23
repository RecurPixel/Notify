using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Configuration;

namespace RecurPixel.Notify;

/// <summary>
/// DI registration extensions for the SendGrid email adapter.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the SendGrid email channel adapter. Delegates to <see cref="SendGridRegistrar"/>.
    /// </summary>
    public static IServiceCollection AddSendGridChannel(
        this IServiceCollection services,
        SendGridOptions options)
    {
        new SendGridRegistrar().Register(services,
            new NotifyOptions { Email = new EmailOptions { SendGrid = options } });
        return services;
    }
}
