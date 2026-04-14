/**
 * =====================================================================================
 * C# CONCEPTS EXPLAINED FOR STUDENTS - Part 2: More Advanced Concepts
 * =====================================================================================
 * 
 * 1. WHAT IS "decimal"?
 *    decimal is a type for precise monetary calculations.
 *    float/double can have rounding errors (e.g., 0.1 + 0.2 = 0.30000000000000004)
 *    decimal is precise: 0.1m + 0.2m = 0.3m
 *    
 *    Always use decimal for money! The 'm' suffix marks a decimal literal.
 * 
 * 2. WHAT IS "DateTime?" (nullable DateTime)?
 *    Just like string?, DateTime? can be null.
 *    - DateTime must have a value
 *    - DateTime? can be null (no date set)
 *    
 *    Example:
 *      public DateTime IssuedUtc { get; private set; }    // Must have a date
 *      public DateTime? IssuedUtc { get; private set; }   // Optional - can be null
 * 
 * 3. WHAT IS "Guid?" (nullable Guid)?
 *    Same concept - a Guid that can be null.
 *    
 *    Example:
 *      public Guid CreatedByUserId { get; }    // Must have a value
 *      public Guid? ConfirmedByUserId { get; } // Optional - can be null
 * 
 * 4. WHAT IS "= null!" (null-forgiving operator)?
 *    This tells the compiler "this will never actually be null when accessed."
 *    It's used for properties that EF Core will populate from the database.
 *    
 *    Without it, you'd get warnings everywhere saying "possible null reference."
 * 
 * 5. WHAT IS "= "EUR"" (property initializer)?
 *    Sets a default value for the property.
 *    This runs BEFORE the constructor.
 *    
 *    Example:
 *      public string Currency { get; private set; } = "EUR";
 *      // Every Invoice starts with EUR as currency
 * 
 * 6. WHAT IS "throw new InvalidOperationException"?
 *    InvalidOperationException is thrown when an operation is not valid
 *    in the current state of the object.
 *    
 *    Example: "Can't issue an invoice that's already voided"
 * 
 * 7. WHAT IS "return" in a void method?
 *    Even in void methods (that don't return anything), return can be used
 *    to exit early from the method.
 *    
 *    Example:
 *      public void Issue() {
 *          if (alreadyIssued) return; // Exit early, don't do anything
 *          // ... rest of code
 *      }
 * 
 * 8. WHAT IS "string.IsNullOrWhiteSpace"?
 *    Checks if a string is null, empty, or contains only whitespace.
 *    
 *    Returns true if:
 *      - null
 *      - "" (empty)
 *      - "   " (whitespace only)
 *    
 *    Safer than just checking for null.
 * 
 * 9. WHAT IS "ToUpperInvariant"?
 *    Converts string to uppercase using invariant (culture-independent) rules.
 *    Always use this for things like currency codes, country codes, etc.
 *    
 *    "EUR".ToUpperInvariant() = "EUR"
 *    "eur".ToUpperInvariant() = "EUR"
 * 
 * 10. WHAT IS" ( $" "string interpolationINV-{number:D6}" )?
 *     $"" is a string literal that lets you embed variables directly.
 *     
 *     "INV-" + number.ToString("D6")  // Old way
 *     $"INV-{number:D6}"              // New way (interpolation)
 *     
 *     :D6 means "format as 6-digit decimal with leading zeros"
 *     So 5 becomes "INV-000005"
 * 
 * 11. WHAT IS "idempotent"?
 *     An operation is idempotent if calling it multiple times has the same
 *     effect as calling it once.
 *     
 *     Example:
 *       // This is idempotent:
 *       if (Status == InvoiceStatus.Issued) return; // Already issued, do nothing
 *       
 *       // If we didn't check, calling Issue() twice would change status twice
 *       // which might cause problems
 * 
 * 12. WHAT IS "sealed class"?
 *     A sealed class cannot be inherited. This is a performance optimization
 *     and also a design choice to prevent others from extending your class.
 * 
 * =====================================================================================
 */

