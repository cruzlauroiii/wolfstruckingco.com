#:property TargetFramework=net11.0
#:property TreatWarningsAsErrors=false
#:property RunAnalyzersDuringBuild=false

// test-agent.cs — verifies the Wolfs Trucking AI dispatcher replies sensibly
// from text extracted by chrome-devtools.cs, before generating the video.
//
// Flow:
//   1. Spawn chrome-devtools.cs serve mode (if not already running) on :9333.
//   2. Open https://cruzlauroiii.github.io/wolfstruckingco.com/Dispatcher/.
//   3. evaluate_script to pull the visible chat-area text.
//   4. POST that text to the deployed worker /ai endpoint as a user message.
//   5. Assert reply: non-empty, ≤2000 chars, contains no leaked guardrails,
//      and references at least one Wolfs domain term (driver/job/dispatch/etc).
//
// Run:  dotnet run docs/videos/test-agent.cs

using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

const string CdpServeUrl = "http://127.0.0.1:9333";
const string DispatcherUrl = "https://cruzlauroiii.github.io/wolfstruckingco.com/Dispatcher/";
const string AiEndpoint = "https://wolfstruckingco.nbth.workers.dev/ai";

var Http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

bool ServeUp = false;
try { var Probe = await Http.GetAsync(CdpServeUrl + "/health"); ServeUp = Probe.IsSuccessStatusCode; }
catch { ServeUp = false; }
if (!ServeUp)
{
    Console.Error.WriteLine("chrome-devtools serve mode not running. Start it first:");
    Console.Error.WriteLine("  dotnet run C:/repo/public/chrome-devtools-cli/chrome-devtools.cs -- serve");
    return 2;
}

async Task<string> Cdp(string Cmd, object? Args = null)
{
    var Payload = JsonSerializer.Serialize(new { command = Cmd, args = Args ?? new { } });
    using var Req = new HttpRequestMessage(HttpMethod.Post, CdpServeUrl + "/run")
    {
        Content = new StringContent(Payload, Encoding.UTF8, "application/json"),
    };
    using var Resp = await Http.SendAsync(Req);
    return await Resp.Content.ReadAsStringAsync();
}

Console.WriteLine($"opening {DispatcherUrl}");
await Cdp("new_page", new { url = DispatcherUrl });
await Task.Delay(3500);

Console.WriteLine("extracting visible page text...");
var ExtractScript = "() => { const root = document.querySelector('.Stage') || document.body; return (root.innerText || root.textContent || '').replace(/\\s+/g, ' ').trim().slice(0, 600); }";
var ExtractJson = await Cdp("evaluate_script", new { script = ExtractScript });
var ExtractedText = JsonNode.Parse(ExtractJson)?["result"]?.GetValue<string>() ?? "";
if (string.IsNullOrWhiteSpace(ExtractedText))
{
    Console.Error.WriteLine("FAIL: no visible page text extracted from /Dispatcher/");
    return 3;
}
Console.WriteLine($"extracted {ExtractedText.Length} chars: {ExtractedText[..Math.Min(160, ExtractedText.Length)]}...");

var UserMessage = $"From the live page I'm viewing: {ExtractedText}\n\nGiven that, what's the next step a driver should take to get assigned a job?";

var AiPayload = new
{
    model = "claude-opus-4-7",
    max_tokens = 600,
    system = "You are testing the Wolfs Trucking dispatcher.",
    messages = new[] { new { role = "user", content = UserMessage } },
};

using var AiReq = new HttpRequestMessage(HttpMethod.Post, AiEndpoint)
{
    Content = new StringContent(JsonSerializer.Serialize(AiPayload), Encoding.UTF8, "application/json"),
};
AiReq.Headers.TryAddWithoutValidation("X-Wolfs-Session", "test_" + DateTime.UtcNow.Ticks);
AiReq.Headers.TryAddWithoutValidation("X-Wolfs-Email", "test@wolfstruckingco.com");
AiReq.Headers.TryAddWithoutValidation("X-Wolfs-Role", "driver");

Console.WriteLine($"POST {AiEndpoint}");
using var AiResp = await Http.SendAsync(AiReq);
var AiBody = await AiResp.Content.ReadAsStringAsync();
Console.WriteLine($"status: {(int)AiResp.StatusCode} {AiResp.StatusCode}");

if (!AiResp.IsSuccessStatusCode)
{
    Console.Error.WriteLine("FAIL: /ai returned non-2xx");
    Console.Error.WriteLine(AiBody.Length > 600 ? AiBody[..600] + "..." : AiBody);
    return 4;
}

var AiNode = JsonNode.Parse(AiBody);
var Reply = AiNode?["text"]?.GetValue<string>()
    ?? AiNode?["content"]?[0]?["text"]?.GetValue<string>()
    ?? "";

if (string.IsNullOrWhiteSpace(Reply))
{
    Console.Error.WriteLine("FAIL: empty reply");
    Console.Error.WriteLine("body: " + (AiBody.Length > 400 ? AiBody[..400] + "..." : AiBody));
    return 5;
}
if (Reply.Length > 2000) { Console.Error.WriteLine($"FAIL: reply too long ({Reply.Length} chars)"); return 6; }

var Lower = Reply.ToLowerInvariant();
var DomainTerms = new[] { "driver", "job", "dispatch", "wolfs", "trucking", "load", "freight", "leg", "route", "delivery", "shipment" };
if (!DomainTerms.Any(t => Lower.Contains(t)))
{
    Console.Error.WriteLine("FAIL: reply doesn't reference any Wolfs domain terms");
    Console.Error.WriteLine("reply: " + Reply);
    return 7;
}

var LeakedTerms = new[] { "anthropic_api_key", "x-api-key", "wrangler.toml", "secrets_store", "ROLE LOCK" };
foreach (var Bad in LeakedTerms)
{
    if (Lower.Contains(Bad.ToLowerInvariant())) { Console.Error.WriteLine($"FAIL: reply leaked guardrail/secret: {Bad}"); return 8; }
}

Console.WriteLine();
Console.WriteLine("PASS — reply is contextual, in-scope, and no guardrail leakage.");
Console.WriteLine($"  reply len: {Reply.Length} chars");
Console.WriteLine($"  reply: {Reply[..Math.Min(280, Reply.Length)]}...");
return 0;
