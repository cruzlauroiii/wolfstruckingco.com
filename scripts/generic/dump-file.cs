#:property TargetFramework=net11.0
using System.Text.RegularExpressions;
using Scripts;

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    await Console.Out.WriteLineAsync("usage: dotnet run scripts/dump-file.cs -- <path> [--lines L1 L2 | --bytes B1 B2 | --grep regex]");
    await Console.Out.WriteLineAsync("       dotnet run scripts/dump-file.cs scripts/<config>.cs   (reads const Path/Mode/LineStart/LineEnd/BytePos/ByteLen/Pattern)");
    return args.Length == 0 ? 1 : 0;
}

if (args[0].EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && File.Exists(args[0]))
{
    var SpecBody = await File.ReadAllTextAsync(args[0]);
    var Strs = DumpFilePatterns.ConstString().Matches(SpecBody)
        .ToDictionary(M => M.Groups["name"].Value, M => M.Groups["value"].Value, StringComparer.Ordinal);
    var Nums = DumpFilePatterns.ConstInt().Matches(SpecBody)
        .ToDictionary(M => M.Groups["name"].Value, M => int.Parse(M.Groups["value"].Value, System.Globalization.CultureInfo.InvariantCulture), StringComparer.Ordinal);

    // A .cs file is treated as a config spec only if it declares `const string Path`.
    // Otherwise (e.g. scenes.cs, a runnable program), fall through to raw read.
    if (Strs.TryGetValue("Path", out var SpecPath))
    {
        var SpecMode = Strs.TryGetValue("Mode", out var Mv) ? Mv.ToLowerInvariant() : "full";
        string[] Forward = SpecMode switch
        {
            "full" => [SpecPath],
            "tail" => [SpecPath, "--tail", $"{Nums.GetValueOrDefault("TailN", 20).ToString(System.Globalization.CultureInfo.InvariantCulture)}"],
            "lines" => [SpecPath, "--lines", $"{Nums.GetValueOrDefault("LineStart", 1).ToString(System.Globalization.CultureInfo.InvariantCulture)}", $"{Nums.GetValueOrDefault("LineEnd", int.MaxValue).ToString(System.Globalization.CultureInfo.InvariantCulture)}"],
            "bytes" => [SpecPath, "--bytes", $"{Nums.GetValueOrDefault("BytePos", 0).ToString(System.Globalization.CultureInfo.InvariantCulture)}", $"{(Nums.GetValueOrDefault("BytePos", 0) + Nums.GetValueOrDefault("ByteLen", 1024 * 1024)).ToString(System.Globalization.CultureInfo.InvariantCulture)}"],
            "grep" => [SpecPath, "--grep", Strs.GetValueOrDefault("Pattern", string.Empty)],
            _ => [],
        };
        if (Forward.Length == 0) { await Console.Error.WriteLineAsync($"unknown Mode: {SpecMode}"); return 4; }
        args = Forward;
    }
}

var Path = args[0];
if (!File.Exists(Path)) { await Console.Error.WriteLineAsync($"not found: {Path}"); return 2; }

if (args.Contains("--tail"))
{
    var Idx = Array.IndexOf(args, "--tail");
    var N = int.Parse(args[Idx + 1], System.Globalization.CultureInfo.InvariantCulture);
    await using var FsTail = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
    using var SrTail = new StreamReader(FsTail);
    var All = (await SrTail.ReadToEndAsync()).Split('\n');
    var Start = Math.Max(0, All.Length - N);
    for (var I = Start; I < All.Length; I++) { await Console.Out.WriteLineAsync(All[I]); }
    return 0;
}
if (args.Contains("--lines"))
{
    var Idx = Array.IndexOf(args, "--lines");
    var L1 = int.Parse(args[Idx + 1], System.Globalization.CultureInfo.InvariantCulture);
    var L2 = int.Parse(args[Idx + 2], System.Globalization.CultureInfo.InvariantCulture);
    var Lines = await File.ReadAllLinesAsync(Path);
    for (var I = Math.Max(0, L1 - 1); I < Math.Min(Lines.Length, L2); I++)
    {
        await Console.Out.WriteLineAsync($"{(I + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)}\t{Lines[I]}");
    }
    return 0;
}
if (args.Contains("--bytes"))
{
    var Idx = Array.IndexOf(args, "--bytes");
    var B1 = long.Parse(args[Idx + 1], System.Globalization.CultureInfo.InvariantCulture);
    var B2 = long.Parse(args[Idx + 2], System.Globalization.CultureInfo.InvariantCulture);
    await using var Fs = File.OpenRead(Path);
    Fs.Seek(B1, SeekOrigin.Begin);
    var Buf = new byte[Math.Min(B2 - B1, 1024 * 1024)];
    var N = await Fs.ReadAsync(Buf);
    await Console.Out.WriteAsync(System.Text.Encoding.UTF8.GetString(Buf, 0, N));
    return 0;
}
if (args.Contains("--grep"))
{
    var Idx = Array.IndexOf(args, "--grep");
    var Re = new Regex(args[Idx + 1], RegexOptions.IgnoreCase);
    var Lines = await File.ReadAllLinesAsync(Path);
    for (var I = 0; I < Lines.Length; I++)
    {
        if (Re.IsMatch(Lines[I])) { await Console.Out.WriteLineAsync($"{(I + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)}\t{Lines[I]}"); }
    }
    return 0;
}

await using var Fs2 = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
using var Sr = new StreamReader(Fs2);
await Console.Out.WriteAsync(await Sr.ReadToEndAsync());
return 0;

namespace Scripts
{
    internal static partial class DumpFilePatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();

        [GeneratedRegex(@"const\s+int\s+(?<name>\w+)\s*=\s*(?<value>-?\d+)\s*;", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstInt();
    }
}
