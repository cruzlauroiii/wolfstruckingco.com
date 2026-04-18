using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SharedUI.Services;

namespace SharedUI.Pages;

public partial class ApplyPage
{
    private const string StatusFresh = "fresh";
    private const string StatusPending = "pending";
    private const string StatusHired = "hired";
    private const string ApplicantsStore = "applicants";
    private const string WorkersStore = "workers";
    private const string FieldStatus = "status";
    private const string FieldId = "id";
    private const string FieldEmail = "email";
    private const string FieldName = "name";
    private const string FieldRoles = "roles";
    private const string ApprovedValue = "approved";
    private const string DefaultGreeting = "You're hired!";
    private const string GreetingFormat = "Welcome aboard, {0}!";
    private const string DefaultSubtitle = "Welcome to Wolfs. Your first job offer is on your driver home.";
    private const string SubtitleHiredFormat = "You're cleared to drive. {0}";
    private const string DefaultBadges = "your assigned roles";
    private const string RolePrefix = "role_";
    private const string RoleSeparator = ", ";
    private const string OpenDriverHomeMsg = "Open your driver home to see your first job offer.";
    private const string Empty = "";

    [Inject]
    private WolfsInteropService Wolfs { get; set; } = null!;

    private string Status { get; set; } = StatusFresh;

    private string Greeting { get; set; } = DefaultGreeting;

    private string SubtitleHired { get; set; } = DefaultSubtitle;

    private string Badges { get; set; } = DefaultBadges;

    protected override async Task OnInitializedAsync()
    {
        var Auth = await Wolfs.AuthGetAsync();
        var Email = Auth?.Email ?? Empty;
        var Applicants = (await Wolfs.DbAllAsync<JsonObject>(ApplicantsStore))
            .Where(R => R is not null)
            .ToList();
        var Mine = Applicants
            .Where(R => string.Equals(R?[FieldEmail]?.GetValue<string>() ?? Empty, Email, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(R => R?[FieldId]?.GetValue<string>() ?? Empty, StringComparer.Ordinal)
            .ToList();
        if (Mine.Count == 0)
        {
            Status = StatusFresh;
            return;
        }

        var Approved = Mine.Exists(R => string.Equals(R?[FieldStatus]?.GetValue<string>() ?? Empty, ApprovedValue, StringComparison.Ordinal));
        Status = Approved ? StatusHired : StatusPending;
        if (!string.Equals(Status, StatusHired, StringComparison.Ordinal)) { return; }
        var Worker = (await Wolfs.DbAllAsync<JsonObject>(WorkersStore))
            .Find(W => W is not null && string.Equals(W[FieldEmail]?.GetValue<string>() ?? Empty, Email, StringComparison.OrdinalIgnoreCase));
        var Name = Worker?[FieldName]?.GetValue<string>() ?? Mine[0]?[FieldName]?.GetValue<string>() ?? Empty;
        if (!string.IsNullOrEmpty(Name)) { Greeting = string.Format(System.Globalization.CultureInfo.InvariantCulture, GreetingFormat, Name); }
        var Roles = Worker?[FieldRoles] as JsonArray;
        if (Roles?.Count > 0)
        {
            Badges = string.Join(RoleSeparator, Roles.Select(R => (R?.GetValue<string>() ?? Empty).Replace(RolePrefix, Empty, StringComparison.Ordinal)));
            SubtitleHired = string.Format(System.Globalization.CultureInfo.InvariantCulture, SubtitleHiredFormat, OpenDriverHomeMsg);
        }
    }
}
