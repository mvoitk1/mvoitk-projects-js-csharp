/**
 * =====================================================================================
 * C# CONCEPTS EXPLAINED FOR STUDENTS - Part 3: Booking Entity
 * =====================================================================================
 * 
 * 1. WHAT IS "DateTime"?
 *    DateTime represents a point in time. It has both date and time components.
 *    
 *    Example:
 *      DateTime now = DateTime.UtcNow;  // Current time
 *      DateTime birthday = new DateTime(1990, 6, 15);  // June 15, 1990
 * 
 * 2. WHAT IS "DateTime.UtcNow"?
 *    Returns current date and time in UTC (Coordinated Universal Time).
 *    UTC is the primary time standard - it doesn't have timezones.
 *    
 *    WHY USE UTC?
 *    - If you store "local time" (e.g., 3:00 PM in New York), and someone
 *      in London looks at it, they'd see wrong time (8:00 PM!)
 *    - With UTC, everyone sees the same time regardless of timezone
 *    - You convert to local time only when displaying to user
 * 
 * 3. WHAT IS "bool"?
 *    Boolean - can only be true or false.
 *    
 *    Example:
 *      public bool IsCancelled { get; private set; }
 *      // IsCancelled can only be true or false
 * 
 * 4. WHAT IS "string[..500]" (range operator)?
 *    This takes only the first 500 characters of a string.
 *    
 *    In C# 8.0+, [..500] means "from start to index 500"
 *    It's the same as .Substring(0, 500) but shorter!
 *    
 *    Example:
 *      string longText = "This is a very long text...";
 *      string shortText = longText[..500]; // First 500 chars
 * 
 * 5. WHAT IS "ternary operator (?: )"?
 *    A shorthand if-else in one line.
 *    
 *    Traditional:
 *      if (amount < 0)
 *          TotalAmount = 0;
 *      else
 *          TotalAmount = amount;
 *    
 *    Ternary:
 *      TotalAmount = amount < 0 ? 0 : amount;
 *    
 *    Syntax: condition ? valueIfTrue : valueIfFalse
 * 
 * 6. WHAT IS "is null" pattern matching?
 *    Modern C# way to check for null.
 *    
 *    Old way:
 *      if (CreatedByUserId == null)
 *    
 *    New way (C# 9+):
 *      if (CreatedByUserId is null)
 *    
 *    This is part of "pattern matching" - more powerful than simple comparison.
 * 
 * 7. WHAT IS "throw ArgumentOutOfRangeException"?
 *    Specifically thrown when a numeric value is outside allowed range.
 *    
 *    Example:
 *      if (attendeeCount < 0)
 *          throw new ArgumentOutOfRangeException(nameof(attendeeCount), "Must be >= 0");
 * 
 * 8. WHAT IS "BookingStatus"?
 *    This is an "enum" (enumeration) - a set of named values.
 *    
 *    Instead of using strings like "Pending" or "Confirmed" everywhere,
 *    we use an enum which prevents typos and gives IntelliSense.
 *    
 *    Defined elsewhere like:
 *      public enum BookingStatus
 *      {
 *          Pending,
 *          Confirmed
 *      }
 * 
 * 9. WHAT IS "?" TRIM AND LENGTH CHECK PATTERN?
 *    This is common validation:
 *    
 *    if (!string.IsNullOrWhiteSpace(reason))
 *    {
 *        reason = reason.Trim();
 *        if (reason.Length > 500) reason = reason[..500];
 *        CancelReason = reason;
 *    }
 *    
 *    Steps:
 *    1. Check if not null/whitespace
 *    2. Trim whitespace from ends
 *    3. If too long, truncate to 500 chars
 *    4. Save the result
 * 
 * =====================================================================================
 */

using VenuePlatform.Contracts.Bookings;
using VenuePlatform.Contracts.Tenancy;

namespace VenuePlatform.BLL.Domain.Bookings;

