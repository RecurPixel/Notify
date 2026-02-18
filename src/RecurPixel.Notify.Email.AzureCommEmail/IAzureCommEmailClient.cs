using System.Threading;
using System.Threading.Tasks;

namespace RecurPixel.Notify.Email.AzureCommEmail;

/// <summary>
/// Abstraction over <see cref="Azure.Communication.Email.EmailClient"/>.
/// Enables unit testing without constructing SDK result types directly.
/// </summary>
public interface IAzureCommEmailClient
{
    /// <summary>
    /// Sends an email and returns the provider message ID, or null on failure.
    /// Throws on unrecoverable errors — caller catches and maps to NotifyResult.
    /// </summary>
    Task<string?> SendAsync(
        string senderAddress,
        string recipientAddress,
        string subject,
        string? html,
        string? plainText,
        CancellationToken ct);
}
