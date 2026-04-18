#:sdk Microsoft.NET.Sdk.Web
#:property TargetFramework=net11.0
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
#:property EnableNETAnalyzers=false
#:property NoWarn=SA1503;SA1649;SA1633;SA1200;SA1201;SA1400;SA1502;SA1128;SA1519;SA1513;SA1516;SA1515;SA1413;IDE1006;RCS1001;RCS1003
// Wolfs — local HTTPS static file server.
//
//   dotnet run serve-local.cs [<root> [<httpsPort> [<httpPort>]]]
//   dotnet run serve-local.cs -- C:\...\main\wwwroot 8443 8080
//
// HTTPS on 8443 by default (from the dotnet dev cert) plus an HTTP → HTTPS redirect
// listener on 8080. Trust the dev cert once with:
//   dotnet dev-certs https --trust
//
// The server maps /wolfstruckingco.com/* onto the given root folder so relative asset
// paths (/wolfstruckingco.com/db.js, /wolfstruckingco.com/wolfs.css, …) resolve the
// same in local dev as they do in production.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

var Root = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "wwwroot"));
var HttpsPort = args.Length > 1 ? int.Parse(args[1]) : 8443;
var HttpPort  = args.Length > 2 ? int.Parse(args[2]) : 8080;

if (!Directory.Exists(Root))
{
    Console.Error.WriteLine($"Content root not found: {Root}");
    return 1;
}

var MimeExtras = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    [".wasm"] = "application/wasm",
    [".dll"]  = "application/octet-stream",
    [".pdb"]  = "application/octet-stream",
    [".dat"]  = "application/octet-stream",
    [".blat"] = "application/octet-stream",
    [".br"]   = "application/octet-stream",
    [".gz"]   = "application/octet-stream",
    [".webcil"] = "application/octet-stream",
    [".pdb"]  = "application/octet-stream",
    [".dat"]  = "application/octet-stream",
    [".blat"] = "application/octet-stream",
    [".br"]   = "application/octet-stream",
    [".gz"]   = "application/gzip",
    [".scss"] = "text/plain; charset=utf-8",
};

var Builder = WebApplication.CreateBuilder();
Builder.WebHost.ConfigureKestrel(Options =>
{
    Options.ListenAnyIP(HttpsPort, L => L.UseHttps());
    Options.ListenAnyIP(HttpPort);
});
var App = Builder.Build();

// HTTP → HTTPS redirect on the plain-text port.
App.Use(async (Ctx, Next) =>
{
    if (!Ctx.Request.IsHttps && Ctx.Request.Host.Port == HttpPort)
    {
        var Host = Ctx.Request.Host.Host;
        var Target = $"https://{Host}:{HttpsPort}{Ctx.Request.Path}{Ctx.Request.QueryString}";
        Ctx.Response.Redirect(Target, permanent: false);
        return;
    }
    await Next().ConfigureAwait(false);
});

var Provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
foreach (var Kvp in MimeExtras) { Provider.Mappings[Kvp.Key] = Kvp.Value; }

var RepoSegment = "/wolfstruckingco.com";
var FileProvider = new PhysicalFileProvider(Root);
App.UseDefaultFiles(new DefaultFilesOptions
{
    RequestPath = RepoSegment,
    FileProvider = FileProvider,
    DefaultFileNames = new List<string> { "index.html" },
});
// SPA fallback middleware for /wolfstruckingco.com/app/* — runs BEFORE UseStaticFiles.
// If the requested URL has NO file extension and the path doesn't physically exist on disk,
// rewrite to /app/index.html so the Blazor router can handle the deep route. Anything with
// an extension (.js, .wasm, .css, .png, ...) is left alone for static-files to serve.
App.Use(async (Ctx, Next) =>
{
    var Path1 = Ctx.Request.Path.Value ?? string.Empty;
    if (Path1.StartsWith("/wolfstruckingco.com/app/", StringComparison.OrdinalIgnoreCase))
    {
        var Rel = Path1["/wolfstruckingco.com/".Length..];
        var Disk = System.IO.Path.Combine(Root, Rel.Replace('/', System.IO.Path.DirectorySeparatorChar));
        var HasExt = !string.IsNullOrEmpty(System.IO.Path.GetExtension(Path1));
        if (!HasExt && !File.Exists(Disk))
        {
            Ctx.Request.Path = "/wolfstruckingco.com/app/index.html";
        }
    }
    await Next(Ctx).ConfigureAwait(false);
});

