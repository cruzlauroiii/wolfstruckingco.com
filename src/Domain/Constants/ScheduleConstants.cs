namespace Domain.Constants;

public static class ScheduleConstants
{
    public const int MinutesPerHour = 60;
    public const int HoursPerShift = 11;
    public const int MaxDrivingHoursPerDay = 11;
    public const int MandatoryBreakMinutes = 30;
    public const int BreakAfterDrivingMinutes = 480;
    public const int MaxOnDutyHoursPerDay = 14;
    public const int RestPeriodHours = 10;
    public const int MaxDrivingHoursPerWeek = 60;
    public const int MaxDrivingHoursPerCycle = 70;
    public const int CycleDays = 8;
    public const double MilesPerGallon = 6.5;
    public const double FuelTankGallons = 150.0;
    public const int FuelStopThresholdPercent = 25;
    public const int PreTripInspectionMinutes = 15;
    public const int PostTripInspectionMinutes = 15;
    public const int LoadingTimeMinutes = 30;
    public const int UnloadingTimeMinutes = 30;
    public const int AverageSpeedMph = 55;
    public const int UrbanSpeedMph = 35;
    public const int HighwaySpeedMph = 65;
}
