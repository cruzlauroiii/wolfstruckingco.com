using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SharedUI.Services;

namespace SharedUI.Pages;

public partial class SchedulePage
{
    private const string SchedulesStore = "schedules";
    private const string FieldId = "id";
    private const string Empty = "";
    private const int EarlierLimit = 4;

    [Inject]
    private WolfsInteropService Wolfs { get; set; } = null!;

    private JsonObject? Latest { get; set; }

    private List<JsonObject> Earlier { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        var Rows = (await Wolfs.DbAllAsync<JsonObject>(SchedulesStore))
            .Where(R => R is not null)
            .OrderByDescending(R => R?[FieldId]?.GetValue<string>() ?? Empty, StringComparer.Ordinal)
            .ToList();
        Latest = Rows.FirstOrDefault();
        Earlier = [.. Rows.Skip(1).Take(EarlierLimit)];
    }
}
