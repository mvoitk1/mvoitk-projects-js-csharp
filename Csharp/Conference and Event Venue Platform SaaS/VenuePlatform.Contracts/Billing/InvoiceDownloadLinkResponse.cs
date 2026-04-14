namespace VenuePlatform.Contracts.Billing;

/// <summary>
/// Response containing a temporary secure download link for an invoice PDF.
/// </summary>
public sealed record InvoiceDownloadLinkResponse(
    string DownloadUrl,
    DateTime ExpiresUtc
);
