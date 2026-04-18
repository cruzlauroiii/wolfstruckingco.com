#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

// tail-file.cs — print the last N lines of a file. Replaces `tail -N path`.
//
//   dotnet run scripts/tail-file.cs -- <path>             # last 10 lines
//   dotnet run scripts/tail-file.cs -- <path> 30          # last 30 lines

if (args.Length == 0)
{
    Console.Error.WriteLine("usage: dotnet run scripts/tail-file.cs -- <path> [N]");
    return 1;
}

var Path = args[0];
var N = args.Length > 1 && int.TryParse(args[1], out var Parsed) ? Parsed : 10;
if (!File.Exists(Path))
{
    Console.Error.WriteLine($"missing: {Path}");
    return 1;
}

using var Stream = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
using var Reader = new StreamReader(Stream);
var Body = Reader.ReadToEnd();
var Lines = Body.Split('\n');
var Start = Math.Max(0, Lines.Length - N);
for (var I = Start; I < Lines.Length; I++)
{
    Console.WriteLine(Lines[I].TrimEnd('\r'));
}
return 0;
