using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SharedUI.Services;

namespace SharedUI.Pages;

public partial class AdminPage
{
    private const string ApplicantsStore = "applicants";
    private const string SchedulesStore = "schedules";
    private const string FieldStatus = "status";
    private const string FieldEmail = "email";
    private const string FieldId = "id";
    private const string PendingStatus = "pending";
    private const string Empty = "";

    [Inject]
    private WolfsInteropService Wolfs { get; set; } = null!;

    private int PendingApplicants { get; set; }

    private int ActiveJobs { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var Apps = await Wolfs.DbAllAsync<JsonObject>(ApplicantsStore);
        PendingApplicants = Apps
            .Where(A => A is not null)
            .GroupBy(A => A?[FieldEmail]?.GetValue<string>() ?? Empty, StringComparer.Ordinal)
            .Select(G => G.OrderByDescending(A => A?[FieldId]?.GetValue<string>() ?? Empty, StringComparer.Ordinal).First())
            .Count(A => string.Equals(A?[FieldStatus]?.GetValue<string>(), PendingStatus, StringComparison.Ordinal));
        var Jobs = await Wolfs.DbAllAsync<JsonObject>(SchedulesStore);
        ActiveJobs = Jobs.Count(J => J is not null);
    }
}
