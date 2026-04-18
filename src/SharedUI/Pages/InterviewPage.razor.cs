using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SharedUI.Services;

namespace SharedUI.Pages;

public partial class InterviewPage
{
    private const string AuditStore = "audit";
    private const string FieldKind = "kind";
    private const string FieldId = "id";
    private const string KindAnswer = "interview.answer";
    private const string Empty = "";

    [Inject]
    private WolfsInteropService Wolfs { get; set; } = null!;

    private JsonObject? Latest { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var Rows = await Wolfs.DbAllAsync<JsonObject>(AuditStore);
        Latest = Rows
            .Where(R => R is not null && string.Equals(R?[FieldKind]?.GetValue<string>(), KindAnswer, StringComparison.Ordinal))
            .OrderByDescending(R => R?[FieldId]?.GetValue<string>() ?? Empty, StringComparer.Ordinal)
            .FirstOrDefault();
    }
}
