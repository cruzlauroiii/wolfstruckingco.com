#:property TargetFramework=net11.0-windows10.0.19041.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
#:project ../../src/SharedUI/SharedUI.csproj

// run-crud-pipeline.cs — drives 84 scenes against the real route pages.
// 1. Reset scene-only stores (no seeding, no demo, no relative captions).
// 2. For each scene:
//    a. Append the row(s) the scene's CRUD action would produce to
//       wolfs-db.jsonl — written to the real platform stores
//       (users, applicants, workers, listings, jobs, purchases, schedules,
//       timesheets, charges, audit). Permission gate enforced.
//    b. Re-prerender the scene's real route page (/SignUp/, /Marketplace/,
//       /HiringHall/, etc.). The page reads the latest DB state via the
//       existing components — no banners, no captions, no SceneAuditPage.
//    c. Drive chrome to navigate to the real URL.
//    d. For form-input scenes, type the actor's REAL data (admin's real
//       email, applicant's real name) into the page's actual <input>/<textarea>
//       elements. Screenshot.
// 3. OCR every PNG. Write ocr.json + frame-references.md.

using System.Net.Http;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using SharedUI.Components;
using SharedUI.Services;

using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;
using Windows.Storage.Streams;

AppContext.SetSwitch("System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault", true);

const string Repo = @"C:\repo\public\wolfstruckingco.com\main";
const string Base = "https://localhost:8443/wolfstruckingco.com";
var DbPath = Path.Combine(Repo, "data", "wolfs-db.jsonl");
var ScenesJsonPath = Path.Combine(Repo, "docs", "videos", "scenes-final.json");
var FrameDir = Path.Combine(Path.GetTempPath(), "wolfs-video", "frames");
Directory.CreateDirectory(FrameDir);
foreach (var F in Directory.GetFiles(FrameDir, "*.png")) { File.Delete(F); }

// 1. Reset scene-effect stores. Keep the original 35 base records intact.
ResetSceneRows(DbPath);
Console.WriteLine($"reset scene-effect stores; db = {File.ReadAllLines(DbPath).Length} base rows");

var Scenes = JsonDocument.Parse(File.ReadAllText(ScenesJsonPath)).RootElement.EnumerateArray().ToArray();
Console.WriteLine($"loaded {Scenes.Length} scenes");
var TotalScenes = Scenes.Length;

// Real actor data — emails, names, locations — sourced from the platform's
// existing workers/applicants store, not invented for this pipeline.
var Drivers = new (string Email, string Name, int Years, string Location, string Role)[]
{
    ("driver1@wolfstruckingco.com", "Liu Wei",                12, "Shanghai CN",      "role_china"),
    ("driver2@wolfstruckingco.com", "Pedro Reyes",            15, "San Pedro CA",     "role_drayage"),
    ("driver3@wolfstruckingco.com", "Maya + Tom (team)",      18, "Phoenix AZ",       "role_team"),
    ("driver4@wolfstruckingco.com", "Jordan Vega",            10, "Wilmington NC",    "role_finalmile"),
};
const string AdminEmail    = "admin@wolfstruckingco.com";
const string EmployerEmail = "wei@shanghai-intl.example";
const string BuyerEmail    = "sam@buyers.example";

// Renderer setup (same DI as generate-statics.cs).
var Services = new ServiceCollection();
Services.AddSingleton<IJSRuntime, StubJsRuntime>();
Services.AddSingleton<WolfsInteropService>();
Services.AddSingleton<VoiceChatService>();
Services.AddSingleton<NavigationManager, StubNavigationManager>();
Services.AddSingleton<HttpClient>();
Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
var Provider = Services.BuildServiceProvider();
await using var Renderer = new HtmlRenderer(Provider, Provider.GetRequiredService<ILoggerFactory>());

var CssPath = Path.Combine(Repo, "src", "SharedUI", "wwwroot", "css", "app.css");
var Css = File.Exists(CssPath) ? File.ReadAllText(CssPath) : "";

var Asm = typeof(MainLayout).Assembly;
var PagesByRoute = Asm.GetTypes()
    .Where(T => typeof(IComponent).IsAssignableFrom(T) && !T.IsAbstract)
    .SelectMany(T => T.GetCustomAttributes<RouteAttribute>().Select(R => (Type: T, Route: R.Template)))
    .ToDictionary(P => P.Route == "/" ? "/" : "/" + P.Route.Trim('/') + "/", P => P.Type);

// Hide the duplicate page-level H1 + .PageHeader since the title now lives in
// the TopBar brand. Keep .Stage padding-top tight so OCR captures top content.
var ExtraCss = ".Stage>h1:first-child,.PageHeader{display:none!important}.TopBar .Brand{flex:1;min-width:0}.TopBar .BrandText{display:inline-block;max-width:100%;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;font-weight:700}";

string Wrap(string Slug, string Body) => $$"""
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width,initial-scale=1,viewport-fit=cover">
<base href="/wolfstruckingco.com/">
<title>{{Slug}} | Wolfs Trucking Co.</title>
<style>{{Css}}{{ExtraCss}}</style>
</head>
<body data-prerender-route="{{Slug}}">
{{Body}}
</body>
</html>
""";

async Task<string> RenderRouteHtml(Type PageType)
{
    return await Renderer.Dispatcher.InvokeAsync(async () =>
    {
        var Params = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            ["Layout"] = typeof(MainLayout),
            ["ChildContent"] = (RenderFragment)(b => { b.OpenComponent(0, PageType); b.CloseComponent(); }),
        });
        var Output = await Renderer.RenderComponentAsync<LayoutView>(Params);
        return Output.ToHtmlString();
    });
}

var Wolfs = Provider.GetRequiredService<WolfsInteropService>();

// Chrome CDP setup.
using var Http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
var TargetsJson = JsonDocument.Parse(await Http.GetStringAsync("http://127.0.0.1:9222/json"));
JsonElement PageT = default;
foreach (var T in TargetsJson.RootElement.EnumerateArray())
{
    if (T.GetProperty("type").GetString() == "page") { PageT = T; break; }
}
var WsUrl = PageT.GetProperty("webSocketDebuggerUrl").GetString()!;
using var Ws = new ClientWebSocket();
await Ws.ConnectAsync(new Uri(WsUrl), default);

int CmdId = 1;
async Task<JsonNode> Send(string Method, object? Params = null)
{
    var Id = CmdId++;
    var Msg = new JsonObject { ["id"] = Id, ["method"] = Method };
    if (Params != null) { Msg["params"] = JsonNode.Parse(JsonSerializer.Serialize(Params)); }
    var Bytes = Encoding.UTF8.GetBytes(Msg.ToJsonString());
    await Ws.SendAsync(Bytes, WebSocketMessageType.Text, true, default);
    var Buf = new byte[1 << 22];
    while (true)
    {
        var Sb = new StringBuilder();
        WebSocketReceiveResult R;
        do { R = await Ws.ReceiveAsync(Buf, default); Sb.Append(Encoding.UTF8.GetString(Buf, 0, R.Count)); } while (!R.EndOfMessage);
        var Doc = JsonNode.Parse(Sb.ToString())!;
        if (Doc["id"]?.GetValue<int>() == Id) { return Doc; }
    }
}

await Send("Page.enable");
await Send("Runtime.enable");
await Send("Network.enable");
await Send("Network.setCacheDisabled", new { cacheDisabled = true });
await Send("Emulation.setDeviceMetricsOverride", new { width = 414, height = 896, deviceScaleFactor = 2, mobile = true });

var SceneRoutes = new List<string>();
var RouteOrdinals = new Dictionary<string, int>();
var FormFills = new List<List<(string Selector, string Value)>>();
string StickyDriverEmail = Drivers[0].Email;

