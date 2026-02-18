namespace RecurPixel.Notify.Sms.AzureCommSms;

/// <summary>
/// Version-agnostic result record for a single ACS SMS send.
/// Avoids direct construction of SDK result types in tests.
/// </summary>
public sealed record AcsSmsResult(
    string  To,
    string? MessageId,
    bool    Successful,
    int     StatusCode,
    string? ErrorMessage);
