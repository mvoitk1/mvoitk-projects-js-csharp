namespace VenuePlatform.DAL.Persistence;

/// <summary>
/// Per-tenant invoice counter for generating sequential invoice numbers.
/// </summary>
public sealed class InvoiceCounter
{
    public Guid CompanyId { get; private set; }
    public int NextInvoiceNumber { get; set; }

    // EF Core constructor
    private InvoiceCounter() { }

    public InvoiceCounter(Guid companyId, int nextInvoiceNumber = 1)
    {
        if (companyId == Guid.Empty) throw new ArgumentException("CompanyId cannot be empty", nameof(companyId));
        if (nextInvoiceNumber < 1) throw new ArgumentException("NextInvoiceNumber must be >= 1", nameof(nextInvoiceNumber));

        CompanyId = companyId;
        NextInvoiceNumber = nextInvoiceNumber;
    }
}