for (int N = 1; N <= TotalScenes; N++)
{
    var Scene = Scenes[N - 1];
    var Pad = N.ToString("000");
    var Target = Scene.GetProperty("target").GetString() ?? "";
    var Narration = Scene.GetProperty("narration").GetString() ?? "";
    var Route = "";
    try
    {
        Route = new Uri(Target).AbsolutePath;
        const string Prefix = "/wolfstruckingco.com";
        if (Route.StartsWith(Prefix, StringComparison.Ordinal)) { Route = Route[Prefix.Length..]; }
        if (string.IsNullOrEmpty(Route)) { Route = "/"; }
    }
    catch { }
    SceneRoutes.Add(Route);
    if (!RouteOrdinals.TryGetValue(Route, out var RouteOrd)) { RouteOrd = 0; }
    RouteOrdinals[Route] = RouteOrd + 1;

    var Inputs = new List<(string Selector, string Value)>();
    var DirectActor = ResolveActor(Route, Narration, Drivers, AdminEmail, EmployerEmail, BuyerEmail);
    var ExplicitDriverMatch = Drivers.Any(D => DirectActor == D.Email)
        && (Narration.ToLowerInvariant().Contains("driver from") || Narration.ToLowerInvariant().Contains("team driver") || Narration.ToLowerInvariant().Contains("driver in") || Narration.ToLowerInvariant().Contains("liu") || Narration.ToLowerInvariant().Contains("pedro") || Narration.ToLowerInvariant().Contains("maya") || Narration.ToLowerInvariant().Contains("jordan"));
    var DriverContextRoute = Route is "/Map/" or "/Track/" or "/Dispatcher/" or "/Applicant/" or "/Documents/" or "/Apply/" or "/Itinerary/" or "/Dashboard/" or "/Job/Offer/";
    var ResolvedActor = DriverContextRoute && !ExplicitDriverMatch && Drivers.Any(D => StickyDriverEmail == D.Email)
        ? StickyDriverEmail
        : DirectActor;
    if (Drivers.Any(D => DirectActor == D.Email) && ExplicitDriverMatch) { StickyDriverEmail = DirectActor; }
    await PerformSceneCrud(N, Route, Narration, RouteOrd, Wolfs, Drivers, AdminEmail, EmployerEmail, BuyerEmail, ResolvedActor, Inputs);
    FormFills.Add(Inputs);

    if (!PagesByRoute.TryGetValue(Route, out var PageType))
    {
        Console.Error.WriteLine($"  ✗ {Pad} no page registered at route {Route}");
        continue;
    }
    SharedUI.Services.WolfsRenderContext.CurrentRoute = Route;
    SharedUI.Services.WolfsRenderContext.CurrentStep = RouteOrd;
    SharedUI.Services.WolfsRenderContext.MenuOpen = Narration.Contains("taps Apply", StringComparison.Ordinal) || Narration.Contains("taps Sell", StringComparison.Ordinal);
    StubJsRuntime.CurrentActorEmail = ResolvedActor;
    StubJsRuntime.CurrentActorRole =
        ResolvedActor.StartsWith("driver", StringComparison.Ordinal) ? "driver" :
        ResolvedActor.StartsWith("admin", StringComparison.Ordinal) ? "admin" :
        ResolvedActor.StartsWith("dispatcher", StringComparison.Ordinal) ? "dispatcher" :
        ResolvedActor.StartsWith("wei", StringComparison.Ordinal) ? "employer" :
        ResolvedActor.StartsWith("sam", StringComparison.Ordinal) ? "buyer" :
        "user";
    var Html = await RenderRouteHtml(PageType);
    var Slug = Route.Trim('/');
    var OutDir = string.IsNullOrEmpty(Slug) ? Path.Combine(Repo, "docs") : Path.Combine(Repo, "docs", Slug.Replace('/', Path.DirectorySeparatorChar));
    Directory.CreateDirectory(OutDir);
    File.WriteAllText(Path.Combine(OutDir, "index.html"), Wrap(Slug, Html));

    var Url = $"{Base}{Route}?cb={Pad}";
    await Send("Page.navigate", new { url = "about:blank" });
    await Task.Delay(120);
    await Send("Network.clearBrowserCache");
    await Send("Page.navigate", new { url = Url });
    await Task.Delay(1500);

    if (Inputs.Count > 0)
    {
        // Native CDP form-fill — single query + focus + insert per scene.
        var DocResp = await Send("DOM.getDocument");
        var RootId = DocResp["result"]?["root"]?["nodeId"]?.GetValue<int>() ?? 0;
        foreach (var (Selector, Value) in Inputs)
        {
            if (RootId == 0) { break; }
            var Sel = await Send("DOM.querySelector", new { nodeId = RootId, selector = Selector });
            var NodeId = Sel["result"]?["nodeId"]?.GetValue<int>() ?? 0;
            if (NodeId == 0) { continue; }
            try { await Send("DOM.focus", new { nodeId = NodeId }); } catch { }
            await Send("Input.insertText", new { text = Value });
        }
    }

    var Shot = await Send("Page.captureScreenshot", new { format = "png" });
    var B64 = Shot["result"]?["data"]?.GetValue<string>();
    if (B64 != null)
    {
        var FramePath = Path.Combine(FrameDir, Pad + ".png");
        File.WriteAllBytes(FramePath, Convert.FromBase64String(B64));
        Console.WriteLine($"  ✓ {Pad} {Route}");
    }
}
await Ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", default);

// 3. OCR.
var Engine = OcrEngine.TryCreateFromLanguage(new Language("en-US"))
             ?? OcrEngine.TryCreateFromUserProfileLanguages()
             ?? throw new InvalidOperationException("no OCR engine available");
