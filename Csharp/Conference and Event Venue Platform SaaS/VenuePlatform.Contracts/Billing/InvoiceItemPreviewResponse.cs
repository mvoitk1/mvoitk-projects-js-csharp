namespace VenuePlatform.Contracts.Billing;

/// <summary>
/// Response DTO for a preview of an invoice line item.
/// </summary>
public sealed record InvoiceItemPreviewResponse(
    string Description,
    decimal LineTotal
);
