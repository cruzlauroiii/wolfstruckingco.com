namespace Domain.Constants;

public static class PurchaseConstants
{
    public const string Store = "purchases";
    public const string IdPrefix = "pur_";
    public const int IdSliceLength = 8;
    public const string IdGuidFormat = "N";
    public const string FieldId = "id";
    public const string FieldStatus = "status";
    public const string FieldTotal = "total";
    public const string FieldPayment = "payment";
    public const string FieldPhotoProof = "photoProof";
    public const string FieldPhotoId = "photoId";
    public const string FieldDriveway = "driveway";
    public const string FieldCallAhead = "callAhead";
    public const string FieldCargoInsurance = "cargoInsurance";
    public const string FieldNote = "note";
    public const string StatusInProgress = "in_progress";
    public const string PaymentOnDelivery = "on_delivery";
    public const int DemoTotal = 48500;
    public const string DemoNote = "Garage is on the right side of the house. Buyer will be in front yard waiting.";
}
