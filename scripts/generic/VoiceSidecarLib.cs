using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;

internal static class VoiceSidecarLib
{
    public static (string? access, string? refresh) LoadTokens()
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "UserSecrets");
        if (Directory.Exists(root))
        {
            foreach (var dir in Directory.EnumerateDirectories(root))
            {
                var file = Path.Combine(dir, "secrets.json");
                if (!File.Exists(file)) continue;
                try
                {
                    var doc = JsonNode.Parse(File.ReadAllText(file))?.AsObject();
                    if (doc is null) continue;
                    string? acc = doc["Voice:ClaudeOAuthAccessToken"]?.GetValue<string>();
                    if (acc is null && doc["Voice"] is JsonObject vo) acc = vo["ClaudeOAuthAccessToken"]?.GetValue<string>();
                    string? refv = doc["Voice:ClaudeOAuthRefreshToken"]?.GetValue<string>();
                    if (refv is null && doc["Voice"] is JsonObject vo2) refv = vo2["ClaudeOAuthRefreshToken"]?.GetValue<string>();
                    if (!string.IsNullOrEmpty(acc)) return (acc, refv);
                }
                catch { }
            }
        }
        var creds = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude", ".credentials.json");
        if (File.Exists(creds))
        {
            try
            {
                var oauth = JsonNode.Parse(File.ReadAllText(creds))?["claudeAiOauth"]?.AsObject();
                return (oauth?["accessToken"]?.GetValue<string>(), oauth?["refreshToken"]?.GetValue<string>());
            }
            catch { }
        }
        return (null, null);
    }

    public static async Task<byte[]> DecodeToLinear16_16k(byte[] input)
    {
        var psi = new ProcessStartInfo("ffmpeg") { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var a in new[] { "-hide_banner", "-loglevel", "error", "-i", "pipe:0", "-af", "silenceremove=start_periods=1:start_threshold=-45dB:start_silence=0.05:detection=peak", "-ac", "1", "-ar", "16000", "-f", "s16le", "-acodec", "pcm_s16le", "pipe:1" }) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi)!;
        var outTask = Task.Run(async () => { using var ms = new MemoryStream(); await p.StandardOutput.BaseStream.CopyToAsync(ms); return ms.ToArray(); });
        await p.StandardInput.BaseStream.WriteAsync(input);
        p.StandardInput.Close();
        var err = await p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();
        if (p.ExitCode != 0) throw new InvalidOperationException("ffmpeg decode failed: " + err);
        return await outTask;
    }

    public static async Task<byte[]> Tts(string text, string voice)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "wolfs-tts-" + Guid.NewGuid().ToString("N") + ".mp3");
        var psi = new ProcessStartInfo("edge-tts") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var a in new[] { "--voice", voice, "--text", text, "--write-media", tmp }) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi)!;
        var err = await p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();
        if (p.ExitCode != 0 || !File.Exists(tmp)) throw new InvalidOperationException("edge-tts failed: " + err);
        try { return await File.ReadAllBytesAsync(tmp); }
        finally { try { File.Delete(tmp); } catch { } }
    }

    public static async Task<string> Stt(byte[] audioBytes, string accessToken)
    {
        var pcm = await DecodeToLinear16_16k(audioBytes);
        var uri = new Uri("wss://api.anthropic.com/api/ws/speech_to_text/voice_stream?encoding=linear16&sample_rate=16000&channels=1&endpointing_ms=2000&utterance_end_ms=3500&language=en");
        using var ws = new ClientWebSocket();
        ws.Options.SetRequestHeader("Authorization", "Bearer " + accessToken);
        ws.Options.SetRequestHeader("User-Agent", "wolfs-voice-sidecar/1.0");
        ws.Options.SetRequestHeader("x-app", "cli");
        await ws.ConnectAsync(uri, CancellationToken.None);
        await ws.SendAsync(Encoding.UTF8.GetBytes("{\"type\":\"KeepAlive\"}"), WebSocketMessageType.Text, true, CancellationToken.None);
        const int chunk = 3200;
        for (int off = 0; off < pcm.Length; off += chunk)
        {
            await ws.SendAsync(new ArraySegment<byte>(pcm, off, Math.Min(chunk, pcm.Length - off)), WebSocketMessageType.Binary, true, CancellationToken.None);
        }
        var silence = new byte[16000 * 2];
        for (int off = 0; off < silence.Length; off += chunk)
        {
            await ws.SendAsync(new ArraySegment<byte>(silence, off, Math.Min(chunk, silence.Length - off)), WebSocketMessageType.Binary, true, CancellationToken.None);
        }
        await ws.SendAsync(Encoding.UTF8.GetBytes("{\"type\":\"CloseStream\"}"), WebSocketMessageType.Text, true, CancellationToken.None);
        var transcript = new StringBuilder();
        var buf = new byte[64 * 1024];
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            WebSocketReceiveResult r;
            try { r = await ws.ReceiveAsync(buf, CancellationToken.None); } catch (WebSocketException) { break; }
            if (r.MessageType == WebSocketMessageType.Close) break;
            if (r.MessageType != WebSocketMessageType.Text) continue;
            try
            {
                var obj = JsonNode.Parse(Encoding.UTF8.GetString(buf, 0, r.Count))?.AsObject();
                var type = obj?["type"]?.GetValue<string>();
                if (type == "TranscriptText") transcript.Append(obj?["data"]?.GetValue<string>() ?? "");
                else if (type is "TranscriptComplete" or "CloseStream" or "Closed") break;
            }
            catch { }
        }
        try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None); } catch { }
        return transcript.ToString().Trim();
    }

    public static Task Cors(HttpListenerContext ctx)
    {
        ctx.Response.Headers["Access-Control-Allow-Origin"] = "*";
        ctx.Response.Headers["Access-Control-Allow-Methods"] = "POST, OPTIONS";
        ctx.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type";
        return Task.CompletedTask;
    }

    public static async Task WriteJson(HttpListenerContext ctx, int status, string jsonBody)
    {
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";
        var bytes = Encoding.UTF8.GetBytes(jsonBody);
        await ctx.Response.OutputStream.WriteAsync(bytes);
        ctx.Response.OutputStream.Close();
    }

    public static string JsonStr(string? s)
    {
        if (s is null) return "null";
        var sb = new StringBuilder("\"");
        foreach (var c in s)
        {
            sb.Append(c switch
            {
                '\\' => "\\\\",
                '"' => "\\\"",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                '\b' => "\\b",
                '\f' => "\\f",
                _ => c < 0x20 ? "\\u" + ((int)c).ToString("x4") : c.ToString()
            });
        }
        return sb.Append('"').ToString();
    }
}
