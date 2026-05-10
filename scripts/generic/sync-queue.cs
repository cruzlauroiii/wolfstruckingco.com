#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Diagnostics;
using System.Globalization;
using System.Text;

if (args.Length < 1) return 1;
var Spec = await File.ReadAllLinesAsync(args[0]);
var Quote = (char)0x22;

string? Get(string Name)
{
    foreach (var Line in Spec)
    {
        var Pat = "const string " + Name + " = ";
        var At = Line.IndexOf(Pat, StringComparison.Ordinal);
        if (At < 0) continue;
        var Tail = Line[(At + Pat.Length)..];
        if (Tail.Length > 0 && Tail[0] == '@') Tail = Tail[1..];
        if (Tail.Length == 0 || Tail[0] != Quote) continue;
        var Term = string.Concat(Quote.ToString(CultureInfo.InvariantCulture), ";");
        var End = Tail.LastIndexOf(Term, StringComparison.Ordinal);
        if (End < 1) continue;
        return Tail[1..End];
    }
    return null;
}

var Repo = Get("Repo")!;
var Queue = Get("Queue")!;
var CurScratchPath = Get("CurScratchPath")!;
var PubExecConfigPath = Get("PubExecConfigPath")!;
var Items = Queue.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

await Console.Error.WriteLineAsync("sync-queue: count=" + Items.Length.ToString(CultureInfo.InvariantCulture));

for (var I = 0; I < Items.Length; I++)
{
    var Rel = Items[I];
    var SourcePath = Path.IsPathRooted(Rel) ? Rel : Path.Combine(Repo, Rel);
    if (!File.Exists(SourcePath))
    {
        await Console.Error.WriteLineAsync("sync-queue: missing " + SourcePath);
        return 2;
    }
    var Bytes = await File.ReadAllBytesAsync(SourcePath);
    var B64 = Convert.ToBase64String(Bytes);
    var Sb = new StringBuilder();
    Sb.AppendLine("return 0;");
    Sb.AppendLine();
    Sb.AppendLine("namespace Scripts");
    Sb.AppendLine("{");
    Sb.AppendLine("    internal static class SyncFileBase64QueueCurScratchConfig");
    Sb.AppendLine("    {");
    Sb.Append("        public const string OutputPath = @").Append(Quote).Append(SourcePath).Append(Quote).AppendLine(";");
    Sb.Append("        public const string InlineBase64 = ").Append(Quote).Append(B64).Append(Quote).AppendLine(";");
    Sb.AppendLine("    }");
    Sb.AppendLine("}");
    await File.WriteAllTextAsync(CurScratchPath, Sb.ToString());

    var Psi = new ProcessStartInfo("dotnet")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        WorkingDirectory = Path.GetDirectoryName(Repo) ?? Directory.GetCurrentDirectory(),
    };
    Psi.ArgumentList.Add("run");
    Psi.ArgumentList.Add(@"main\scripts\generic\pub-exec.cs");
    Psi.ArgumentList.Add(PubExecConfigPath);
    using var P = Process.Start(Psi)!;
    var ReadOut = P.StandardOutput.ReadToEndAsync();
    var ReadErr = P.StandardError.ReadToEndAsync();
    await P.WaitForExitAsync();
    var Out = await ReadOut;
    var Err = await ReadErr;
    await Console.Error.WriteLineAsync("sync-queue: " + (I + 1).ToString(CultureInfo.InvariantCulture) + "/" + Items.Length.ToString(CultureInfo.InvariantCulture) + " " + Rel + " exit=" + P.ExitCode.ToString(CultureInfo.InvariantCulture));
    if (P.ExitCode != 0)
    {
        await Console.Error.WriteLineAsync("sync-queue out: " + Out);
        await Console.Error.WriteLineAsync("sync-queue err: " + Err);
        return P.ExitCode;
    }
}
return 0;
