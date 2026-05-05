using System;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SharedUI.Services;

namespace SharedUI.Pages;

public partial class MapPage
{
    internal const string MainRoad = "M 40 700 Q 110 670 170 580 Q 220 500 250 400 Q 290 270 360 150";

    private const string PointsCsv = "40,700|75,690|110,670|140,625|170,580|195,540|220,500|235,450|250,400|270,335|290,270|325,210|360,150";
    private const char PointSep = '|';
    private const char CoordSep = ',';

    private const string AuditStore = "audit";
    private const string FieldKind = "kind";
    private const string FieldId = "id";
    private const string FieldProgress = "progress";
    private const string FieldStep1 = "step1";
    private const string FieldDistance = "distance";
    private const string FieldActor = "actor";
    private const string FieldEtaMinutes = "etaMinutes";
    private const string KindNav = "nav.read";
    private const string MilesUnit = " mi";

    private const string IconStart = "↗";
    private const string IconRight = "→";
    private const string IconTurn = "↘";
    private const string IconDone = "✓";
    private const string DefaultStep = "Continue on the highway.";
    private const string DefaultNavId = "default";
    private const string DefaultNavStep = "Starting navigation. Continue to the highway.";
    private const string DefaultNavDistance = "120 mi";
    private const int DefaultNavProgress = 35;
    private const int DefaultEtaMinutes = 95;
    private const string Dash = "—";
    private const string Empty = "";

    private const string ColorClear = "#22c55e";
    private const string ColorMedium = "#f59e0b";
    private const string ColorCongested = "#ef4444";

    private const string KindTrack = "track.update";
    private const string FieldSubject = "subject";
    private const string KwDelay = "delay";
    private const string KwCongest = "congest";

    private const int MaxProgress = 95;
    private const int ProgressPerScene = 7;
    private const double ProgressDenom = 100.0;
    private const int Quarter = 25;
    private const int Half = 50;
    private const int ThreeQuarter = 80;
    private const int ViewBoxCenterX = 207;
    private const int ViewBoxCenterY = 400;
    private const int FullProgress = 100;
    private const double FullProgressD = 100.0;
    private const string DistanceFormat = "{0:N0}{1}";

    private const string D1Prefix = "driver1";
    private const string D2Prefix = "driver2";
    private const string D3Prefix = "driver3";
    private const string DriverEmailSuffix = "@wolfstrucking.co";

    private const string OriginPlaceholder = "Origin";
    private const string DestPlaceholder = "Destination";

    private const string OriginD1 = "BYD Hefei Plant, Anhui CN";
    private const string DestD1 = "Yangshan Port, Shanghai CN";
    private const string OriginD2 = "Port of Los Angeles, CA";
    private const string DestD2 = "Phoenix, AZ";
    private const string OriginD3 = "Phoenix, AZ";
    private const string DestD3 = "Memphis, TN";
    private const string OriginD4 = "Memphis, TN";
    private const string DestD4 = "1418 Oak St, Wilmington NC";