using VenuePlatform.Contracts.Billing;

namespace VenuePlatform.BLL.Domain.Billing;

/**
 * Invoice - A bill for a booking
 * 
 * Invoices represent charges to clients. They have a lifecycle:
 * 1. Draft - Just created, not sent to client
 * 2. Issued - Sent to client, expects payment
 * 3. Sent - Actually emailed/sent to client
 * 4. Paid - Payment received
 * 5. Void - Cancelled/invalid (can never be paid)
 * 
 * Key concepts:
 * - Invoice is a "snapshot" - it captures the total at creation time
 * - Even if booking prices change later, the invoice amount stays the same
 * - InvoiceNumber is human-readable (INV-000001, INV-000002, etc.)
 */
public sealed class Invoice
{
    // ==================== Core Properties ====================
    
    /// <summary>
    /// Unique identifier for this invoice
    /// </summary>
    public Guid Id { get; private set; }
    
    /// <summary>
    /// Which company this invoice belongs to
    /// </summary>
    public Guid CompanyId { get; private set; }
    
    /// <summary>
    /// Which booking this invoice is for
    /// </summary>
    public Guid BookingId { get; private set; }
    
    /// <summary>
    /// When this invoice was created (UTC)
    /// </summary>
    public DateTime CreatedUtc { get; private set; }
    
    /// <summary>
    /// Who created this invoice (user ID)
    /// </summary>
    public Guid CreatedByUserId { get; private set; }
    
    /// <summary>
    /// Subtotal before tax/fees
    /// Using decimal for precise money calculations!
    /// </summary>
    public decimal SubtotalAmount { get; private set; }
    
    /// <summary>
    /// Currency code (default: EUR)
    /// </summary>
    public string Currency { get; private set; } = "EUR";
    
    /// <summary>
    /// Current status of the invoice
    /// </summary>
    public InvoiceStatus Status { get; private set; }
    
    /// <summary>
    /// Sequential invoice number (1, 2, 3...)
    /// </summary>
    public int InvoiceNumber { get; private set; }
    
    /// <summary>
    /// Human-readable invoice number (INV-000001)
    /// </summary>
    public string InvoiceNumberText { get; private set; } = null!;

    // ==================== Issue/Void Metadata ====================
    // These track when and who issued/voided the invoice
    
    public DateTime? IssuedUtc { get; private set; }  // When it was issued (nullable)
    public Guid? IssuedByUserId { get; private set; }  // Who issued it
    public DateTime? VoidedUtc { get; private set; }    // When it was voided
    public Guid? VoidedByUserId { get; private set; }  // Who voided it

    // ==================== Sent Metadata ====================
    // Tracks email/PDF sending
    
    public DateTime? SentUtc { get; private set; }      // When it was sent
    public Guid? SentByUserId { get; private set; }     // Who sent it

    // ==================== Paid Metadata ====================
    // Tracks payment
    
    public DateTime? PaidUtc { get; private set; }      // When payment was received
    public Guid? PaidByUserId { get; private set; }    // Who recorded the payment

    /**
     * Empty constructor for Entity Framework Core
     * EF needs this to create objects when loading from database
     */
    private Invoice() { }

