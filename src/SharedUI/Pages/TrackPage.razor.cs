using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SharedUI.Services;

namespace SharedUI.Pages;

public partial class TrackPage
{
    private const string AuditStore = "audit";
    private const string FieldKind = "kind";
    private const string FieldId = "id";
    private const string KindTrack = "track.update";
    private const string Empty = "";
    private const int StepLimit = 5;

    [Inject]
    private WolfsInteropService Wolfs { get; set; } = null!;

    private JsonObject? Latest { get; set; }

    private List<JsonObject> Steps { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        var Rows = (await Wolfs.DbAllAsync<JsonObject>(AuditStore))
            .Where(R => R is not null && string.Equals(R?[FieldKind]?.GetValue<string>(), KindTrack, StringComparison.Ordinal))
            .OrderByDescending(R => R?[FieldId]?.GetValue<string>() ?? Empty, StringComparer.Ordinal)
            .ToList();
        Latest = Rows.FirstOrDefault();
        Steps = [.. Rows.Take(StepLimit)];
    }
}
