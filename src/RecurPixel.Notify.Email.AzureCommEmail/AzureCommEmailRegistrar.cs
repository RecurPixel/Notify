using Azure.Communication.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RecurPixel.Notify.Channels;
using RecurPixel.Notify.Configuration;
using RecurPixel.Notify.Email.AzureCommEmail;

namespace RecurPixel.Notify;

[ChannelAdapter("email", "azurecommemail")]
internal sealed class AzureCommEmailRegistrar : IAdapterRegistrar
{
    public bool IsConfigured(NotifyOptions options)
        => !string.IsNullOrEmpty(options.Email?.AzureCommEmail?.ConnectionString);

    public void Register(IServiceCollection services, NotifyOptions options)
    {
        var opts = options.Email!.AzureCommEmail!;
        services.AddSingleton(Options.Create(opts));

        services.AddSingleton<IAzureCommEmailClient>(_ =>
            new AzureCommEmailClientWrapper(new EmailClient(opts.ConnectionString)));

        services.AddSingleton<AzureCommEmailChannel>();
        services.TryAddKeyedSingleton<INotificationChannel, AzureCommEmailChannel>("email:azurecommemail");
    }
}
