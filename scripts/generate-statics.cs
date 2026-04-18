#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
#:project ../src/SharedUI/SharedUI.csproj

// generate-statics.cs — prerender every routable SharedUI Razor page to a
// standalone HTML file under docs/Generated/<Route>/index.html (default) or, with
// --in-place, directly into docs/<Route>/index.html as a drop-in replacement for
// the existing hand-written static markup.
//
//   dotnet run scripts/generate-statics.cs                            # → docs/Generated/
//   dotnet run scripts/generate-statics.cs -- --in-place              # → docs/<Route>/
//   dotnet run scripts/generate-statics.cs -- C:\…\main               # explicit repo root
//   dotnet run scripts/generate-statics.cs -- --in-place C:\…\main    # both
//
// Components that depend on JS interop receive a no-op stub so OnInitializedAsync
// runs without throwing — the rendered page shows the empty/default state and is
// later hydrated by the live SPA at /app when JS runs.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using System.Reflection;
using SharedUI.Components;
using SharedUI.Services;

// Re-enable reflection-based JSON serialization so WolfsInteropService.DbAllAsync<T>
// can deserialize generic lists without source-generated TypeInfo.
AppContext.SetSwitch("System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault", true);

var InPlace = args.Contains("--in-place");
var Positional = args.Where(A => !A.StartsWith("--")).ToArray();
var Repo = Positional.Length > 0 ? Positional[0] : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
var DocsRoot = Path.Combine(Repo, "docs");
if (!Directory.Exists(DocsRoot))
{
    Console.Error.WriteLine($"docs not found: {DocsRoot}");
    return 1;
}
var WriteRoot = InPlace ? DocsRoot : Path.Combine(DocsRoot, "Generated");
Console.WriteLine($"output → {WriteRoot}{(InPlace ? "  (in-place: replacing docs/<Route>/index.html)" : "")}");

var CssPath = Path.Combine(Repo, "src", "SharedUI", "wwwroot", "css", "app.css");
var Css = File.Exists(CssPath) ? File.ReadAllText(CssPath) : string.Empty;
Console.WriteLine($"inlining {Css.Length:N0} bytes of app.css → every page is fully standalone");

var Asm = typeof(MainLayout).Assembly;
var Pages = Asm.GetTypes()
    .Where(T => typeof(IComponent).IsAssignableFrom(T) && !T.IsAbstract)
    .Select(T => (Type: T, Routes: T.GetCustomAttributes<RouteAttribute>().Select(R => R.Template).ToArray()))
    .Where(P => P.Routes.Length > 0)
    .ToList();

Console.WriteLine($"found {Pages.Count} routable pages");

var Services = new ServiceCollection();
Services.AddSingleton<IJSRuntime, StubJsRuntime>();
Services.AddSingleton<WolfsInteropService>();
Services.AddSingleton<VoiceChatService>();
Services.AddSingleton<NavigationManager, StubNavigationManager>();
Services.AddSingleton<HttpClient>();
Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
var Provider = Services.BuildServiceProvider();
var LoggerFactory = Provider.GetRequiredService<ILoggerFactory>();

await using var Renderer = new HtmlRenderer(Provider, LoggerFactory);

var Written = 0;
var Skipped = 0;
foreach (var (Type, Routes) in Pages)
{
    foreach (var Route in Routes)
    {
        var Slug = Route.Trim('/');
        var SafeName = string.IsNullOrEmpty(Slug) ? "index" : Slug;
        try
        {
            var PageType = Type;
            var Html = await Renderer.Dispatcher.InvokeAsync(async () =>
            {
                var Params = ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    ["Layout"] = typeof(MainLayout),
                    ["ChildContent"] = (RenderFragment)(b => { b.OpenComponent(0, PageType); b.CloseComponent(); }),
                });
                var Output = await Renderer.RenderComponentAsync<LayoutView>(Params);
                return Output.ToHtmlString();
            });
            var OutDir = string.IsNullOrEmpty(Slug)
                ? WriteRoot
                : Path.Combine(WriteRoot, Slug.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(OutDir);
            var OutPath = Path.Combine(OutDir, "index.html");
            File.WriteAllText(OutPath, WrapHtml(SafeName, Html, Css));
            Written++;
            Console.WriteLine($"  ✓ {Path.GetRelativePath(Repo, OutPath)}");
        }
        catch (Exception Ex)
        {
            Skipped++;
            Console.Error.WriteLine($"  ✗ {SafeName}: {Ex.GetType().Name}: {Ex.Message.Split('\n')[0]}");
        }
    }
}

Console.WriteLine();
Console.WriteLine($"done — wrote {Written} static page(s), skipped {Skipped}");
return 0;

