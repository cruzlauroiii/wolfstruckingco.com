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
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;
using File = System.IO.File;
using Path = System.IO.Path;

namespace CdpTool;

public sealed partial class CdpCli
{
""" + "\n" + body.Trim() + "\n}\n";

string Extract(string start, string end)
{
    var s = text.IndexOf(start, StringComparison.Ordinal);
    var e = text.IndexOf(end, s, StringComparison.Ordinal);
    if (s < 0 || e < 0 || e <= s) throw new InvalidOperationException("markers not found: " + start);
    var body = text[s..e];
    text = text[..s] + text[e..];
    return body;
}

var serve = Extract("    private async Task RunServeModeAsync", "    private static readonly int[] FallbackPorts");
var connection = Extract("    private static readonly int[] FallbackPorts", "    internal async Task<JsonNode?> SendCommandAsync");
var send = Extract("    internal async Task<JsonNode?> SendCommandAsync", "    private static string BuildUidSelector");
var selector = Extract("    private static string BuildUidSelector", "}\r\n}");
send += "\n" + selector;

await File.WriteAllTextAsync(Path.Combine(dir, "CdpCliServe.cs"), Wrap(serve));
await File.WriteAllTextAsync(Path.Combine(dir, "CdpCliConnection.cs"), Wrap(connection));
await File.WriteAllTextAsync(Path.Combine(dir, "CdpCliTransport.cs"), Wrap(send));

var include = "#:include CdpCliServe.cs\r\n#:include CdpCliConnection.cs\r\n#:include CdpCliTransport.cs\r\n";
var insertAt = text.IndexOf("#:include CdpSetup.cs", StringComparison.Ordinal);
if (insertAt < 0) return 3;
var lineEnd = text.IndexOf('\n', insertAt);
text = text.Insert(lineEnd + 1, include);
await File.WriteAllTextAsync(source, text);
Console.WriteLine("chrome-devtools sections split");
return 0;
