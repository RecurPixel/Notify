using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RecurPixel.Notify.Sms.AzureCommSms;

/// <summary>
/// Abstraction over <see cref="Azure.Communication.Sms.SmsClient"/>.
/// Enables unit testing without constructing SDK result types directly.
/// </summary>
public interface IAzureCommSmsClient
{
    /// <summary>Sends an SMS to a single recipient.</summary>
    Task<AcsSmsResult> SendAsync(
        string from,
        string to,
        string message,
        CancellationToken ct);

    /// <summary>Sends an SMS to multiple recipients in one API call.</summary>
    Task<IReadOnlyList<AcsSmsResult>> SendBulkAsync(
        string from,
        IReadOnlyList<string> to,
        string message,
        CancellationToken ct);
}
