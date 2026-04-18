#:property TargetFramework=net11.0
#:property PublishAot=false
#:property PublishTrimmed=false
#:property AnalysisLevel=none
#:property RunAnalyzers=false

// voice-sidecar.cs — local HTTP bridge used by the Dispatcher "Call" UI.
//
//   dotnet run voice-sidecar.cs
//
// POST /tts  body: {"text":"..."}              → audio/mpeg (edge-tts en-US-AriaNeural — same voice as the narrated walkthrough)
// POST /stt  body: webm/ogg/mp4 audio bytes    → {"text":"..."} via Anthropic voice_stream WebSocket (OAuth from dotnet user-secrets)
//
// Secrets (one-time): from a shell in a .NET project dir (e.g. any PrTask/Server folder that's already initialised):
//   dotnet user-secrets set "Voice:ClaudeOAuthAccessToken"  "<sk-ant-oat01-...>"
//   dotnet user-secrets set "Voice:ClaudeOAuthRefreshToken" "<sk-ant-ort01-...>"   (optional, for future refresh support)
// Or the sidecar will fall back to reading %USERPROFILE%\.claude\.credentials.json — the same file the /voice tool uses.
//
// NB: deliberately does not use browser-native SpeechRecognition / speechSynthesis. The user asked for the /voice
//     tool's STT pipeline (Anthropic voice_stream) and the video's Aria voice for TTS — this sidecar owns both.
using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;

int port = 9334;
string voice = "en-US-AriaNeural";
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--port" && i + 1 < args.Length) port = int.Parse(args[++i]);
    else if (args[i] == "--voice" && i + 1 < args.Length) voice = args[++i];
}

(string? access, string? refresh) LoadTokens()
{

    // 1) dotnet user-secrets (shared across any project that set Voice:*).
    //    We read the user-secrets store directly — works regardless of which project id owns it,
    //    by scanning %APPDATA%/Microsoft/UserSecrets/*/secrets.json for the Voice keys.
    var userSecretsRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "UserSecrets");
    if (Directory.Exists(userSecretsRoot))
    {
        foreach (var dir in Directory.EnumerateDirectories(userSecretsRoot))
        {
            var file = Path.Combine(dir, "secrets.json");
            if (!File.Exists(file)) continue;
            try
            {
                var doc = JsonNode.Parse(File.ReadAllText(file))?.AsObject();
                if (doc is null) continue;
                string? acc = doc["Voice:ClaudeOAuthAccessToken"]?.GetValue<string>();
                if (acc is null && doc["Voice"] is JsonObject vo) acc = vo["ClaudeOAuthAccessToken"]?.GetValue<string>();
                string? ref_ = doc["Voice:ClaudeOAuthRefreshToken"]?.GetValue<string>();
                if (ref_ is null && doc["Voice"] is JsonObject vo2) ref_ = vo2["ClaudeOAuthRefreshToken"]?.GetValue<string>();
                if (!string.IsNullOrEmpty(acc)) return (acc, ref_);
            }
            catch { }
        }
    }

    // 2) Fallback: %USERPROFILE%/.claude/.credentials.json — same file /voice consumes.
    var creds = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude", ".credentials.json");
    if (File.Exists(creds))
    {
        try
        {
            var doc = JsonNode.Parse(File.ReadAllText(creds))?.AsObject();
            var oauth = doc?["claudeAiOauth"]?.AsObject();
            return (oauth?["accessToken"]?.GetValue<string>(), oauth?["refreshToken"]?.GetValue<string>());
        }
        catch { }
    }
    return (null, null);
}

async Task<byte[]> DecodeToLinear16_16k(byte[] input)
{

    // Decode whatever the browser sent (webm/opus, mp4/aac, ogg) to raw 16-bit PCM mono @16kHz.
    // We pipe the input in on stdin and read stdout, so ffmpeg never touches disk.
    var psi = new ProcessStartInfo
    {
        FileName = "ffmpeg",
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    };

    // `silenceremove` drops leading silence below -50 dB up to the first 100ms of voiced
    // audio. Anthropic's voice_stream behaves badly when the stream opens with >~500ms
    // of pure silence — it returns an empty transcript or drops the first phrase. The
    // browser's MediaRecorder always opens with a blank window while the mic ramps up,
    // so we strip it here before streaming to the STT server.
    foreach (var arg in new[]
    {
        "-hide_banner", "-loglevel", "error",
        "-i", "pipe:0",

        // Strip leading silence only; keep trailing silence intact because Anthropic's
        // voice_stream uses it to finalize the end-of-utterance transcript. -45dB is low
        // enough to clip browser-mic hum without eating voiced speech.
        "-af", "silenceremove=start_periods=1:start_threshold=-45dB:start_silence=0.05:detection=peak",
        "-ac", "1",
        "-ar", "16000",
        "-f", "s16le",
        "-acodec", "pcm_s16le",
        "pipe:1",
    }) psi.ArgumentList.Add(arg);
    var p = Process.Start(psi)!;
    var stdoutTask = Task.Run(async () =>
    {
        using var ms = new MemoryStream();
        await p.StandardOutput.BaseStream.CopyToAsync(ms);
        return ms.ToArray();
    });
    await p.StandardInput.BaseStream.WriteAsync(input);
    p.StandardInput.Close();
    var err = await p.StandardError.ReadToEndAsync();
    await p.WaitForExitAsync();
    if (p.ExitCode != 0) throw new InvalidOperationException("ffmpeg decode failed: " + err);
    return await stdoutTask;
}

