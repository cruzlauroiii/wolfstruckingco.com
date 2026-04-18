using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SharedUI.Services;

namespace SharedUI.Pages;

public partial class InvestorsUsersPage
{
    private const string UsersStore = "users";
    private const string FieldRole = "role";
    private const string RoleAdmin = "admin";
    private const string RoleDriver = "driver";
    private const string RoleUser = "user";
    private const string Empty = "";

    [Inject]
    private WolfsInteropService Wolfs { get; set; } = null!;

    private List<Dictionary<string, object>> Users { get; set; } = [];

    private int Total { get; set; }

    private int AdminCount { get; set; }

    private int DriverCount { get; set; }

    private int UserCount { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Users = await Wolfs.DbAllAsync<Dictionary<string, object>>(UsersStore);
        Total = Users.Count;
        AdminCount = Users.Count(U => Get(U, FieldRole) == RoleAdmin);
        DriverCount = Users.Count(U => Get(U, FieldRole) == RoleDriver);
        UserCount = Users.Count(U => Get(U, FieldRole) == RoleUser);
    }

    private static string Get(Dictionary<string, object> D, string K) =>
        D.TryGetValue(K, out var V) ? V?.ToString() ?? Empty : Empty;
}