    private const string BackgroundD1 = """<path d="M 0 720 Q 70 760 130 740 Q 200 720 260 700 Q 320 680 414 660" fill="none" stroke="#cfe2f3" stroke-width="50" stroke-linecap="round"/><path d="M 0 580 Q 80 540 160 530 Q 240 525 320 535 Q 360 540 414 530" fill="none" stroke="#cfe2f3" stroke-width="44" stroke-linecap="round"/><path d="M 0 220 Q 90 240 180 230 Q 270 220 360 200 Q 400 195 414 195" fill="none" stroke="#cfe2f3" stroke-width="48" stroke-linecap="round"/><path d="M 220 0 Q 230 200 250 380 Q 270 580 280 800" fill="none" stroke="#dbe2ea" stroke-width="14" stroke-linecap="round"/>""";
    private const string BackgroundD2 = """<path d="M 0 700 L 80 690 L 180 685 L 280 695 L 414 700" fill="none" stroke="#fde68a" stroke-width="20" stroke-linecap="round"/><path d="M 60 0 L 80 200 L 100 400 L 130 600 L 150 800" fill="none" stroke="#dbe2ea" stroke-width="16" stroke-linecap="round"/><path d="M 280 0 L 290 220 L 320 440 L 340 660 L 360 800" fill="none" stroke="#dbe2ea" stroke-width="14" stroke-linecap="round"/><circle cx="60" cy="730" r="6" fill="#94a3b8"/><circle cx="200" cy="500" r="5" fill="#94a3b8"/><circle cx="340" cy="220" r="6" fill="#94a3b8"/>""";
    private const string BackgroundD3 = """<path d="M 0 700 Q 100 680 200 670 Q 300 660 414 640" fill="none" stroke="#dbe2ea" stroke-width="22" stroke-linecap="round"/><path d="M 0 460 Q 110 470 220 450 Q 330 435 414 420" fill="none" stroke="#dbe2ea" stroke-width="20" stroke-linecap="round"/><path d="M 0 220 L 110 215 L 220 220 L 330 215 L 414 220" fill="none" stroke="#dbe2ea" stroke-width="18" stroke-linecap="round"/><path d="M 100 0 L 120 200 L 140 400 L 160 600 L 170 800" fill="none" stroke="#dbe2ea" stroke-width="13" stroke-linecap="round"/><path d="M 320 0 L 310 200 L 300 400 L 290 600 L 280 800" fill="none" stroke="#dbe2ea" stroke-width="12" stroke-linecap="round"/>""";
    private const string BackgroundD4 = """<path d="M 0 720 L 100 700 L 220 705 L 340 695 L 414 690" fill="none" stroke="#cfe2f3" stroke-width="36" stroke-linecap="round"/><path d="M 0 540 Q 90 530 180 520 Q 270 510 360 500 Q 400 495 414 495" fill="none" stroke="#cfe2f3" stroke-width="30" stroke-linecap="round"/><path d="M 0 360 Q 110 355 220 345 Q 330 335 414 320" fill="none" stroke="#cfe2f3" stroke-width="26" stroke-linecap="round"/><path d="M 80 0 Q 100 200 130 400 Q 160 600 190 800" fill="none" stroke="#dbe2ea" stroke-width="14" stroke-linecap="round"/><path d="M 290 0 Q 280 200 290 400 Q 300 600 310 800" fill="none" stroke="#dbe2ea" stroke-width="13" stroke-linecap="round"/>""";

    private const string LabelsD1Svg = """<text x="20" y="680" font-size="11" fill="#94a3b8" font-weight="700">G15 Expwy</text><text x="40" y="270" font-size="11" fill="#94a3b8" font-weight="700">Yangtze River</text><text x="280" y="760" font-size="11" fill="#94a3b8" font-weight="700">G42 Hwy</text><text x="80" y="540" font-size="11" fill="#94a3b8" font-weight="700">Hangzhou Bay</text>""";
    private const string LabelsD2Svg = """<text x="20" y="680" font-size="11" fill="#94a3b8" font-weight="700">I-10 East</text><text x="60" y="290" font-size="11" fill="#94a3b8" font-weight="700">I-15</text><text x="290" y="250" font-size="11" fill="#94a3b8" font-weight="700">US-60</text><text x="140" y="470" font-size="11" fill="#94a3b8" font-weight="700">Mojave Desert</text>""";
    private const string LabelsD3Svg = """<text x="20" y="680" font-size="11" fill="#94a3b8" font-weight="700">I-40 East</text><text x="60" y="470" font-size="11" fill="#94a3b8" font-weight="700">Albuquerque</text><text x="280" y="460" font-size="11" fill="#94a3b8" font-weight="700">Amarillo</text><text x="60" y="240" font-size="11" fill="#94a3b8" font-weight="700">Oklahoma City</text>""";
    private const string LabelsD4Svg = """<text x="20" y="680" font-size="11" fill="#94a3b8" font-weight="700">I-40 East</text><text x="40" y="530" font-size="11" fill="#94a3b8" font-weight="700">Nashville</text><text x="280" y="360" font-size="11" fill="#94a3b8" font-weight="700">Raleigh</text><text x="80" y="250" font-size="11" fill="#94a3b8" font-weight="700">I-95 South</text>""";

    private static readonly (double X, double Y)[] Pts = Parse(PointsCsv);

    [Inject]
    private WolfsInteropService Wolfs { get; set; } = null!;

    private JsonObject? Latest { get; set; }

    private int Progress { get; set; }

    private double DriverX => PointAt(Progress / ProgressDenom).X;

    private double DriverY => PointAt(Progress / ProgressDenom).Y;

    private string TurnIcon => Progress switch
    {
        < Quarter => IconStart,
        < Half => IconRight,
        < ThreeQuarter => IconTurn,
        _ => IconDone,
    };

    private string TurnText => Latest?[FieldStep1]?.ToString() ?? DefaultStep;

    private string LegOrigin { get; set; } = OriginPlaceholder;

    private string LegDestination { get; set; } = DestPlaceholder;

    private string? LegBackgroundSvg { get; set; }

    private string? LegLabelsSvg { get; set; }

    private string AheadColor { get; set; } = ColorClear;

    private double CameraDx => ViewBoxCenterX - DriverX;