var OcrMap = new JsonObject();
foreach (var File1 in Directory.GetFiles(FrameDir, "*.png").OrderBy(F => F))
{
    var Name = Path.GetFileNameWithoutExtension(File1);
    var Sf = await StorageFile.GetFileFromPathAsync(File1);
    using var Stream = await Sf.OpenAsync(FileAccessMode.Read);
    var Decoder = await BitmapDecoder.CreateAsync(Stream);
    var Bitmap = await Decoder.GetSoftwareBitmapAsync();
    var Result = await Engine.RecognizeAsync(Bitmap);
    var Text = (Result.Text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
    OcrMap[Name] = Text;
}
File.WriteAllText(Path.Combine(Repo, "docs", "videos", "ocr.json"), OcrMap.ToJsonString(new() { WriteIndented = true }));

var Strs = OcrMap.Select(K => K.Value!.GetValue<string>()).ToList();
var Distinct = Strs.Distinct().Count();
Console.WriteLine($"\nOCR uniqueness: {Distinct}/{TotalScenes}");

// 4. frame-references.md.
var Sb2 = new StringBuilder();
Sb2.AppendLine($"# Wolfs Trucking — Frame References ({TotalScenes} atomic CRUD scenes)");
Sb2.AppendLine();
Sb2.AppendLine($"Every scene is a real user CRUD action on its real route page (`/SignUp/`, `/Login/`, `/Applicant/`, etc.). Non-CRUD observational narrations (page opens, system events) are excluded. CRUD goes through `WolfsInteropService.DbPutAsync` against the platform's real stores (`applicants`, `listings`, `schedules`, `timesheets`, `charges`, `audit`) with permission gate. Form inputs receive the actor's real data (admin email, applicant name, etc.) — there is no caption, banner, or scene wrapper.");
Sb2.AppendLine();
Sb2.AppendLine("| # | Frame | Route | Narration | Extracted Text (OCR) |");
Sb2.AppendLine("|---|---|---|---|---|");
int Idx2 = 0;
foreach (var Kv in OcrMap)
{
    Idx2++;
    var Pad = Kv.Key;
    var Origin = Idx2 <= SceneRoutes.Count ? SceneRoutes[Idx2 - 1] : "";
    var Narr = Idx2 <= Scenes.Length ? Scenes[Idx2 - 1].GetProperty("narration").GetString() ?? "" : "";
    var Text = (Kv.Value!.GetValue<string>() ?? "").Replace("|", "\\|").Trim();
    if (Text.Length > 240) { Text = Text[..240] + "…"; }
    if (Narr.Length > 160) { Narr = Narr[..160] + "…"; }
    Sb2.AppendLine($"| {Pad} | ![{Pad}](file:///C:/Users/user1/AppData/Local/Temp/wolfs-video/frames/{Pad}.png) | `{Origin}` | {Narr} | {Text} |");
}
Sb2.AppendLine();
Sb2.AppendLine($"OCR uniqueness: **{Distinct}/{TotalScenes}**.");
File.WriteAllText(Path.Combine(Repo, "docs", "videos", "frame-references.md"), Sb2.ToString());
Console.WriteLine($"wrote frame-references.md");
return 0;

static void ResetSceneRows(string DbPath)
{
    var Lines = File.ReadAllLines(DbPath)
        .Where(L => !L.Contains("\"id\":\"aud_scene_") &&
                    !L.Contains("\"id\":\"sch_scene_") &&
                    !L.Contains("\"id\":\"lst_scene_") &&
                    !L.Contains("\"_store\":\"users\"") &&
                    !L.Contains("\"_store\":\"applicants\""))
        .ToArray();
    File.WriteAllLines(DbPath, Lines);
}

static string ResolveActor(
    string Route, string Narration,
    (string Email, string Name, int Years, string Location, string Role)[] Drivers,
    string AdminEmail, string EmployerEmail, string BuyerEmail)
{
    var L = Narration.ToLowerInvariant();
    bool DriverRoute = Route is "/Applicant/" or "/Interview/" or "/Documents/" or "/Map/" or "/Itinerary/" or "/Apply/" or "/Dashboard/";
    string DefaultActor = Route switch
    {
        "/Documents/" => Drivers[0].Email,
        "/Applicant/" => Drivers[0].Email,
        "/Interview/" => Drivers[0].Email,
        "/Map/" => Drivers[0].Email,
        "/Itinerary/" => Drivers[0].Email,
        "/Apply/" => Drivers[0].Email,
        "/Dashboard/" => Drivers[0].Email,
        "/Employer/Post/" => EmployerEmail,
        "/Marketplace/" => EmployerEmail,
        "/Dispatcher/" => "dispatcher@wolfstruckingco.com",
        _ => AdminEmail,
    };
    return
        L.Contains("driver from china") || L.Contains("driver one") || L.Contains("liu wei") ? Drivers[0].Email :
        L.Contains("driver from los angeles") || L.Contains("driver two") || L.Contains("pedro") ? Drivers[1].Email :
        L.Contains("team driver in phoenix") || L.Contains("driver three") || L.Contains("maya") ? Drivers[2].Email :
        L.Contains("driver in wilmington") || L.Contains("driver four") || L.Contains("jordan") ? Drivers[3].Email :
        DriverRoute ? DefaultActor :
        L.Contains("dispatcher") ? "dispatcher@wolfstruckingco.com" :
        L.Contains("admin ") ? AdminEmail :
        L.Contains("car seller") || L.Contains("seller") || L.Contains("wei ") || L.Contains("employer") ? EmployerEmail :
        L.Contains("car buyer") || L.Contains("buyer") || L.Contains("sam ") ? BuyerEmail :
        DefaultActor;
}

static async Task PerformSceneCrud(
    int N, string Route, string Narration, int RouteOrdinal, WolfsInteropService Wolfs,
    (string Email, string Name, int Years, string Location, string Role)[] Drivers,
    string AdminEmail, string EmployerEmail, string BuyerEmail,
    string Actor,
    List<(string, string)> Inputs)
{

    var Pad = N.ToString("000");
    var At = "2026-04-30T08:" + (N % 60).ToString("00") + ":00";
    var L = Narration.ToLowerInvariant();

    if (Route == "/SignUp/")
    {
        Inputs.Add(("input[type=email]", Actor));
        Inputs.Add(("input[type=password]", new string('•', 12)));
        await Wolfs.DbPutAsync("audit", new JsonObject
        {
            ["id"] = "aud_scene_" + Pad,
            ["scene"] = N,
            ["kind"] = "auth.signup",
            ["subject"] = Narration,
            ["actor"] = Actor,
            ["permission"] = "auth.signup",
            ["op"] = "CREATE",
            ["note"] = Narration,
            ["at"] = At,
        });
        return;
    }
    if (Route == "/Login/")
    {
        Inputs.Add(("input[type=email]", Actor));
        await Wolfs.DbPutAsync("audit", new JsonObject
        {
            ["id"] = "aud_scene_" + Pad,
            ["scene"] = N,
            ["kind"] = "auth.signin",
            ["subject"] = Narration,
            ["actor"] = Actor,
            ["permission"] = "auth.signin",
            ["op"] = "READ",
            ["note"] = Narration,
            ["at"] = At,
        });
        return;
    }
    if (Route == "/Applicant/")
    {
        var ResolvedDriver = Drivers.FirstOrDefault(D => D.Email == Actor);
        await Wolfs.DbPutAsync("applicants", new JsonObject
        {
            ["id"] = "aud_scene_" + Pad,
            ["name"] = ResolvedDriver.Name ?? Actor,
            ["email"] = Actor,
            ["location"] = ResolvedDriver.Location ?? "",
            ["experienceYears"] = ResolvedDriver.Years,
            ["status"] = "pending",
            ["note"] = Narration,
            ["actor"] = Actor,
            ["permission"] = "applicant.write",
        });
        if (L.Contains("license", StringComparison.Ordinal) || L.Contains("scan", StringComparison.Ordinal) || L.Contains("send", StringComparison.Ordinal))
        {
            var IsTeam = Actor == Drivers[2].Email;
            if (IsTeam)
            {
                await Wolfs.DbPutAsync("audit", new JsonObject
                {
                    ["id"] = "aud_scene_" + Pad + "_license_maya",
                    ["scene"] = N,
                    ["kind"] = "documents.upload",
                    ["subject"] = "Driver's license (Maya) — uploaded via chat",
                    ["actor"] = Actor,
                    ["permission"] = "documents.upload",
                    ["op"] = "CREATE",
                    ["note"] = "license-maya.jpg",
                    ["at"] = At,
                });
                await Wolfs.DbPutAsync("audit", new JsonObject
                {
                    ["id"] = "aud_scene_" + Pad + "_license_tom",
                    ["scene"] = N,
                    ["kind"] = "documents.upload",
                    ["subject"] = "Driver's license (Tom) — uploaded via chat",
                    ["actor"] = Actor,
                    ["permission"] = "documents.upload",
                    ["op"] = "CREATE",
                    ["note"] = "license-tom.jpg",
                    ["at"] = At,
                });
            }
            else
            {
                await Wolfs.DbPutAsync("audit", new JsonObject
                {
                    ["id"] = "aud_scene_" + Pad + "_license",
                    ["scene"] = N,
                    ["kind"] = "documents.upload",
                    ["subject"] = "Driver's license — uploaded via chat",
                    ["actor"] = Actor,
                    ["permission"] = "documents.upload",
                    ["op"] = "CREATE",
                    ["note"] = "drivers-license.jpg",
                    ["at"] = At,
                });
            }
        }
        if (L.Contains("scan", StringComparison.Ordinal) || L.Contains("send", StringComparison.Ordinal) || L.Contains("pass", StringComparison.Ordinal) || L.Contains("cert", StringComparison.Ordinal) || L.Contains("paper", StringComparison.Ordinal))
        {
            var (BadgeSubject, BadgeNote) = Actor == Drivers[0].Email ? ("China export driver pass — uploaded via chat", "china-export-driver-pass.pdf")
                : Actor == Drivers[1].Email ? ("TWIC port pass + drayage card — uploaded via chat", "twic-and-drayage.pdf")
                : Actor == Drivers[2].Email ? ("Team-driver papers — uploaded via chat", "team-driver-cert.pdf")
                : ("Auto-handling cert — uploaded via chat", "auto-handling-cert.pdf");
            await Wolfs.DbPutAsync("audit", new JsonObject
            {
                ["id"] = "aud_scene_" + Pad + "_badge",
                ["scene"] = N,
                ["kind"] = "documents.upload",
                ["subject"] = BadgeSubject,
                ["actor"] = Actor,
                ["permission"] = "documents.upload",
                ["op"] = "CREATE",
                ["note"] = BadgeNote,
                ["at"] = At,
            });
        }
        return;
    }
    if (Route == "/Interview/")
    {
        await Wolfs.DbPutAsync("audit", new JsonObject
        {
            ["id"] = "aud_scene_" + Pad,
            ["scene"] = N,
            ["kind"] = "interview.answer",
            ["subject"] = Narration,
            ["actor"] = Actor,
            ["permission"] = "interview.write",
            ["op"] = "UPDATE",
            ["note"] = Narration,
            ["at"] = At,
        });
        return;
    }
    if (Route == "/Documents/")
    {
        await Wolfs.DbPutAsync("audit", new JsonObject
        {
            ["id"] = "aud_scene_" + Pad,
            ["scene"] = N,
            ["kind"] = "documents.upload",
            ["subject"] = Narration,
            ["actor"] = Actor,
            ["permission"] = "documents.upload",
            ["op"] = "CREATE",
            ["note"] = Narration,
            ["at"] = At,
        });
        return;
    }
    if (Route == "/HiringHall/")
    {
        bool BatchApprove = L.Contains("approve all") || L.Contains("hired at the same time") || L.Contains("approves all") || L.Contains("all four drivers are hired");
        if (BatchApprove)
        {
            for (int Di = 0; Di < Drivers.Length; Di++)
            {
                var D = Drivers[Di];
                await Wolfs.DbPutAsync("applicants", new JsonObject
                {
                    ["id"] = "aud_scene_" + Pad + "_" + Di,
                    ["name"] = D.Name,
                    ["email"] = D.Email,
                    ["location"] = D.Location,
                    ["experienceYears"] = D.Years,
                    ["status"] = "approved",
                    ["note"] = Narration,
                    ["actor"] = AdminEmail,
                    ["permission"] = "applicant.approve",
                });
            }
            return;
        }
        var TargetDriver = Drivers.FirstOrDefault(D => L.Contains(D.Name.ToLowerInvariant().Split(' ')[0])) is { Email: var E, Name: var Nm } && E is not null
            ? (Email: E, Name: Nm, Loc: Drivers.First(X => X.Email == E).Location, Yrs: Drivers.First(X => X.Email == E).Years)
            : (Email: Drivers[0].Email, Name: Drivers[0].Name, Loc: Drivers[0].Location, Yrs: Drivers[0].Years);
        await Wolfs.DbPutAsync("applicants", new JsonObject
        {
            ["id"] = "aud_scene_" + Pad,
            ["name"] = TargetDriver.Name,
            ["email"] = TargetDriver.Email,
            ["location"] = TargetDriver.Loc,
            ["experienceYears"] = TargetDriver.Yrs,
            ["status"] = L.Contains("approves") || L.Contains("approved") ? "approved" : "pending",
            ["note"] = Narration,
            ["actor"] = AdminEmail,
            ["permission"] = "applicant.approve",
        });
        return;
    }
    if (Route == "/Marketplace/")
    {
        await Wolfs.DbPutAsync("listings", new JsonObject
        {
            ["id"] = "lst_scene_byd",
            ["title"] = "BYD Han EV — pickup from Shanghai factory",
            ["description"] = "Brand-new BYD Han EV. Picked up directly from the BYD Hefei factory and delivered to your door. Pay on delivery.",
            ["price"] = 48500.00,
            ["sellerEmail"] = EmployerEmail,
            ["status"] = L.Contains("close") || L.Contains("archive") ? "closed" : "active",
            ["createdAt"] = At,
            ["actor"] = L.Contains("sam") || L.Contains("buyer") || L.Contains("buy") ? BuyerEmail : EmployerEmail,
            ["permission"] = L.Contains("sam") || L.Contains("buyer") || L.Contains("buy") ? "purchase.write" : "listing.write",
        });
        return;
    }
    if (Route == "/Employer/Post/")
    {
        await Wolfs.DbPutAsync("audit", new JsonObject
        {
            ["id"] = "aud_scene_" + Pad,
            ["scene"] = N,
            ["kind"] = "listing.config",
            ["subject"] = Narration,
            ["actor"] = EmployerEmail,
            ["permission"] = "listing.write",
            ["op"] = "UPDATE",
            ["note"] = Narration,
            ["at"] = At,
        });
        return;
    }
    if (Route == "/Schedule/")
    {
        await Wolfs.DbPutAsync("schedules", new JsonObject
        {
            ["id"] = "sch_scene_" + Pad,
            ["leg"] = "leg3_recomputed",
            ["from"] = "Phoenix AZ",
            ["to"] = "Memphis TN relay",
            ["mileage"] = 1480,
            ["driveHours"] = 22,
            ["startsAt"] = "2026-06-12T18:30:00",
            ["endsAt"] = "2026-06-14T17:00:00",
            ["graceMinutes"] = 240,
            ["recomputed"] = true,
            ["recomputeReason"] = "Upstream leg 2 delayed +90 min by I-10 crash; team-driver HOS still fits",
            ["actor"] = "dispatcher@wolfstruckingco.com",
            ["permission"] = "schedule.recompute",
        });
        return;
    }
    if (Route == "/Map/")
    {
        var TurnText = MapTurnText(L);
        var DriverLeg = DriverLegFromActor(Actor, Drivers);
        await Wolfs.DbPutAsync("audit", new JsonObject
        {
            ["id"] = "aud_scene_" + Pad,
            ["scene"] = N,
            ["kind"] = "nav.read",
            ["subject"] = TurnText,
            ["actor"] = Actor,
            ["permission"] = "nav.read",
            ["op"] = "READ",
            ["originName"] = DriverLeg.From,
            ["destName"] = DriverLeg.To,
            ["distance"] = DriverLeg.Distance,
            ["etaMinutes"] = Math.Max(15, ExtractInt(Narration, "mi") * 60 / 60),
            ["step1"] = TurnText,
            ["step1voice"] = TurnText,
            ["note"] = Narration,
            ["at"] = At,
        });
        return;
    }
    if (Route == "/Itinerary/")
    {
        var WorkerEmail = Actor.Contains("driver", StringComparison.Ordinal) ? Actor : Drivers[0].Email;
        await Wolfs.DbPutAsync("timesheets", new JsonObject
        {
            ["id"] = "aud_scene_" + Pad,
            ["workerEmail"] = WorkerEmail,
            ["jobId"] = "job_byd",
            ["leg"] = "leg" + N,
            ["status"] = "completed",
            ["earnings"] = ExtractInt(Narration, "$"),
            ["onTime"] = !L.Contains("delay"),
            ["startsAt"] = At,
            ["endsAt"] = "2026-04-30T09:" + (N % 60).ToString("00") + ":00",
            ["note"] = Narration,
            ["actor"] = WorkerEmail,
            ["permission"] = "itinerary.write",
        });
        return;
    }
    if (Route == "/Track/")
    {
        var (Status, Location) = TrackStatus(L);
        await Wolfs.DbPutAsync("audit", new JsonObject
        {
            ["id"] = "aud_scene_" + Pad,
            ["scene"] = N,
            ["kind"] = "track.update",
            ["subject"] = Status,
            ["actor"] = AdminEmail,
            ["permission"] = "track.read",
            ["op"] = "READ",
            ["note"] = Location,
            ["at"] = At,
        });
        return;
    }
    if (Route == "/Investors/KPI/")
    {
        var (KpiAmount, KpiKind, KpiTerms) = KpiEntry(L, Narration);
        var KpiAt = new DateTime(2026, 6, 15, 18, 0, 0, DateTimeKind.Local).AddMinutes(N).ToString("yyyy-MM-ddTHH:mm:ss");
        await Wolfs.DbPutAsync("charges", new JsonObject
        {
            ["id"] = "aud_scene_" + Pad,
            ["scene"] = N,
            ["amount"] = KpiAmount,
            ["kind"] = KpiKind,
            ["payer"] = "platform",
            ["terms"] = KpiTerms,
            ["at"] = KpiAt,
            ["debt"] = false,
            ["actor"] = AdminEmail,
            ["permission"] = "kpi.read",
        });
        return;
    }
    if (Route == "/Dispatcher/")
    {
        var Permission =
            L.Contains("schedule") ? "schedule.recompute" :
            L.Contains("nav") || L.Contains("reroute") ? "nav.read" :
            L.Contains("itinerary") || L.Contains("delivery") ? "itinerary.write" :
            L.Contains("track") ? "track.read" :
            L.Contains("listing") || L.Contains("close") ? "listing.write" :
            L.Contains("purchase") ? "purchase.write" :
            "audit.write";
        var OnBehalf =
            L.Contains("driver from china") || L.Contains("driver 1") || L.Contains("liu wei") ? Drivers[0].Email :
            L.Contains("driver from los angeles") || L.Contains("driver 2") || L.Contains("pedro") ? Drivers[1].Email :
            L.Contains("team driver in phoenix") || L.Contains("driver 3") || L.Contains("maya") ? Drivers[2].Email :
            L.Contains("driver in wilmington") || L.Contains("driver 4") || L.Contains("jordan") ? Drivers[3].Email :
            L.Contains("buyer") || L.Contains("sam") ? BuyerEmail :
            Drivers.Any(D => Actor == D.Email) ? Actor :
            Drivers[0].Email;
        await Wolfs.DbPutAsync("audit", new JsonObject
        {
            ["id"] = "aud_scene_" + Pad,
            ["scene"] = N,
            ["kind"] = "dispatcher.action",
            ["subject"] = Narration,
            ["actor"] = "dispatcher@wolfstruckingco.com",
            ["permission"] = Permission,
            ["op"] = "UPDATE",
            ["onBehalfOf"] = OnBehalf,
            ["note"] = Narration,
            ["at"] = At,
        });
        return;
    }
}

static (double Amount, string Kind, string Terms) KpiEntry(string Lower, string Narration)
{
    if (Lower.Contains("all four drivers were paid", StringComparison.Ordinal)) { return (2520, "driver_payouts_total", "All four drivers paid · total $2,520"); }
    if (Lower.Contains("driver from china was paid", StringComparison.Ordinal)) { return (320, "driver_d1_paid", "Driver 1 — Liu Wei · leg 1 BYD→Shanghai port"); }
    if (Lower.Contains("driver from los angeles was paid", StringComparison.Ordinal)) { return (420, "driver_d2_paid", "Driver 2 — Pedro Reyes · leg 2 LA port→Phoenix"); }
    if (Lower.Contains("team driver in phoenix was paid", StringComparison.Ordinal)) { return (1180, "driver_d3_paid", "Driver 3 — Maya + Tom · leg 3 Phoenix→Memphis"); }
    if (Lower.Contains("driver in wilmington was paid", StringComparison.Ordinal)) { return (600, "driver_d4_paid", "Driver 4 — Jordan Vega · leg 4 Memphis→Wilmington"); }
    if (Lower.Contains("paid back for the factory", StringComparison.Ordinal)) { return (18000, "factory_reimbursement", "Driver 1 reimbursed for factory cash"); }
    if (Lower.Contains("all shipping costs", StringComparison.Ordinal)) { return (1115, "shipping_paid", "Ocean freight + ISF + harbor + processing fees paid"); }
    if (Lower.Contains("customs fees", StringComparison.Ordinal)) { return (26312, "customs_paid", "Section 301 + base tariff + HMF + MPF paid"); }
    if (Lower.Contains("buyer paid in full", StringComparison.Ordinal)) { return (48500, "buyer_payment", "Sam Carter · pay-on-delivery cleared at door"); }
    if (Lower.Contains("platform earned", StringComparison.Ordinal)) { return (3635, "platform_fee", "Platform commission · 7.5% of cleared revenue"); }
    if (Lower.Contains("every delivery was on time", StringComparison.Ordinal)) { return (0, "otp_metric", "100% on-time across all 4 legs"); }
    if (Lower.Contains("every payment cleared", StringComparison.Ordinal)) { return (0, "clearance_metric", "Zero failed transactions across all rails"); }
    if (Lower.Contains("admin opens", StringComparison.Ordinal)) { return (0, "view", "Admin opened KPI dashboard"); }
    return (0, "kpi_event", Narration);
}

static string MapTurnText(string Lower)
{
    var Voice = Lower.IndexOf("voice says:", StringComparison.Ordinal);
    if (Voice >= 0)
    {
        var Rest = Lower[(Voice + "voice says:".Length)..].Trim().TrimEnd('.');
        if (Rest.Length > 0) { return char.ToUpperInvariant(Rest[0]) + Rest[1..] + "."; }
    }
    if (Lower.Contains("starts the map", StringComparison.Ordinal)) { return "Starting navigation. Continue to the highway."; }
    if (Lower.Contains("arrive", StringComparison.Ordinal)) { return "You have arrived."; }
    return "Continue on the highway.";
}

static (string Status, string Location) TrackStatus(string Lower)
{
    if (Lower.Contains("leaves shanghai", StringComparison.Ordinal)) { return ("Departed Shanghai", "Shanghai Yangshan port"); }
    if (Lower.Contains("halfway", StringComparison.Ordinal)) { return ("Mid-Pacific", "Pacific Ocean — halfway crossing"); }
    if (Lower.Contains("crosses the ocean", StringComparison.Ordinal) || Lower.Contains("watches the ship", StringComparison.Ordinal)) { return ("Ocean transit", "Pacific Ocean"); }
    if (Lower.Contains("arrives at los angeles", StringComparison.Ordinal)) { return ("Arrived at Port of LA", "Port of Los Angeles"); }
    if (Lower.Contains("congestion", StringComparison.Ordinal) || Lower.Contains("traffic stops", StringComparison.Ordinal) || Lower.Contains("crash", StringComparison.Ordinal)) { return ("GPS-detected congestion", "I-10 East mile 78 — heavy traffic, ETA auto-recomputed"); }
    if (Lower.Contains("delivered", StringComparison.Ordinal)) { return ("Delivered", "1418 Oak Street, Wilmington NC"); }
    return ("In transit", "Live shipment update");
}

static (string From, string To, string Distance) DriverLegFromActor(string Actor, (string Email, string Name, int Years, string Location, string Role)[] Drivers)
{
    if (Actor == Drivers[0].Email) { return ("BYD Hefei plant", "Shanghai Yangshan port", "480 mi"); }
    if (Actor == Drivers[1].Email) { return ("Port of LA", "Phoenix AZ relay", "370 mi"); }
    if (Actor == Drivers[2].Email) { return ("Phoenix AZ", "Memphis TN relay", "1,480 mi"); }
    if (Actor == Drivers[3].Email) { return ("Memphis TN", "1418 Oak St, Wilmington NC", "875 mi"); }
    return ("Origin", "Destination", "—");
}

static string ExtractFrom(string S)
{
    var Idx = S.IndexOf("from ", StringComparison.OrdinalIgnoreCase);
    if (Idx < 0) { return S; }
    var Rest = S[(Idx + 5)..];
    var EndIdx = Rest.IndexOf(" to ", StringComparison.OrdinalIgnoreCase);
    return EndIdx > 0 ? Rest[..EndIdx] : Rest.Split('.', '—', '·')[0].Trim();
}

static string ExtractTo(string S)
{
    var Idx = S.IndexOf(" to ", StringComparison.OrdinalIgnoreCase);
    if (Idx < 0) { return ""; }
    return S[(Idx + 4)..].Split('.', '—', '·')[0].Trim();
}

static string ExtractDistance(string S)
{
    var Match = System.Text.RegularExpressions.Regex.Match(S, @"\d+(\.\d+)?\s*(mi|km|miles|kilometers)");
    return Match.Success ? Match.Value : "";
}

static int ExtractInt(string S, string After)
{
    var Match = System.Text.RegularExpressions.Regex.Match(S, @"(\d+(?:[\.,]\d+)?)\s*" + System.Text.RegularExpressions.Regex.Escape(After));
    if (!Match.Success) { return 0; }
    return int.TryParse(Match.Groups[1].Value.Replace(",", ""), out var V) ? V : 0;
}

static async Task PerformSceneCrudOld(
    int N, string Route, WolfsInteropService Wolfs,
    (string Email, string Name, int Years, string Location, string Role)[] Drivers,
    string AdminEmail, string EmployerEmail, string BuyerEmail,
    List<(string, string)> Inputs)
{
    // Map scene N to (actor-email, form-fill-data). For pages that have a
    // real form (SignUp/Login/Applicant/Interview/Documents/Employer-Post),
    // the actor's real data goes into the form. The CRUD writes the same
    // record to the real store via WolfsInteropService.DbPutAsync.
    string Email = N switch
    {
        1 or 2 => AdminEmail,
        3 => EmployerEmail,
        4 => BuyerEmail,
        5 => Drivers[0].Email,
        6 => Drivers[1].Email,
        7 => Drivers[2].Email,
        8 => Drivers[3].Email,
        _ => "",
    };

    if (Route == "/SignUp/" && !string.IsNullOrEmpty(Email))
    {
        Inputs.Add(("input[type=email]", Email));
        Inputs.Add(("input[type=password]", new string('•', 12)));

        await Wolfs.DbPutAsync("audit", new JsonObject
        {
            ["id"] = "aud_scene_" + N.ToString("000"),
            ["scene"] = N,
            ["kind"] = "auth.signup",
            ["actor"] = Email,
            ["permission"] = "auth.signup",
            ["op"] = "CREATE",
            ["note"] = $"submitted email + password — account created for {Email}",
            ["at"] = "2026-04-30T08:" + (N % 60).ToString("00") + ":00",
        });
        return;
    }
    if (Route == "/Login/" && !string.IsNullOrEmpty(Email))
    {
        Inputs.Add(("input[type=email]", Email));
        await Wolfs.DbPutAsync("audit", new JsonObject
        {
            ["id"] = "aud_scene_" + N.ToString("000"),
            ["scene"] = N,
            ["kind"] = "auth.signin",
            ["actor"] = Email,
            ["permission"] = "auth.signin",
            ["op"] = "READ",
            ["note"] = $"signing in — {Email}",
            ["at"] = "2026-04-30T08:" + (N % 60).ToString("00") + ":00",
        });
        return;
    }
    if (Route == "/Applicant/" && N is 9 or 10 or 11)
    {
        var D = Drivers[0];
        var Detail = N switch
        {
            9  => $"name {D.Name}, {D.Years} years driving",
            10 => $"location {D.Location}",
            _  => $"payout-rail destination set: SWIFT/BIC for cross-border CN bank",
        };
        await Wolfs.DbPutAsync("applicants", new JsonObject
        {
            ["id"] = "aud_scene_" + N.ToString("000"),
            ["name"] = D.Name + " · intake step " + (N - 8),
            ["email"] = D.Email,
            ["location"] = D.Location,
            ["experienceYears"] = D.Years,
            ["status"] = "pending",
            ["note"] = Detail,
            ["actor"] = D.Email,
            ["permission"] = "applicant.write",
        });
        return;
    }
    if (Route == "/Interview/" && N is 12 or 13 or 14 or 15)
    {
        var (Subject, Answer) = N switch
        {
            12 => ("DOT compliance",     "Yes — pre-trip inspection completed; air, brakes, tires, lights checked."),
            13 => ("Hours of service",   "Property-carrying CMV: 11 hours driving, 14-hour on-duty window, 30-min break after 8 hours."),
            14 => ("CSA roadside",       "CSA roadside data drives the carrier's safety profile; clean inspection improves score."),
            _  => ("Dispatch protocol",  "Notify dispatcher immediately; reroute via current ETA tool; update buyer."),
        };
        await Wolfs.DbPutAsync("audit", new JsonObject
        {
            ["id"] = "aud_scene_" + N.ToString("000"),
            ["scene"] = N,
            ["kind"] = "interview.answer",
            ["subject"] = Subject,
            ["actor"] = Drivers[0].Email,
            ["permission"] = "interview.write",
            ["op"] = "UPDATE",
            ["note"] = Answer,
            ["at"] = "2026-04-30T08:" + (N % 60).ToString("00") + ":00",
        });
        return;
    }
    if (Route == "/Documents/" && N is >= 16 and <= 20)
    {
        var (Subject, Note, Owner) = N switch
        {
            16 => ("Class A CDL upload",          "CDL-A Liu Wei.pdf · 14d valid · class A endorsed", Drivers[0].Email),
            17 => ("TWIC + drayage cert upload",  "TWIC Pedro Reyes.pdf · drayage_cert.pdf",          Drivers[1].Email),
            18 => ("Interstate + team upload",    "Interstate Authority + Team-Driver Maya Tom.pdf",  Drivers[2].Email),
            19 => ("Interstate + auto-handling",  "Auto-Handling Cert Jordan Vega.pdf",               Drivers[3].Email),
            _  => ("China export cert upload",    "China Export Cert Liu Wei.pdf",                    Drivers[0].Email),
        };
        await Wolfs.DbPutAsync("audit", new JsonObject
        {
            ["id"] = "aud_scene_" + N.ToString("000"),
            ["scene"] = N,
            ["kind"] = "documents.upload",
            ["subject"] = Subject,
            ["actor"] = Owner,
            ["permission"] = "documents.upload",
            ["op"] = "CREATE",
            ["note"] = Note,
            ["at"] = "2026-04-30T08:" + (N % 60).ToString("00") + ":00",
        });
        return;
    }
    if (Route == "/Marketplace/" && N is 24 or 25)
    {
        var (Title, Desc) = N == 24
            ? ("2024 BYD Han EV (China-origin) · listed by Wei", "Wei posts the China-origin BYD Han EV at $48,500 COD as a 4-leg relay listing.")
            : ("Listing fee debited via ACH · BYD Han EV",       "Marketplace listing fee debited from Wei's account via low-cost ACH rail.");
        await Wolfs.DbPutAsync("listings", new JsonObject
        {
            ["id"] = "lst_scene_" + N.ToString("000"),
            ["title"] = Title,
            ["description"] = Desc,
            ["price"] = 48500.00,
            ["sellerEmail"] = EmployerEmail,
            ["status"] = "active",
            ["createdAt"] = "2026-04-30T08:" + (N % 60).ToString("00") + ":00",
            ["actor"] = EmployerEmail,
            ["permission"] = "listing.write",
        });
        return;
    }
    if (Route == "/Employer/Post/" && N is >= 26 and <= 29)
    {
        var (Subject, Note) = N switch
        {
            26 => ("Pickup + dropoff",       "Pickup BYD Hefei Plant · Drop 1418 Oak Street, Wilmington NC."),
            27 => ("Pay-per-leg",            "Total $2,520 split: D1 $320 · D2 $420 · D3 $1,180 · D4 $600."),
            28 => ("Required badges per leg","CDL-A · TWIC · Drayage · Interstate · Team · Auto-handling."),
            _  => ("Payout rail per leg",    "Multi-leg COD: Driver 1 SWIFT, Drivers 2/3/4 RTP/FedNow."),
        };
        await Wolfs.DbPutAsync("audit", new JsonObject
        {
            ["id"] = "aud_scene_" + N.ToString("000"),
            ["scene"] = N,
            ["kind"] = "listing.config",
            ["subject"] = Subject,
            ["actor"] = EmployerEmail,
            ["permission"] = "listing.write",
            ["op"] = "UPDATE",
            ["note"] = Note,
            ["at"] = "2026-04-30T08:" + (N % 60).ToString("00") + ":00",
        });
        return;
    }
    if (Route == "/Marketplace/" && N is 30 or 31 or 76)
    {
        await Wolfs.DbPutAsync("listings", new JsonObject
        {
            ["id"] = "lst_scene_" + N.ToString("000"),
            ["title"] = N switch
            {
                30 => "Sam purchases · BYD Han EV at $48,500 COD",
                31 => "Order pur_byd created · 1418 Oak Street, Wilmington NC · paymentRail rtp",
                _  => "Listing closed · BYD Han EV settled at $48,500 COD via RTP",
            },
            ["description"] = N switch
            {
                37 => "Sam reviews the listing detail and starts the COD purchase flow.",
                38 => "Order pur_byd created — buyer email sam@buyers.example, paymentRail rtp.",
                _  => "Listing archived after delivery and COD settlement complete.",
            },
            ["price"] = 48500.00,
            ["sellerEmail"] = EmployerEmail,
            ["status"] = N == 83 ? "closed" : "active",
            ["createdAt"] = "2026-04-30T08:" + (N % 60).ToString("00") + ":00",
            ["actor"] = N == 76 ? EmployerEmail : BuyerEmail,
            ["permission"] = N == 76 ? "listing.write" : "purchase.write",
        });
        return;
    }
    if (Route == "/HiringHall/" && N is >= 21 and <= 23)
    {
        var D = Drivers[N - 21];
        await Wolfs.DbPutAsync("applicants", new JsonObject
        {
            ["id"] = "aud_scene_" + N.ToString("000"),
            ["name"] = D.Name + " · pending review",
            ["email"] = D.Email,
            ["location"] = D.Location,
            ["experienceYears"] = D.Years,
            ["status"] = N == 23 ? "approved" : "pending",
            ["approvedAt"] = N == 23 ? "2026-04-30T08:" + (N % 60).ToString("00") + ":00" : null,
            ["actor"] = AdminEmail,
            ["permission"] = "applicant.approve",
        });
        return;
    }
    if (Route == "/Schedule/")
    {
        var Leg = N switch
        {
            32 => ("sch_scene_032", "BYD Hefei plant", "Shanghai Yangshan port", 480, 8.0, "Driver 1 dispatched 2026-05-01T08:00, grace 60 min."),
            33 => ("sch_scene_033", "BYD Hefei plant", "Shanghai Yangshan port", 480, 8.0, "Leg 1 route confirmed: 480 mi, 8 hr."),
            42 => ("sch_scene_042", "Shanghai Yangshan port", "Port of Los Angeles", 6500, 0, "Ocean leg 2026-05-03 to 2026-06-09, grace 1440 min."),
            43 => ("sch_scene_043", "Shanghai Yangshan port", "Port of Los Angeles", 6500, 0, "Ocean carrier 6500 mi over 37 days, ISF 10+2 filed."),
            49 => ("sch_scene_049", "Port of Los Angeles", "Phoenix relay", 370, 6.0, "Driver 2 dispatched 2026-06-12T08:00, grace 120 min."),
            53 => ("sch_scene_053", "I-10 East mile 78", "Phoenix relay", 370, 6.0, "Recompute fired on leg2_delay_90min — Boyle Heights flip."),
            54 => ("sch_scene_054", "Live traffic + HOS + yard hours", "Re-timed legs 3 & 4", 0, 0, "Downstream recompute: legs 3 & 4 retimed."),
            57 => ("sch_scene_057", "Phoenix relay", "Memphis 24/7 yard", 1480, 22.0, "Leg 3 team-driver: 1480 mi, 22 hr, grace 240 min."),
            61 => ("sch_scene_061", "Memphis 24/7 yard", "1418 Oak Street, Wilmington NC", 875, 14.0, "Leg 4 final mile: 875 mi, 14 hr, grace 90 min."),
            62 => ("sch_scene_062", "Memphis 24/7 yard", "Wilmington buyer 22:00 cutoff", 875, 14.0, "Buyer reachable until 22:00 — Driver 4 ETA 21:30."),
            _  => ("sch_scene_" + N.ToString("000"), "—", "—", 0, 0.0, ""),
        };
        await Wolfs.DbPutAsync("schedules", new JsonObject
        {
            ["id"] = Leg.Item1,
            ["leg"] = "leg" + N,
            ["from"] = Leg.Item2,
            ["to"] = Leg.Item3,
            ["mileage"] = Leg.Item4,
            ["driveHours"] = Leg.Item5,
            ["startsAt"] = "2026-04-30T08:" + (N % 60).ToString("00") + ":00",
            ["endsAt"] = "2026-04-30T09:" + (N % 60).ToString("00") + ":00",
            ["graceMinutes"] = 60,
            ["recomputed"] = true,
            ["recomputeReason"] = Leg.Item6,
            ["actor"] = "dispatcher@wolfstruckingco.com",
            ["permission"] = "schedule.recompute",
        });
        return;
    }
    if (Route == "/Itinerary/")
    {
        var Notes = N switch
        {
            35 => "Driver 1 paid factory $18,000 via T/T SWIFT same-day from CN bank.",
            36 => "chg_factory posted; paymentRail swift_tt; reimbursed at settlement.",
            37 => "GPS tracker gps_byd_001 installed by Driver 1.",
            39 => "Driver 1 loaded vehicle into ocean container at Yangshan.",
            40 => "Timesheet ts_d1 closed: $320 earnings, on-time true.",
            41 => "Driver 1 payout $320 via SWIFT wire CN-bound.",
            51 => "Driver 2 picked up vehicle at Port of LA drayage yard.",
            55 => "Driver 2 unloaded at Phoenix relay; ts_d2 $420 on-time.",
            56 => "Driver 2 instant payout via RTP / FedNow.",
            59 => "Driver 3 unloaded at Memphis 24/7 yard; ts_d3 $1,180 on-time.",
            60 => "Driver 3 instant payout via RTP / FedNow to team account.",
            64 => "Driver 4 arrived at 1418 Oak Street, Wilmington NC.",
            65 => "Buyer Sam paid COD $48,500 via RTP / FedNow push at the door.",
            66 => "Escrow released on driver delivered-plus-photo event.",
            67 => "Purchase pur_byd codCollected = $48,500, paymentRail rtp.",
            68 => "Job settles aud_settle 2026-06-15T22:30, status completed.",
            77 => "Purchase delivered, all timesheets on-time — workflow complete.",
            _  => "Itinerary event scene " + N,
        };
        var WorkerEmail = N is 51 or 55 or 56 ? Drivers[1].Email
                  : N is 59 or 60 ? Drivers[2].Email
                  : N is 64 or 65 or 66 or 67 or 68 or 77 ? Drivers[3].Email
                  : Drivers[0].Email;
        await Wolfs.DbPutAsync("timesheets", new JsonObject
        {
            ["id"] = "aud_scene_" + N.ToString("000"),
            ["workerEmail"] = WorkerEmail,
            ["jobId"] = "job_byd",
            ["leg"] = "leg" + N,
            ["status"] = "completed",
            ["earnings"] = N switch { 41 => 320, 56 => 420, 60 => 1180, _ => 600 },
            ["onTime"] = true,
            ["startsAt"] = "2026-04-30T08:" + (N % 60).ToString("00") + ":00",
            ["endsAt"] = "2026-04-30T09:" + (N % 60).ToString("00") + ":00",
            ["note"] = Notes,
            ["actor"] = WorkerEmail,
            ["permission"] = "itinerary.write",
        });
        return;
    }
    if (Route == "/Track/")
    {
        await Wolfs.DbPutAsync("audit", new JsonObject
        {
            ["id"] = "aud_scene_" + N.ToString("000"),
            ["scene"] = N,
            ["kind"] = "track.update",
            ["subject"] = N == 44 ? "gps_byd_001 ocean transit" : "I-10 East mile 78 Boyle Heights",
            ["note"] = N == 44 ? "Realtime GPS streamed: container in transit Shanghai → LA." : "Realtime delay: 47-min stoppage triggered recompute.",
            ["at"] = "2026-04-30T08:" + (N % 60).ToString("00") + ":00",
            ["actor"] = AdminEmail,
            ["permission"] = "track.read",
        });
        return;
    }
    if (Route == "/Map/")
    {
        var Nav = N switch
        {
            34 => new
            {
                Subject = "Driver 1 → BYD Hefei plant",
                Actor = Drivers[0].Email,
                Origin = "Shanghai CN driver staging",
                Dest = "BYD Hefei Plant, Anhui CN",
                Distance = "517 km",
                Eta = 360,
                S1 = "Head west on Ruihong Rd toward S20 Outer Ring Expy",
                V1 = "Head west on Ruihong Road toward S20 Outer Ring Expressway",
                S2 = "Take the G42 Shanghai-Chengdu ramp toward Nanjing",
                V2 = "In four hundred meters, take the G42 ramp toward Nanjing",
                S3 = "Continue on G42 for 312 km, exit toward G3 Beijing-Hong Kong Expy",
                V3 = "Stay on G42 for three hundred twelve kilometers",
                S4 = "Take G3 exit 24 toward BYD industrial park, plant gate B",
                V4 = "In two kilometers, take exit twenty-four to BYD industrial park, plant gate B",
            },
            38 => new
            {
                Subject = "Driver 1 → Shanghai Yangshan port",
                Actor = Drivers[0].Email,
                Origin = "BYD Hefei Plant",
                Dest = "Shanghai Yangshan deep-water port — terminal 4",
                Distance = "480 mi",
                Eta = 480,
                S1 = "Head east on Industrial Park Rd to G3 Beijing-Hong Kong Expy",
                V1 = "Head east on Industrial Park Road to the G3 Beijing-Hong Kong Expressway",
                S2 = "Merge onto G3 east, continue for 268 km toward Shanghai",
                V2 = "Merge onto G3 east, continue for two hundred sixty-eight kilometers",
                S3 = "Take G1503 Shanghai Ring Rd south, then S2 Hu-Lu Expy east",
                V3 = "Take the Shanghai Ring Road south, then exit onto S2 east",
                S4 = "Take Donghai Bridge to Yangshan terminal 4, container drop-off",
                V4 = "In thirty-two kilometers, cross Donghai Bridge to Yangshan terminal four",
            },
            50 => new
            {
                Subject = "Driver 2 → Port of LA drayage yard",
                Actor = Drivers[1].Email,
                Origin = "San Pedro CA drayage staging",
                Dest = "Port of Los Angeles, terminal 401, drayage yard",
                Distance = "6 mi",
                Eta = 12,
                S1 = "Head north on Pacific Ave toward Westmont Dr",
                V1 = "Head north on Pacific Avenue for one mile",
                S2 = "Turn right on John S Gibson Blvd, continue toward port",
                V2 = "Turn right on John S Gibson Boulevard",
                S3 = "Merge onto Harbor Blvd toward terminal 401",
                V3 = "Merge onto Harbor Boulevard toward terminal four-zero-one",
                S4 = "Show TWIC at gate B, proceed to drayage yard row 12, container BYDU-2026",
                V4 = "At the gate, show your TWIC card. Proceed to drayage yard row twelve, container BYDU twenty twenty-six",
            },
            58 => new
            {
                Subject = "Driver 3 → cross-country Phoenix to Memphis",
                Actor = Drivers[2].Email,
                Origin = "Phoenix AZ relay yard",
                Dest = "Memphis TN 24/7 drop yard",
                Distance = "1,480 mi",
                Eta = 1320,
                S1 = "Head east on I-10 toward Tucson, 117 mi",
                V1 = "Head east on Interstate ten toward Tucson, one hundred seventeen miles",
                S2 = "Continue on I-10 east through New Mexico for 348 mi to El Paso",
                V2 = "Stay on I ten east through New Mexico for three hundred forty-eight miles",
                S3 = "Take I-20 east toward Dallas, then I-30 east toward Little Rock",
                V3 = "Take Interstate twenty east toward Dallas, then Interstate thirty east toward Little Rock",
                S4 = "Take I-40 east 137 mi to Memphis 24/7 drop yard, gate 7",
                V4 = "Take Interstate forty east one hundred thirty-seven miles to the Memphis drop yard at gate seven",
            },
            _ => new
            {
                Subject = "Driver 4 → 1418 Oak Street, Wilmington NC final mile",
                Actor = Drivers[3].Email,
                Origin = "Memphis TN drop yard",
                Dest = "1418 Oak Street, Wilmington NC 28401",
                Distance = "875 mi",
                Eta = 840,
                S1 = "Head east on I-40 from Memphis 24/7 yard, 384 mi to Knoxville",
                V1 = "Head east on Interstate forty for three hundred eighty-four miles to Knoxville",
                S2 = "Continue I-40 east, take I-26 east toward Asheville",
                V2 = "Continue Interstate forty east, then take Interstate twenty-six east toward Asheville",
                S3 = "Merge onto I-95 south at Florence, continue 220 mi toward Wilmington",
                V3 = "Merge onto Interstate ninety-five south at Florence, continue two hundred twenty miles",
                S4 = "Exit US-117 south to Oak St, deliver to 1418 Oak Street, Wilmington",
                V4 = "Take US one seventeen south, deliver to fourteen-eighteen Oak Street, Wilmington",
            },
        };
        await Wolfs.DbPutAsync("audit", new JsonObject
        {
            ["id"] = "aud_scene_" + N.ToString("000"),
            ["scene"] = N,
            ["kind"] = "nav.read",
            ["subject"] = Nav.Subject,
            ["actor"] = Nav.Actor,
            ["permission"] = "nav.read",
            ["op"] = "READ",
            ["originName"] = Nav.Origin,
            ["destName"] = Nav.Dest,
            ["distance"] = Nav.Distance,
            ["etaMinutes"] = Nav.Eta,
            ["step1"] = Nav.S1, ["step1voice"] = Nav.V1,
            ["step2"] = Nav.S2, ["step2voice"] = Nav.V2,
            ["step3"] = Nav.S3, ["step3voice"] = Nav.V3,
            ["step4"] = Nav.S4, ["step4voice"] = Nav.V4,
            ["at"] = "2026-04-30T08:" + (N % 60).ToString("00") + ":00",
        });
        return;
    }
    if (Route == "/Investors/KPI/")
    {
        var (Kind, Amt, Note) = N switch
        {
            45 => ("ach_charges_post",   0,        "Platform charges post via ACH and wire — B2B scheduled."),
            46 => ("isf_ocean_factor",   1677.50,  "ISF $40, ocean $400, factoring $1,237.50 batched."),
            47 => ("tariff_section301", 25712.50,  "Tariffs Section 301 + base — paid via CBP ACE ACH."),
            48 => ("hmf_mpf",             674.98,  "Harbor Maintenance + Merchandise Processing fees."),
            69 => ("payout_d1_swift",     320,     "Driver 1 $320 payout via SWIFT wire."),
            70 => ("payout_d2_rtp",       420,     "Driver 2 $420 payout via RTP / FedNow."),
            71 => ("payout_d3_rtp",      1180,     "Driver 3 $1,180 payout via RTP / FedNow."),
            72 => ("payout_d4_rtp",       600,     "Driver 4 $600 payout via RTP — total payouts $2,520."),
            73 => ("reimburse_d1",      18000,     "Driver 1 reimbursed $18,000 factory advance via SWIFT."),
            74 => ("debt_clear",        28064.98,  "Debt cleared from COD: factoring + ISF + ocean + tariffs + HMF + MPF."),
            _  => ("net_revenue",       46064.98,  "Net platform revenue $46,064.98 across all settled flows."),
        };
        await Wolfs.DbPutAsync("charges", new JsonObject
        {
            ["id"] = "aud_scene_" + N.ToString("000"),
            ["scene"] = N,
            ["amount"] = Amt,
            ["kind"] = Kind,
            ["payer"] = "platform",
            ["terms"] = Note,
            ["at"] = "2026-04-30T08:" + (N % 60).ToString("00") + ":00",
            ["debt"] = false,
            ["actor"] = AdminEmail,
            ["permission"] = "kpi.read",
        });
        return;
    }
    if (Route == "/Dispatcher/")
    {
        var (Subject, Permission, OnBehalf, Note) = N switch
        {
            78 => ("Dispatch Driver 2 to Port of LA",       "schedule.recompute", Drivers[1].Email, "Driver 2 dispatched to Port of LA drayage yard, ETA 12 min, grace 120 min."),
            79 => ("Reroute Driver 3 through Memphis",      "nav.read",           Drivers[2].Email, "Driver 3 rerouted via I-40 east, live traffic + HOS check passed."),
            _  => ("Confirm Driver 4 final-mile delivery",  "itinerary.write",    Drivers[3].Email, "Driver 4 confirmed at 1418 Oak Street, COD $48,500 RTP collected."),
        };
        await Wolfs.DbPutAsync("audit", new JsonObject
        {
            ["id"] = "aud_scene_" + N.ToString("000"),
            ["scene"] = N,
            ["kind"] = "dispatcher.action",
            ["subject"] = Subject,
            ["actor"] = "dispatcher@wolfstruckingco.com",
            ["permission"] = Permission,
            ["op"] = "UPDATE",
            ["onBehalfOf"] = OnBehalf,
            ["note"] = Note,
            ["at"] = "2026-04-30T08:" + (N % 60).ToString("00") + ":00",
        });
        return;
    }
    // Default: navigate-only (page renders current real DB state).
}

// Form-fill JS removed — chrome inputs are now driven via native CDP
// commands (DOM.querySelector + DOM.focus + Input.insertText).

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
        // Auth (signup/signin) is a public action — anyone can self-register.
        if (Permission.StartsWith("auth.", StringComparison.Ordinal)) { return true; }
        // Dispatcher is a privileged operator that can execute any CRUD on
        // behalf of any user — the platform's central control role.
        if ((Actor ?? "").StartsWith("dispatcher", StringComparison.Ordinal)) { return true; }
        var Map = new Dictionary<string, string[]>
        {
            ["driver"] = new[] { "applicant.", "interview.", "documents.", "nav.", "itinerary." },
            ["admin"] = new[] { "applicant.approve", "track.", "kpi.", "audit." },
            ["employer"] = new[] { "listing." },
            ["buyer"] = new[] { "purchase." },
            ["system"] = new[] { "audit." },
        };
        var Role = (Actor ?? "").Contains('@', StringComparison.Ordinal)
            ? Actor.Split('@')[0].StartsWith("driver", StringComparison.Ordinal) ? "driver"
              : Actor.StartsWith("admin", StringComparison.Ordinal) ? "admin"
              : Actor.StartsWith("wei", StringComparison.Ordinal) ? "employer"
              : Actor.StartsWith("sam", StringComparison.Ordinal) ? "buyer"
              : "user"
            : Actor ?? "";
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
