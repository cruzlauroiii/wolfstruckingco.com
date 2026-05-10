using System;
using System.Globalization;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Domain.Constants;
using Microsoft.AspNetCore.Components;
using SharedUI.Services;

namespace SharedUI.Pages;

public partial class SellPayPage
{
    private const string PublishLabelDefault = "Publish job";

    private const string PublishLabelDone = "Published \u2713";

    private const string PublishedMessageFormat = "Listing {0} published.";

    [Inject]
    private WolfsInteropService Wolfs { get; set; } = null!;

    private string TotalPay { get; set; } = ListingConstants.DemoPay;

    private string Legs { get; set; } = ListingConstants.DemoLegs;

    private string PublishLabel { get; set; } = PublishLabelDefault;

    private string StatusMessage { get; set; } = string.Empty;

    private async Task PublishJobAsync()
    {
        var Id = ListingConstants.IdPrefix + Guid.NewGuid().ToString(ListingConstants.IdGuidFormat, CultureInfo.InvariantCulture)[..ListingConstants.IdSliceLength];
        var Listing = new JsonObject
        {
            [ListingConstants.FieldId] = Id,
            [ListingConstants.FieldName] = ListingConstants.DemoName,
            [ListingConstants.FieldPrice] = ListingConstants.DemoPrice,
            [ListingConstants.FieldStatus] = ListingConstants.StatusAvailable,
            [ListingConstants.FieldPay] = TotalPay,
            [ListingConstants.FieldLegs] = Legs,
            [ListingConstants.FieldOrigin] = ListingConstants.DemoOrigin,
            [ListingConstants.FieldDestination] = ListingConstants.DemoDestination,
            [ListingConstants.FieldDescription] = ListingConstants.DemoDescription,
        };
        await Wolfs.DbPutAsync(ListingConstants.Store, Listing);
        PublishLabel = PublishLabelDone;
        StatusMessage = string.Format(CultureInfo.InvariantCulture, PublishedMessageFormat, Id);
    }
}
