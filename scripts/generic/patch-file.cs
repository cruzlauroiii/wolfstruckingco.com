#:property TargetFramework=net11.0

// patch-file.cs — Generic. Substring (or regex) replacement on a file, or a
// BATCH of (path, find, replace) tuples in one invocation. Replaces ad-hoc
// Edit calls on non-cs source files (.razor, .html, .css, .js, .json, .md).
// Usage:
//   dotnet run scripts/patch-file.cs -- <path> --find "literal" --replace "new"
//   dotnet run scripts/patch-file.cs -- <path> --regex "(?i)pattern" --replace "new"
//   dotnet run scripts/patch-file.cs -- <path> --find-file <patch.txt> --replace-file <new.txt>
//   dotnet run scripts/patch-file.cs -- --batch <jsonl>     # one patch per line
// --once : require exactly one match; exit 1 if zero or 2+
// --all  : replace every occurrence (default)
// --dry  : show match count and don't write
//
// Specific helpers (item #23) build a list of {path, find, replace} JSON
// objects and pass them via --batch, so all the file I/O logic stays HERE.
// Each JSONL line: {"path":"...","find":"...","replace":"...","once":true|false}
using System.Text.Json;
using System.Text.RegularExpressions;

if (args.Length < 2 || args[0] is "-h" or "--help")
{
    await Console.Out.WriteLineAsync("usage: dotnet run scripts/patch-file.cs -- <path> --find \"literal\" --replace \"new\" [--once] [--all] [--dry]");
    await Console.Out.WriteLineAsync("       dotnet run scripts/patch-file.cs -- <path> --regex \"pattern\" --replace \"new\" [...]");
    await Console.Out.WriteLineAsync("       dotnet run scripts/patch-file.cs -- <path> --find-file <patch.txt> --replace-file <new.txt>");
    await Console.Out.WriteLineAsync("       dotnet run scripts/patch-file.cs -- --batch <jsonl>          # one {path,find,replace} per line");
    return args.Length == 0 ? 1 : 0;
}

if (args[0] == "--batch")
{
    if (args.Length < 2) { await Console.Error.WriteLineAsync("--batch requires a jsonl file"); return 6; }
    var BatchPath = args[1];
    if (!File.Exists(BatchPath)) { await Console.Error.WriteLineAsync($"batch file not found: {BatchPath}"); return 7; }
    var Total = 0;
    var Failed = 0;
    foreach (var Line in await File.ReadAllLinesAsync(BatchPath))
    {
        if (string.IsNullOrWhiteSpace(Line)) { continue; }
        using var Doc = JsonDocument.Parse(Line);
        var P = Doc.RootElement.GetProperty("path").GetString() ?? string.Empty;
        var Find2 = Doc.RootElement.GetProperty("find").GetString() ?? string.Empty;
        var Replace2 = Doc.RootElement.GetProperty("replace").GetString() ?? string.Empty;
        var Once2 = Doc.RootElement.TryGetProperty("once", out var O2) && O2.GetBoolean();
        var Idempotent = Doc.RootElement.TryGetProperty("idempotent", out var I2) && I2.GetBoolean();
        if (!File.Exists(P)) { await Console.Error.WriteLineAsync($"  [skip] not found: {P}"); Failed++; continue; }
        var Body = await File.ReadAllTextAsync(P);
        var Nl = Body.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var FindNorm = Find2.Replace("\n", Nl, StringComparison.Ordinal);
        var ReplaceNorm = Replace2.Replace("\n", Nl, StringComparison.Ordinal);
        if (Idempotent && Body.Contains(ReplaceNorm, StringComparison.Ordinal)) { await Console.Out.WriteLineAsync($"  [skip] already applied: {P}"); continue; }
        if (!Body.Contains(FindNorm, StringComparison.Ordinal)) { await Console.Error.WriteLineAsync($"  [miss] anchor not found in {P}"); Failed++; continue; }
        var Updated2 = Once2 ? ReplaceOnce(Body, FindNorm, ReplaceNorm) : Body.Replace(FindNorm, ReplaceNorm);
        await File.WriteAllTextAsync(P, Updated2);
        Total++;
        await Console.Out.WriteLineAsync($"  [ok]   {P}");
    }
    await Console.Out.WriteLineAsync($"--batch: {Total.ToString(System.Globalization.CultureInfo.InvariantCulture)} applied, {Failed.ToString(System.Globalization.CultureInfo.InvariantCulture)} failed");
    return Failed == 0 ? 0 : 8;
}

static string ReplaceOnce(string Source, string Find, string Replace)
{
    var Idx = Source.IndexOf(Find, StringComparison.Ordinal);
    return Idx < 0 ? Source : Source[..Idx] + Replace + Source[(Idx + Find.Length)..];
}

var Path = args[0];
if (!File.Exists(Path)) { await Console.Error.WriteLineAsync($"not found: {Path}"); return 2; }

string GetArg(string Name)
{
    var I = Array.IndexOf(args, Name);
    return I >= 0 && I + 1 < args.Length ? args[I + 1] : string.Empty;
}
bool HasFlag(string Name) => Array.IndexOf(args, Name) >= 0;

string Find;
string Replace;
var Regex = HasFlag("--regex");
if (HasFlag("--find-file"))
{
    Find = await File.ReadAllTextAsync(GetArg("--find-file"));
    Replace = HasFlag("--replace-file") ? await File.ReadAllTextAsync(GetArg("--replace-file")) : GetArg("--replace");
}
else if (HasFlag("--regex"))
{
    Find = GetArg("--regex");
    Replace = GetArg("--replace");
}
else
{
    Find = GetArg("--find");
    Replace = GetArg("--replace");
}

if (string.IsNullOrEmpty(Find)) { await Console.Error.WriteLineAsync("--find / --regex / --find-file required"); return 3; }

var Original = await File.ReadAllTextAsync(Path);
int Count;
string Updated;
if (Regex)
{
    var Re = new Regex(Find, RegexOptions.Singleline);
    Count = Re.Count(Original);
    Updated = Re.Replace(Original, Replace);
}
else
{
    Count = 0;
    var Idx = 0;
    while ((Idx = Original.IndexOf(Find, Idx, StringComparison.Ordinal)) >= 0) { Count++; Idx += Find.Length; }
    Updated = Original.Replace(Find, Replace);
}

await Console.Out.WriteLineAsync($"matches: {Count.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
if (HasFlag("--dry")) { return 0; }
if (Count == 0) { await Console.Error.WriteLineAsync("no matches — file unchanged"); return 4; }
if (HasFlag("--once") && Count != 1) { await Console.Error.WriteLineAsync($"--once requires exactly 1 match (got {Count.ToString(System.Globalization.CultureInfo.InvariantCulture)})"); return 5; }
await File.WriteAllTextAsync(Path, Updated);
await Console.Out.WriteLineAsync($"wrote {Path} ({Updated.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)} chars)");
return 0;
