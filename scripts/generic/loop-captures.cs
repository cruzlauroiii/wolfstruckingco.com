#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;
using System.Text.Json;

if (args.Length < 1)
{
    return 1;
}

var Spec = await File.ReadAllLinesAsync(args[0]);

string? Get(string Name)
{
    foreach (var Line in Spec)
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

    return null;
}

var Repo = Get("Repo")!;
var ScenesJsonPath = Get("ScenesJsonPath")!;
var AudioDir = Get("AudioDir")!;
var FramesDir = Get("FramesDir")!;
var OutDir = Get("OutDir")!;
var WaitMs = Get("WaitMs") ?? "10000";

Directory.CreateDirectory(FramesDir);
Directory.CreateDirectory(OutDir);

var Json = await File.ReadAllTextAsync(ScenesJsonPath);
using var Doc = JsonDocument.Parse(Json);

var SpecificDir = Path.Combine(Repo, "scripts", "specific");
var CapturePath = Path.Combine(Repo, "scripts", "generic", "capture-one-scene.cs");

var Done = 0;
var Failed = 0;
var Skipped = 0;
foreach (var Entry in Doc.RootElement.EnumerateArray())
{
    var Url = Entry.GetProperty("target").GetString() ?? string.Empty;
    var CbMatch = System.Text.RegularExpressions.Regex.Match(Url, "cb=([0-9]+[a-z]?)");
    var Pad = CbMatch.Success ? CbMatch.Groups[1].Value : string.Empty;
    if (string.IsNullOrEmpty(Pad) || string.IsNullOrEmpty(Url))
    {
        continue;
    }

    var Mp4 = Path.Combine(OutDir, $"scene-{Pad}.mp4");
    var Mp3 = Path.Combine(AudioDir, $"scene-{Pad}.mp3");
    var Png = Path.Combine(FramesDir, $"scene-{Pad}.png");

    var Config = $"return 0;\nnamespace Scripts\n{{\n    internal static class CaptureOneSceneScratchConfig\n    {{\n        public const string Repo = @\u0022{Repo}\u0022;\n        public const string Url = \u0022{Url}\u0022;\n        public const string PngPath = @\u0022{Png}\u0022;\n        public const string Mp3Path = @\u0022{Mp3}\u0022;\n        public const string Mp4Path = @\u0022{Mp4}\u0022;\n        public const string Pad = \u0022{Pad}\u0022;\n        public const string WaitMs = \u0022{WaitMs}\u0022;\n    }}\n}}\n";
    var ConfigPath = Path.Combine(SpecificDir, $"capture-one-scene-loop-{Pad}-config.cs");
    await File.WriteAllTextAsync(ConfigPath, Config);

    var Psi = new ProcessStartInfo("dotnet")
    {
        WorkingDirectory = Repo,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    };
    Psi.ArgumentList.Add("run");
    Psi.ArgumentList.Add(CapturePath);
    Psi.ArgumentList.Add(ConfigPath);

    using var P = Process.Start(Psi)!;
    var StdOut = await P.StandardOutput.ReadToEndAsync();
    var StdErr = await P.StandardError.ReadToEndAsync();
    await P.WaitForExitAsync();

    if (P.ExitCode == 0 && File.Exists(Mp4))
    {
        Done++;
        await Console.Error.WriteLineAsync($"[{Done}] {Pad} ok");
    }
    else
    {
        if (File.Exists(Mp4))
        {
            Skipped++;
        }
        else
        {
            Failed++;
            await Console.Error.WriteLineAsync($"[fail] {Pad} exit={P.ExitCode} err={StdErr.Replace('\n', ' ').Replace('\r', ' ')[..Math.Min(200, StdErr.Length)]}");
        }
    }
}

await Console.Error.WriteLineAsync($"loop-captures: done={Done} failed={Failed} skipped={Skipped}");
return Failed > 0 ? 4 : 0;
