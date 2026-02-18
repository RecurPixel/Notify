using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Communication.Email;

namespace RecurPixel.Notify.Email.AzureCommEmail;

/// <summary>
/// Production wrapper around <see cref="EmailClient"/>.
/// </summary>
internal sealed class AzureCommEmailClientWrapper : IAzureCommEmailClient
{
    private readonly EmailClient _client;

    public AzureCommEmailClientWrapper(EmailClient client)
    {
        _client = client;
    }

    public async Task<string?> SendAsync(
        string senderAddress,
        string recipientAddress,
        string subject,
        string? html,
        string? plainText,
        CancellationToken ct)
    {
        var message = new EmailMessage(
            senderAddress: senderAddress,
            content: new EmailContent(subject)
            {
                Html = html,
                PlainText = plainText
            },
            recipients: new EmailRecipients(
                new List<EmailAddress> { new(recipientAddress) }));

        var operation = await _client.SendAsync(WaitUntil.Completed, message, ct);
        return operation.Id;
    }
}
