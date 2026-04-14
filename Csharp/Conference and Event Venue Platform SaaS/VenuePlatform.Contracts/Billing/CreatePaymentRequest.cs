namespace VenuePlatform.Contracts.Billing;

/// <summary>
/// Request to record a payment against an invoice.
/// </summary>
public sealed record CreatePaymentRequest(
    decimal Amount,
    DateTime PaidUtc,
    string Method,
    string? Reference
);
