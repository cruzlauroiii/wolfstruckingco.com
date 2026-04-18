namespace WolfsTruckingCo.Domain.Models;

public sealed class ItineraryEntity
{
    public string Id { get; set; } = string.Empty;
    public string DriverId { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public ICollection<ItineraryStopEntity> Stops { get; } = [];
    public double TotalMiles { get; set; }
    public double TotalDrivingMinutes { get; set; }
    public double FuelGallonsNeeded { get; set; }
    public int DeliveryCount { get; set; }
}
