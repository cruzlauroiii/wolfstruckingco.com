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
    private const string FieldSubject = "subject";
    private const string KindTrack = "track.update";
    private const string Empty = "";
    private const int StepLimit = 5;
    private const int OceanStartX = 40;
    private const int OceanStartY = 50;
    private const int OceanEndX = 380;
    private const int OceanEndY = 50;
    private const int OceanArcY = 80;
    private const int FullProgress = 100;
    private const int HalfProgress = 50;
    private const int QuarterProgress = 25;
    private const int ThreeQuartersProgress = 75;
    private const int ArcCurveScale = 4;
    private const string KwDeparted = "departed";
    private const string KwHalfway = "halfway";
    private const string KwMidPacific = "mid-pacific";
    private const string KwOceanTransit = "ocean transit";
    private const string KwArrived = "arrived";
    private const string KwDelayed = "delayed";
    private const string KwDelivered = "delivered";

    [Inject]
    private WolfsInteropService Wolfs { get; set; } = null!;

    private JsonObject? Latest { get; set; }

    private List<JsonObject> Steps { get; set; } = [];

    private int ShipProgress { get; set; }

    private double ShipX => OceanStartX + ((OceanEndX - OceanStartX) * ShipProgress / (double)FullProgress);

    private double ShipY
    {
        get
        {
            var T = ShipProgress / (double)FullProgress;
            var Lerp = OceanStartY + ((OceanEndY - OceanStartY) * T);
            var Arc = (OceanArcY - OceanStartY) * ArcCurveScale * T * (1 - T);
            return Lerp + Arc;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        var Rows = (await Wolfs.DbAllAsync<JsonObject>(AuditStore))
            .Where(R => R is not null && R[FieldKind]?.GetValue<string>() == KindTrack)
            .OrderByDescending(R => R?[FieldId]?.GetValue<string>() ?? Empty)
            .ToList();
        Latest = Rows.FirstOrDefault();
        Steps = [.. Rows.Take(StepLimit)];
        var Subject = (Latest?[FieldSubject]?.ToString() ?? Empty).ToLowerInvariant();
        ShipProgress = ProgressFromSubject(Subject);
    }

    private static int ProgressFromSubject(string Subject) =>
        Subject.Contains(KwDeparted, System.StringComparison.Ordinal) ? QuarterProgress :
        Subject.Contains(KwHalfway, System.StringComparison.Ordinal) || Subject.Contains(KwMidPacific, System.StringComparison.Ordinal) ? HalfProgress :
        Subject.Contains(KwOceanTransit, System.StringComparison.Ordinal) ? ThreeQuartersProgress :
        Subject.Contains(KwArrived, System.StringComparison.Ordinal) ? FullProgress :
        Subject.Contains(KwDelayed, System.StringComparison.Ordinal) || Subject.Contains(KwDelivered, System.StringComparison.Ordinal) ? FullProgress :
        0;
}
