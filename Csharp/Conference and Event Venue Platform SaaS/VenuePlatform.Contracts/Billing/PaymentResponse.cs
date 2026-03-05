namespace VenuePlatform.Contracts.Billing;

/// <summary>
/// Response containing payment details.
/// </summary>
public sealed record PaymentResponse(
    Guid Id,
    Guid InvoiceId,
    decimal Amount,
    DateTime PaidUtc,
    string Method,
    string? Reference,
    Guid CreatedByUserId,
    DateTime CreatedUtc
);
