// Payment - Records a payment made against an invoice (manual recording, not integrated with payment gateway)
namespace VenuePlatform.BLL.Domain.Billing;

/// <summary>
/// Payment record - tracks when someone pays an invoice.
/// This is manual recording (e.g., staff marks "cash received" or "bank transfer received").
/// </summary>
public sealed class Payment
{
    public Guid Id { get; private set; }              // Unique ID
    public Guid CompanyId { get; private set; }       // Company owning this
    public Guid InvoiceId { get; private set; }       // Invoice being paid
    public decimal Amount { get; private set; }       // Payment amount
    public DateTime PaidUtc { get; private set; }     // When payment was made
    public string Method { get; private set; } = null!;  // Payment method (Cash, Card, Bank Transfer)
    public string? Reference { get; private set; }     // Payment reference (transaction ID, receipt #)
    public Guid CreatedByUserId { get; private set; } // Who recorded the payment
    public DateTime CreatedUtc { get; private set; }  // When record was created

    // Required for Entity Framework Core
    private Payment() { }

    // Constructor with validation
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
        // Validate inputs
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

        // Trim and truncate reference if provided
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
