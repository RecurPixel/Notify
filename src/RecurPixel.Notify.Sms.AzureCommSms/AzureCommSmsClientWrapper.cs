using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Communication.Sms;

namespace RecurPixel.Notify.Sms.AzureCommSms;

/// <summary>
/// Production wrapper around <see cref="SmsClient"/>.
/// </summary>
internal sealed class AzureCommSmsClientWrapper : IAzureCommSmsClient
{
    private readonly SmsClient _client;

    public AzureCommSmsClientWrapper(SmsClient client)
    {
        _client = client;
    }

    public async Task<AcsSmsResult> SendAsync(
        string from,
        string to,
        string message,
        CancellationToken ct)
    {
        var response = await _client.SendAsync(from, to, message, cancellationToken: ct);
        var item     = response.Value;
        return new AcsSmsResult(
            To:           item.To,
            MessageId:    item.MessageId,
            Successful:   item.Successful,
            StatusCode:   item.HttpStatusCode,
            ErrorMessage: item.ErrorMessage);
    }

    public async Task<IReadOnlyList<AcsSmsResult>> SendBulkAsync(
        string from,
        IReadOnlyList<string> to,
        string message,
        CancellationToken ct)
    {
        var response = await _client.SendAsync(from, to, message, cancellationToken: ct);
        return response.Value
            .Select(item => new AcsSmsResult(
                To:           item.To,
                MessageId:    item.MessageId,
                Successful:   item.Successful,
                StatusCode:   item.HttpStatusCode,
                ErrorMessage: item.ErrorMessage))
            .ToList();
    }
}
