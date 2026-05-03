#:property TargetFramework=net11.0
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { return 2; }

var ConstRe = E2eScenesPatterns.ConstString();
string? ScenesPath = null;
string? Origin = null;
string? OutputPath = null;
foreach (var (Name, Value) in ConstRe.Matches(await File.ReadAllTextAsync(SpecPath)).Select(M => (M.Groups["name"].Value, M.Groups["value"].Value)))
{
    if (Name == "ScenesPath") { ScenesPath = Value; }
    else if (Name == "Origin") { Origin = Value; }
    else if (Name == "OutputPath") { OutputPath = Value; }
}
if (ScenesPath is null || Origin is null || OutputPath is null) { return 3; }
if (!File.Exists(ScenesPath)) { return 4; }

using var ScenesDoc = JsonDocument.Parse(await File.ReadAllTextAsync(ScenesPath));
var Scenes = ScenesDoc.RootElement.EnumerateArray().ToArray();
using var Http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

var Report = new StringBuilder();
Report.AppendLine("[");
var First = true;
var Pass = 0;
var Fail = 0;
foreach (var Scene in Scenes)
{
    var Target = Scene.TryGetProperty("target", out var T) ? T.GetString() : null;
    if (string.IsNullOrEmpty(Target)) { continue; }
    var Path = Target.Replace("https://localhost:8443", string.Empty, StringComparison.Ordinal).Replace("http://localhost:8443", string.Empty, StringComparison.Ordinal);
    var Url = Origin + Path;
    var Status = 0;
    try
    {
        using var Req = new HttpRequestMessage(HttpMethod.Head, Url);
        using var Resp = await Http.SendAsync(Req);
        Status = (int)Resp.StatusCode;
    }
#pragma warning disable CA1031
    catch
#pragma warning restore CA1031
    {
        Status = -1;
    }

    if (Status == 200) { Pass++; } else { Fail++; }
    if (!First) { Report.AppendLine(","); }
    First = false;
    Report.Append(System.Globalization.CultureInfo.InvariantCulture, $"  {{\"path\":\"{Path}\",\"status\":{Status.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}");
}

Report.AppendLine();
Report.AppendLine("]");
var FullOut = System.IO.Path.IsPathRooted(OutputPath) ? OutputPath : System.IO.Path.Combine(Environment.CurrentDirectory, OutputPath);
var Dir = System.IO.Path.GetDirectoryName(FullOut);
if (!string.IsNullOrEmpty(Dir)) { Directory.CreateDirectory(Dir); }
await File.WriteAllTextAsync(FullOut, Report.ToString());
return Fail == 0 ? 0 : 5;

namespace Scripts
{
    internal static partial class E2eScenesPatterns
    {
        [GeneratedRegex("""const\s+string\s+(?<name>\w+)\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ConstString();
    }
}
