using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SharedUI.Services;

namespace SharedUI.Pages;

public partial class LoginPage
{
    private const string RoleAdmin = "admin";
    private const string RoleDriver = "driver";
    private const string RoleUser = "user";
    private const string HomeAdmin = "HiringHall";
    private const string HomeDriver = "Dashboard";
    private const string HomeUser = "Marketplace";
    private const string HomeApply = "Apply";
    private const string PasswordRequired = "Password is required.";
    private const string ApplicantsStore = "applicants";
    private const string FieldStatus = "status";
    private const string FieldEmail = "email";
    private const string ApprovedValue = "approved";
    private const string Empty = "";
    private const char AtSign = '@';

    private static readonly HashSet<string> KnownRoles = [RoleAdmin, RoleDriver, RoleUser];

    private static readonly Dictionary<string, string> RoleHome = new()
    {
        [RoleAdmin] = HomeAdmin,
        [RoleDriver] = HomeDriver,
        [RoleUser] = HomeUser,
    };

    [Inject]
    private WolfsInteropService Wolfs { get; set; } = null!;

    [Inject]
    private NavigationManager Nav { get; set; } = null!;

    private LoginModel Form { get; set; } = new();

    private bool Busy { get; set; }

    private string? Error { get; set; }

    private async Task SubmitAsync()
    {
        Busy = true;
        Error = null;
        var Email = Form.Email.Trim().ToLowerInvariant();
        var KnownRole = ResolveKnownRole(Email);
        if (KnownRole is null && string.IsNullOrEmpty(Form.Password))
        {
            Busy = false;
            Error = PasswordRequired;
            return;
        }

        var Role = KnownRole ?? RoleUser;
        await Wolfs.AuthSetAsync(Role, Email, null);
        var Target = await ResolveHomeAsync(Role, Email);
        Busy = false;
        Nav.NavigateTo(Target, forceLoad: false);
    }

    private async Task<string> ResolveHomeAsync(string Role, string Email)
    {
        if (Role != RoleDriver) { return RoleHome.GetValueOrDefault(Role, HomeUser); }
        var Approved = (await Wolfs.DbAllAsync<JsonObject>(ApplicantsStore))
            .Any(R => R is not null
                && string.Equals(R[FieldEmail]?.GetValue<string>() ?? Empty, Email, StringComparison.OrdinalIgnoreCase)
                && string.Equals(R[FieldStatus]?.GetValue<string>() ?? Empty, ApprovedValue, StringComparison.Ordinal));
        return Approved ? HomeDriver : HomeApply;
    }

    private static string? ResolveKnownRole(string Input)
    {
        var V = (Input ?? Empty).Trim().ToLowerInvariant();
        if (KnownRoles.Contains(V)) { return V; }
        var At = V.IndexOf(AtSign);
        if (At <= 0) { return null; }
        var User = V[..At];
        return KnownRoles.Contains(User) ? User : null;
    }

    public sealed class LoginModel
    {
        public string Email { get; set; } = Empty;

        public string Password { get; set; } = Empty;
    }
}