    /**
     * Constructor - Creates a new Invoice in Draft status
     * 
     * @param id - Unique identifier (usually passed from outside)
     * @param companyId - Company owning this invoice
     * @param bookingId - Booking this invoice is for
     * @param createdUtc - When created
     * @param createdByUserId - Who created it
     * @param subtotalAmount - The amount to charge
     * @param currency - Currency code (defaults to EUR)
     */
    public Invoice(
        Guid id,
        Guid companyId,
        Guid bookingId,
        DateTime createdUtc,
        Guid createdByUserId,
        decimal subtotalAmount,
        string currency = "EUR")
    {
        // Validate all parameters
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty", nameof(id));
        if (companyId == Guid.Empty) throw new ArgumentException("CompanyId cannot be empty", nameof(companyId));
        if (bookingId == Guid.Empty) throw new ArgumentException("BookingId cannot be empty", nameof(bookingId));
        if (createdByUserId == Guid.Empty) throw new ArgumentException("CreatedByUserId cannot be empty", nameof(createdByUserId));
        if (subtotalAmount < 0) throw new ArgumentException("SubtotalAmount cannot be negative", nameof(subtotalAmount));
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency cannot be empty", nameof(currency));
        if (currency.Length != 3) throw new ArgumentException("Currency must be 3 letters", nameof(currency));

        // Initialize properties
        Id = id;
        CompanyId = companyId;
        BookingId = bookingId;
        CreatedUtc = createdUtc;
        CreatedByUserId = createdByUserId;
        SubtotalAmount = subtotalAmount;
        Currency = currency.ToUpperInvariant(); // Convert to uppercase (e.g., "eur" -> "EUR")
        Status = InvoiceStatus.Draft; // New invoices start as Draft
    }

    /**
     * SetSubtotalAmount - Updates the invoice amount
     * 
     * Only allowed if amount is not negative
     */
    public void SetSubtotalAmount(decimal amount)
    {
        if (amount < 0) throw new ArgumentException("SubtotalAmount cannot be negative", nameof(amount));
        SubtotalAmount = amount;
    }

    /**
     * SetNumber - Sets the invoice number
     * 
     * Also generates the human-readable version (INV-000001)
     */
    public void SetNumber(int number)
    {
        if (number < 1) throw new ArgumentException("InvoiceNumber must be >= 1", nameof(number));
        InvoiceNumber = number;
        InvoiceNumberText = $"INV-{number:D6}"; // Format as 6-digit with leading zeros
    }

    /**
     * Issue - Changes status from Draft to Issued
     * 
     * Idempotent - calling twice does nothing the second time
     * 
     * @param userId - Who is issuing the invoice
     * @param issuedUtc - When it was issued
     */
    public void Issue(Guid userId, DateTime issuedUtc)
    {
        // Can't issue a voided invoice
        if (Status == InvoiceStatus.Void) throw new InvalidOperationException("Void invoice cannot be issued.");
        
        // Already issued? Do nothing (idempotent)
        if (Status == InvoiceStatus.Issued) return;

        // Update status and track who/when
        Status = InvoiceStatus.Issued;
        IssuedUtc = issuedUtc;
        IssuedByUserId = userId;
    }

    /**
     * Void - Cancels the invoice
     * 
     * Once voided, it cannot be issued or marked as paid.
     * Idempotent - calling twice does nothing the second time.
     */
    public void Void(Guid userId, DateTime voidedUtc)
    {
        // Already voided? Do nothing (idempotent)
        if (Status == InvoiceStatus.Void) return;

        Status = InvoiceStatus.Void;
        VoidedUtc = voidedUtc;
        VoidedByUserId = userId;
    }

    /**
     * MarkSent - Records when invoice was sent to client
     */
    public void MarkSent(Guid userId, DateTime sentUtc)
    {
        // Can't mark void invoice as sent
        if (Status == InvoiceStatus.Void) throw new InvalidOperationException("Void invoice cannot be marked as sent.");
        
        // Already sent? Do nothing (idempotent)
        if (SentUtc != null) return;

        SentUtc = sentUtc;
        SentByUserId = userId;
    }

    /**
     * MarkPaid - Records payment received
     */
    public void MarkPaid(Guid userId, DateTime paidUtc)
    {
        // Can't mark void invoice as paid
        if (Status == InvoiceStatus.Void) throw new InvalidOperationException("Void invoice cannot be marked as paid.");
        
        // Already paid? Do nothing (idempotent)
        if (PaidUtc != null) return;

        PaidUtc = paidUtc;
        PaidByUserId = userId;
    }
}
