using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SharedUI.Services;

namespace SharedUI.Pages;

public partial class HiringHallPage
{
    private const string Store = "applicants";
    private const string FieldEmail = "email";
    private const string FieldId = "id";
    private const string FieldStatus = "status";
    private const string FieldName = "name";
    private const string Approved = "approved";
    private const string Empty = "";
    private const string Question = "?";

    [Inject]
    private WolfsInteropService Wolfs { get; set; } = null!;

    private List<JsonObject> Applicants { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        Applicants = [.. (await Wolfs.DbAllAsync<JsonObject>(Store))
            .Where(A => A is not null)
            .GroupBy(A => A?[FieldEmail]?.GetValue<string>() ?? Empty)
            .Select(G => G.OrderByDescending(A => A?[FieldId]?.GetValue<string>() ?? Empty).First())
            .OrderBy(A => A?[FieldEmail]?.GetValue<string>() ?? Empty)];
    }

    private static string FirstLetter(string? Name)
    {
        var Token = (Name ?? Empty).Split(' ').FirstOrDefault();
        return string.IsNullOrEmpty(Token) ? Question : Token[..1].ToUpperInvariant();
    }

    private static bool IsApproved(JsonObject A) =>
        A[FieldStatus]?.GetValue<string>() == Approved;
}
