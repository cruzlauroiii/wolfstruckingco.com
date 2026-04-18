namespace Domain.Constants;

public static class DeliveryConstants
{
    public const string StatusPending = "Pending";
    public const string StatusInTransit = "InTransit";
    public const string StatusDelivered = "Delivered";
    public const string StatusFailed = "Failed";
    public const string StatusCancelled = "Cancelled";
    public const string PriorityHigh = "High";
    public const string PriorityMedium = "Medium";
    public const string PriorityLow = "Low";
    public const int MaxDeliveriesPerDay = 12;
    public const int SignatureRequiredWeightLbs = 50;
}
