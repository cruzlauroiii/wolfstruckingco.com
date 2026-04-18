#:property TargetFramework=net11.0
if (args.Length == 0) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/tail-file.cs -- <path> [N]"); return 1; }

var Path = args[0];
var N = args.Length > 1 && int.TryParse(args[1], out var Parsed) ? Parsed : 10;
if (!File.Exists(Path)) { await Console.Error.WriteLineAsync($"missing: {Path}"); return 1; }

await using var Stream = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
using var Reader = new StreamReader(Stream);
var Body = await Reader.ReadToEndAsync();
var Lines = Body.Split('\n');
var Start = Math.Max(0, Lines.Length - N);
for (var I = Start; I < Lines.Length; I++)
{
    await Console.Out.WriteLineAsync(Lines[I].TrimEnd('\r'));
}
return 0;
