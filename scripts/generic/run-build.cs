#:property TargetFramework=net11.0

using System.Diagnostics;

if (args.Length < 1)
{
    return 1;
}

var Specs = await File.ReadAllLinesAsync(args[0]);

string Get(string Name)
{
    foreach (var Line in Specs)
    {
        var Pat = "const string " + Name + " = ";
        var At = Line.IndexOf(Pat, StringComparison.Ordinal);
        if (At < 0)
        {
            continue;
        }

        var Tail = Line[(At + Pat.Length)..];
        if (Tail.Length > 0 && Tail[0] == '@')
        {
            Tail = Tail[1..];
        }

        if (Tail.Length == 0 || Tail[0] != '\u0022')
        {
            continue;
        }

        var End = Tail.LastIndexOf("\u0022;", StringComparison.Ordinal);
        if (End < 1)
        {
            continue;
        }

        return Tail[1..End];
    }

    return string.Empty;
}

var Repo = Get("Repo");
var Csproj = Path.Combine(Repo, Get("CsprojRel").Replace('/', Path.DirectorySeparatorChar));
var Publish = Path.Combine(Path.GetTempPath(), "wolfs-blazor-publish");
var Target = Path.Combine(Repo, "wwwroot", Get("TargetSubdir"));

if (!File.Exists(Csproj))
{
    return 5;
}

if (Directory.Exists(Publish))
{
    Directory.Delete(Publish, recursive: true);
}

var Psi = new ProcessStartInfo("dotnet")
{
    WorkingDirectory = Repo,
    UseShellExecute = false,
};
foreach (var Arg in new[] { "publish", Csproj, "-c", "Release", "-o", Publish, "--nologo" })
{
    Psi.ArgumentList.Add(Arg);
}

using var Proc = Process.Start(Psi);
if (Proc is null)
{
    return 6;
}

await Proc.WaitForExitAsync();
if (Proc.ExitCode != 0)
{
    return 7;
}

var Src = Path.Combine(Publish, "wwwroot");
if (!Directory.Exists(Src))
{
    return 8;
}

if (Directory.Exists(Target))
{
    Directory.Delete(Target, recursive: true);
}

Directory.CreateDirectory(Target);
foreach (var Dir in Directory.GetDirectories(Src, "*", SearchOption.AllDirectories))
{
    Directory.CreateDirectory(Dir.Replace(Src, Target, StringComparison.Ordinal));
}

foreach (var Fpath in Directory.GetFiles(Src, "*", SearchOption.AllDirectories))
{
    File.Copy(Fpath, Fpath.Replace(Src, Target, StringComparison.Ordinal), overwrite: true);
}

return 0;
