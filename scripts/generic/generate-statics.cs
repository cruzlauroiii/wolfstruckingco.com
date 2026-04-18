#:property TargetFramework=net11.0
#:property EnableTrimAnalyzer=false
#:project ../../src/SharedUI/SharedUI.csproj
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Scripts;
using SharedUI.Components;
using SharedUI.Services;

AppContext.SetSwitch("System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault", true);

if (args.Length > 0 && args[0].EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && File.Exists(args[0]))
{
    var ConfigDir = Path.GetDirectoryName(Path.GetFullPath(args[0]))!;
    var SpecRepo = Path.GetFullPath(Path.Combine(ConfigDir, "..", ".."));
    args = ["--in-place", SpecRepo];
}

var InPlace = args.Contains("--in-place");
var Positional = args.Where(A => !A.StartsWith("--", StringComparison.Ordinal)).ToArray();
var Repo = Positional.Length > 0 ? Positional[0] : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
var DocsRoot = Path.Combine(Repo, "docs");
if (!Directory.Exists(DocsRoot))
{
    await Console.Error.WriteLineAsync($"docs not found: {DocsRoot}").ConfigureAwait(false);
    return 1;
}
var WriteRoot = InPlace ? DocsRoot : Path.Combine(DocsRoot, "Generated");

var CssPath = Path.Combine(Repo, "src", "SharedUI", "wwwroot", "css", "app.css");
var Css = File.Exists(CssPath) ? await File.ReadAllTextAsync(CssPath).ConfigureAwait(false) : string.Empty;

var Asm = typeof(MainLayout).Assembly;
var Pages = Asm.GetTypes()
    .Where(T => typeof(IComponent).IsAssignableFrom(T) && !T.IsAbstract)
    .Select(T => (Type: T, Routes: T.GetCustomAttributes<RouteAttribute>().Select(R => R.Template).ToArray()))
    .Where(P => P.Routes.Length > 0)
    .ToList();

var Services = new ServiceCollection();
Services.AddSingleton<IJSRuntime>(_ => new StubJsRuntime());
Services.AddSingleton<WolfsInteropService>();
Services.AddSingleton<NavigationManager>(_ => new StubNavigationManager());
Services.AddSingleton<HttpClient>();
Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
Services.AddAuthorizationCore();
Services.AddSingleton<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider, LocalStorageAuthStateProvider>();
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
                    ["ChildContent"] = (RenderFragment)(B => { B.OpenComponent<LayoutView>(0); B.AddComponentParameter(1, "Layout", typeof(MainLayout)); B.AddComponentParameter(2, "ChildContent", (RenderFragment)(C => { C.OpenComponent(0, PageType); C.CloseComponent(); })); B.CloseComponent(); }),
                });
                var Output = await Renderer.RenderComponentAsync<Microsoft.AspNetCore.Components.Authorization.CascadingAuthenticationState>(Params);
                return Output.ToHtmlString();
            });
            var OutDir = string.IsNullOrEmpty(Slug)
                ? WriteRoot
                : Path.Combine(WriteRoot, Slug.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(OutDir);
            var OutPath = Path.Combine(OutDir, "index.html");
            await File.WriteAllTextAsync(OutPath, WrapHtml(SafeName, Html, Css)).ConfigureAwait(false);
            Written++;
        }
#pragma warning disable CA1031 // Catch-all is intentional: prerender is best-effort, log + continue per page
        catch (Exception Ex)
#pragma warning restore CA1031
        {
            Skipped++;
            await Console.Error.WriteLineAsync($"  ✗ {SafeName}: {Ex.GetType().Name}: {Ex.Message.Split('\n')[0]}").ConfigureAwait(false);
        }
    }
}

return 0;

