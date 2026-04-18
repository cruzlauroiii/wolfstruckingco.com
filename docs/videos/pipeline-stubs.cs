using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SharedUI.Services;

namespace VideoPipeline;

internal sealed class StubJsRuntime : IJSRuntime
{
    private static readonly string DbFile = Path.Combine(@"C:\repo\public\wolfstruckingco.com\main", "data", "wolfs-db.jsonl");
    private static Dictionary<string, string> DbCache = LoadDb();

    public static string CurrentActorEmail { get; set; } = "admin@wolfstruckingco.com";
    public static string CurrentActorRole { get; set; } = "admin";

    private static Dictionary<string, string> LoadDb()
    {
        var Result = new Dictionary<string, string>();
        if (!File.Exists(DbFile)) { return Result; }
        var Buckets = new Dictionary<string, List<string>>();
        foreach (var Line in File.ReadAllLines(DbFile))
        {
            if (string.IsNullOrWhiteSpace(Line)) { continue; }
            using var Doc = JsonDocument.Parse(Line);
            if (!Doc.RootElement.TryGetProperty("_store", out var StoreEl)) { continue; }
            var Store = StoreEl.GetString() ?? "";
            if (!Buckets.TryGetValue(Store, out var List1)) { List1 = new(); Buckets[Store] = List1; }
            var Obj = new JsonObject();
            foreach (var P in Doc.RootElement.EnumerateObject())
            {
                if (P.Name == "_store") { continue; }
                Obj[P.Name] = JsonNode.Parse(P.Value.GetRawText());
            }
            List1.Add(Obj.ToJsonString());
        }
        foreach (var Kv in Buckets) { Result[Kv.Key] = "[" + string.Join(",", Kv.Value) + "]"; }
        return Result;
    }

    private static bool PermissionAllowed(string Actor, string Permission)
    {
        if (string.IsNullOrWhiteSpace(Permission)) { return false; }
        if (Permission.StartsWith("auth.", StringComparison.Ordinal)) { return true; }
        if ((Actor ?? "").StartsWith("dispatcher", StringComparison.Ordinal)) { return true; }
        var Map = new Dictionary<string, string[]>
        {
            ["driver"] = new[] { "applicant.", "interview.", "documents.", "nav.", "itinerary." },
            ["admin"] = new[] { "applicant.approve", "track.", "kpi.", "audit." },
            ["employer"] = new[] { "listing." },
            ["buyer"] = new[] { "purchase." },
            ["system"] = new[] { "audit." },
        };
        var ActorOrEmpty = Actor ?? string.Empty;
        var Role = ActorOrEmpty.Contains('@', StringComparison.Ordinal)
            ? ActorOrEmpty.Split('@')[0].StartsWith("driver", StringComparison.Ordinal) ? "driver"
              : ActorOrEmpty.StartsWith("admin", StringComparison.Ordinal) ? "admin"
              : ActorOrEmpty.StartsWith("wei", StringComparison.Ordinal) ? "employer"
              : ActorOrEmpty.StartsWith("sam", StringComparison.Ordinal) ? "buyer"
              : "user"
            : ActorOrEmpty;
        return Map.TryGetValue(Role, out var Allowed) && Allowed.Any(P => Permission.StartsWith(P, StringComparison.Ordinal));
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string Identifier, object?[]? Args) => Stub<TValue>(Identifier, Args);
    public ValueTask<TValue> InvokeAsync<TValue>(string Identifier, CancellationToken Tk, object?[]? Args) => Stub<TValue>(Identifier, Args);

    private static ValueTask<TValue> Stub<TValue>(string Identifier, object?[]? Args)
    {
        if (Identifier.Contains("dbPut", StringComparison.OrdinalIgnoreCase))
        {
            var Store = Args?[0]?.ToString() ?? "";
            var Value = Args?[1] as JsonObject;
            if (Value is null) { return ValueTask.FromResult(default(TValue)!); }
            var Permission = Value["permission"]?.GetValue<string>() ?? Store;
            var Actor = Value["actor"]?.GetValue<string>() ?? "";
            if (!PermissionAllowed(Actor, Permission))
            {
                throw new UnauthorizedAccessException($"actor '{Actor}' denied permission '{Permission}'");
            }
            var Wrapped = new JsonObject { ["_store"] = Store };
            foreach (var Kv in Value) { Wrapped[Kv.Key] = Kv.Value is null ? null : JsonNode.Parse(Kv.Value.ToJsonString()); }
            File.AppendAllText(DbFile, Wrapped.ToJsonString() + Environment.NewLine);
            DbCache = LoadDb();
            return ValueTask.FromResult(default(TValue)!);
        }
        if (typeof(TValue) == typeof(string) && Identifier.Contains("dbAllJson", StringComparison.OrdinalIgnoreCase))
        {
            var Store = Args?[0]?.ToString() ?? "";
            var Json = DbCache.TryGetValue(Store, out var V) ? V : "[]";
            return ValueTask.FromResult((TValue)(object)Json);
        }
        if (typeof(TValue) == typeof(string) && Identifier.Contains("localStorage.getItem", StringComparison.OrdinalIgnoreCase))
        {
            var Key = Args?[0]?.ToString() ?? "";
            var Value = Key switch
            {
                "wolfs_role" => CurrentActorRole,
                "wolfs_email" => CurrentActorEmail,
                _ => string.Empty,
            };
            return ValueTask.FromResult((TValue)(object)Value);
        }
        if (typeof(TValue) == typeof(string)) { return ValueTask.FromResult((TValue)(object)""); }
        if (typeof(TValue) == typeof(AuthState)) { return ValueTask.FromResult((TValue)(object)new AuthState(CurrentActorRole, CurrentActorEmail, null)); }
        if (typeof(TValue) == typeof(WorkerResponse)) { return ValueTask.FromResult((TValue)(object)new WorkerResponse(true, 200, "")); }
        return ValueTask.FromResult(default(TValue)!);
    }
}

internal sealed class StubNavigationManager : NavigationManager
{
    public StubNavigationManager() => Initialize("https://localhost/", "https://localhost/");
    protected override void NavigateToCore(string Uri, bool ForceLoad) { }
}
