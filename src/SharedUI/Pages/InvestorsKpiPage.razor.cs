using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SharedUI.Services;

namespace SharedUI.Pages;

public partial class InvestorsKpiPage
{
    private const string ChargesStore = "charges";
    private const string FieldId = "id";
    private const string FieldAt = "at";
    private const string FieldKind = "kind";
    private const string FieldAmount = "amount";
    private const string KindKpiEvent = "kpi_event";
    private const string Empty = "";
    private const int RecentLimit = 5;

    [Inject]
    private WolfsInteropService Wolfs { get; set; } = null!;

    private List<JsonObject> RecentCharges { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        var All = await Wolfs.DbAllAsync<JsonObject>(ChargesStore);
        RecentCharges = [.. All
            .Where(R => R is not null && (R[FieldAmount]?.GetValue<double>() ?? 0) > 0)
            .OrderByDescending(R => R?[FieldAt]?.GetValue<string>() ?? Empty)
            .ThenByDescending(R => R?[FieldId]?.GetValue<string>() ?? Empty)
            .Take(RecentLimit)];
    }
}
