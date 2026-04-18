#:property TargetFramework=net11.0

// patch-trailing-ws.cs - strip trailing whitespace from a single .cs file.
if (args.Length == 0) { await Console.Error.WriteLineAsync("usage: patch-trailing-ws.cs <path>"); return 1; }
var P = args[0];
if (!File.Exists(P)) { await Console.Error.WriteLineAsync($"not found: {P}"); return 2; }
var Lines = await File.ReadAllLinesAsync(P);
var Changed = false;
for (var I = 0; I < Lines.Length; I++)
{
    var Trimmed = Lines[I].TrimEnd();
    if (Trimmed.Length != Lines[I].Length) { Lines[I] = Trimmed; Changed = true; }
}
if (!Changed) { await Console.Out.WriteLineAsync("no trailing ws"); return 0; }
await File.WriteAllLinesAsync(P, Lines);
await Console.Out.WriteLineAsync($"stripped trailing ws from {P}");
return 0;
