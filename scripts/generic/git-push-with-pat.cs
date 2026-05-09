#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;
using System.Text.Json;

if (args.Length < 1) return 1;
var Spec = await File.ReadAllLinesAsync(args[0]);

string? Get(string Name)
{
    foreach (var Line in Spec)
    {
        var Pat = "const string " + Name + " = ";
        var At = Line.IndexOf(Pat, StringComparison.Ordinal);
        if (At < 0) continue;
        var Tail = Line[(At + Pat.Length)..];
        if (Tail.Length > 0 && Tail[0] == '@') Tail = Tail[1..];
        if (Tail.Length == 0 || Tail[0] != '\u0022') continue;
        var End = Tail.LastIndexOf("\u0022;", StringComparison.Ordinal);
        if (End < 1) continue;
        return Tail[1..End];
    }
    return null;
}

var SecretsJsonPath = Get("SecretsJsonPath")!;
var Repo = Get("Repo")!;
var Owner = Get("Owner")!;
var RepoName = Get("RepoName")!;
var Branch = Get("Branch") ?? "main";

using var Doc = JsonDocument.Parse(await File.ReadAllTextAsync(SecretsJsonPath));
var PatStr = Doc.RootElement.TryGetProperty("GitHub:Pat", out var Pp) ? Pp.GetString() ?? string.Empty : string.Empty;
if (string.IsNullOrEmpty(PatStr)) return 2;

var Url = "https://x-access-token:" + PatStr + "@github.com/" + Owner + "/" + RepoName + ".git";
var Psi = new ProcessStartInfo("git") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = Repo };
Psi.ArgumentList.Add("push");
Psi.ArgumentList.Add(Url);
Psi.ArgumentList.Add(Branch);
using var P = Process.Start(Psi)!;
var Out = await P.StandardOutput.ReadToEndAsync();
var Err = await P.StandardError.ReadToEndAsync();
await P.WaitForExitAsync();
await Console.Out.WriteAsync(Out);
await Console.Error.WriteAsync(Err.Replace(PatStr, "***", StringComparison.Ordinal));
return P.ExitCode;
