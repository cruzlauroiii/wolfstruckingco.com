using System;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SharedUI.Services;

namespace SharedUI.Pages;

public partial class JobOfferPage
{
    private const string SchedulesStore = "schedules";
    private const string WorkersStore = "workers";
    private const string FieldId = "id";
    private const string FieldFrom = "from";
    private const string FieldTo = "to";
    private const string FieldMileage = "mileage";
    private const string FieldDriveHours = "driveHours";
    private const string FieldStartsAt = "startsAt";
    private const string FieldGrace = "graceMinutes";
    private const string FieldLeg = "leg";
    private const string FieldEmail = "email";
    private const string FieldWorkerId = "workerId";

    private const string DefaultTitle = "Multi-leg car delivery";
    private const string DefaultSubtitle = "Hefei → Wilmington · 4 legs";
    private const string DefaultPay = "320";
    private const string Dash = "—";
    private const string Zero = "0";
    private const string Empty = "";

    private const string PayD2 = "420";
    private const string PayD3 = "1180";
    private const string PayD4 = "600";
    private const string LegD2 = "leg2";
    private const string LegD3 = "leg3";
    private const string LegD4 = "leg4";

    private const string MilesFormat = "{0} mi";
    private const string HoursFormat = "{0} hr";
    private const string SubtitleFormat = "{0} → {1}";

    [Inject]
    private WolfsInteropService Wolfs { get; set; } = null!;

    private string Title { get; set; } = DefaultTitle;

    private string Subtitle { get; set; } = DefaultSubtitle;

    private string Pay { get; set; } = DefaultPay;

    private string Distance { get; set; } = Dash;

    private string Hours { get; set; } = Dash;

    private string From { get; set; } = Dash;

    private string To { get; set; } = Dash;

    private string Starts { get; set; } = Dash;

    private string Grace { get; set; } = Dash;

    protected override async Task OnInitializedAsync()
    {
        var Auth = await Wolfs.AuthGetAsync();
        var Email = Auth?.Email ?? Empty;
        var Worker = (await Wolfs.DbAllAsync<JsonObject>(WorkersStore))
            .FirstOrDefault(W => W is not null && string.Equals(W[FieldEmail]?.GetValue<string>() ?? Empty, Email, StringComparison.OrdinalIgnoreCase));
        var WorkerId = Worker?[FieldId]?.GetValue<string>() ?? Empty;
        var Schedules = await Wolfs.DbAllAsync<JsonObject>(SchedulesStore);
        var Latest = !string.IsNullOrEmpty(WorkerId)
            ? Schedules.FirstOrDefault(J => J is not null && string.Equals(J[FieldWorkerId]?.GetValue<string>() ?? Empty, WorkerId, StringComparison.Ordinal))
            : Schedules.Where(R => R is not null).OrderByDescending(R => R?[FieldId]?.GetValue<string>() ?? Empty).FirstOrDefault();
        if (Latest is null) { return; }
        From = Latest[FieldFrom]?.ToString() ?? Dash;
        To = Latest[FieldTo]?.ToString() ?? Dash;
        Distance = string.Format(CultureInfo.InvariantCulture, MilesFormat, Latest[FieldMileage]?.ToString() ?? Zero);
        Hours = string.Format(CultureInfo.InvariantCulture, HoursFormat, Latest[FieldDriveHours]?.ToString() ?? Zero);
        Starts = Latest[FieldStartsAt]?.ToString() ?? Dash;
        Grace = Latest[FieldGrace]?.ToString() ?? Dash;
        var Leg = Latest[FieldLeg]?.ToString() ?? Empty;
        Pay = Leg switch
        {
            var L when L.Contains(LegD2, StringComparison.OrdinalIgnoreCase) => PayD2,
            var L when L.Contains(LegD3, StringComparison.OrdinalIgnoreCase) => PayD3,
            var L when L.Contains(LegD4, StringComparison.OrdinalIgnoreCase) => PayD4,
            _ => DefaultPay,
        };
        Subtitle = string.Format(CultureInfo.InvariantCulture, SubtitleFormat, From, To);
    }
}
