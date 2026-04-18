#:property TargetFramework=net11.0
#:property PublishAot=false

// Native .NET 11 file-based program that publishes the Blazor WASM client and stages the
// output into docs/app/ for GitHub Pages. No PowerShell, no third-party actions, no NuGet.
//
//   dotnet run scripts/publish-pages.cs -- [--repo <path>] [--basePath /wolfstruckingco.com/app/] [--subdir app]
//
// Idempotent: removes prior publish/ and docs/app/_framework + _content before re-mirroring.
using System.Diagnostics;
using System.Text.RegularExpressions;

var Repo = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ".."));
var BasePath = "/wolfstruckingco.com/app/";
var Subdir = "app";

ParseArgs(args, ref Repo, ref BasePath, ref Subdir);

var Client = Path.Combine(Repo, "src", "Client");
var Publish = Path.Combine(Repo, "publish");
var Docs = Path.Combine(Repo, "docs");
var WasmDocs = string.IsNullOrEmpty(Subdir) ? Docs : Path.Combine(Docs, Subdir);

if (Directory.Exists(Publish))
{
    Directory.Delete(Publish, recursive: true);
}

Console.WriteLine($"→ dotnet publish {Client} -c Release -o {Publish}");
var Psi = new ProcessStartInfo
{
    FileName = "dotnet",
    UseShellExecute = false,
};
Psi.ArgumentList.Add("publish");
Psi.ArgumentList.Add(Client);
Psi.ArgumentList.Add("-c");
Psi.ArgumentList.Add("Release");
Psi.ArgumentList.Add("-o");
Psi.ArgumentList.Add(Publish);
Psi.ArgumentList.Add("--nologo");
Psi.ArgumentList.Add("-v");
Psi.ArgumentList.Add("minimal");
var Proc = Process.Start(Psi);
Proc?.WaitForExit();
if (Proc?.ExitCode != 0)
{
    Console.Error.WriteLine($"dotnet publish exit {Proc?.ExitCode}");
    return 1;
}

var IndexPath = Path.Combine(Publish, "wwwroot", "index.html");
var Html = File.ReadAllText(IndexPath);
#pragma warning disable MA0110, SYSLIB1045
var BaseHrefRx = new Regex("<base href=\"[^\"]*\"\\s*/?>", RegexOptions.Compiled);
#pragma warning restore MA0110, SYSLIB1045
Html = BaseHrefRx.Replace(Html, $"<base href=\"{BasePath}\" />");
File.WriteAllText(IndexPath, Html);

var NoJekyll = Path.Combine(Publish, "wwwroot", ".nojekyll");
File.WriteAllText(NoJekyll, string.Empty);

var NotFound = Path.Combine(Publish, "wwwroot", "404.html");
var NotFoundHtml = "<!doctype html><html><head><meta charset=\"utf-8\" /><title>Redirecting</title><script>"
    + "var L=location;L.replace(L.protocol+'//'+L.hostname+(L.port?':'+L.port:'')+L.pathname.split('/').slice(0,2).join('/')"
    + "+'/?p=/'+L.pathname.slice(1).split('/').slice(1).join('/').replace(/&/g,'~and~')"
    + "+(L.search?'&q='+L.search.slice(1).replace(/&/g,'~and~'):'')+L.hash);"
    + "</script></head><body></body></html>";
File.WriteAllText(NotFound, NotFoundHtml);

if (!Directory.Exists(WasmDocs))
{
    Directory.CreateDirectory(WasmDocs);
}
foreach (var Sub in new[] { "_framework", "_content" })
{
    var Tgt = Path.Combine(WasmDocs, Sub);
    if (Directory.Exists(Tgt))
    {
        Directory.Delete(Tgt, recursive: true);
    }
}
CopyDirectory(Path.Combine(Publish, "wwwroot"), WasmDocs);

Console.WriteLine();
Console.WriteLine($"Published to {Publish}/wwwroot and mirrored to {WasmDocs}");
Console.WriteLine("Static marketing pages at /docs/* are untouched.");
return 0;

static void ParseArgs(string[] Argv, ref string Repo, ref string BasePath, ref string Subdir)
{
    for (var I = 0; I < Argv.Length - 1; I += 2)
    {
        var Key = Argv[I];
        var Val = Argv[I + 1];
        if (Key == "--repo")
        {
            Repo = Path.GetFullPath(Val);
        }
        else if (Key == "--basePath")
        {
            BasePath = Val;
        }
        else if (Key == "--subdir")
        {
            Subdir = Val;
        }
    }
}

static void CopyDirectory(string Src, string Dst)
{
    if (!Directory.Exists(Dst))
    {
        Directory.CreateDirectory(Dst);
    }
    foreach (var Fp in Directory.GetFiles(Src))
    {
        var DstFile = Path.Combine(Dst, Path.GetFileName(Fp));
        File.Copy(Fp, DstFile, overwrite: true);
    }
    foreach (var SubDir in Directory.GetDirectories(Src))
    {
        var DstSubDir = Path.Combine(Dst, Path.GetFileName(SubDir));
        CopyDirectory(SubDir, DstSubDir);
    }
}