async Task<byte[]> Tts(string text)
{

    // edge-tts reads --text and writes mp3 to --write-media. We point it at a scratch file so we
    // don't have to bolt on its Azure TLS handshake ourselves.
    var tmp = Path.Combine(Path.GetTempPath(), "wolfs-tts-" + Guid.NewGuid().ToString("N") + ".mp3");
    var psi = new ProcessStartInfo { FileName = "edge-tts", RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
    psi.ArgumentList.Add("--voice"); psi.ArgumentList.Add(voice);
    psi.ArgumentList.Add("--text"); psi.ArgumentList.Add(text);
    psi.ArgumentList.Add("--write-media"); psi.ArgumentList.Add(tmp);
    var p = Process.Start(psi)!;
    var err = await p.StandardError.ReadToEndAsync();
    await p.WaitForExitAsync();
    if (p.ExitCode != 0 || !File.Exists(tmp)) throw new InvalidOperationException("edge-tts failed: " + err);
    try { return await File.ReadAllBytesAsync(tmp); }
    finally { try { File.Delete(tmp); } catch { } }
}

async Task<string> Stt(byte[] audioBytes, string accessToken)
{
    var pcm = await DecodeToLinear16_16k(audioBytes);

    // Larger endpointing so brief mid-sentence pauses ("hi hello … how are you") don't
    // prematurely split the utterance. We already decide when the turn is over via the
    // client-side VAD + stop() call.
    var qs = "encoding=linear16&sample_rate=16000&channels=1&endpointing_ms=2000&utterance_end_ms=3500&language=en";
    var uri = new Uri("wss://api.anthropic.com/api/ws/speech_to_text/voice_stream?" + qs);
    using var ws = new ClientWebSocket();
    ws.Options.SetRequestHeader("Authorization", "Bearer " + accessToken);
    ws.Options.SetRequestHeader("User-Agent", "wolfs-voice-sidecar/1.0");
    ws.Options.SetRequestHeader("x-app", "cli");
    await ws.ConnectAsync(uri, CancellationToken.None);

    await ws.SendAsync(Encoding.UTF8.GetBytes("{\"type\":\"KeepAlive\"}"), WebSocketMessageType.Text, true, CancellationToken.None);

    // Stream the PCM in ~100ms chunks (3200 bytes = 1600 samples @16kHz @16-bit) so the server
    // endpointer can start producing partial transcripts while we're still sending.
    const int chunk = 3200;
    for (int off = 0; off < pcm.Length; off += chunk)
    {
        var len = Math.Min(chunk, pcm.Length - off);
        await ws.SendAsync(new ArraySegment<byte>(pcm, off, len), WebSocketMessageType.Binary, true, CancellationToken.None);
    }

    // Pad 1 second of silence so the server endpointer sees a clean utterance boundary
    // AND has time to finalize the tail transcript before we tell it to close.
    var silence = new byte[16000 * 2];
    for (int off = 0; off < silence.Length; off += chunk)
    {
        var len = Math.Min(chunk, silence.Length - off);
        await ws.SendAsync(new ArraySegment<byte>(silence, off, len), WebSocketMessageType.Binary, true, CancellationToken.None);
    }
    await ws.SendAsync(Encoding.UTF8.GetBytes("{\"type\":\"CloseStream\"}"), WebSocketMessageType.Text, true, CancellationToken.None);
    Console.WriteLine($"[stt] sent {pcm.Length} bytes PCM + 1s silence pad → waiting for final transcript");

    var transcript = new StringBuilder();
    var buf = new byte[64 * 1024];
    var deadline = DateTime.UtcNow.AddSeconds(20);

    // Keep reading until the server actually closes the socket (or we hit the deadline).
    // The original code broke on the first `TranscriptEndpoint`, which fires after the
    // first mid-utterance pause — that's why "hi hello how are you" was chopping to
    // "hi hello". Endpoint events are informational; only CloseStream/Close ends the turn.
    while (DateTime.UtcNow < deadline)
    {
        WebSocketReceiveResult r;
        try { r = await ws.ReceiveAsync(buf, CancellationToken.None); }
        catch (WebSocketException) { break; }
        if (r.MessageType == WebSocketMessageType.Close) { break; }
        if (r.MessageType != WebSocketMessageType.Text) { continue; }
        var msg = Encoding.UTF8.GetString(buf, 0, r.Count);
        Console.WriteLine($"[stt<-] {msg}");
        try
        {
            var obj = JsonNode.Parse(msg)?.AsObject();
            var type = obj?["type"]?.GetValue<string>();
            if (type == "TranscriptText")
            {
                var data = obj?["data"]?.GetValue<string>() ?? string.Empty;
                transcript.Append(data);
            }
            else if (type == "TranscriptComplete" || type == "CloseStream" || type == "Closed")
            {

                // authoritative end-of-turn from the server
                break;
            }

            // TranscriptEndpoint / TranscriptPartial are informational only — keep listening.
        }
        catch { }
    }
    try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None); } catch { }
    return transcript.ToString().Trim();
}

