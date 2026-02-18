using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace RecurPixel.Notify.Push.Fcm;

/// <summary>
/// Result of a single FCM send within a multicast batch.
/// Returned by <see cref="IFcmMessagingClient.SendMulticastAsync"/> in the same order
/// as the input tokens.
/// </summary>
internal sealed class FcmSendResponse
{
    /// <summary>True if FCM accepted the message for this token.</summary>
    public bool IsSuccess { get; init; }

    /// <summary>FCM message ID on success; null on failure.</summary>
    public string? MessageId { get; init; }

    /// <summary>Error message on failure; null on success.</summary>
    public string? Error { get; init; }
}

/// <summary>
/// Abstracts Firebase Cloud Messaging calls for testability.
/// The real implementation calls the Firebase Admin SDK.
/// Tests inject a mock — no Firebase credentials needed in tests.
/// </summary>
internal interface IFcmMessagingClient
{
    /// <summary>Sends a single push notification to one device token.</summary>
    Task<string> SendAsync(
        string token,
        string? title,
        string body,
        CancellationToken ct);

    /// <summary>
    /// Sends a push notification to multiple device tokens in one API call (multicast).
    /// Returns one <see cref="FcmSendResponse"/> per token, in the same order as
    /// <paramref name="tokens"/>.
    /// </summary>
    Task<IReadOnlyList<FcmSendResponse>> SendMulticastAsync(
        IReadOnlyList<string> tokens,
        string? title,
        string body,
        CancellationToken ct);
}

/// <summary>
/// Real <see cref="IFcmMessagingClient"/> implementation backed by the Firebase Admin SDK.
/// Firebase app initialisation is idempotent — safe to call from multiple registrations.
/// </summary>
internal sealed class FirebaseMessagingClient : IFcmMessagingClient
{
    private static readonly object _initLock = new();

    /// <summary>
    /// Initialises the default Firebase app if it has not already been initialised.
    /// Thread-safe. Accepts a full JSON string or an absolute file path.
    /// </summary>
    internal static void EnsureInitialized(string serviceAccountJson)
    {
        if (FirebaseApp.DefaultInstance is not null) return;

        lock (_initLock)
        {
            if (FirebaseApp.DefaultInstance is not null) return;

            var credential = File.Exists(serviceAccountJson)
                ? GoogleCredential.FromFile(serviceAccountJson)
                : GoogleCredential.FromJson(serviceAccountJson);

            FirebaseApp.Create(new AppOptions { Credential = credential });
        }
    }

    /// <inheritdoc />
    public async Task<string> SendAsync(
        string token,
        string? title,
        string body,
        CancellationToken ct)
    {
        var message = new Message
        {
            Token = token,
            Notification = new Notification { Title = title, Body = body }
        };

        return await FirebaseMessaging.DefaultInstance.SendAsync(message, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FcmSendResponse>> SendMulticastAsync(
        IReadOnlyList<string> tokens,
        string? title,
        string body,
        CancellationToken ct)
    {
        var message = new MulticastMessage
        {
            Tokens = tokens.ToList(),
            Notification = new Notification { Title = title, Body = body }
        };

        var batch = await FirebaseMessaging.DefaultInstance
            .SendEachForMulticastAsync(message, ct);

        // Firebase guarantees one response per token in the same order as input
        return batch.Responses
            .Select(r => new FcmSendResponse
            {
                IsSuccess = r.IsSuccess,
                MessageId = r.MessageId,
                Error = r.Exception?.Message
            })
            .ToList();
    }
}
