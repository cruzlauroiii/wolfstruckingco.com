#:property TargetFramework=net11.0
#:property PublishAot=false
#:property PublishTrimmed=false
#:property AnalysisLevel=none
#:property RunAnalyzers=false

using System.Net;
using System.Text;
using System.Text.Json.Nodes;

int port = 9334;
string voice = "en-US-JennyNeural";
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--port" && i + 1 < args.Length) port = int.Parse(args[++i]);
    else if (args[i] == "--voice" && i + 1 < args.Length) voice = args[++i];
}

var (accessToken, _) = VoiceSidecarLib.LoadTokens();
if (string.IsNullOrEmpty(accessToken))
{
    Console.Error.WriteLine("warn: no OAuth access token found. /stt will 401.");
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
            await VoiceSidecarLib.Cors(ctx);
            if (ctx.Request.HttpMethod == "OPTIONS")
            {
                ctx.Response.StatusCode = 204;
                ctx.Response.OutputStream.Close();
                return;
            }
            var path = ctx.Request.Url!.AbsolutePath.TrimEnd('/');
            if (path == "/health")
            {
                await VoiceSidecarLib.WriteJson(ctx, 200, "{\"ok\":true,\"voice\":" + VoiceSidecarLib.JsonStr(voice) + ",\"stt\":" + (!string.IsNullOrEmpty(accessToken) ? "true" : "false") + "}");
                return;
            }
            if (path == "/tts" && ctx.Request.HttpMethod == "POST")
            {
                using var sr = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
                var doc = JsonNode.Parse(await sr.ReadToEndAsync())?.AsObject();
                var text = doc?["text"]?.GetValue<string>() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(text)) { await VoiceSidecarLib.WriteJson(ctx, 400, "{\"error\":\"text required\"}"); return; }
                var mp3 = await VoiceSidecarLib.Tts(text, voice);
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "audio/mpeg";
                await ctx.Response.OutputStream.WriteAsync(mp3);
                ctx.Response.OutputStream.Close();
                return;
            }
            if (path == "/stt" && ctx.Request.HttpMethod == "POST")
            {
                if (string.IsNullOrEmpty(accessToken)) { await VoiceSidecarLib.WriteJson(ctx, 401, "{\"error\":\"OAuth token not configured\"}"); return; }
                using var ms = new MemoryStream();
                await ctx.Request.InputStream.CopyToAsync(ms);
                var audio = ms.ToArray();
                if (audio.Length == 0) { await VoiceSidecarLib.WriteJson(ctx, 400, "{\"error\":\"empty body\"}"); return; }
                var text = await VoiceSidecarLib.Stt(audio, accessToken);
                await VoiceSidecarLib.WriteJson(ctx, 200, "{\"text\":" + VoiceSidecarLib.JsonStr(text) + "}");
                return;
            }
            await VoiceSidecarLib.WriteJson(ctx, 404, "{\"error\":\"not found\"}");
        }
        catch (Exception ex)
        {
            try { await VoiceSidecarLib.WriteJson(ctx, 500, "{\"error\":" + VoiceSidecarLib.JsonStr(ex.Message) + "}"); } catch { }
            Console.Error.WriteLine("req err: " + ex);
        }
    });
}
