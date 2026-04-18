using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SharedUI.Services;

namespace SharedUI.Pages;

public partial class ItineraryPage
{
    private const string TimesheetsStore = "timesheets";
    private const string FieldId = "id";
    private const string FieldEarnings = "earnings";
    private const string Empty = "";

    [Inject]
    private WolfsInteropService Wolfs { get; set; } = null!;

    private List<JsonObject> Timesheets { get; set; } = [];

    private decimal Total { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var Rows = await Wolfs.DbAllAsync<JsonObject>(TimesheetsStore);
        Timesheets = [.. Rows
            .Where(R => R is not null)
            .OrderByDescending(R => R?[FieldId]?.GetValue<string>() ?? Empty)];
        Total = Timesheets.Sum(R => R[FieldEarnings] is null
            ? 0m
            : (decimal)(R[FieldEarnings]?.GetValue<double>() ?? 0));
    }
}