App.UseStaticFiles(new StaticFileOptions
{
    RequestPath = RepoSegment,
    FileProvider = FileProvider,
    ContentTypeProvider = Provider,
    // Serve files with unknown extensions (.fingerprint, .icudt, etc.) — Blazor publish output
    // includes hashed filenames the MIME provider doesn't always recognise.
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/octet-stream",
    OnPrepareResponse = Ctx =>
    {
        // Dev server: never cache. Source files change frequently during a video build,
        // and stale HTML/JS in Chrome's HTTP cache leaks old role placeholders + old JS
        // logic into freshly-rendered scenes. Forcing no-store keeps every navigate honest.
        Ctx.Context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        Ctx.Context.Response.Headers["Pragma"] = "no-cache";
        Ctx.Context.Response.Headers["Expires"] = "0";
    },
});

// Reverse proxy for the voice sidecar so browser calls stay on the same HTTPS origin.
// JS on an HTTPS page can't fetch http://localhost:9334 directly without the browser
// flagging mixed content, so we accept same-origin /sidecar/* calls and forward them
// to the plain-HTTP sidecar server-side.
var SidecarClient = new HttpClient(new HttpClientHandler { UseProxy = false });
SidecarClient.Timeout = TimeSpan.FromMinutes(2);
App.Map("/sidecar/{*rest}", async (HttpContext Ctx) =>
{
    var Rest = Ctx.Request.RouteValues["rest"]?.ToString() ?? string.Empty;
    var Target = $"http://localhost:9334/{Rest}{Ctx.Request.QueryString}";
    using var Req = new HttpRequestMessage(new HttpMethod(Ctx.Request.Method), Target);
    foreach (var H in Ctx.Request.Headers)
    {
        if (H.Key.Equals("Host", StringComparison.OrdinalIgnoreCase)) { continue; }
        try { Req.Headers.TryAddWithoutValidation(H.Key, H.Value.ToArray()); } catch { }
    }
    if (Ctx.Request.ContentLength > 0 || Ctx.Request.Headers.ContainsKey("Transfer-Encoding"))
    {
        using var Ms = new MemoryStream();
        await Ctx.Request.Body.CopyToAsync(Ms).ConfigureAwait(false);
        Ms.Position = 0;
        Req.Content = new ByteArrayContent(Ms.ToArray());
        if (Ctx.Request.ContentType is string Ct) { Req.Content.Headers.TryAddWithoutValidation("Content-Type", Ct); }
    }
    try
    {
        using var Resp = await SidecarClient.SendAsync(Req, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        Ctx.Response.StatusCode = (int)Resp.StatusCode;
        foreach (var H in Resp.Headers) { Ctx.Response.Headers[H.Key] = H.Value.ToArray(); }
        foreach (var H in Resp.Content.Headers) { Ctx.Response.Headers[H.Key] = H.Value.ToArray(); }
        Ctx.Response.Headers.Remove("transfer-encoding");
        await Resp.Content.CopyToAsync(Ctx.Response.Body).ConfigureAwait(false);
    }
    catch (HttpRequestException Ex)
    {
        Ctx.Response.StatusCode = 502;
        await Ctx.Response.WriteAsync($"Sidecar unreachable: {Ex.Message}").ConfigureAwait(false);
    }
});

// Root redirect to /wolfstruckingco.com/ so the landing page shows up at /.
App.MapGet("/", (HttpContext Ctx) => Results.Redirect("/wolfstruckingco.com/"));


Console.WriteLine($"Serving {Root}");
Console.WriteLine($"  HTTPS: https://localhost:{HttpsPort}/wolfstruckingco.com/");
Console.WriteLine($"  HTTP:  http://localhost:{HttpPort}/wolfstruckingco.com/  (→ HTTPS)");
Console.WriteLine("Stop with Ctrl+C.");
await App.RunAsync().ConfigureAwait(false);
return 0;
