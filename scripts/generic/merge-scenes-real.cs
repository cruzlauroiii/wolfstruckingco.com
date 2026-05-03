#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
using System.Text;
using System.Text.Json;

if (args.Length < 1) { return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { return 2; }

var Specs = await File.ReadAllLinesAsync(SpecPath);
string? Read(string Name)
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

var RoutesPath = Read("RoutesPath");
var NarrationsPath = Read("NarrationsPath");
var OutputPath = Read("OutputPath");
var ProfilesRootDir = Read("ProfilesRootDir") ?? "C:\\chrome-profiles";
var DefaultProfile = Read("DefaultProfile") ?? "car-seller-google";
if (RoutesPath is null || NarrationsPath is null || OutputPath is null) return 3;
if (!File.Exists(RoutesPath) || !File.Exists(NarrationsPath)) return 4;

var Routes = JsonDocument.Parse(await File.ReadAllTextAsync(RoutesPath)).RootElement.EnumerateArray().Select(E => E.GetString() ?? "").Where(S => !string.IsNullOrEmpty(S)).ToArray();
var NarrJson = JsonDocument.Parse(await File.ReadAllTextAsync(NarrationsPath)).RootElement;
var NarrByUrl = new Dictionary<string, string>(StringComparer.Ordinal);
foreach (var N in NarrJson.EnumerateArray())
{
    var U = N.GetProperty("url").GetString() ?? "";
    var Text = N.GetProperty("narration").GetString() ?? "";
    NarrByUrl[U] = Text;
}

var Sb = new StringBuilder();
Sb.Append("[\n");
for (var I = 0; I < Routes.Length; I++)
{
    var Url = Routes[I];
    var Pad = (I + 1).ToString("000", System.Globalization.CultureInfo.InvariantCulture);
    NarrByUrl.TryGetValue(Url, out var Narr);
    Narr ??= Url;
    var EscUrl = Url.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
    var EscNarr = Narr.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
    var ProfileDir = $"{ProfilesRootDir}\\{DefaultProfile}";
    var EscProfile = ProfileDir.Replace("\\", "\\\\", StringComparison.Ordinal);
    Sb.Append("  {\"index\":");
    Sb.Append((I + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
    Sb.Append(",\"pad\":\"");
    Sb.Append(Pad);
    Sb.Append("\",\"url\":\"");
    Sb.Append(EscUrl);
    Sb.Append("\",\"narration\":\"");
    Sb.Append(EscNarr);
    Sb.Append("\",\"selector\":\"");
    Sb.Append("");
    Sb.Append("\",\"typeText\":\"");
    Sb.Append("");
    Sb.Append("\",\"profileDir\":\"");
    Sb.Append(EscProfile);
    Sb.Append("\"}");
    if (I < Routes.Length - 1) Sb.Append(",");
    Sb.Append("\n");
}
Sb.Append("]\n");
var OutDir = Path.GetDirectoryName(OutputPath);
if (!string.IsNullOrEmpty(OutDir) && !Directory.Exists(OutDir)) Directory.CreateDirectory(OutDir);
await File.WriteAllTextAsync(OutputPath, Sb.ToString());
return 0;
