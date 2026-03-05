namespace VenuePlatform.Contracts.Billing;

/// <summary>
/// Response DTO for a preview of what an invoice would look like if created from a booking.
/// </summary>
public sealed record InvoicePreviewResponse(
    Guid BookingId,
    decimal StoredBookingTotalAmount,
    decimal RecomputedSubtotalAmount,
    decimal? BookingWideOverrideHourlyRate,
    IReadOnlyList<InvoiceItemPreviewResponse> Items
);