/**
 * Booking - A reservation of a space
 * 
 * Bookings represent a customer's reservation of one or more spaces.
 * 
 * Key properties:
 * - Title: Name/title of the booking
 * - StartUtc/EndUtc: When the booking occurs
 * - AttendeeCount: How many people
 * - Status: Pending or Confirmed
 * - IsCancelled: Has the booking been cancelled?
 * 
 * Lifecycle:
 * 1. Created with Pending status
 * 2. Can be Confirmed (by venue staff)
 * 3. Can be Cancelled at any time
 */
public sealed class Booking : ITenantScoped
{
    // ==================== Core Properties ====================
    
    /// <summary>
    /// Unique identifier for this booking
    /// </summary>
    public Guid Id { get; private set; }
    
    /// <summary>
    /// Company this booking belongs to
    /// </summary>
    public Guid CompanyId { get; private set; }
    
    /// <summary>
    /// Client who made this booking
    /// </summary>
    public Guid ClientId { get; private set; }
    
    /// <summary>
    /// Title/name of the booking
    /// </summary>
    public string Title { get; private set; } = null!;
    
    /// <summary>
    /// When the booking starts (UTC)
    /// </summary>
    public DateTime StartUtc { get; private set; }
    
    /// <summary>
    /// When the booking ends (UTC)
    /// </summary>
    public DateTime EndUtc { get; private set; }
    
    /// <summary>
    /// Number of attendees expected
    /// </summary>
    public int AttendeeCount { get; private set; }
    
    /// <summary>
    /// When the booking was created (UTC)
    /// </summary>
    public DateTime CreatedUtc { get; private set; }
    
    /// <summary>
    /// Has this booking been cancelled?
    /// </summary>
    public bool IsCancelled { get; private set; }
    
    /// <summary>
    /// When the booking was cancelled (nullable - only set if cancelled)
    /// </summary>
    public DateTime? CancelledUtc { get; private set; }
    
    /// <summary>
    /// Reason for cancellation (optional)
    /// </summary>
    public string? CancelReason { get; private set; }
    
    /// <summary>
    /// Total amount to charge
    /// </summary>
    public decimal TotalAmount { get; private set; }
    
    /// <summary>
    /// If using space configuration, which one (optional)
    /// </summary>
    public Guid? SpaceConfigurationId { get; private set; }
    
    /// <summary>
    /// Current status: Pending or Confirmed
    /// </summary>
    public BookingStatus Status { get; private set; }
    
    /// <summary>
    /// Who created this booking (nullable - might be client-facing)
    /// </summary>
    public Guid? CreatedByUserId { get; private set; }
    
    /// <summary>
    /// Who confirmed this booking (nullable - only if confirmed)
    /// </summary>
    public Guid? ConfirmedByUserId { get; private set; }
    
    /// <summary>
    /// Who cancelled this booking (nullable - only if cancelled)
    /// </summary>
    public Guid? CancelledByUserId { get; private set; }

    /**
     * Empty constructor for Entity Framework Core
     * Required for loading from database
     */
    private Booking() { } // EF