static string WrapHtml(string Title, string Body, string Css)
{
    var Display = char.ToUpperInvariant(Title[0]) + Title[1..];
    var Slug = Title.Equals("index", StringComparison.OrdinalIgnoreCase) ? "" : Title;
    return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width,initial-scale=1,viewport-fit=cover">
<base href="/wolfstruckingco.com/">
<title>{{Display}} | Wolfs Trucking Co.</title>
<meta name="description" content="{{Display}} — Wolfs Trucking Co. Three-role logistics platform: freight marketplace, real-time tracking, voice navigation, and a dispatcher you can call.">
<meta name="theme-color" content="#ff6b35">
<meta name="color-scheme" content="light">
<link rel="canonical" href="https://cruzlauroiii.github.io/wolfstruckingco.com/{{Slug}}">
<link rel="icon" type="image/svg+xml" href="/wolfstruckingco.com/icon.svg">
<style>
{{Css}}
</style>
</head>
<body data-prerender-route="{{Slug}}">
<script src="/wolfstruckingco.com/db.js"></script>
<script src="/wolfstruckingco.com/wolfs-interop-shim.js"></script>
{{Body}}
<script src="/wolfstruckingco.com/app/_framework/blazor.webassembly.js" autostart="false"></script>
<script>
Blazor.start({
  loadBootResource: function (type, name, defaultUri, integrity) {
    if (type === 'manifest' || type === 'configuration' || type === 'dotnetjs' || type === 'dotnetwasm' || type === 'assembly' || type === 'pdb' || type === 'globalization' || type === 'manifest') {
      return '/wolfstruckingco.com/app/_framework/' + name;
    }
    return null;
  }
});
</script>
</body>
</html>

""";
}

internal sealed class StubJsRuntime : IJSRuntime
{
    private static readonly Dictionary<string, string> DbCache = LoadDb();
    private static Dictionary<string, string> LoadDb()
    {
        var Result = new Dictionary<string, string>();
        var Path1 = Path.Combine(@"C:\repo\public\wolfstruckingco.com\main", "data", "wolfs-db.jsonl");
        if (!File.Exists(Path1)) { return Result; }
        var Buckets = new Dictionary<string, List<string>>();
        foreach (var Line in File.ReadAllLines(Path1))
        {
            if (string.IsNullOrWhiteSpace(Line)) { continue; }
            using var Doc = System.Text.Json.JsonDocument.Parse(Line);
            if (!Doc.RootElement.TryGetProperty("_store", out var StoreEl)) { continue; }
            var Store = StoreEl.GetString() ?? "";
            if (!Buckets.TryGetValue(Store, out var List1)) { List1 = new(); Buckets[Store] = List1; }
            // Strip the _store field from output so consumers see clean records.
            var Obj = new System.Text.Json.Nodes.JsonObject();
            foreach (var P in Doc.RootElement.EnumerateObject())
            {
                if (P.Name == "_store") { continue; }
                Obj[P.Name] = System.Text.Json.Nodes.JsonNode.Parse(P.Value.GetRawText());
            }
            List1.Add(Obj.ToJsonString());
        }
        foreach (var Kv in Buckets) { Result[Kv.Key] = "[" + string.Join(",", Kv.Value) + "]"; }
        return Result;
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string Identifier, object?[]? Args) => Stub<TValue>(Identifier, Args);
    public ValueTask<TValue> InvokeAsync<TValue>(string Identifier, CancellationToken CancellationToken, object?[]? Args) => Stub<TValue>(Identifier, Args);

    private static ValueTask<TValue> Stub<TValue>(string Identifier, object?[]? Args)
    {
        if (typeof(TValue) == typeof(string))
        {
            if (Identifier.Contains("dbAllJson", StringComparison.OrdinalIgnoreCase))
            {
                var Store = Args?.FirstOrDefault()?.ToString() ?? "";
                var Json = DbCache.TryGetValue(Store, out var V) ? V : "[]";
                return ValueTask.FromResult((TValue)(object)Json);
            }
            return ValueTask.FromResult((TValue)(object)"");
        }
        if (typeof(TValue) == typeof(AuthState))
        {
            return ValueTask.FromResult((TValue)(object)new AuthState(null, null, null));
        }
        if (typeof(TValue) == typeof(WorkerResponse))
        {
            return ValueTask.FromResult((TValue)(object)new WorkerResponse(true, 200, ""));
        }
        return ValueTask.FromResult(default(TValue)!);
    }
}

internal sealed class StubNavigationManager : NavigationManager
{
    public StubNavigationManager() => Initialize("https://localhost/", "https://localhost/");
    protected override void NavigateToCore(string Uri, bool ForceLoad) { }
}
