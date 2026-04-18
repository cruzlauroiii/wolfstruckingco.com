#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/patch-worker-callback-fix.cs scripts/<config>.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var Strings = new Dictionary<string, string>(StringComparer.Ordinal);
foreach (var Pair in WorkerCallbackFixPatterns.ConstString().Matches(await File.ReadAllTextAsync(SpecPath)).Select(M => (M.Groups[1].Value, M.Groups[2].Value))) { Strings[Pair.Item1] = Pair.Item2; }
foreach (var Required in new[] { "WorkerCs", "Find", "Replace" })
{
    if (!Strings.ContainsKey(Required)) { await Console.Error.WriteLineAsync($"specific missing const string {Required}"); return 3; }
}
var WorkerCs = Strings["WorkerCs"];
var Find = Strings["Find"];
var Replace = Strings["Replace"];

if (!File.Exists(WorkerCs)) { await Console.Error.WriteLineAsync($"not found: {WorkerCs}"); return 1; }

var Body = await File.ReadAllTextAsync(WorkerCs);
var ArrayMatch = WorkerCallbackFixPatterns.JsBody().Match(Body);
if (!ArrayMatch.Success) { await Console.Error.WriteLineAsync("could not find JsBody() array"); return 2; }

var Lines = WorkerCallbackFixPatterns.B64Line().Matches(ArrayMatch.Value).Select(M => M.Groups[1].Value).ToList();
var B64 = string.Concat(Lines);
var Js = Encoding.UTF8.GetString(Convert.FromBase64String(B64));
if (!Js.Contains(Find, StringComparison.Ordinal)) { await Console.Error.WriteLineAsync("anchor not found"); return 4; }
if (Js.Contains(Replace, StringComparison.Ordinal)) { await Console.Out.WriteLineAsync("already patched"); return 0; }
var Patched = Js.Replace(Find, Replace, StringComparison.Ordinal);
var NewB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(Patched));

const int LineLen = 100;
var Sb = new StringBuilder();
for (var I = 0; I < NewB64.Length; I += LineLen)
{
    Sb.Append("    \"").Append(NewB64.AsSpan(I, Math.Min(LineLen, NewB64.Length - I))).Append("\",\n");
}
var NewArrayBody = $"static string[] JsBody() => new[]\n{{\n{Sb.ToString().TrimEnd('\n')}\n}};";
var NewWorkerCs = Body[..ArrayMatch.Index] + NewArrayBody + Body[(ArrayMatch.Index + ArrayMatch.Length)..];
await File.WriteAllTextAsync(WorkerCs, NewWorkerCs);
await Console.Out.WriteLineAsync($"patched {WorkerCs}");

var DeployPsi = new ProcessStartInfo("dotnet", "run scripts/deploy-worker.cs") { UseShellExecute = false, WorkingDirectory = Paths.Repo };
using var Proc = Process.Start(DeployPsi)!;
await Proc.WaitForExitAsync();
return Proc.ExitCode;

namespace Scripts
{
    internal static partial class WorkerCallbackFixPatterns
    {
        [GeneratedRegex("""const\s+string\s+(\w+)\s*=\s*@?"((?:[^"\\]|\\.)*)"\s*;""")]
        internal static partial Regex ConstString();

        [GeneratedRegex(@"static string\[\] JsBody\(\) => new\[\]\s*\{[^}]*\};", RegexOptions.Singleline)]
        internal static partial Regex JsBody();

        [GeneratedRegex("\"([A-Za-z0-9+/=]+)\"")]
        internal static partial Regex B64Line();
    }
}
