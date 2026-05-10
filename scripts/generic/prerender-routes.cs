#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;

if (args.Length < 1) return 1;
var SpecPath = args[0];
if (!File.Exists(SpecPath)) return 2;
var Specs = await File.ReadAllLinesAsync(SpecPath);

string? Get(string Name)
{
    foreach (var Line in Specs)
    {
        var Idx = Line.IndexOf("const string " + Name + " = ", StringComparison.Ordinal);
        if (Idx < 0) continue;
        var After = Line.Substring(Idx + 13 + Name.Length + 3);
        if (After.StartsWith("@", StringComparison.Ordinal)) After = After.Substring(1);
        if (!After.StartsWith("\"", StringComparison.Ordinal)) continue;
        var End = After.LastIndexOf("\";", StringComparison.Ordinal);
        if (End < 1) continue;
        return After.Substring(1, End - 1);
    }
    return null;
}

var BaseUrl = Get("BaseUrl") ?? "";
var RoutesCsv = Get("Routes") ?? "";
var DocsDir = Get("DocsDir") ?? "";
var Repo = Get("Repo") ?? Environment.CurrentDirectory;
var HydrateMs = int.Parse(Get("HydrateMs") ?? "6000");
if (string.IsNullOrEmpty(BaseUrl) || string.IsNullOrEmpty(RoutesCsv) || string.IsNullOrEmpty(DocsDir)) return 3;

var Routes = RoutesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
Directory.CreateDirectory(DocsDir);

string Esc(string s) => s.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

async Task<(int rc, string log)> Cdp(string body)
{
    var Cfg = Path.Combine(Path.GetTempPath(), $"cdp-pre-{Guid.NewGuid():N}.cs");
    var Log = Path.Combine(Path.GetTempPath(), $"cdp-pre-log-{Guid.NewGuid():N}.txt");
    var Full = $"return 0;\nnamespace Scripts\n{{\n    internal static class CdpRun\n    {{\n        {body}\n        public const string OutputPath = \"{Esc(Log)}\";\n    }}\n}}\n";
    await File.WriteAllTextAsync(Cfg, Full);
    var Psi = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = Repo };
    Psi.ArgumentList.Add("run");
    Psi.ArgumentList.Add("main/scripts/generic/chrome-devtools.cs");
    Psi.ArgumentList.Add(Cfg);
    using var P = Process.Start(Psi)!;
    var Ot = P.StandardOutput.ReadToEndAsync();
    var Et = P.StandardError.ReadToEndAsync();
    var Done = await Task.WhenAny(P.WaitForExitAsync(), Task.Delay(60000));
    if (Done != (Task)P.WaitForExitAsync()) { try { P.Kill(true); } catch { } }
    var L = File.Exists(Log) ? await File.ReadAllTextAsync(Log) : "";
    try { File.Delete(Cfg); } catch { }
    try { File.Delete(Log); } catch { }
    return (P.ExitCode, L);
}

var Ok = 0;
var Fail = new List<string>();
foreach (var R in Routes)
{
    var Path1 = R.Trim('/');
    var Url = BaseUrl.TrimEnd('/') + "/" + Path1 + (string.IsNullOrEmpty(Path1) ? "" : "/");
    var NavBody = $"public const string Command = \"new_page\"; public const string Url = \"{Esc(Url)}\";";
    var Nav = await Cdp(NavBody);
    if (Nav.rc != 0) { Fail.Add(R + ":nav"); continue; }
    await Task.Delay(HydrateMs);
    var ListBody = "public const string Command = \"list_pages\";";
    var List = await Cdp(ListBody);
    var PageIdx = "1";
    foreach (var Ln in List.log.Split('\n'))
    {
        var T = Ln.Trim();
        if (!T.Contains("wolfstruckingco", StringComparison.OrdinalIgnoreCase)) continue;
        var Colon = T.IndexOf(':');
        if (Colon < 1) continue;
        var Idx = T[..Colon].Trim();
        if (Idx.All(char.IsDigit)) { PageIdx = Idx; break; }
    }
    var EvalBody = "public const string Command = \"evaluate_script\"; public const string PageId = \"" + PageIdx + "\"; public const string Function = \"() => '<!DOCTYPE html>\\\\n' + document.documentElement.outerHTML\";";
    var Eval = await Cdp(EvalBody);
    if (Eval.rc != 0 || string.IsNullOrEmpty(Eval.log)) { Fail.Add(R + ":eval"); continue; }
    var Html = Eval.log;
    var Idx2 = Html.IndexOf("<!DOCTYPE", StringComparison.OrdinalIgnoreCase);
    if (Idx2 < 0)
    {
        var Q = Html.IndexOf("\"\\u003C!DOCTYPE", StringComparison.OrdinalIgnoreCase);
        if (Q >= 0)
        {
            var End = Html.LastIndexOf('\"');
            if (End > Q)
            {
                var Json = Html[Q..(End + 1)];
                try { Html = System.Text.Json.JsonSerializer.Deserialize<string>(Json) ?? Html; } catch { }
            }
        }
    }
    else { Html = Html[Idx2..]; }
    var OutPath = string.IsNullOrEmpty(Path1)
        ? Path.Combine(DocsDir, "index.html")
        : Path.Combine(DocsDir, Path1, "index.html");
    var Dir = Path.GetDirectoryName(OutPath);
    if (!string.IsNullOrEmpty(Dir)) Directory.CreateDirectory(Dir);
    await File.WriteAllTextAsync(OutPath, Html);
    Ok++;
    Console.WriteLine($"  {R} -> {OutPath}");
}
Console.WriteLine($"DONE ok={Ok} fail={Fail.Count} {string.Join(",", Fail)}");
return Fail.Count == 0 ? 0 : 5;
