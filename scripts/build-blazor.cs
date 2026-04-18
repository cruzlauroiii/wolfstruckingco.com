#:property TargetFramework=net11.0
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
#:property EnableNETAnalyzers=false
#:property NoWarn=SA1503;SA1649;SA1633;SA1200;SA1201;SA1400;SA1502;SA1128;SA1519;SA1513;SA1516;SA1515;SA1413;IDE1006;RCS1001;RCS1003
#pragma warning disable SA1503, SA1649, SA1633, SA1200, SA1201, SA1400, SA1502, SA1128, SA1519, SA1513, SA1516, SA1515, SA1413, IDE1006, RCS1001, RCS1003
// Wolfs — Blazor WebAssembly build + deploy.
//
//   dotnet run scripts/build-blazor.cs
//
// Publishes src/Client (which references SharedUI + Domain) as static
// WASM + HTML bundles, then copies the output into main/wwwroot/app/ so the local
// serve-local.cs dev server and GitHub Pages both serve it. Base-href is rewritten so
// the app works at both /wolfstruckingco.com/app/ and its live URL.

using System.Diagnostics;

var ProjectRoot = FindProjectRoot(Directory.GetCurrentDirectory())
               ?? FindProjectRoot(AppContext.BaseDirectory)
               ?? Directory.GetCurrentDirectory();
var Csproj  = Path.Combine(ProjectRoot, "src", "Client", "Client.csproj");
var Publish = Path.Combine(Path.GetTempPath(), "wolfs-blazor-publish");
var Target  = Path.Combine(ProjectRoot, "wwwroot", "app");

if (!File.Exists(Csproj))
{
    Console.Error.WriteLine($"Client csproj not found at {Csproj}. Run from the main/ folder.");
    return 1;
}

Console.WriteLine($"==> dotnet publish {Csproj}");
if (Run("dotnet", "publish", Csproj, "-c", "Release", "-o", Publish, "--nologo") != 0)
{
    Console.Error.WriteLine("dotnet publish failed");
    return 1;
}

var SourceRoot = Path.Combine(Publish, "wwwroot");
if (!Directory.Exists(SourceRoot))
{
    Console.Error.WriteLine($"Publish did not produce a wwwroot at {SourceRoot}");
    return 1;
}

Console.WriteLine($"==> sync {SourceRoot} -> {Target}");
if (Directory.Exists(Target)) { Directory.Delete(Target, true); }
Directory.CreateDirectory(Target);
CopyRecursive(SourceRoot, Target);

// Rewrite <base href> so the Blazor app loads correctly under the repo sub-path.
var Index = Path.Combine(Target, "index.html");
if (File.Exists(Index))
{
    var Html = File.ReadAllText(Index);
    Html = Html.Replace("<base href=\"/\" />", "<base href=\"/wolfstruckingco.com/app/\" />");
    File.WriteAllText(Index, Html);
}

Console.WriteLine($"==> Blazor WASM deployed to {Target}");
Console.WriteLine("Visit http://localhost:8080/wolfstruckingco.com/app/");
return 0;

static int Run(string FileName, params string[] Argv)
{
    var Psi = new ProcessStartInfo { FileName = FileName, UseShellExecute = false };
    foreach (var A in Argv) { Psi.ArgumentList.Add(A); }
    using var P = Process.Start(Psi);
    if (P is null) { return 1; }
    P.WaitForExit();
    return P.ExitCode;
}

static void CopyRecursive(string Source, string Dest)
{
    foreach (var D in Directory.GetDirectories(Source, "*", SearchOption.AllDirectories))
    {
        Directory.CreateDirectory(D.Replace(Source, Dest));
    }
    foreach (var F in Directory.GetFiles(Source, "*", SearchOption.AllDirectories))
    {
        File.Copy(F, F.Replace(Source, Dest), overwrite: true);
    }
}

static string? FindProjectRoot(string Start)
{
    var Dir = new DirectoryInfo(Start);
    while (Dir is not null)
    {
        if (Directory.Exists(Path.Combine(Dir.FullName, "src")) &&
            Directory.Exists(Path.Combine(Dir.FullName, "wwwroot")) &&
            File.Exists(Path.Combine(Dir.FullName, "src", "Client", "Client.csproj")))
        {
            return Dir.FullName;
        }
        Dir = Dir.Parent;
    }
    return null;
}