static string WrapHtml(string Title, string Body, string Css)
{
    const string SsoSnippet = "(function(){try{var q=new URLSearchParams(location.search);var sso=q.get('sso');if(sso){var p=sso.toLowerCase();var realEmail=q.get('email');var realSess=q.get('session');localStorage.setItem('wolfs_session',realSess||('sso-'+p+'-'+Date.now()));localStorage.setItem('wolfs_role','user');localStorage.setItem('wolfs_email',realEmail||('demo@'+p+'.example'));var base=location.pathname.replace(/Login\\/?$/,'');location.replace(base+'Marketplace/');}}catch(e){}})();";
    const string HeaderAuthSnippet = "(function(){function paint(){try{var role=localStorage.getItem('wolfs_role');var email=localStorage.getItem('wolfs_email')||'';if(!role){return;}var name=email.split('@')[0]||'user';var actions=document.querySelector('.TopActions');if(!actions){return;}var anchors=actions.querySelectorAll('a');for(var i=0;i<anchors.length;i++){var a=anchors[i];if((a.textContent||'').trim()==='Sign In'){a.outerHTML='<a href=\"Marketplace\">Dashboard</a><button type=\"button\" class=\"LinkBtn\" id=\"WolfsSignOut\">Sign out ('+name+')</button>';break;}}var btn=document.getElementById('WolfsSignOut');if(btn){btn.addEventListener('click',function(){['wolfs_role','wolfs_email','wolfs_session'].forEach(function(k){localStorage.removeItem(k);});location.replace(location.pathname.replace(/[^\\/]+\\/?$/,''));});}}catch(e){}}if(document.readyState==='loading'){document.addEventListener('DOMContentLoaded',paint);}else{paint();}})();";
    var Display = char.ToUpperInvariant(Title[0]) + Title[1..];
    var Slug = Title.Equals("index", StringComparison.OrdinalIgnoreCase) ? string.Empty : Title;
    var HtmlLines = new List<string>
    {
        "<!DOCTYPE html>",
        "<html lang=\"en\">",
        "<head>",
        "<meta charset=\"UTF-8\">",
        "<meta name=\"viewport\" content=\"width=device-width,initial-scale=1,viewport-fit=cover\">",
        "<base href=\"/wolfstruckingco.com/\">",
        $"<title>{Display} | Wolfs Trucking Co.</title>",
        $"<meta name=\"description\" content=\"{Display} — Wolfs Trucking Co. Three-role logistics platform: freight marketplace, real-time tracking, voice navigation, and a dispatcher you can call.\">",
        "<meta name=\"theme-color\" content=\"#ff6b35\">",
        "<meta name=\"color-scheme\" content=\"light\">",
        $"<link rel=\"canonical\" href=\"https://cruzlauroiii.github.io/wolfstruckingco.com/{Slug}\">",
        "<link rel=\"icon\" type=\"image/svg+xml\" href=\"/wolfstruckingco.com/icon.svg\">",
        "<style>",
        Css,
        "</style>",
        "</head>",
        $"<body data-prerender-route=\"{Slug}\">",
        "<script>",
        SsoSnippet,
        "</script>",
        "<div id=\"app\">",
        Body,
        "</div>",
        "<script>",
        HeaderAuthSnippet,
        "</script>",
        "<script src=\"/wolfstruckingco.com/app/_framework/blazor.webassembly.js\" autostart=\"true\"></script>",
        "<script>",
        "Blazor.start({",
        "  loadBootResource: function (type, name, defaultUri, integrity) {",
        "    if (type === 'manifest' || type === 'configuration' || type === 'dotnetjs' || type === 'dotnetwasm' || type === 'assembly' || type === 'pdb' || type === 'globalization' || type === 'manifest') {",
        "      return '/wolfstruckingco.com/app/_framework/' + name;",
        "    }",
        "    return null;",
        "  }",
        "});",
        "</script>",
        "</body>",
        "</html>",
        string.Empty,
    };
    return string.Join("\n", HtmlLines);
}

namespace Scripts
{
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
            var Store = StoreEl.GetString() ?? string.Empty;
            if (!Buckets.TryGetValue(Store, out var List1)) { List1 = []; Buckets[Store] = List1; }
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

#pragma warning disable IDE1006 // Override/interface impl param names must match base (camelCase) — S927/RCS1168 require this
    public ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(string identifier, object?[]? args) => Stub<TValue>(identifier, args);
    public ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => Stub<TValue>(identifier, args);
#pragma warning restore IDE1006

    private static ValueTask<TValue> Stub<TValue>(string Identifier, object?[]? Args)
    {
        if (typeof(TValue) == typeof(string))
        {
            if (Identifier.Contains("dbAllJson", StringComparison.OrdinalIgnoreCase))
            {
                var Store = Args?.FirstOrDefault()?.ToString() ?? string.Empty;
                var Json = DbCache.TryGetValue(Store, out var V) ? V : "[]";
                return ValueTask.FromResult((TValue)(object)Json);
            }
            return ValueTask.FromResult((TValue)(object)string.Empty);
        }
        return typeof(TValue) == typeof(AuthState) ? ValueTask.FromResult((TValue)(object)new AuthState(null, null, null))
            : typeof(TValue) == typeof(WorkerResponse) ? ValueTask.FromResult((TValue)(object)new WorkerResponse(true, 200, string.Empty))
            : ValueTask.FromResult(default(TValue)!);
    }
}

internal sealed class StubNavigationManager : NavigationManager
{
    public StubNavigationManager()
    {
        Initialize("https://localhost/", "https://localhost/");
    }

#pragma warning disable IDE1006 // Override param names must match base (camelCase) — S927/RCS1168 require this
    protected override void NavigateToCore(string uri, bool forceLoad) => throw new NotSupportedException("static prerender does not navigate");
#pragma warning restore IDE1006
}
}
