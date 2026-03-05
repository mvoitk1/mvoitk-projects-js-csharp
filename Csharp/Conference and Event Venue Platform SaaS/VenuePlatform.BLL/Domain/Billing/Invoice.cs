using VenuePlatform.Contracts.Billing;

namespace VenuePlatform.BLL.Domain.Billing;

/// <summary>
/// Invoice entity - a snapshot of charges for a booking at creation time.
/// </summary>
public sealed class Invoice
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid BookingId { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public decimal SubtotalAmount { get; private set; }
    public string Currency { get; private set; } = "EUR";
    public InvoiceStatus Status { get; private set; }
    public int InvoiceNumber { get; private set; }
    public string InvoiceNumberText { get; private set; } = null!;

    // Issue/Void metadata
    public DateTime? IssuedUtc { get; private set; }
    public Guid? IssuedByUserId { get; private set; }
    public DateTime? VoidedUtc { get; private set; }
    public Guid? VoidedByUserId { get; private set; }

    // Sent metadata (email/PDF sending tracking)
    public DateTime? SentUtc { get; private set; }
    public Guid? SentByUserId { get; private set; }

    // Paid metadata (Phase 8.3)
    public DateTime? PaidUtc { get; private set; }
    public Guid? PaidByUserId { get; private set; }

    // EF Core constructor
    private Invoice() { }

    public Invoice(
        Guid id,
        Guid companyId,
        Guid bookingId,
        DateTime createdUtc,
        Guid createdByUserId,
        decimal subtotalAmount,
        string currency = "EUR")
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty", nameof(id));
        if (companyId == Guid.Empty) throw new ArgumentException("CompanyId cannot be empty", nameof(companyId));
        if (bookingId == Guid.Empty) throw new ArgumentException("BookingId cannot be empty", nameof(bookingId));
        if (createdByUserId == Guid.Empty) throw new ArgumentException("CreatedByUserId cannot be empty", nameof(createdByUserId));
        if (subtotalAmount < 0) throw new ArgumentException("SubtotalAmount cannot be negative", nameof(subtotalAmount));
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency cannot be empty", nameof(currency));
        if (currency.Length != 3) throw new ArgumentException("Currency must be 3 letters", nameof(currency));

        Id = id;
        CompanyId = companyId;
        BookingId = bookingId;
        CreatedUtc = createdUtc;
        CreatedByUserId = createdByUserId;
        SubtotalAmount = subtotalAmount;
        Currency = currency.ToUpperInvariant();
        Status = InvoiceStatus.Draft;
    }

    public void SetSubtotalAmount(decimal amount)
    {
        if (amount < 0) throw new ArgumentException("SubtotalAmount cannot be negative", nameof(amount));
        SubtotalAmount = amount;
    }

    public void SetNumber(int number)
    {
        if (number < 1) throw new ArgumentException("InvoiceNumber must be >= 1", nameof(number));
        InvoiceNumber = number;
        InvoiceNumberText = $"INV-{number:D6}";
    }

    public void Issue(Guid userId, DateTime issuedUtc)
    {
        if (Status == InvoiceStatus.Void) throw new InvalidOperationException("Void invoice cannot be issued.");
        if (Status == InvoiceStatus.Issued) return; // idempotent

        Status = InvoiceStatus.Issued;
        IssuedUtc = issuedUtc;
        IssuedByUserId = userId;
    }

    public void Void(Guid userId, DateTime voidedUtc)
    {
        if (Status == InvoiceStatus.Void) return; // idempotent

        Status = InvoiceStatus.Void;
        VoidedUtc = voidedUtc;
        VoidedByUserId = userId;
    }

    public void MarkSent(Guid userId, DateTime sentUtc)
    {
        if (Status == InvoiceStatus.Void) throw new InvalidOperationException("Void invoice cannot be marked as sent.");
        if (SentUtc != null) return; // idempotent

        SentUtc = sentUtc;
        SentByUserId = userId;
    }

    public void MarkPaid(Guid userId, DateTime paidUtc)
    {
        if (Status == InvoiceStatus.Void) throw new InvalidOperationException("Void invoice cannot be marked as paid.");
        if (PaidUtc != null) return; // idempotent

        PaidUtc = paidUtc;
        PaidByUserId = userId;
    }
}
