using System;
using System.Globalization;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Domain.Constants;
using Microsoft.AspNetCore.Components;
using SharedUI.Services;

namespace SharedUI.Pages;

public partial class BuyNotesPage
{
    private const string ConfirmLabelDefault = "Confirm order \u2014 pay $48,500 on delivery";

    private const string ConfirmLabelDone = "Confirmed \u2713";

    private const string ConfirmedFormat = "Order {0} confirmed.";

    [Inject]
    private WolfsInteropService Wolfs { get; set; } = null!;

    private bool PhotoProof { get; set; } = true;

    private bool PhotoId { get; set; } = true;

    private bool Driveway { get; set; }

    private bool CallAhead { get; set; }

    private bool CargoInsurance { get; set; } = true;

    private string DriverNote { get; set; } = PurchaseConstants.DemoNote;

    private string ConfirmLabel { get; set; } = ConfirmLabelDefault;

    private string StatusMessage { get; set; } = string.Empty;

    private async Task ConfirmOrderAsync()
    {
        var Id = PurchaseConstants.IdPrefix + Guid.NewGuid().ToString(PurchaseConstants.IdGuidFormat, CultureInfo.InvariantCulture)[..PurchaseConstants.IdSliceLength];
        var Purchase = new JsonObject
        {
            [PurchaseConstants.FieldId] = Id,
            [PurchaseConstants.FieldStatus] = PurchaseConstants.StatusInProgress,
            [PurchaseConstants.FieldTotal] = PurchaseConstants.DemoTotal,
            [PurchaseConstants.FieldPayment] = PurchaseConstants.PaymentOnDelivery,
            [PurchaseConstants.FieldPhotoProof] = PhotoProof,
            [PurchaseConstants.FieldPhotoId] = PhotoId,
            [PurchaseConstants.FieldDriveway] = Driveway,
            [PurchaseConstants.FieldCallAhead] = CallAhead,
            [PurchaseConstants.FieldCargoInsurance] = CargoInsurance,
            [PurchaseConstants.FieldNote] = DriverNote,
        };
        await Wolfs.DbPutAsync(PurchaseConstants.Store, Purchase);
        ConfirmLabel = ConfirmLabelDone;
        StatusMessage = string.Format(CultureInfo.InvariantCulture, ConfirmedFormat, Id);
    }
}
