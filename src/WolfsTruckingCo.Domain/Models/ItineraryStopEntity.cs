namespace WolfsTruckingCo.Domain.Models;

public sealed class ItineraryStopEntity
{
    public int Order { get; set; }
    public string StopType { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime ArrivalTime { get; set; }
    public DateTime DepartureTime { get; set; }
    public int DurationMinutes { get; set; }
    public string Activity { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string DeliveryId { get; set; } = string.Empty;
}
