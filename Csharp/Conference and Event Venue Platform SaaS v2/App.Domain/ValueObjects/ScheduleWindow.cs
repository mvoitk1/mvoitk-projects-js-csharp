namespace App.Domain.ValueObjects;

public class ScheduleWindow
{
    public DateTime StartsAt { get; private set; }
    public DateTime EndsAt { get; private set; }

    private ScheduleWindow()
    {
    }

    public ScheduleWindow(DateTime startsAt, DateTime endsAt)
    {
        if (endsAt <= startsAt)
        {
            throw new ArgumentException("Schedule end must be after start.");
        }

        StartsAt = startsAt;
        EndsAt = endsAt;
    }

    public int DurationMinutes => (int) (EndsAt - StartsAt).TotalMinutes;

    public bool Overlaps(ScheduleWindow other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return StartsAt < other.EndsAt && other.StartsAt < EndsAt;
    }
}
