#:property TargetFramework=net11.0

using System;
using System.IO;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

if (args.Length < 1) { return 1; }
var SpecPath = args[0];
if (!File.Exists(SpecPath)) { return 2; }

var Body = await File.ReadAllTextAsync(SpecPath);
string SFromCfg(string Name, string Default)
{
    var VerbM = Regex.Match(Body, @"const\s+string\s+" + Name + "\\s*=\\s*@\"(?<v>(?:[^\"]|\"\")*)\"\\s*;", RegexOptions.ExplicitCapture);
    if (VerbM.Success) { return VerbM.Groups["v"].Value.Replace("\"\"", "\""); }
    var RegM = Regex.Match(Body, @"const\s+string\s+" + Name + "\\s*=\\s*\"(?<v>(?:[^\"\\\\]|\\\\.)*)\"\\s*;", RegexOptions.ExplicitCapture);
    return RegM.Success ? Regex.Unescape(RegM.Groups["v"].Value) : Default;
}

var Voice = SFromCfg("Voice", "en-US-AnaNeural");
var Text = SFromCfg("Text", string.Empty);
var OutputPath = SFromCfg("OutputPath", string.Empty);
var Rate = SFromCfg("Rate", "+5%");
var Pitch = SFromCfg("Pitch", "+0Hz");

if (string.IsNullOrEmpty(Text)) { await Console.Error.WriteLineAsync("Text const required"); return 3; }
if (string.IsNullOrEmpty(OutputPath)) { await Console.Error.WriteLineAsync("OutputPath const required"); return 4; }

var ConnectionId = Guid.NewGuid().ToString("N");
var TrustedToken = "6A5AA1D4EAFF4E9FB37E23D68491D6F4";

static string GenerateSecMsGec(string Token)
{
    var FileTime = DateTime.UtcNow.ToFileTimeUtc();
    FileTime -= FileTime % 3_000_000_000L;
    var Input = FileTime.ToString(System.Globalization.CultureInfo.InvariantCulture) + Token;
    var Bytes = SHA256.HashData(Encoding.ASCII.GetBytes(Input));
    return Convert.ToHexString(Bytes);
}

var SecMsGec = GenerateSecMsGec(TrustedToken);
var GecVersion = "1-130.0.2849.68";
var WsUri = new Uri(
    "wss://speech.platform.bing.com/consumer/speech/synthesize/readaloud/edge/v1?" +
    "TrustedClientToken=" + TrustedToken +
    "&Sec-MS-GEC=" + SecMsGec +
    "&Sec-MS-GEC-Version=" + GecVersion +
    "&ConnectionId=" + ConnectionId);

using var Ws = new ClientWebSocket();
Ws.Options.SetRequestHeader("Origin", "chrome-extension://jdiccldimpdaibmpdkjnbmckianbfold");
Ws.Options.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36 Edg/130.0.0.0");
Ws.Options.SetRequestHeader("Accept-Encoding", "gzip, deflate, br");
Ws.Options.SetRequestHeader("Accept-Language", "en-US,en;q=0.9");
Ws.Options.SetRequestHeader("Pragma", "no-cache");
Ws.Options.SetRequestHeader("Cache-Control", "no-cache");
using var Cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
await Ws.ConnectAsync(WsUri, Cts.Token);

string Now() => DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", System.Globalization.CultureInfo.InvariantCulture);
async Task SendStr(string Payload)
{
    var Bytes = Encoding.UTF8.GetBytes(Payload);
    await Ws.SendAsync(Bytes, WebSocketMessageType.Text, true, Cts.Token);
}

var ConfigMsg =
    "X-Timestamp:" + Now() + "\r\n" +
    "Content-Type:application/json; charset=utf-8\r\n" +
    "Path:speech.config\r\n\r\n" +
    "{\"context\":{\"synthesis\":{\"audio\":{\"metadataoptions\":{\"sentenceBoundaryEnabled\":\"false\",\"wordBoundaryEnabled\":\"false\"},\"outputFormat\":\"riff-24khz-16bit-mono-pcm\"}}}}";
await SendStr(ConfigMsg);

var SafeText = System.Net.WebUtility.HtmlEncode(Text);
var Ssml =
    "<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>" +
    "<voice name='" + Voice + "'><prosody rate='" + Rate + "' pitch='" + Pitch + "'>" + SafeText + "</prosody></voice></speak>";

var SsmlMsg =
    "X-RequestId:" + ConnectionId + "\r\n" +
    "X-Timestamp:" + Now() + "\r\n" +
    "Content-Type:application/ssml+xml\r\n" +
    "Path:ssml\r\n\r\n" +
    Ssml;
await SendStr(SsmlMsg);

using var AudioBuffer = new MemoryStream();
var Buf = new byte[1 << 16];
while (Ws.State == WebSocketState.Open)
{
    using var Frame = new MemoryStream();
    WebSocketReceiveResult Result;
    do
    {
        Result = await Ws.ReceiveAsync(Buf, Cts.Token);
        if (Result.MessageType == WebSocketMessageType.Close) { break; }
        await Frame.WriteAsync(Buf.AsMemory(0, Result.Count), Cts.Token);
    }
    while (!Result.EndOfMessage);
    if (Result.MessageType == WebSocketMessageType.Close) { break; }

    var FrameBytes = Frame.ToArray();
    if (Result.MessageType == WebSocketMessageType.Text)
    {
        var TextMsg = Encoding.UTF8.GetString(FrameBytes);
        if (TextMsg.Contains("Path:turn.end", StringComparison.Ordinal)) { break; }
    }
    else
    {
        var SepIdx = -1;
        for (var I = 0; I <= FrameBytes.Length - 4; I++)
        {
            if (FrameBytes[I] == 0x0D && FrameBytes[I + 1] == 0x0A && FrameBytes[I + 2] == 0x0D && FrameBytes[I + 3] == 0x0A) { SepIdx = I; break; }
        }
        if (SepIdx > 0)
        {
            var AudioStart = SepIdx + 4;
            await AudioBuffer.WriteAsync(FrameBytes.AsMemory(AudioStart, FrameBytes.Length - AudioStart), Cts.Token);
        }
    }
}

try
{
    await Ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", Cts.Token);
}
catch (WebSocketException)
{
}

if (AudioBuffer.Length == 0) { await Console.Error.WriteLineAsync("no audio received"); return 5; }
await File.WriteAllBytesAsync(OutputPath, AudioBuffer.ToArray());
return 0;
