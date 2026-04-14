namespace App.DTO.v1.Venues.Common;

public class ScheduleWindowDto
{
    public DateTime StartsAt { get; init; }
    public DateTime EndsAt { get; init; }
    public int DurationMinutes { get; init; }
}
