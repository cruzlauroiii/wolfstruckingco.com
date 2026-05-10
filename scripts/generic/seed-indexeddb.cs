#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

using System.Globalization;
using System.Net.Http;
using System.Text;

if (args.Length < 1) return 1;
var Spec = await File.ReadAllLinesAsync(args[0]);

string? Get(string Name)
{
    foreach (var Line in Spec)
    {
        var Pat = "const string " + Name + " = ";
        var At = Line.IndexOf(Pat, StringComparison.Ordinal);
        if (At < 0) continue;
        var Tail = Line[(At + Pat.Length)..];
        if (Tail.Length > 0 && Tail[0] == '@') Tail = Tail[1..];
        if (Tail.Length == 0 || Tail[0] != '\u0022') continue;
        var End = Tail.LastIndexOf("\u0022;", StringComparison.Ordinal);
        if (End < 1) continue;
        return Tail[1..End];
    }
    return null;
}

var JsonlPath = Get("JsonlPath") ?? System.IO.Path.Combine(Environment.GetEnvironmentVariable("WOLFS_REPO") ?? Environment.CurrentDirectory, "data", "wolfs-db.jsonl");
var BaseUrl = Get("BaseUrl") ?? "https://cruzlauroiii.github.io/wolfstruckingco.com/";
var ServeUrl = Get("ServeUrl") ?? "http://127.0.0.1:9334/";
const string DbName = "wolfs";
const int DbVersion = 2;
var BootstrapStores = new[] { "users", "workers", "jobs", "timesheets", "applicants", "listings", "purchases", "badges", "roles", "customers", "audit", "schedules", "charges" };

static string EscJsonString(string S)
{
    var Sb = new StringBuilder();
    foreach (var Ch in S)
    {
        if (Ch == '\u0022') Sb.Append("\\\u0022");
        else if (Ch == '\\') Sb.Append("\\\\");
        else if (Ch == '\n') Sb.Append("\\n");
        else if (Ch == '\r') Sb.Append("\\r");
        else if (Ch == '\t') Sb.Append("\\t");
        else if (Ch < 0x20) Sb.Append("\\u").Append(((int)Ch).ToString("x4", CultureInfo.InvariantCulture));
        else Sb.Append(Ch);
    }
    return Sb.ToString();
}

var Lines = await File.ReadAllLinesAsync(JsonlPath);
var Rows = new List<string>();
var StoreSet = new SortedSet<string>(StringComparer.Ordinal);
foreach (var Bs in BootstrapStores) StoreSet.Add(Bs);
foreach (var L in Lines)
{
    if (string.IsNullOrWhiteSpace(L)) continue;
    Rows.Add(L);
    using var Doc = System.Text.Json.JsonDocument.Parse(L);
    if (Doc.RootElement.TryGetProperty("_store", out var SP) && SP.ValueKind == System.Text.Json.JsonValueKind.String)
    {
        var Sn = SP.GetString();
        if (!string.IsNullOrEmpty(Sn)) StoreSet.Add(Sn);
    }
}
var StoresJsSb = new StringBuilder();
StoresJsSb.Append('[');
var StoresFirst = true;
foreach (var Sn in StoreSet)
{
    if (!StoresFirst) StoresJsSb.Append(',');
    StoresFirst = false;
    StoresJsSb.Append('\'').Append(Sn).Append('\'');
}
StoresJsSb.Append(']');
var StoresJs = StoresJsSb.ToString();
await Console.Error.WriteLineAsync("seed-indexeddb: loaded " + Rows.Count.ToString(CultureInfo.InvariantCulture) + " rows");

using var Http = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
async Task<string> Post(string Cmd)
{
    using var Req = new HttpRequestMessage(HttpMethod.Post, new Uri(ServeUrl)) { Content = new StringContent(Cmd) };
    using var R = await Http.SendAsync(Req).ConfigureAwait(false);
    return await R.Content.ReadAsStringAsync().ConfigureAwait(false);
}

var ProbeOk = false;
for (var I = 0; I < 30; I++)
{
    var R = await Post("list_pages");
    if (!R.StartsWith("ERR:", StringComparison.Ordinal)) { ProbeOk = true; break; }
    await Task.Delay(2000);
}
if (!ProbeOk) { await Console.Error.WriteLineAsync("chrome-devtools serve unreachable on " + ServeUrl); return 2; }

