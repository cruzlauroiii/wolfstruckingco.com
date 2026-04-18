#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:property TargetFramework=net11.0
#:include SharedScripts.cs
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/count-lines.cs scripts/<config>.cs"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }

var Strs = await ScratchConfig.LoadStringsAsync(SpecPath);
string? FilePath = null;
string? Root = null;
string? Pattern = null;
foreach (var (Name, Value) in Strs.Select(K => (K.Key, K.Value)))
{
    if (Name == "FilePath") { FilePath = Value; }
    else if (Name == "Root") { Root = Value; }
    else if (Name == "Pattern") { Pattern = Value; }
}
if (FilePath is null || Root is null || Pattern is null)
{
    await Console.Error.WriteLineAsync("specific must declare const string FilePath, Root, and Pattern");
    return 3;
}

if (Root.Length > 0)
{
    if (Pattern.Length == 0) { await Console.Error.WriteLineAsync("multi-file mode requires non-empty Pattern"); return 5; }
    if (!Directory.Exists(Root)) { await Console.Error.WriteLineAsync($"root not found: {Root}"); return 4; }
    var Total = 0L;
    foreach (var F in Directory.EnumerateFiles(Root, Pattern, SearchOption.AllDirectories))
    {
        var Count = (await File.ReadAllLinesAsync(F)).Length;
        var Rel = Path.GetRelativePath(Root, F).Replace('\\', '/');
        await Console.Out.WriteLineAsync($"{Count.ToString(System.Globalization.CultureInfo.InvariantCulture)}\t{Rel}");
        Total += Count;
    }
    await Console.Out.WriteLineAsync($"{Total.ToString(System.Globalization.CultureInfo.InvariantCulture)}\ttotal");
    return 0;
}

if (FilePath.Length == 0) { await Console.Error.WriteLineAsync("single-file mode requires non-empty FilePath"); return 6; }
if (!File.Exists(FilePath)) { await Console.Error.WriteLineAsync($"file not found: {FilePath}"); return 7; }
var Lines = (await File.ReadAllLinesAsync(FilePath)).Length;
await Console.Out.WriteLineAsync($"{Lines.ToString(System.Globalization.CultureInfo.InvariantCulture)}\t{Path.GetFileName(FilePath)}");
return 0;



