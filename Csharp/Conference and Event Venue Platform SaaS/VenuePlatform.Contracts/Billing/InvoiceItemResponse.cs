namespace VenuePlatform.Contracts.Billing;

/// <summary>
/// Response DTO for an invoice line item.
/// </summary>
public sealed record InvoiceItemResponse(
    Guid Id,
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal
);