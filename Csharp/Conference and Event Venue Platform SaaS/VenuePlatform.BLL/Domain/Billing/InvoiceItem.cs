// InvoiceItem - A single line item on an invoice (e.g., "Room rental: 2 hours x €50")
namespace VenuePlatform.BLL.Domain.Billing;

/// <summary>
/// Invoice line item - represents a charge for something on an invoice.
/// Created as a snapshot when invoice is generated - doesn't change even if rates change.
/// </summary>
public sealed class InvoiceItem
{
    public Guid Id { get; private set; }           // Unique ID
    public Guid CompanyId { get; private set; }    // Company owning this
    public Guid InvoiceId { get; private set; }    // Parent invoice
    public string Description { get; private set; } = string.Empty;  // What was charged
    public int Quantity { get; private set; }     // Units (hours, days, etc.)
    public decimal UnitPrice { get; private set; } // Price per unit
    public decimal LineTotal { get; private set; } // Quantity * UnitPrice

    // Required for Entity Framework Core to load from database
    private InvoiceItem() { }

    // Constructor with validation
    public InvoiceItem(
        Guid id,
        Guid companyId,
        Guid invoiceId,
        string description,
        int quantity,
        decimal unitPrice,
        decimal lineTotal)
    {
        // Validate all inputs
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty", nameof(id));
        if (companyId == Guid.Empty) throw new ArgumentException("CompanyId cannot be empty", nameof(companyId));
        if (invoiceId == Guid.Empty) throw new ArgumentException("InvoiceId cannot be empty", nameof(invoiceId));
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Description cannot be empty", nameof(description));
        if (description.Length > 200) throw new ArgumentException("Description cannot exceed 200 characters", nameof(description));
        if (quantity < 1) throw new ArgumentException("Quantity must be at least 1", nameof(quantity));
        if (unitPrice < 0) throw new ArgumentException("UnitPrice cannot be negative", nameof(unitPrice));
        if (lineTotal < 0) throw new ArgumentException("LineTotal cannot be negative", nameof(lineTotal));

        Id = id;
        CompanyId = companyId;
        InvoiceId = invoiceId;
        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
        LineTotal = lineTotal;
    }

    /// <summary>
    /// Factory method - creates invoice item with LineTotal auto-calculated.
    /// </summary>
    public static InvoiceItem Create(
        Guid id,
        Guid companyId,
        Guid invoiceId,
        string description,
        int quantity,
        decimal unitPrice)
    {
        var lineTotal = quantity * unitPrice;  // Auto-calculate
        return new InvoiceItem(id, companyId, invoiceId, description, quantity, unitPrice, lineTotal);
    }
}
