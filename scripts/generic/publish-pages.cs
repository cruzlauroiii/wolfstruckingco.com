#:property TargetFramework=net11.0
#:property PublishAot=false
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include ../specific/publish-pages-config.cs
using System.Diagnostics;
using System.Text.RegularExpressions;
using Scripts;

var Repo = PublishPagesConfig.Repo;
var BasePath = PublishPagesConfig.DefaultBasePath;
var Subdir = PublishPagesConfig.DefaultSubdir;

ParseArgs(args, ref Repo, ref BasePath, ref Subdir);

var Client = Path.Combine(Repo, "src", "Client");
var Publish = Path.Combine(Repo, "publish");
var Docs = Path.Combine(Repo, "docs");
var WasmDocs = string.IsNullOrEmpty(Subdir) ? Docs : Path.Combine(Docs, Subdir);

if (Directory.Exists(Publish)) { Directory.Delete(Publish, recursive: true); }

var Psi = new ProcessStartInfo { FileName = "dotnet", UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
Psi.ArgumentList.Add("publish");
Psi.ArgumentList.Add(Client);
Psi.ArgumentList.Add("-c");
Psi.ArgumentList.Add("Release");
Psi.ArgumentList.Add("-o");
Psi.ArgumentList.Add(Publish);
Psi.ArgumentList.Add("--nologo");
Psi.ArgumentList.Add("-v");
Psi.ArgumentList.Add("quiet");
var Proc = Process.Start(Psi)!;
var PubOut = Proc.StandardOutput.ReadToEndAsync();
var PubErr = Proc.StandardError.ReadToEndAsync();
await Proc.WaitForExitAsync();
if (Proc.ExitCode != 0)
{
    await Console.Error.WriteAsync(await PubErr);
    await Console.Error.WriteAsync(await PubOut);
    await Console.Error.WriteLineAsync($"publish exit {Proc.ExitCode.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
    return 1;
}

var IndexPath = Path.Combine(Publish, "wwwroot", "index.html");
var Html = await File.ReadAllTextAsync(IndexPath);
Html = PublishPatterns.BaseHref().Replace(Html, $"<base href=\"{BasePath}\" />");
await File.WriteAllTextAsync(IndexPath, Html);

var NoJekyll = Path.Combine(Publish, "wwwroot", ".nojekyll");
await File.WriteAllTextAsync(NoJekyll, string.Empty);

var NotFound = Path.Combine(Publish, "wwwroot", "404.html");
var NotFoundHtml = "<!doctype html><html><head><meta charset=\"utf-8\" /><title>Redirecting</title><script>var L=location;L.replace(L.protocol+'//'+L.hostname+(L.port?':'+L.port:'')+L.pathname.split('/').slice(0,2).join('/')+'/?p=/'+L.pathname.slice(1).split('/').slice(1).join('/').replace(/&/g,'~and~')+(L.search?'&q='+L.search.slice(1).replace(/&/g,'~and~'):'')+L.hash);</script></head><body></body></html>";
await File.WriteAllTextAsync(NotFound, NotFoundHtml);

if (!Directory.Exists(WasmDocs)) { Directory.CreateDirectory(WasmDocs); }
foreach (var Sub in new[] { "_framework", "_content" })
{
    var Tgt = Path.Combine(WasmDocs, Sub);
    if (Directory.Exists(Tgt)) { Directory.Delete(Tgt, recursive: true); }
}
CopyDirectory(Path.Combine(Publish, "wwwroot"), WasmDocs);

return 0;

static void ParseArgs(string[] Argv, ref string Repo, ref string BasePath, ref string Subdir)
{
    for (var I = 0; I < Argv.Length - 1; I += 2)
    {
        var Key = Argv[I];
        var Val = Argv[I + 1];
        if (Key == "--repo") { Repo = Path.GetFullPath(Val); }
        else if (Key == "--basePath") { BasePath = Val; }
        else if (Key == "--subdir") { Subdir = Val; }
    }
}

static void CopyDirectory(string Src, string Dst)
{
    if (!Directory.Exists(Dst)) { Directory.CreateDirectory(Dst); }
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

namespace Scripts
{
    internal static partial class PublishPatterns
    {
        [GeneratedRegex("<base href=\"[^\"]*\"\\s*/?>")]
        internal static partial Regex BaseHref();
    }
}
