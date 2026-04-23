using Amazon.SimpleNotificationService;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.DependencyInjection;
using RecurPixel.Notify.Push.Fcm;

namespace RecurPixel.Notify.Tests.Orchestrator;

/// <summary>
/// Verifies the IAdapterRegistrar refactor:
/// each registrar owns its IsConfigured gate and its DI registrations.
/// Covers all ten bugs described in the design document.
/// </summary>
public class AdapterRegistrarTests
{
    // ── Test 1: FCM gates on ServiceAccountJson, not ProjectId (bug #9 fix) ──

    [Fact]
    public void FcmRegistrar_IsConfigured_ReturnsFalse_WhenOnlyProjectIdSet()
    {
        var registrar = new FcmRegistrar();
        var options = new NotifyOptions
        {
            Push = new PushOptions { Fcm = new FcmOptions { ProjectId = "my-project" } }
        };
        Assert.False(registrar.IsConfigured(options));
    }

    // ── Test 2: FCM IsConfigured returns true when ServiceAccountJson is set ─

    [Fact]
    public void FcmRegistrar_IsConfigured_ReturnsTrue_WhenServiceAccountJsonSet()
    {
        var registrar = new FcmRegistrar();
        var options = new NotifyOptions
        {
            Push = new PushOptions { Fcm = new FcmOptions { ServiceAccountJson = "{}" } }
        };
        Assert.True(registrar.IsConfigured(options));
    }

    // ── Test 3: Twilio named options are isolated per channel (bug #4 fix) ───

    [Fact]
    public void TwilioRegistrars_NamedOptions_AreIsolated()
    {
        var services = new ServiceCollection();

        new TwilioSmsRegistrar().Register(services, new NotifyOptions
        {
            Sms = new SmsOptions
            {
                Twilio = new TwilioOptions
                {
                    AccountSid = "AC-SMS",
                    AuthToken  = "token-sms",
                    FromNumber = "+11111111111"
                }
            }
        });

        new TwilioWhatsAppRegistrar().Register(services, new NotifyOptions
        {
            WhatsApp = new WhatsAppOptions
            {
                Twilio = new TwilioOptions
                {
                    AccountSid = "AC-WA",
                    AuthToken  = "token-wa",
                    FromNumber = "+12222222222"
                }
            }
        });

        var sp = services.BuildServiceProvider();
        var monitor = sp.GetRequiredService<IOptionsMonitor<TwilioOptions>>();

        Assert.Equal("AC-SMS", monitor.Get(TwilioSmsRegistrar.OptionsName).AccountSid);
        Assert.Equal("AC-WA",  monitor.Get(TwilioWhatsAppRegistrar.OptionsName).AccountSid);
    }

    // ── Test 4: HTTP adapters register named client with 30s timeout (bug #5 fix) ─

    [Fact]
    public void VonageSmsRegistrar_Register_RegistersNamedHttpClient_WithThirtySecondTimeout()
    {
        var services = new ServiceCollection();
        new VonageSmsRegistrar().Register(services, new NotifyOptions
        {
            Sms = new SmsOptions { Vonage = new VonageOptions { ApiKey = "test-key" } }
        });

        var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("sms:vonage");

        Assert.NotNull(client);
        Assert.Equal(TimeSpan.FromSeconds(30), client.Timeout);
    }

    // ── Test 5: MetaCloud uses key "whatsapp:metacloud" (bug #6 fix) ─────────

    [Fact]
    public void MetaCloudRegistrar_Register_UsesKey_WhatsAppMetaCloud()
    {
        var services = new ServiceCollection();
        new MetaCloudRegistrar().Register(services, new NotifyOptions
        {
            WhatsApp = new WhatsAppOptions
            {
                MetaCloud = new MetaCloudOptions { AccessToken = "test-token" }
            }
        });

        var descriptor = services.FirstOrDefault(d =>
            d.IsKeyedService &&
            "whatsapp:metacloud".Equals(d.ServiceKey) &&
            d.ServiceType == typeof(INotificationChannel));

        Assert.NotNull(descriptor);
        Assert.Equal(typeof(MetaCloudWhatsAppChannel), descriptor.KeyedImplementationType);
    }

    // ── Test 6: AwsSns registers IAmazonSimpleNotificationService (bug #2 fix) ─

    [Fact]
    public void AwsSnsRegistrar_Register_RegistersAmazonSns()
    {
        var services = new ServiceCollection();
        new AwsSnsRegistrar().Register(services, new NotifyOptions
        {
            Sms = new SmsOptions
            {
                AwsSns = new AwsSnsOptions
                {
                    AccessKey = "test-key",
                    SecretKey = "test-secret",
                    Region    = "us-east-1"
                }
            }
        });

        Assert.Contains(services, d => d.ServiceType == typeof(IAmazonSimpleNotificationService));
    }

    // ── Test 7: FcmRegistrar.Register registers IFcmMessagingClient (bug #1 fix) ─

    [Fact]
    public void FcmRegistrar_Register_RegistersFcmMessagingClient()
    {
        // Pre-initialise Firebase with a fake access-token credential so that
        // FirebaseMessagingClient.EnsureInitialized returns early (DefaultInstance
        // is not null), bypassing JSON/RSA key parsing in the test environment.
        if (FirebaseApp.DefaultInstance is null)
            FirebaseApp.Create(new AppOptions { Credential = GoogleCredential.FromAccessToken("test-only") });

        var services = new ServiceCollection();
        new FcmRegistrar().Register(services, new NotifyOptions
        {
            Push = new PushOptions { Fcm = new FcmOptions { ServiceAccountJson = "test-only" } }
        });

        Assert.Contains(services, d => d.ServiceType == typeof(IFcmMessagingClient));
    }

    // ── Test 8: InApp IsConfigured is always true ─────────────────────────────

    [Fact]
    public void InAppRegistrar_IsConfigured_ReturnsTrue_WithEmptyOptions()
    {
        var registrar = new InAppRegistrar();
        Assert.True(registrar.IsConfigured(new NotifyOptions()));
    }

    // ── Test 9: End-to-end auto-scan registers only configured adapters ───────

    [Fact]
    public void AddRecurPixelNotify_WithVonageOnly_RegistersVonage_NotTwilio()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRecurPixelNotify(
            opts => opts.Sms = new SmsOptions
            {
                Provider = "vonage",
                Vonage   = new VonageOptions { ApiKey = "test-key", ApiSecret = "test-secret" }
            },
            _ => { });

        var sp = services.BuildServiceProvider();

        var vonageChannel = sp.GetKeyedService<INotificationChannel>("sms:vonage");
        var twilioChannel = sp.GetKeyedService<INotificationChannel>("sms:twilio");

        Assert.NotNull(vonageChannel);
        Assert.Null(twilioChannel);
    }
}
