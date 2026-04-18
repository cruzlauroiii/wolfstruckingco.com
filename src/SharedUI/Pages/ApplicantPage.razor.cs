using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SharedUI.Services;

namespace SharedUI.Pages;

public partial class ApplicantPage
{
    private const string AppStore = "applicants";
    private const string FieldId = "id";
    private const string FieldName = "name";
    private const string FieldYears = "experienceYears";
    private const string FieldLocation = "location";
    private const string FieldEmail = "email";

    private const string D1Prefix = "driver1";
    private const string D2Prefix = "driver2";
    private const string D3Prefix = "driver3";

    private const string Q1 = "What's your name and how many years driving?";
    private const string A1Format = "{0}. {1} years on the road.";

    private const string Q2 = "Send a scan of your driver's license, please.";
    private const string A2 = "Sent.";
    private const string S2 = "drivers-license.jpg";
    private const string Q2Team = "Send scans of both team driver licenses.";
    private const string A2Team = "Both attached.";
    private const string S2Team = "license-maya.jpg + license-tom.jpg";

    private const string BadgeQ1 = "Do you have a China export driver pass? We need it for the BYD factory pickup.";
    private const string BadgeA1 = "Yes, I have it. Sending now.";
    private const string BadgeS1 = "china-export-driver-pass.pdf";
    private const string BadgeQ2 = "Do you have a TWIC port pass and drayage card for Long Beach?";
    private const string BadgeA2 = "Yes, both attached.";
    private const string BadgeS2 = "twic-and-drayage.pdf";
    private const string BadgeQ3 = "You're driving as a team — send the team-driver papers.";
    private const string BadgeA3 = "Here you go.";
    private const string BadgeS3 = "team-driver-cert.pdf";
    private const string BadgeQ4 = "For final-mile auto handoff, send your auto-handling cert.";
    private const string BadgeA4 = "Attached.";
    private const string BadgeS4 = "auto-handling-cert.pdf";

    private const string Empty = "";
    private const int SubstepsPerDriver = 3;
    private const int SubstepLicense = 1;
    private const int SubstepBadge = 2;

    [Inject]
    private WolfsInteropService Wolfs { get; set; } = null!;

    private List<Exchange> Pairs { get; } = [];

    private const int VisibleWindow = 3;

    private IEnumerable<Exchange> VisiblePairs =>
        Pairs.Count <= VisibleWindow ? Pairs : Pairs.Skip(Pairs.Count - VisibleWindow);

    protected override async Task OnInitializedAsync()
    {
        var Latest = (await Wolfs.DbAllAsync<JsonObject>(AppStore))
            .Where(R => R is not null)
            .OrderByDescending(R => R?[FieldId]?.GetValue<string>() ?? Empty)
            .FirstOrDefault();
        if (Latest is null) { return; }

        var Name = Latest[FieldName]?.GetValue<string>() ?? Empty;
        var Years = Latest[FieldYears]?.GetValue<int>() ?? 0;
        var Email = Latest[FieldEmail]?.GetValue<string>() ?? Empty;
        var Substep = WolfsRenderContext.CurrentStep % SubstepsPerDriver;

        var (BadgeQ, BadgeA, BadgeScan) = Email switch
        {
            var E when E.StartsWith(D1Prefix, StringComparison.Ordinal) => (BadgeQ1, BadgeA1, BadgeS1),
            var E when E.StartsWith(D2Prefix, StringComparison.Ordinal) => (BadgeQ2, BadgeA2, BadgeS2),
            var E when E.StartsWith(D3Prefix, StringComparison.Ordinal) => (BadgeQ3, BadgeA3, BadgeS3),
            _ => (BadgeQ4, BadgeA4, BadgeS4),
        };

        var IsTeam = Email.StartsWith(D3Prefix, StringComparison.Ordinal);
        Pairs.Add(new Exchange(Q1, string.Format(CultureInfo.InvariantCulture, A1Format, Name, Years), Empty));
        if (Substep >= SubstepLicense)
        {
            Pairs.Add(IsTeam
                ? new Exchange(Q2Team, A2Team, S2Team)
                : new Exchange(Q2, A2, S2));
        }
        if (Substep >= SubstepBadge) { Pairs.Add(new Exchange(BadgeQ, BadgeA, BadgeScan)); }
    }

    public sealed record Exchange(string Question, string Answer, string Scan);
}