    /**
     * Constructor - Creates a new Booking in Pending status
     * 
     * @param companyId - Company owning this booking
     * @param clientId - Client who booked
     * @param title - Booking title
     * @param startUtc - Start time (UTC)
     * @param endUtc - End time (UTC)
     * @param attendeeCount - Expected number of attendees
     */
    public Booking(Guid companyId, Guid clientId, string title, DateTime startUtc, DateTime endUtc, int attendeeCount)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));
        if (title.Length > 200)
            throw new ArgumentException("Title cannot exceed 200 characters.", nameof(title));
        if (startUtc >= endUtc)
            throw new ArgumentException("Start time must be before end time.", nameof(startUtc));
        if (attendeeCount < 0)
            throw new ArgumentException("Attendee count cannot be negative.", nameof(attendeeCount));

        // Initialize
        Id = Guid.NewGuid();
        CompanyId = companyId;
        ClientId = clientId;
        Title = title.Trim();
        StartUtc = startUtc;
        EndUtc = endUtc;
        AttendeeCount = attendeeCount;
        CreatedUtc = DateTime.UtcNow;
        IsCancelled = false;
        TotalAmount = 0;
        Status = BookingStatus.Pending;
    }

    /**
     * Cancel - Cancels this booking
     * 
     * Idempotent - calling multiple times keeps the original cancel info
     * 
     * @param reason - Optional reason for cancellation
     * @param cancelledUtc - When it was cancelled
     * @param userId - Who cancelled it
     */
    public void Cancel(string? reason, DateTime cancelledUtc, Guid userId)
    {
        // Already cancelled? Don't overwrite the original cancellation info (idempotent)
        if (IsCancelled) return;

        IsCancelled = true;
        CancelledUtc = cancelledUtc;
        CancelledByUserId = userId;

        // If there's a reason, clean it up and save
        if (!string.IsNullOrWhiteSpace(reason))
        {
            reason = reason.Trim();
            // Truncate if too long
            if (reason.Length > 500) reason = reason[..500];
            CancelReason = reason;
        }
    }

    /**
     * Confirm - Changes status from Pending to Confirmed
     * 
     * Can only confirm non-cancelled bookings.
     * Idempotent - if already confirmed, does nothing.
     * 
     * @param userId - Who confirmed the booking
     */
    public void Confirm(Guid userId)
    {
        // Can't confirm a cancelled booking
        if (IsCancelled)
            throw new InvalidOperationException("Cancelled booking cannot be confirmed.");
        
        // Only set if transitioning from Pending to Confirmed (idempotent)
        if (Status != BookingStatus.Confirmed)
        {
            Status = BookingStatus.Confirmed;
            ConfirmedByUserId = userId;
        }
    }

    /**
     * SetTotalAmount - Sets the total amount to charge
     * 
     * If negative amount is passed, defaults to 0
     * 
     * @param amount - The amount to charge
     */
    public void SetTotalAmount(decimal amount)
    {
        // Ternary operator: if amount < 0, use 0, else use amount
        TotalAmount = amount < 0 ? 0 : amount;
    }

    /**
     * SetSpaceConfigurationId - Links a space configuration
     * 
     * Space configurations group multiple spaces together.
     * 
     * @param spaceConfigurationId - Configuration ID or null
     */
    public void SetSpaceConfigurationId(Guid? spaceConfigurationId)
    {
        SpaceConfigurationId = spaceConfigurationId;
    }

    /**
     * UpdateDetails - Updates booking information
     * 
     * Note: Can't update details of cancelled bookings.
     * 
     * @param title - New title
     * @param startUtc - New start time
     * @param endUtc - New end time
     * @param attendeeCount - New attendee count
     */
    public void UpdateDetails(string title, DateTime startUtc, DateTime endUtc, int attendeeCount)
    {
        // Validate title
        if (string.IsNullOrWhiteSpace(title)) 
            throw new ArgumentException("Title is required.", nameof(title));
        title = title.Trim();
        if (title.Length > 200) 
            throw new ArgumentException("Title must be at most 200 characters.", nameof(title));
        
        // Validate times
        if (startUtc >= endUtc) 
            throw new ArgumentException("StartUtc must be before EndUtc.");
        
        // Validate attendees
        if (attendeeCount < 0) 
            throw new ArgumentOutOfRangeException(nameof(attendeeCount), "AttendeeCount must be >= 0.");

        Title = title;
        StartUtc = startUtc;
        EndUtc = endUtc;
        AttendeeCount = attendeeCount;
    }

    /**
     * SetCreatedBy - Records who created the booking
     * 
     * Idempotent - only sets if not already set.
     * This is useful when the creator is determined after creation.
     * 
     * @param userId - The user ID to set
     */
    public void SetCreatedBy(Guid userId)
    {
        // Only set if not already set (idempotent)
        if (CreatedByUserId is null)
        {
            CreatedByUserId = userId;
        }
    }
}
