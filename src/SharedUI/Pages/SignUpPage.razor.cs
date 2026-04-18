using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SharedUI.Services;

namespace SharedUI.Pages;

public partial class SignUpPage
{
    private const string AuditStore = "audit";
    private const string FieldKind = "kind";
    private const string FieldId = "id";
    private const string KindSignup = "auth.signup";
    private const string SignupApi = "/api/signup";
    private const string MarketplaceHome = "/Marketplace";
    private const string DefaultRole = "user";
    private const string AtToken = "@";
    private const string DotToken = ".";
    private const string DashToken = "-";
    private const string FailedFormat = "Sign up failed ({0}): {1}";
    private const string Empty = "";

    [Inject]
    private WolfsInteropService Wolfs { get; set; } = null!;

    [Inject]
    private NavigationManager Nav { get; set; } = null!;

    private SignUpModel Form { get; set; } = new();

    private bool Busy { get; set; }

    private string? Error { get; set; }

    private List<System.Text.Json.Nodes.JsonObject> RecentSignups { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        var Rows = await Wolfs.DbAllAsync<System.Text.Json.Nodes.JsonObject>(AuditStore);
        RecentSignups = [.. Rows
            .Where(R => R is not null && R[FieldKind]?.GetValue<string>() == KindSignup)
            .OrderByDescending(R => R?[FieldId]?.GetValue<string>() ?? Empty)];
    }

    private async Task SubmitAsync()
    {
        Busy = true;
        Error = null;
        var Username = Form.Email.ToLowerInvariant().Replace(AtToken, DashToken).Replace(DotToken, DashToken);
        var Resp = await Wolfs.WorkerPostAsync(SignupApi, new { username = Username, email = Form.Email, password = Form.Password, role = DefaultRole });
        Busy = false;
        if (!Resp.Ok)
        {
            Error = string.Format(CultureInfo.InvariantCulture, FailedFormat, Resp.Status, Resp.Body);
            return;
        }

        await Wolfs.AuthSetAsync(DefaultRole, Form.Email, null);
        Nav.NavigateTo(MarketplaceHome, true);
    }

    public sealed class SignUpModel
    {
        public string Email { get; set; } = Empty;

        public string Password { get; set; } = Empty;
    }
}
