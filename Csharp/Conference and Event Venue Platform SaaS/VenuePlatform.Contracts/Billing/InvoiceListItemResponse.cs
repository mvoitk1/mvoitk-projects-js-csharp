namespace VenuePlatform.Contracts.Billing;

/// <summary>
/// Response DTO for listing invoices (lightweight, no items).
/// </summary>
public sealed record InvoiceListItemResponse(
    Guid Id,
    Guid BookingId,
    DateTime CreatedUtc,
    InvoiceStatus Status,
    string Currency,
    decimal SubtotalAmount,
    int InvoiceNumber,
    string InvoiceNumberText,
    DateTime? IssuedUtc,
    DateTime? VoidedUtc,
    DateTime? SentUtc,
    // Phase 8.1: Payment totals
    decimal AmountPaid,
    decimal AmountDue,
    bool IsPaid,
    // Phase 8.3: Paid metadata (lightweight)
    DateTime? PaidUtc
);