static async Task Cors(HttpListenerContext ctx)
{
    ctx.Response.Headers["Access-Control-Allow-Origin"] = "*";
    ctx.Response.Headers["Access-Control-Allow-Methods"] = "POST, OPTIONS";
    ctx.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type";
    await Task.CompletedTask;
}

static async Task WriteJson(HttpListenerContext ctx, int status, string jsonBody)
{
    ctx.Response.StatusCode = status;
    ctx.Response.ContentType = "application/json";
    var bytes = Encoding.UTF8.GetBytes(jsonBody);
    await ctx.Response.OutputStream.WriteAsync(bytes);
    ctx.Response.OutputStream.Close();
}

static string JsonStr(string? s)
{
    if (s is null) return "null";
    var sb = new StringBuilder();
    sb.Append('"');
    foreach (var c in s)
    {
        switch (c)
        {
            case '\\': sb.Append("\\\\"); break;
            case '"': sb.Append("\\\""); break;
            case '\n': sb.Append("\\n"); break;
            case '\r': sb.Append("\\r"); break;
            case '\t': sb.Append("\\t"); break;
            case '\b': sb.Append("\\b"); break;
            case '\f': sb.Append("\\f"); break;
            default:
                if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                else sb.Append(c);
                break;
        }
    }
    sb.Append('"');
    return sb.ToString();
}

var (accessToken, _) = LoadTokens();
if (string.IsNullOrEmpty(accessToken))
{
    Console.Error.WriteLine("warn: no OAuth access token found. /stt will 401 until you set Voice:ClaudeOAuthAccessToken or log in via /voice.");
}

var listener = new HttpListener();
listener.Prefixes.Add($"http://localhost:{port}/");
listener.Prefixes.Add($"http://127.0.0.1:{port}/");
listener.Start();
Console.WriteLine($"voice-sidecar listening on http://localhost:{port}/  (tts voice={voice}, stt={(string.IsNullOrEmpty(accessToken) ? "DISABLED" : "ready")})");

while (true)
{
    var ctx = await listener.GetContextAsync();
    _ = Task.Run(async () =>
    {
        try
        {
            await Cors(ctx);
            if (ctx.Request.HttpMethod == "OPTIONS") { ctx.Response.StatusCode = 204; ctx.Response.OutputStream.Close(); return; }
            var path = ctx.Request.Url!.AbsolutePath.TrimEnd('/');
            if (path == "/health")
            {
                await WriteJson(ctx, 200, "{\"ok\":true,\"voice\":" + JsonStr(voice) + ",\"stt\":" + (!string.IsNullOrEmpty(accessToken) ? "true" : "false") + "}");
                return;
            }
            if (path == "/tts" && ctx.Request.HttpMethod == "POST")
            {
                using var sr = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
                var body = await sr.ReadToEndAsync();
                var doc = JsonNode.Parse(body)?.AsObject();
                var text = doc?["text"]?.GetValue<string>() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(text)) { await WriteJson(ctx, 400, "{\"error\":\"text required\"}"); return; }
                var mp3 = await Tts(text);
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "audio/mpeg";
                await ctx.Response.OutputStream.WriteAsync(mp3);
                ctx.Response.OutputStream.Close();
                return;
            }
            if (path == "/stt" && ctx.Request.HttpMethod == "POST")
            {
                if (string.IsNullOrEmpty(accessToken)) { await WriteJson(ctx, 401, "{\"error\":\"OAuth token not configured\"}"); return; }
                using var ms = new MemoryStream();
                await ctx.Request.InputStream.CopyToAsync(ms);
                var audio = ms.ToArray();
                if (audio.Length == 0) { await WriteJson(ctx, 400, "{\"error\":\"empty body\"}"); return; }
                var text = await Stt(audio, accessToken);
                await WriteJson(ctx, 200, "{\"text\":" + JsonStr(text) + "}");
                return;
            }
            await WriteJson(ctx, 404, "{\"error\":\"not found\"}");
        }
        catch (Exception ex)
        {
            try { await WriteJson(ctx, 500, "{\"error\":" + JsonStr(ex.Message) + "}"); } catch { }
            Console.Error.WriteLine("req err: " + ex);
        }
    });
}
