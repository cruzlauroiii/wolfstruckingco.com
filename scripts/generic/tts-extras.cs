#:property TargetFramework=net11.0

using System.Diagnostics;
using System.Text.Json;

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

var ScenesJsonPath = Get("ScenesJsonPath");
var AudioDir = Get("AudioDir");
var ModelName = Get("ModelName");
var VoicesCsv = Get("Voices");

if (!File.Exists(ScenesJsonPath))
{
    return 2;
}

Directory.CreateDirectory(AudioDir);
var Voices = VoicesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
var Json = await File.ReadAllTextAsync(ScenesJsonPath);
using var Doc = JsonDocument.Parse(Json);

var Idx = 0;
foreach (var Entry in Doc.RootElement.EnumerateArray())
{
    var Pad = Entry.GetProperty("pad").GetString() ?? string.Empty;
    var Narration = Entry.GetProperty("narration").GetString() ?? string.Empty;
    var Mp3 = Path.Combine(AudioDir, "scene-" + Pad + ".mp3");
    if (File.Exists(Mp3))
    {
        Idx++;
        continue;
    }

    var Voice = Voices[Idx % Voices.Length];
    var Psi = new ProcessStartInfo("tts")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    };
    foreach (var Arg in new[] { "--text", Narration, "--model_name", ModelName, "--speaker_idx", Voice, "--out_path", Mp3 })
    {
        Psi.ArgumentList.Add(Arg);
    }

    using var Proc = Process.Start(Psi);
    if (Proc is null)
    {
        return 3;
    }

    _ = await Proc.StandardOutput.ReadToEndAsync();
    var Err = await Proc.StandardError.ReadToEndAsync();
    await Proc.WaitForExitAsync();
    if (Proc.ExitCode != 0)
    {
        await Console.Error.WriteLineAsync("tts FAILED for " + Pad + ": " + Err);
        return 4;
    }

    Idx++;
}

return 0;
