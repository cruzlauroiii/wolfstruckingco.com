namespace Domain.Models;

public sealed class ScheduleEntry
{
    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public int DurationMinutes { get; set; }

    public string Activity { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string StopType { get; set; } = string.Empty;

    public double MilesFromPrevious { get; set; }

    public string Notes { get; set; } = string.Empty;

    public bool IsCompleted { get; set; }
}
