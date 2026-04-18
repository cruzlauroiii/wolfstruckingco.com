namespace Domain.Models;

public sealed class DeliveryEntity
{
    public string Id { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string PickupAddress { get; set; } = string.Empty;
    public double PickupLatitude { get; set; }
    public double PickupLongitude { get; set; }
    public string DeliveryAddress { get; set; } = string.Empty;
    public double DeliveryLatitude { get; set; }
    public double DeliveryLongitude { get; set; }
    public double WeightLbs { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime ScheduledPickup { get; set; }
    public DateTime ScheduledDelivery { get; set; }
    public DateTime? ActualDelivery { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool SignatureRequired { get; set; }
}
