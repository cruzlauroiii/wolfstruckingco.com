using System.Collections.Generic;

namespace SharedUI.Pages;

public partial class ServicesPage
{
    private const string IconBox = "📦";
    private const string IconTruck = "🚚";
    private const string IconRig = "🚛";
    private const string IconSnow = "❄️";
    private const string IconCrane = "🏗️";
    private const string IconWarehouse = "🏭";
    private const string IconBolt = "⚡";

    private const string NameDrayage = "Container Drayage";
    private const string DescDrayage = "Port of LA / Long Beach drayage to inland warehouses. Live container status.";
    private const string NameLtl = "LTL Shipping";
    private const string DescLtl = "Less Than Truckload — perfect for 1-6 pallet shipments at competitive rates.";
    private const string NameFtl = "FTL Dedicated";
    private const string DescFtl = "Full Truckload dedicated routes for high-volume customers.";
    private const string NameReefer = "Refrigerated";
    private const string DescReefer = "Reefer freight with temperature monitoring — pharma, produce, frozen.";
    private const string NameFlatbed = "Flatbed";
    private const string DescFlatbed = "Steel, lumber, equipment, oversized loads.";
    private const string NameWarehouse = "3PL Warehouse";
    private const string DescWarehouse = "Pick-and-pack, cross-docking, inventory management.";
    private const string NameExpress = "Express Same-Day";
    private const string DescExpress = "Guaranteed delivery windows for time-critical freight.";

    private static readonly List<ServiceItem> ServiceList =
    [
        new(IconBox, NameDrayage, DescDrayage),
        new(IconTruck, NameLtl, DescLtl),
        new(IconRig, NameFtl, DescFtl),
        new(IconSnow, NameReefer, DescReefer),
        new(IconCrane, NameFlatbed, DescFlatbed),
        new(IconWarehouse, NameWarehouse, DescWarehouse),
        new(IconBolt, NameExpress, DescExpress),
    ];

    private sealed record ServiceItem(string Icon, string Name, string Description);
}
