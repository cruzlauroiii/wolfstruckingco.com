#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include ../specific/build-blazor-config.cs
using System.Diagnostics;
using Scripts;

var ProjectRoot = FindProjectRoot(Directory.GetCurrentDirectory())
               ?? FindProjectRoot(AppContext.BaseDirectory)
               ?? Directory.GetCurrentDirectory();
var Csproj = Path.Combine(ProjectRoot, BuildBlazorConfig.ClientCsprojRel.Replace('/', Path.DirectorySeparatorChar));
var Publish = Path.Combine(Path.GetTempPath(), BuildBlazorConfig.PublishTempDir);
var Target = Path.Combine(ProjectRoot, "wwwroot", BuildBlazorConfig.TargetWwwrootSubdir);

if (!File.Exists(Csproj))
{
    await Console.Error.WriteLineAsync($"Client csproj not found at {Csproj}. Run from the main/ folder.");
    return 1;
}

await Console.Out.WriteLineAsync($"==> dotnet publish {Csproj}");
if (Run("dotnet", "publish", Csproj, "-c", "Release", "-o", Publish, "--nologo") != 0)
{
    await Console.Error.WriteLineAsync("dotnet publish failed");
    return 1;
}

var SourceRoot = Path.Combine(Publish, "wwwroot");
if (!Directory.Exists(SourceRoot))
{
    await Console.Error.WriteLineAsync($"Publish did not produce a wwwroot at {SourceRoot}");
    return 1;
}

await Console.Out.WriteLineAsync($"==> sync {SourceRoot} -> {Target}");
if (Directory.Exists(Target)) { Directory.Delete(Target, true); }
Directory.CreateDirectory(Target);
CopyRecursive(SourceRoot, Target);

var Index = Path.Combine(Target, "index.html");
if (File.Exists(Index))
{
    var Html = await File.ReadAllTextAsync(Index);
    Html = Html.Replace(BuildBlazorConfig.BaseHrefSearch, BuildBlazorConfig.BaseHrefReplace);
    await File.WriteAllTextAsync(Index, Html);
}

Run("dotnet", "run", "scripts/purge-files.cs", "scripts/purge-build-artifacts.cs");

await Console.Out.WriteLineAsync($"==> Blazor WASM deployed to {Target}");
await Console.Out.WriteLineAsync($"Visit {BuildBlazorConfig.LocalUrl}");
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

