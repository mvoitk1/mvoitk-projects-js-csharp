using VenuePlatform.Contracts.Bookings;
using VenuePlatform.Contracts.Tenancy;

namespace VenuePlatform.BLL.Domain.Bookings;

public sealed class Booking : ITenantScoped
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid ClientId { get; private set; }
    public string Title { get; private set; } = null!;
    public DateTime StartUtc { get; private set; }
    public DateTime EndUtc { get; private set; }
    public int AttendeeCount { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public bool IsCancelled { get; private set; }
    public DateTime? CancelledUtc { get; private set; }
    public string? CancelReason { get; private set; }
    public decimal TotalAmount { get; private set; }
    public Guid? SpaceConfigurationId { get; private set; }
    public BookingStatus Status { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public Guid? ConfirmedByUserId { get; private set; }
    public Guid? CancelledByUserId { get; private set; }

    private Booking() { } // EF

    public Booking(Guid companyId, Guid clientId, string title, DateTime startUtc, DateTime endUtc, int attendeeCount)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));
        if (title.Length > 200)
            throw new ArgumentException("Title cannot exceed 200 characters.", nameof(title));
        if (startUtc >= endUtc)
            throw new ArgumentException("Start time must be before end time.", nameof(startUtc));
        if (attendeeCount < 0)
            throw new ArgumentException("Attendee count cannot be negative.", nameof(attendeeCount));

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

    public void Cancel(string? reason, DateTime cancelledUtc, Guid userId)
    {
        if (IsCancelled) return; // idempotent - do not overwrite actor

        IsCancelled = true;
        CancelledUtc = cancelledUtc;
        CancelledByUserId = userId;

        if (!string.IsNullOrWhiteSpace(reason))
        {
            reason = reason.Trim();
            if (reason.Length > 500) reason = reason[..500];
            CancelReason = reason;
        }
    }

    public void Confirm(Guid userId)
    {
        if (IsCancelled)
            throw new InvalidOperationException("Cancelled booking cannot be confirmed.");
        
        // Idempotent: only set if transitioning from Pending to Confirmed
        if (Status != BookingStatus.Confirmed)
        {
            Status = BookingStatus.Confirmed;
            ConfirmedByUserId = userId;
        }
    }

    public void SetTotalAmount(decimal amount)
    {
        TotalAmount = amount < 0 ? 0 : amount;
    }

    public void SetSpaceConfigurationId(Guid? spaceConfigurationId)
    {
        SpaceConfigurationId = spaceConfigurationId;
    }

    public void UpdateDetails(string title, DateTime startUtc, DateTime endUtc, int attendeeCount)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.", nameof(title));
        title = title.Trim();
        if (title.Length > 200) throw new ArgumentException("Title must be at most 200 characters.", nameof(title));
        if (startUtc >= endUtc) throw new ArgumentException("StartUtc must be before EndUtc.");
        if (attendeeCount < 0) throw new ArgumentOutOfRangeException(nameof(attendeeCount), "AttendeeCount must be >= 0.");

        Title = title;
        StartUtc = startUtc;
        EndUtc = endUtc;
        AttendeeCount = attendeeCount;
    }

    public void SetCreatedBy(Guid userId)
    {
        // No-op if already set (idempotent)
        if (CreatedByUserId is null)
        {
            CreatedByUserId = userId;
        }
    }
}
