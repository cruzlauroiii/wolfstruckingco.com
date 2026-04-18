#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
#:include script-paths.cs
#:include ../specific/wipe-db-config.cs

// wipe-db.cs - Generic. Posts to the worker's admin-only wipe endpoint
// declared in wipe-db-config.cs (WipeUrl, VerifyUrl, RoleValue, etc.)
// to clear every R2 collection. Optionally verifies the wipe by GETing
// VerifyUrl and asserting an empty items array.
//
// Output: status line per step. Exit code 0 = wipe ok + verify empty,
// non-zero on any HTTP failure or non-empty verify.
//
// Usage: dotnet run scripts/wipe-db.cs
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Scripts;

using var Hc = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
Hc.DefaultRequestHeaders.UserAgent.ParseAdd("WolfsTruckingCo-wipe-db/1.0");

var Session = WipeDbConfig.SessionPrefix + Guid.NewGuid().ToString("N")[..16];
using var WipeReq = new HttpRequestMessage(HttpMethod.Post, WipeDbConfig.WipeUrl);
WipeReq.Headers.TryAddWithoutValidation(WipeDbConfig.SessionHeader, Session);
WipeReq.Headers.TryAddWithoutValidation(WipeDbConfig.RoleHeader, WipeDbConfig.RoleValue);
WipeReq.Content = new StringContent(string.Empty);
WipeReq.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

HttpResponseMessage WipeResp;
try { WipeResp = await Hc.SendAsync(WipeReq); }
catch (HttpRequestException Ex) { await Console.Error.WriteLineAsync($"wipe transport error: {Ex.Message}"); return 1; }
catch (TaskCanceledException) { await Console.Error.WriteLineAsync("wipe timeout"); return 1; }

if (!WipeResp.IsSuccessStatusCode) { return 2; }

using var VerifyReq = new HttpRequestMessage(HttpMethod.Get, WipeDbConfig.VerifyUrl);
HttpResponseMessage VerifyResp;
try { VerifyResp = await Hc.SendAsync(VerifyReq); }
catch (HttpRequestException Ex) { await Console.Error.WriteLineAsync($"verify transport error: {Ex.Message}"); return 3; }
catch (TaskCanceledException) { await Console.Error.WriteLineAsync("verify timeout"); return 3; }

var VerifyBody = await VerifyResp.Content.ReadAsStringAsync();
if (!VerifyResp.IsSuccessStatusCode) { return 4; }

try
{
    using var Doc = JsonDocument.Parse(VerifyBody);
    var Count = Doc.RootElement.TryGetProperty("count", out var C) ? C.GetInt32() : -1;
    var Items = Doc.RootElement.TryGetProperty("items", out var I) ? I.GetArrayLength() : -1;
    if (Count == 0 && Items == 0) { return 0; }
    await Console.Error.WriteLineAsync($"verify mismatch: count={Count} items={Items}");
    return 5;
}
catch (JsonException Ex) { await Console.Error.WriteLineAsync($"verify parse error: {Ex.Message}"); return 6; }

