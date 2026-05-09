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
var VoicesCsv = Get("VoicesCsv");

if (!File.Exists(ScenesJsonPath))
{
    return 2;
}

if (string.IsNullOrEmpty(AudioDir))
{
    return 3;
}

Directory.CreateDirectory(AudioDir);
var Voices = VoicesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
if (Voices.Length == 0)
{
    return 4;
}

var Json = await File.ReadAllTextAsync(ScenesJsonPath);
using var Doc = JsonDocument.Parse(Json);

var Idx = 0;
foreach (var Entry in Doc.RootElement.EnumerateArray())
{
    var Tgt = Entry.GetProperty("target").GetString() ?? string.Empty;
    var Pad = string.Empty;
    var CbIdx = Tgt.IndexOf("cb=", StringComparison.Ordinal);
    if (CbIdx >= 0)
    {
        var Start = CbIdx + 3;
        var Stop = Start;
        while (Stop < Tgt.Length && (char.IsDigit(Tgt[Stop]) || (Tgt[Stop] >= 'a' && Tgt[Stop] <= 'z')))
        {
            Stop++;
        }
        Pad = Tgt[Start..Stop];
    }
    var Narration = Entry.GetProperty("narration").GetString() ?? string.Empty;
    var Mp3 = Path.Combine(AudioDir, "scene-" + Pad + ".mp3");
    if (File.Exists(Mp3))
    {
        Idx++;
        continue;
    }

    var Voice = Voices[Idx % Voices.Length];
    var Psi = new ProcessStartInfo("python")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        ArgumentList = { "-m", "edge_tts", "--voice", Voice, "--text", Narration, "--write-media", Mp3 },
    };

    using var Proc = Process.Start(Psi);
    if (Proc is null)
    {
        return 5;
    }

    _ = await Proc.StandardOutput.ReadToEndAsync();
    var Err = await Proc.StandardError.ReadToEndAsync();
    await Proc.WaitForExitAsync();
    if (Proc.ExitCode != 0)
    {
        await Console.Error.WriteLineAsync("edge-tts FAILED for " + Pad + ": " + Err);
        return 6;
    }

    Idx++;
}

return 0;
