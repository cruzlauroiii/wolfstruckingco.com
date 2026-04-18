#:property TargetFramework=net11.0

// fetch-url.cs - Generic. Reads a sibling specific .cs file (passed as
// args[0]) and runs every probe declared in its Probes array against
// the optional BaseUrl const. The specific carries ALL probe data.
//
// A specific declares an optional BaseUrl const string and a Probes
// table whose tuple elements are Label, Path, Mode, Pattern, Method,
// Follow. Mode is one of body, head, grep, redirect. Follow controls
// HTTP redirect following: 0 = manual capture of Location, 1 = auto.
// For redirect mode set Follow to 0 and Pattern to the substring or
// regex the Location header must match.
//
// Compact output rule: only result data + errors. No progress logs.
//
// Usage: dotnet run scripts/fetch-url.cs scripts/<specific>.cs
using System.Net.Http;
using System.Text.RegularExpressions;
using Scripts;

if (args.Length < 1) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/fetch-url.cs <specific.cs>"); return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { await Console.Error.WriteLineAsync($"specific not found: {SpecPath}"); return 2; }
var Source = await File.ReadAllTextAsync(SpecPath);

string? BaseUrl = null;
var BaseMatch = FetchUrlPatterns.BaseUrlConst().Match(Source);
if (BaseMatch.Success) { BaseUrl = Regex.Unescape(BaseMatch.Groups["value"].Value); }

var ProbesMatch = FetchUrlPatterns.ProbesArray().Match(Source);
if (!ProbesMatch.Success)
{
    await Console.Error.WriteLineAsync("specific must declare: (string Label, string Path, string Mode, string Pattern, string Method, int Follow)[] Probes = [ ... ];");
    return 3;
}
var ProbesBody = ProbesMatch.Groups["body"].Value;
var Probes = FetchUrlPatterns.ProbeTuple().Matches(ProbesBody)
    .Select(M => (
        Label: Regex.Unescape(M.Groups["label"].Value),
        ProbePath: Regex.Unescape(M.Groups["path"].Value),
        Mode: Regex.Unescape(M.Groups["mode"].Value),
        Pattern: Regex.Unescape(M.Groups["pattern"].Value),
        Method: Regex.Unescape(M.Groups["method"].Value),
        Follow: int.Parse(M.Groups["follow"].Value, System.Globalization.CultureInfo.InvariantCulture)))
    .ToList();
if (Probes.Count == 0) { await Console.Error.WriteLineAsync("no probes parsed from Probes array"); return 4; }

var Failed = 0;
foreach (var (Label, ProbePath, Mode, Pattern, Method, Follow) in Probes)
{
    var Url = ProbePath.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? ProbePath : (BaseUrl + ProbePath);
    await Console.Out.WriteLineAsync($"=== {Label} ({Method} {Mode} {Url}) ===");
    using var Handler = new HttpClientHandler { AllowAutoRedirect = Follow != 0, CheckCertificateRevocationList = true };
    using var Hc = new HttpClient(Handler) { Timeout = TimeSpan.FromSeconds(30) };
    Hc.DefaultRequestHeaders.UserAgent.ParseAdd("WolfsTruckingCo-fetch-url/3.0");
    using var Req = new HttpRequestMessage(new HttpMethod(Method), Url);
    HttpResponseMessage Resp;
    try { Resp = await Hc.SendAsync(Req); }
    catch (HttpRequestException Ex) { await Console.Error.WriteLineAsync($"  [fail] {Ex.Message}"); Failed++; continue; }
    catch (TaskCanceledException) { await Console.Error.WriteLineAsync("  [fail] timeout"); Failed++; continue; }

    if (Mode == "redirect")
    {
        var Location = Resp.Headers.Location?.ToString() ?? string.Empty;
        var Status = (int)Resp.StatusCode;
        await Console.Out.WriteLineAsync($"  status={Status.ToString(System.Globalization.CultureInfo.InvariantCulture)} location={Location}");
        if (string.IsNullOrEmpty(Pattern))
        {
            if (Status is < 300 or >= 400) { await Console.Error.WriteLineAsync($"  [fail] expected 3xx, got {Status.ToString(System.Globalization.CultureInfo.InvariantCulture)}"); Failed++; }
            continue;
        }
        var Re = new Regex(Pattern, RegexOptions.IgnoreCase);
        if (!Re.IsMatch(Location)) { await Console.Error.WriteLineAsync($"  [fail] location does not match /{Pattern}/"); Failed++; }
        continue;
    }

    var Body = await Resp.Content.ReadAsStringAsync();

    if (Mode == "head")
    {
        await Console.Out.WriteLineAsync($"  {((int)Resp.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture)} {Resp.ReasonPhrase} | {Body.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)} bytes");
        continue;
    }
    if (Mode == "grep")
    {
        var Re = new Regex(Pattern, RegexOptions.IgnoreCase);
        var Lines = Body.Split('\n');
        var Hits = 0;
        for (var Idx = 0; Idx < Lines.Length; Idx++)
        {
            if (!Re.IsMatch(Lines[Idx])) { continue; }
            Hits++;
            await Console.Out.WriteLineAsync($"  {(Idx + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)}\t{Lines[Idx].TrimEnd()}");
        }
        if (Hits == 0) { await Console.Error.WriteLineAsync($"  [fail] no match for /{Pattern}/"); Failed++; }
        continue;
    }
    await Console.Out.WriteAsync(Body);
}
return Failed == 0 ? 0 : 5;

namespace Scripts
{
    internal static partial class FetchUrlPatterns
    {
        [GeneratedRegex("""const\s+string\s+BaseUrl\s*=\s*@?"(?<value>(?:[^"\\]|\\.)*)"\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex BaseUrlConst();

        [GeneratedRegex("""\(\s*string\s+Label\s*,\s*string\s+Path\s*,\s*string\s+Mode\s*,\s*string\s+Pattern\s*,\s*string\s+Method\s*,\s*int\s+Follow\s*\)\[\]\s+Probes\s*=\s*\[(?<body>[\s\S]*?)\]\s*;""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ProbesArray();

        [GeneratedRegex("""\(\s*"(?<label>(?:[^"\\]|\\.)*)"\s*,\s*"(?<path>(?:[^"\\]|\\.)*)"\s*,\s*"(?<mode>(?:[^"\\]|\\.)*)"\s*,\s*"(?<pattern>(?:[^"\\]|\\.)*)"\s*,\s*"(?<method>(?:[^"\\]|\\.)*)"\s*,\s*(?<follow>\d+)\s*\)""", RegexOptions.ExplicitCapture)]
        internal static partial Regex ProbeTuple();
    }
}
