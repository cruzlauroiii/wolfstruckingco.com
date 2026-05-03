#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;
using System.Text.RegularExpressions;

if (args.Length < 1) return 1;
var SpecPath = args[0];
if (!File.Exists(SpecPath)) return 2;
var Specs = await File.ReadAllLinesAsync(SpecPath);

string Unesc(string s)
{
    var sb = new System.Text.StringBuilder(s.Length);
    for (int i = 0; i < s.Length; i++)
    {
        if (s[i] == '\\' && i + 1 < s.Length)
        {
            char n = s[i + 1];
            if (n == '"') { sb.Append('"'); i++; }
            else if (n == '\\') { sb.Append('\\'); i++; }
            else if (n == 'n') { sb.Append('\n'); i++; }
            else if (n == 't') { sb.Append('\t'); i++; }
            else if (n == 'r') { sb.Append('\r'); i++; }
            else sb.Append(s[i]);
        }
        else sb.Append(s[i]);
    }
    return sb.ToString();
}

string? Get(string Name)
{
    foreach (var Line in Specs)
    {
        var Idx = Line.IndexOf("const string " + Name + " = ", StringComparison.Ordinal);
        if (Idx < 0) continue;
        var After = Line.Substring(Idx + 13 + Name.Length + 3);
        bool Verb = After.StartsWith("@", StringComparison.Ordinal);
        if (Verb) After = After.Substring(1);
        if (!After.StartsWith("\"", StringComparison.Ordinal)) continue;
        var End = After.LastIndexOf("\";", StringComparison.Ordinal);
        if (End < 1) continue;
        var Raw = After.Substring(1, End - 1);
        return Verb ? Raw : Unesc(Raw);
    }
    return null;
}

var WorkDir = Get("WorkDir") ?? Environment.CurrentDirectory;
var OutputPath = Get("OutputPath") ?? "";

var ArgList = new List<string>();
for (int I = 1; I <= 30; I++)
{
    var A = Get($"Arg{I}");
    if (A is null) break;
    ArgList.Add(A);
}

var Psi = new ProcessStartInfo("git") { WorkingDirectory = WorkDir, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
foreach (var A in ArgList) Psi.ArgumentList.Add(A);
using var P = Process.Start(Psi)!;
var OutTask = P.StandardOutput.ReadToEndAsync();
var ErrTask = P.StandardError.ReadToEndAsync();
await Task.WhenAll(OutTask, ErrTask, P.WaitForExitAsync());
var Combined = OutTask.Result + ErrTask.Result;
if (!string.IsNullOrEmpty(OutputPath))
{
    var Dir = Path.GetDirectoryName(OutputPath);
    if (!string.IsNullOrEmpty(Dir)) Directory.CreateDirectory(Dir);
    await File.WriteAllTextAsync(OutputPath, Combined);
}
else
{
    Console.Write(Combined);
}
return P.ExitCode;