await Post("new_page --url " + BaseUrl);
await Task.Delay(3000);

var VerStr = DbVersion.ToString(CultureInfo.InvariantCulture);

var ResetJs = "() => { return (async () => { try { await new Promise(r => { const req = indexedDB.deleteDatabase('" + DbName + "'); req.onsuccess = r; req.onerror = r; req.onblocked = r; }); const stores = " + StoresJs + "; await new Promise((res, rej) => { const r = indexedDB.open('" + DbName + "', " + VerStr + "); r.onupgradeneeded = () => { const d = r.result; stores.forEach(s => { if (!d.objectStoreNames.contains(s)) d.createObjectStore(s, { keyPath: 'id' }); }); }; r.onsuccess = () => { r.result.close(); res(true); }; r.onerror = () => rej(r.error); }); return 'reset:ok'; } catch(e) { return 'reset:err:' + ((e && e.message) ? e.message : String(e)); } })(); }";
var ResetCmd = "evaluate_script --function \u0022" + EscJsonString(ResetJs) + "\u0022";
var ResetResp = await Post(ResetCmd);
await Console.Error.WriteLineAsync("reset: " + ResetResp.Trim());

var BatchSize = 25;
for (var I = 0; I < Rows.Count; I += BatchSize)
{
    var BatchSb = new StringBuilder();
    BatchSb.Append('[');
    for (var J = I; J < Math.Min(I + BatchSize, Rows.Count); J++)
    {
        if (J > I) BatchSb.Append(',');
        BatchSb.Append(Rows[J]);
    }
    BatchSb.Append(']');
    var Js = "() => { return (async () => { try { const data = " + BatchSb.ToString() + "; const db = await new Promise((res, rej) => { const r = indexedDB.open('" + DbName + "', " + VerStr + "); r.onsuccess = () => res(r.result); r.onerror = () => rej(r.error); }); let i = 0; for (const row of data) { try { const s = row._store; const c = Object.assign({}, row); delete c._store; if (!db.objectStoreNames.contains(s)) { return 'err:row-' + i + ':missing-store-' + s; } await new Promise((res, rej) => { const tx = db.transaction(s, 'readwrite'); tx.objectStore(s).put(c); tx.oncomplete = () => res(true); tx.onerror = () => rej(tx.error); }); i++; } catch(e) { return 'err:row-' + i + ':' + ((e && e.message) ? e.message : String(e)); } } db.close(); return 'ok:' + i; } catch(e) { return 'err:outer:' + ((e && e.message) ? e.message : String(e)); } })(); }";
    var Cmd = "evaluate_script --function \u0022" + EscJsonString(Js) + "\u0022";
    var R2 = await Post(Cmd);
    var Trimmed = R2.Trim();
    await Console.Error.WriteLineAsync("batch " + (I / BatchSize + 1).ToString(CultureInfo.InvariantCulture) + " / " + ((Rows.Count + BatchSize - 1) / BatchSize).ToString(CultureInfo.InvariantCulture) + ": " + (Trimmed.Length > 200 ? Trimmed[..200] : Trimmed));
}

var VerifyJs = "() => { return (async () => { try { const db = await new Promise((res, rej) => { const r = indexedDB.open('" + DbName + "', " + VerStr + "); r.onsuccess = () => res(r.result); r.onerror = () => rej(r.error); }); const counts = {}; const stores = " + StoresJs + "; for (const s of stores) { counts[s] = await new Promise((res) => { const tx = db.transaction(s, 'readonly'); const req = tx.objectStore(s).count(); req.onsuccess = () => res(req.result); req.onerror = () => res(-1); }); } db.close(); return JSON.stringify(counts); } catch(e) { return 'verify:err:' + ((e && e.message) ? e.message : String(e)); } })(); }";
var VerifyCmd = "evaluate_script --function \u0022" + EscJsonString(VerifyJs) + "\u0022";
var VerifyResp = await Post(VerifyCmd);
await Console.Error.WriteLineAsync("verify: " + VerifyResp.Trim());
await Console.Error.WriteLineAsync("seed-indexeddb: done, " + Rows.Count.ToString(CultureInfo.InvariantCulture) + " rows seeded");
return 0;
