namespace VenuePlatform.BLL.Domain.Billing;

/// <summary>
/// Payment entity - records a payment made against an invoice (manual record only).
/// </summary>
public sealed class Payment
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid InvoiceId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime PaidUtc { get; private set; }
    public string Method { get; private set; } = null!;
    public string? Reference { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedUtc { get; private set; }

    // EF Core constructor
    private Payment() { }

    public Payment(
        Guid id,
        Guid companyId,
        Guid invoiceId,
        decimal amount,
        DateTime paidUtc,
        string method,
        string? reference,
        Guid createdByUserId,
        DateTime createdUtc)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty", nameof(id));
        if (companyId == Guid.Empty) throw new ArgumentException("CompanyId cannot be empty", nameof(companyId));
        if (invoiceId == Guid.Empty) throw new ArgumentException("InvoiceId cannot be empty", nameof(invoiceId));
        if (amount <= 0) throw new ArgumentException("Amount must be greater than 0", nameof(amount));
        if (string.IsNullOrWhiteSpace(method)) throw new ArgumentException("Method is required", nameof(method));

        method = method.Trim();
        if (method.Length > 30) throw new ArgumentException("Method must be at most 30 characters", nameof(method));

        if (createdByUserId == Guid.Empty) throw new ArgumentException("CreatedByUserId cannot be empty", nameof(createdByUserId));

        Id = id;
        CompanyId = companyId;
        InvoiceId = invoiceId;
        Amount = amount;
        PaidUtc = paidUtc;
        Method = method;

        if (!string.IsNullOrWhiteSpace(reference))
        {
            reference = reference.Trim();
            if (reference.Length > 100) reference = reference[..100];
            Reference = reference;
        }

        CreatedByUserId = createdByUserId;
        CreatedUtc = createdUtc;
    }
}
