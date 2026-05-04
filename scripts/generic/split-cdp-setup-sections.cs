#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Text.RegularExpressions;

if (args.Length < 1) return 1;
var spec = await File.ReadAllTextAsync(args[0]);
string Get(string name)
{
    var m = Regex.Match(spec, @"const\s+string\s+" + name + @"\s*=\s*@?""(?<v>[^""]*)""");
    return m.Success ? m.Groups["v"].Value : "";
}

var source = Get("SourcePath");
if (!File.Exists(source)) return 2;
var text = await File.ReadAllTextAsync(source);
var dir = Path.GetDirectoryName(source)!;

string Wrap(string body) => """
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using File = System.IO.File;
using Path = System.IO.Path;

namespace CdpTool;

public sealed partial class CdpCli
{
""" + "\n" + body.Trim() + "\n}\n";

string ExtractRange(string start, string end)
{
    var s = text.IndexOf(start, StringComparison.Ordinal);
    var e = text.IndexOf(end, s, StringComparison.Ordinal);
    if (s < 0 || e < 0 || e <= s) throw new InvalidOperationException(start);
    var body = text[s..e];
    text = text[..s] + text[e..];
    return body;
}

string ExtractToFinalBrace(string start)
{
    var s = text.IndexOf(start, StringComparison.Ordinal);
    var e = text.LastIndexOf("\n}", StringComparison.Ordinal);
    if (s < 0 || e < 0 || e <= s) throw new InvalidOperationException(start);
    var body = text[s..e];
    text = text[..s] + text[e..];
    return body;
}

var ui = ExtractRange("    private static void ExecuteScreenshotDesktop", "    private static (string Command, Dictionary<string, object> Args) ParseArgs");
var argsHelp = ExtractToFinalBrace("    private static (string Command, Dictionary<string, object> Args) ParseArgs");
await File.WriteAllTextAsync(Path.Combine(dir, "CdpSetupUi.cs"), Wrap(ui));
await File.WriteAllTextAsync(Path.Combine(dir, "CdpSetupArgs.cs"), Wrap(argsHelp));

var marker = "using Path = System.IO.Path;\r\n";
var insertAt = text.IndexOf(marker, StringComparison.Ordinal);
text = text.Insert(insertAt + marker.Length, "\r\n#:include CdpSetupUi.cs\r\n#:include CdpSetupArgs.cs\r\n");
await File.WriteAllTextAsync(source, text);
Console.WriteLine("CdpSetup sections split");
return 0;
