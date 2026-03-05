namespace VenuePlatform.Contracts.Billing;

/// <summary>
/// Response DTO for a complete invoice with items.
/// </summary>
public sealed record InvoiceResponse(
    Guid Id,
    Guid BookingId,
    DateTime CreatedUtc,
    Guid CreatedByUserId,
    string Currency,
    InvoiceStatus Status,
    decimal SubtotalAmount,
    int InvoiceNumber,
    string InvoiceNumberText,
    IReadOnlyList<InvoiceItemResponse> Items,
    DateTime? IssuedUtc,
    Guid? IssuedByUserId,
    DateTime? VoidedUtc,
    Guid? VoidedByUserId,
    DateTime? SentUtc,
    Guid? SentByUserId,
    // Phase 8.1: Payment totals
    decimal AmountPaid,
    decimal AmountDue,
    bool IsPaid,
    // Phase 8.3: Paid metadata
    DateTime? PaidUtc,
    Guid? PaidByUserId
);