    private double CameraDy => ViewBoxCenterY - DriverY;

    private string RemainingDistance
    {
        get
        {
            var Total = ParseLeading(Latest?[FieldDistance]?.ToString() ?? Empty);
            if (Total <= 0) { return Dash; }
            var Remaining = (int)System.Math.Round(Total * (FullProgress - Progress) / FullProgressD);
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, DistanceFormat, Remaining, MilesUnit);
        }
    }

    private int RemainingEta
    {
        get
        {
            var TotalEta = Latest?[FieldEtaMinutes]?.GetValue<int>() ?? 0;
            return TotalEta * (FullProgress - Progress) / FullProgress;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        System.Collections.Generic.IEnumerable<JsonObject> Audit;
        try { Audit = await Wolfs.DbAllAsync<JsonObject>(AuditStore); }
        catch (Exception E) when (E is Microsoft.JSInterop.JSException or InvalidOperationException) { Audit = System.Array.Empty<JsonObject>(); }
        var Rows = Audit
            .Where(R => R is not null && string.Equals(R?[FieldKind]?.GetValue<string>(), KindNav, StringComparison.Ordinal))
            .OrderByDescending(R => R?[FieldId]?.GetValue<string>() ?? Empty, StringComparer.Ordinal)
            .ToList();
        Latest = Rows.FirstOrDefault();
        Latest ??= new JsonObject
            {
                [FieldKind] = KindNav,
                [FieldId] = DefaultNavId,
                [FieldProgress] = DefaultNavProgress,
                [FieldStep1] = DefaultNavStep,
                [FieldDistance] = DefaultNavDistance,
                [FieldActor] = D1Prefix + DriverEmailSuffix,
                [FieldEtaMinutes] = DefaultEtaMinutes,
            };

        Progress = Latest[FieldProgress]?.GetValue<int>() ?? System.Math.Min(MaxProgress, Rows.Count * ProgressPerScene);
        var Email = Latest[FieldActor]?.ToString() ?? Empty;

        var (Origin, Destination, Background, Labels) = Email switch
        {
            var E when E.StartsWith(D1Prefix, StringComparison.Ordinal) => (OriginD1, DestD1, BackgroundD1, LabelsD1Svg),
            var E when E.StartsWith(D2Prefix, StringComparison.Ordinal) => (OriginD2, DestD2, BackgroundD2, LabelsD2Svg),
            var E when E.StartsWith(D3Prefix, StringComparison.Ordinal) => (OriginD3, DestD3, BackgroundD3, LabelsD3Svg),
            _ => (OriginD4, DestD4, BackgroundD4, LabelsD4Svg),
        };
        LegOrigin = Origin;
        LegDestination = Destination;
        LegBackgroundSvg = Background;
        LegLabelsSvg = Labels;

        var LatestTrack = (Audit ?? [])
            .Where(R => R is not null && string.Equals(R?[FieldKind]?.GetValue<string>(), KindTrack, StringComparison.Ordinal))
            .OrderByDescending(R => R?[FieldId]?.GetValue<string>() ?? Empty, StringComparer.Ordinal)
            .FirstOrDefault();
        var TrackSubject = (LatestTrack?[FieldSubject]?.ToString() ?? Empty).ToLowerInvariant();
        if (TrackSubject.Contains(KwDelay, StringComparison.Ordinal) || TrackSubject.Contains(KwCongest, StringComparison.Ordinal))
        {
            AheadColor = ColorCongested;
        }
        else if (Progress > Half)
        {
            AheadColor = ColorMedium;
        }
    }

    private static int ParseLeading(string S)
    {
        var Sb = new System.Text.StringBuilder();
        foreach (var Ch in S)
        {
            if (char.IsDigit(Ch)) { Sb.Append(Ch); continue; }
            if (Ch is ',' or ' ') { continue; }
            break;
        }

        return int.TryParse(Sb.ToString(), CultureInfo.InvariantCulture, out var V) ? V : 0;
    }

    private static (double X, double Y)[] Parse(string Csv) =>
    [
        .. Csv.Split(PointSep)
            .Select(P => P.Split(CoordSep))
            .Select(C => (double.Parse(C[0], CultureInfo.InvariantCulture),
                double.Parse(C[1], CultureInfo.InvariantCulture))),
    ];

    private static (double X, double Y) PointAt(double T)
    {
        var Idx = (int)System.Math.Round(T * (Pts.Length - 1));
        if (Idx < 0) { Idx = 0; }
        if (Idx > Pts.Length - 1) { Idx = Pts.Length - 1; }
        return Pts[Idx];
    }
}
