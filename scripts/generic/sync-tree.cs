#:property TargetFramework=net11.0

using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run sync-tree.cs <config>"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

string? Source = null;
string? Dest = null;
foreach (var (Name, Value) in SyncTreePatterns.ConstString().Matches(await File.ReadAllTextAsync(SpecPath)).Select(M => (M.Groups["n"].Value, M.Groups["v"].Value)))
{
    if (Name == "Source") { Source = Value; }
    else if (Name == "Dest") { Dest = Value; }
}
if (string.IsNullOrEmpty(Source) || string.IsNullOrEmpty(Dest)) { await Console.Error.WriteLineAsync("specific must declare const string Source and Dest"); return 3; }
if (!Directory.Exists(Source)) { await Console.Error.WriteLineAsync($"source not found: {Source}"); return 4; }
if (!Directory.Exists(Dest)) { await Console.Error.WriteLineAsync($"dest not found: {Dest}"); return 5; }

foreach (var Fp in Directory.EnumerateFiles(Source, "*", SearchOption.AllDirectories))
{
    var Rel = Path.GetRelativePath(Source, Fp);
    var Tgt = Path.Combine(Dest, Rel);
    var TgtDir = Path.GetDirectoryName(Tgt);
    if (!string.IsNullOrEmpty(TgtDir) && !Directory.Exists(TgtDir)) { Directory.CreateDirectory(TgtDir); }
    File.Copy(Fp, Tgt, overwrite: true);
}
return 0;

namespace Scripts
{
    internal static partial class SyncTreePatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<n>\w+)\s*=\s*@?"(?<v>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();
    }
}
