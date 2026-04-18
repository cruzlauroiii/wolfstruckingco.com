using System;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SharedUI.Services;

namespace SharedUI.Pages;

public partial class DashboardPage
{
    private const string ScheduleStore = "schedules";
    private const string TimesheetStore = "timesheets";
    private const string ApplicantsStore = "applicants";
    private const string WorkersStore = "workers";
    private const string FieldId = "id";
    private const string FieldEarnings = "earnings";
    private const string FieldEmail = "email";
    private const string FieldName = "name";
    private const string FieldWorkerId = "workerId";
    private const string DefaultGreeting = "Driver home";
    private const string DriverFallback = "Driver";
    private const string ZeroEarnings = "0";
    private const string NumberFormat = "N0";
    private const string GreetingFormat = "Welcome, {0}";
    private const string Empty = "";
    private const char AtSign = '@';

    [Inject]
    private WolfsInteropService Wolfs { get; set; } = null!;

    private JsonObject? NextJob { get; set; }

    private string WeekEarnings { get; set; } = ZeroEarnings;

    private string Greeting { get; set; } = DefaultGreeting;

    protected override async Task OnInitializedAsync()
    {
        var Sheets = await Wolfs.DbAllAsync<JsonObject>(TimesheetStore);
        var Total = Sheets.Where(S => S is not null).Sum(S => S?[FieldEarnings]?.GetValue<double>() ?? 0);
        WeekEarnings = Total.ToString(NumberFormat, CultureInfo.InvariantCulture);

        var Auth = await Wolfs.AuthGetAsync();
        var Email = Auth?.Email ?? Empty;
        var Worker = (await Wolfs.DbAllAsync<JsonObject>(WorkersStore))
            .FirstOrDefault(W => W is not null && string.Equals(W[FieldEmail]?.GetValue<string>() ?? Empty, Email, StringComparison.OrdinalIgnoreCase));
        var WorkerId = Worker?[FieldId]?.GetValue<string>() ?? Empty;

        var Schedules = await Wolfs.DbAllAsync<JsonObject>(ScheduleStore);
        NextJob = !string.IsNullOrEmpty(WorkerId)
            ? Schedules.FirstOrDefault(J => J is not null && string.Equals(J[FieldWorkerId]?.GetValue<string>() ?? Empty, WorkerId, StringComparison.Ordinal))
            : Schedules.Where(J => J is not null).OrderByDescending(J => J?[FieldId]?.GetValue<string>() ?? Empty, StringComparer.Ordinal).FirstOrDefault();

        var DisplayName = (await Wolfs.DbAllAsync<JsonObject>(ApplicantsStore))
            .Where(R => R is not null && string.Equals(R[FieldEmail]?.GetValue<string>() ?? Empty, Email, StringComparison.OrdinalIgnoreCase))
            .Select(R => R?[FieldName]?.GetValue<string>() ?? Empty)
            .FirstOrDefault(N => !string.IsNullOrEmpty(N));
        var Handle = !string.IsNullOrEmpty(DisplayName)
            ? DisplayName
            : Email.Split(AtSign).FirstOrDefault() ?? DriverFallback;
        Greeting = string.IsNullOrEmpty(Email)
            ? DefaultGreeting
            : string.Format(CultureInfo.InvariantCulture, GreetingFormat, Handle);
    }
}
