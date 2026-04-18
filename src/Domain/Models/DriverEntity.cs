namespace Domain.Models;

public sealed class DriverEntity
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string LicenseClass { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string HomeBase { get; set; } = string.Empty;
    public double HoursAvailable { get; set; }
    public bool IsActive { get; set; }
}
