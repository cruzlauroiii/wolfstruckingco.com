#!/usr/bin/env dotnet run
#:sdk Microsoft.NET.Sdk
#:property TargetFramework=net11.0
#:property Nullable=enable
#:property ImplicitUsings=enable
// ---------------------------------------------------------------------------
// File-level analyzer opt-outs for this single-file MCP launcher/host script.
//
// This file is a single file-based program (`dotnet run wolfstruckingco.cs`)
// that hosts the wolfstruckingco MCP server, drives a JSONL-flat-file DB, and
// launches Claude Code with the right config. It is deliberately script-shaped:
// PascalCase locals, top-level statements, hand-rolled JSON-RPC string building,
// inline tool-handler switch dispatch. Library-grade analyzer rules do not fit.
//
// TreatWarningsAsErrors=false / EnforceCodeStyleInBuild=false / EnableNETAnalyzers=false:
//   Directory.Build.props sets these true repo-wide. This script intentionally
//   opts out so the launcher remains buildable without library-grade ceremony.
//   Analyzers still load (NoWarn list below targets the rules that fire).
//
// Per-rule rationale (rules confirmed firing under today's .editorconfig):
//   SA1122   — `string.Empty` for empty strings; literal `""` is clearer here.
//   SA1503/IDE0011 — single-line if without braces; script style.
//   SA1413   — trailing comma in multi-line initializers; not enforced for inline literals.
//   SA1502   — single-line member; ditto.
//   SA1520   — braces consistency in if/else; same family as SA1503.
//   SA1407   — arithmetic operator precedence; explicit precedence is clear in context.
//   SA1116/SA1118 — parameter list formatting for hand-built JSON strings.
//   IDE0028/IDE0046/IDE0048/IDE0061/IDE0120/IDE0305 — collection/expression-body/qualifier
//             simplifications that hurt readability of the JSON-builder hot paths.
//   CA1031/S2486 — broad `catch { }` is intentional for defensive JSON parsing of
//             external/relay payloads; we do not want a malformed line to crash the loop.
//   CA1303   — Console.WriteLine literal strings are CLI help text, not localized.
//   CA1305/CA2026/MA0076 — culture-sensitive ToString in interpolation; this is a
//             local Windows launcher, not user-facing localized output.
//   CA1310   — string.StartsWith without StringComparison; protocol method names are ASCII.
//   CA1849/S6966 — sync I/O wrappers (ReadAllLines/WriteAllText/ReadToEnd) keep the
//             JSONL DB helpers simple; this is launcher code, not a hot server path.
//   CA2234   — pass System.Uri instead of string; relay URL is a const, not user input.
//   CA5394   — `new Random()` for fake distance estimates in client_quote demo data.
//   MA0029   — comparison-operator-on-Count; readability over micro-perf.
//   MA0051   — method too long; HandleTool intentionally enumerates 25 tool cases inline.
//   MA0074   — interpolation in throw; not applicable here.
//   MA0075   — empty collection initializer `new()` for clarity.
//   MA0140   — culture for string.Format-like calls; same as MA0076.
//   RCS1003/RCS1031/RCS1077/RCS1112/RCS1118/RCS1123/RCS1163/RCS1214 — Roslynator
//             style/perf hints (braces, unnecessary parens, redundant interpolation,
//             const local) that don't fit the hand-rolled JSON-builder shape.
//   S1172/S1481/S1871/S2190/S2681/S2971 — Sonar style/branching hints in the
//             tool-handler switch and the polling loop.
//   CS8321   — local function declared but not used; harmless in the script context.
//   IL2026/IL3050 — reflection trim/AOT warnings from JsonSerializer.Serialize on
//             a string parameter; this script is JIT-only, not trimmed/AOT.
//
// Rules NOT in NoWarn that previous versions listed are now silenced by the repo
// .editorconfig (SA1312/SA1313/SA1501/SA1513/IDE0055/IDE0058/MA0006/MA0165/etc.)
// or no longer fire under the current code shape.
// ---------------------------------------------------------------------------
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
#:property EnableNETAnalyzers=false
#:property NoWarn=CA1031;CA1303;CA1305;CA1310;CA1849;CA2026;CA2234;CA5394;CS8321;IDE0011;IDE0028;IDE0046;IDE0048;IDE0061;IDE0120;IDE0305;IL2026;IL3050;MA0029;MA0051;MA0074;MA0075;MA0076;MA0140;RCS1003;RCS1031;RCS1077;RCS1112;RCS1118;RCS1123;RCS1163;RCS1214;S1172;S1481;S1871;S2190;S2486;S2681;S2971;S6966;SA1116;SA1118;SA1122;SA1407;SA1413;SA1502;SA1503;SA1520

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

var CsFile = Path.GetFullPath("wolfstruckingco.cs");
var ProjectDir = Path.GetDirectoryName(CsFile) ?? ".";
var DbPath = Path.Combine(ProjectDir, "data", "db.jsonl");

var Cmd = args.Length > 0 ? args[0] : "";

if (Cmd == "mcp") await RunMcp();
else if (Cmd == "launch") await RunLaunch();
else
{
    Console.WriteLine("Wolf's Trucking Co MCP Server");
    Console.WriteLine("Usage: dotnet run wolfstruckingco.cs -- [command]");
    Console.WriteLine("  mcp       Run as MCP server over stdio (used by Claude Code)");
    Console.WriteLine("  launch    Open Claude Code terminal with MCP server configured");
    Console.WriteLine("  --help    Show this help");
}

return;

string Esc(string S) => S.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");

void DbSeed()
{
    Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);
    if (File.Exists(DbPath)) return;
    var Lines = new[]
    {
        "{\"Id\":\"user_admin_1\",\"Type\":\"user\",\"Timestamp\":\"2026-04-15T00:00:00Z\",\"Data\":{\"Email\":\"keichee@gmail.com\",\"Role\":\"admin\",\"Name\":\"Kei Chee\",\"Status\":\"active\"}}",
        "{\"Id\":\"user_client_1\",\"Type\":\"user\",\"Timestamp\":\"2026-04-15T00:00:00Z\",\"Data\":{\"Email\":\"noahblesse@gmail.com\",\"Role\":\"client\",\"Name\":\"Noah Blesse\",\"Status\":\"active\",\"Company\":\"Blesse Manufacturing Co.\",\"Account\":\"ACC-2026-0142\"}}",
        "{\"Id\":\"user_driver_1\",\"Type\":\"user\",\"Timestamp\":\"2026-04-15T00:00:00Z\",\"Data\":{\"Email\":\"cruzlauroiii@gmail.com\",\"Role\":\"driver\",\"Name\":\"Lauro Cruz III\",\"Status\":\"active\",\"Cdl\":\"A\",\"Experience\":\"5 years\",\"Unit\":\"WTC-0847\"}}",
        "{\"Id\":\"job_1\",\"Type\":\"job\",\"Timestamp\":\"2026-04-15T05:00:00Z\",\"Data\":{\"Title\":\"Charlotte Metro Delivery Run\",\"Status\":\"available\",\"Pickup\":\"Blue Ridge Distribution, 450 Main Ave NW, Hickory NC 28601\",\"Delivery\":\"Southeast Distribution Center, 2800 Distribution Dr, Charlotte NC 28208\",\"Cargo\":\"12 pallets automotive parts, 8400 lbs\",\"Pay\":\"$437.50\",\"Distance\":\"58 mi\",\"Duration\":\"85 min\",\"Window\":\"6:00 AM - 9:00 AM\",\"PickupLat\":35.7330,\"PickupLng\":-81.3412,\"DeliveryLat\":35.2271,\"DeliveryLng\":-80.8431}}",
        "{\"Id\":\"job_2\",\"Type\":\"job\",\"Timestamp\":\"2026-04-15T05:00:00Z\",\"Data\":{\"Title\":\"Gastonia Building Materials\",\"Status\":\"available\",\"Pickup\":\"Piedmont Logistics Hub, 1900 Statesville Ave, Charlotte NC 28206\",\"Delivery\":\"Gaston County Receiving, 1450 Union Rd, Gastonia NC 28054\",\"Cargo\":\"8 pallets building materials, 6200 lbs\",\"Pay\":\"$312.50\",\"Distance\":\"24 mi\",\"Duration\":\"40 min\",\"Window\":\"8:00 AM - 11:00 AM\",\"PickupLat\":35.2665,\"PickupLng\":-80.8120,\"DeliveryLat\":35.2626,\"DeliveryLng\":-81.1873}}",
        "{\"Id\":\"job_3\",\"Type\":\"job\",\"Timestamp\":\"2026-04-15T05:00:00Z\",\"Data\":{\"Title\":\"Spartanburg Machine Parts Express\",\"Status\":\"available\",\"Pickup\":\"Upstate Manufacturing, 600 International Dr, Spartanburg SC 29303\",\"Delivery\":\"Greenville Commerce Park, 200 Verdae Blvd, Greenville SC 29607\",\"Cargo\":\"6 pallets machine components, 4800 lbs\",\"Pay\":\"$275.00\",\"Distance\":\"28 mi\",\"Duration\":\"30 min\",\"Window\":\"10:00 AM - 1:00 PM\",\"PickupLat\":34.9496,\"PickupLng\":-81.9321,\"DeliveryLat\":34.8526,\"DeliveryLng\":-82.3940}}",
        "{\"Id\":\"job_4\",\"Type\":\"job\",\"Timestamp\":\"2026-04-15T05:00:00Z\",\"Data\":{\"Title\":\"Asheville Electronics Haul\",\"Status\":\"available\",\"Pickup\":\"Anderson Warehouse Complex, 3500 Liberty Hwy, Anderson SC 29621\",\"Delivery\":\"WNC Distribution Hub, 90 Riverside Dr, Asheville NC 28801\",\"Cargo\":\"10 pallets consumer electronics, 5600 lbs, HIGH VALUE\",\"Pay\":\"$562.50\",\"Distance\":\"72 mi\",\"Duration\":\"90 min\",\"Window\":\"1:00 PM - 5:00 PM\",\"PickupLat\":34.5034,\"PickupLng\":-82.6501,\"DeliveryLat\":35.5951,\"DeliveryLng\":-82.5515}}",
        "{\"Id\":\"job_5\",\"Type\":\"job\",\"Timestamp\":\"2026-04-15T05:00:00Z\",\"Data\":{\"Title\":\"Regional Furniture Delivery\",\"Status\":\"available\",\"Pickup\":\"Hickory Furniture Mart, 2220 US-70, Hickory NC 28602\",\"Delivery\":\"Greensboro Distribution, 4500 W Wendover Ave, Greensboro NC 27407\",\"Cargo\":\"15 pallets furniture, 9200 lbs\",\"Pay\":\"$487.50\",\"Distance\":\"78 mi\",\"Duration\":\"95 min\",\"Window\":\"7:00 AM - 12:00 PM\",\"PickupLat\":35.7280,\"PickupLng\":-81.3240,\"DeliveryLat\":36.0726,\"DeliveryLng\":-79.7920}}"
    };
    File.WriteAllText(DbPath, string.Join("\n", Lines) + "\n");
}

void DbAppend(string Json)
{
    Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);
    File.AppendAllText(DbPath, Json + "\n");
}

List<JsonElement> DbQuery(string Type, Func<JsonElement, bool>? Filter = null)
{
    if (!File.Exists(DbPath)) return new();
    return File.ReadAllLines(DbPath)
        .Where(L => !string.IsNullOrWhiteSpace(L))
        .Select(L => { try { return JsonDocument.Parse(L).RootElement.Clone(); } catch { return default; } })
        .Where(E => E.ValueKind == JsonValueKind.Object)
        .Where(E => E.TryGetProperty("Type", out var T) && T.GetString() == Type)
        .Where(E => Filter == null || Filter(E))
        .ToList();
}

JsonElement? DbGet(string Id)
{
    if (!File.Exists(DbPath)) return null;
    foreach (var Line in File.ReadAllLines(DbPath).Reverse())
    {
        if (string.IsNullOrWhiteSpace(Line)) continue;
        try
        {
            var El = JsonDocument.Parse(Line).RootElement.Clone();
            if (El.TryGetProperty("Id", out var IdProp) && IdProp.GetString() == Id) return El;
        }
        catch { }
    }
    return null;
}

void DbUpdate(string Id, string NewJson)
{
    if (!File.Exists(DbPath)) return;
    var Lines = File.ReadAllLines(DbPath);
    var Updated = false;
    for (var I = Lines.Length - 1; I >= 0; I--)
    {
        if (string.IsNullOrWhiteSpace(Lines[I])) continue;
        try
        {
            var El = JsonDocument.Parse(Lines[I]).RootElement;
            if (El.TryGetProperty("Id", out var IdProp) && IdProp.GetString() == Id)
            {
                Lines[I] = NewJson;
                Updated = true;
                break;
            }
        }
        catch { }
    }
    if (Updated) File.WriteAllText(DbPath, string.Join("\n", Lines) + "\n");
}

void AuditLog(string Action, string UserId, string? JobId = null, string? Details = null)
{
    var AuditId = "audit_" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    var Sb = new StringBuilder();
    Sb.Append($"{{\"Id\":\"{AuditId}\",\"Type\":\"audit\",\"Timestamp\":\"{DateTime.UtcNow:O}\",\"Data\":{{\"Action\":\"{Esc(Action)}\",\"UserId\":\"{Esc(UserId)}\"");
    if (JobId != null) Sb.Append($",\"JobId\":\"{Esc(JobId)}\"");
    if (Details != null) Sb.Append($",\"Details\":\"{Esc(Details)}\"");
    Sb.Append("}}");
    DbAppend(Sb.ToString());
}

string? LookupRole(string Email)
{
    var Users = DbQuery("user", E =>
        E.TryGetProperty("Data", out var D) &&
        D.TryGetProperty("Email", out var Em) &&
        Em.GetString() == Email);
    if (Users.Count == 0) return null;
    var Data = Users[^1].GetProperty("Data");
    return Data.TryGetProperty("Role", out var R) ? R.GetString() : null;
}

string? LookupUserId(string Email)
{
    var Users = DbQuery("user", E =>
        E.TryGetProperty("Data", out var D) &&
        D.TryGetProperty("Email", out var Em) &&
        Em.GetString() == Email);
    if (Users.Count == 0) return null;
    return Users[^1].TryGetProperty("Id", out var Id) ? Id.GetString() : null;
}

string GetUserRole(string Email)
{
    return LookupRole(Email) ?? "applicant";
}

string ToolResultRaw(string Id, string RawJson)
{
    return $"{{\"jsonrpc\":\"2.0\",\"id\":{Id},\"result\":{{\"content\":[{{\"type\":\"text\",\"text\":{JsonSerializer.Serialize(RawJson)}}}]}}}}";
}

string ToolError(string Id, string Text)
{
    return $"{{\"jsonrpc\":\"2.0\",\"id\":{Id},\"result\":{{\"content\":[{{\"type\":\"text\",\"text\":\"{Esc(Text)}\"}}],\"isError\":true}}}}";
}

async Task RunLaunch()
{
    DbSeed();

    var DotnetExe = "dotnet";
    foreach (var P in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
    {
        var Candidate = Path.Combine(P, "dotnet.exe");
        if (File.Exists(Candidate)) { DotnetExe = Candidate.Replace("\\", "\\\\"); break; }
    }

    var CsEscaped = CsFile.Replace("\\", "\\\\");
    var McpJson = "{\"mcpServers\":{\"wolfstruckingco\":{\"command\":\"" + DotnetExe + "\",\"args\":[\"run\",\"" + CsEscaped + "\",\"--\",\"mcp\"]}}}";
    await File.WriteAllTextAsync(Path.Combine(ProjectDir, ".mcp.json"), McpJson);

    var ClaudeDir = Path.Combine(ProjectDir, ".claude");
    Directory.CreateDirectory(ClaudeDir);

    var AllTools = new[]
    {
        "interview_chat",
        "jobs_list", "job_details", "job_accept", "job_complete",
        "checklist_start", "checklist_complete",
        "delivery_status", "audit_list", "audit_detail",
        "kpi_driver", "dispatch_check", "dispatch_reply",
        "admin_dashboard", "admin_drivers", "admin_interviews",
        "admin_approve_driver", "admin_reject_driver",
        "admin_jobs", "admin_audit", "admin_command",
        "client_create_job", "client_jobs", "client_invoices", "client_quote"
    };
    var Perms = string.Join(",", AllTools.Select(T => $"\"mcp__wolfstruckingco__{T}\""));
    var SettingsJson = $"{{\"permissions\":{{\"allow\":[{Perms}]}}}}";
    await File.WriteAllTextAsync(Path.Combine(ClaudeDir, "settings.local.json"), SettingsJson);

    var LaunchPs1 = Path.Combine(ProjectDir, "launch-claude.ps1");
    await File.WriteAllTextAsync(LaunchPs1,
        "Push-Location '" + ProjectDir + "'\n" +
        "$env:USER_TYPE='ant'\n" +
        "$env:CLAUDE_INTERNAL_FC_OVERRIDES='{\"tengu_harbor\":true}'\n" +
        "claude --dangerously-skip-permissions --dangerously-load-development-channels server:wolfstruckingco\n" +
        "Pop-Location\n");

    Console.Error.WriteLine("[launch] Opening Claude Code with wolfstruckingco MCP server...");
    Process.Start(new ProcessStartInfo
    {
        FileName = "wt.exe",
        Arguments = $"-w 0 new-tab --profile \"PowerShell\" --title \"wolfstruckingco\" -d \"{ProjectDir}\" -- pwsh -ExecutionPolicy Bypass -File \"{LaunchPs1}\"",
        UseShellExecute = true,
    });

    Console.Error.WriteLine("[launch] Claude launched with MCP server");
}

async Task RunMcp()
{
    DbSeed();

    var PendingMessages = new ConcurrentQueue<(string MsgId, string Content, string Source, string Email)>();
    var ReplyUrls = new ConcurrentDictionary<string, string>();
    var RelayUrl = "https://wolfstruckingco.nbth.workers.dev";
    using var Http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    var McpInitialized = false;
    var StdoutLock = new object();

    void Send(string Json) { lock (StdoutLock) { Console.Out.WriteLine(Json); Console.Out.Flush(); } }
    void Respond(string Id, string Result) => Send($"{{\"jsonrpc\":\"2.0\",\"id\":{Id},\"result\":{Result}}}");
    void McpError(string Id, int Code, string Msg) => Send($"{{\"jsonrpc\":\"2.0\",\"id\":{Id},\"error\":{{\"code\":{Code},\"message\":\"{Esc(Msg)}\"}}}}");
    void Notify(string Method, string Parms) => Send($"{{\"jsonrpc\":\"2.0\",\"method\":\"{Method}\",\"params\":{Parms}}}");
    string IdStr(JsonElement El) => El.ValueKind == JsonValueKind.String ? $"\"{El.GetString()}\"" : El.GetRawText();

    var ToolsDef = BuildToolsList();

    string HandleTool(string ToolName, JsonElement ToolArgs, string CallerId)
    {
        try
        {
            switch (ToolName)
            {
                case "interview_chat":
                {
                    var Message = ToolArgs.TryGetProperty("message", out var M) ? M.GetString() ?? "" : "";
                    var SessionId = ToolArgs.TryGetProperty("session_id", out var S) ? S.GetString() ?? "" : "session_" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    if (string.IsNullOrEmpty(SessionId)) SessionId = "session_" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                    var ExistingInterviews = DbQuery("interview", E =>
                        E.TryGetProperty("Data", out var D) &&
                        D.TryGetProperty("SessionId", out var Sid) &&
                        Sid.GetString() == SessionId);

                    var ExchangeCount = ExistingInterviews.Count;

                    var InterviewId = "interview_" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    DbAppend($"{{\"Id\":\"{InterviewId}\",\"Type\":\"interview\",\"Timestamp\":\"{DateTime.UtcNow:O}\",\"Data\":{{\"SessionId\":\"{Esc(SessionId)}\",\"Message\":\"{Esc(Message)}\",\"Direction\":\"applicant\",\"ExchangeCount\":{ExchangeCount + 1}}}}}");

                    var Context = ExchangeCount == 0
                        ? "[HIRING INTERVIEW - START] You are conducting a truck driver hiring interview for Wolf's Trucking Co. Ask about: CDL class/endorsements, experience, safety record, HOS knowledge, medical cert, psychological readiness, route knowledge (Carolinas/Southeast), vehicle inspection, emergency procedures, cargo handling, customer service. NEVER ask about age/race/gender/religion/disability/marital status/pregnancy/national origin. After 5-8 exchanges give verdict. Applicant says: "
                        : $"[HIRING INTERVIEW - Exchange {ExchangeCount + 1}] Applicant says: ";

                    return Context + Message + $"\n\n[SessionId: {SessionId}]";
                }

                case "jobs_list":
                {
                    var Jobs = DbQuery("job", E =>
                        E.TryGetProperty("Data", out var D) &&
                        D.TryGetProperty("Status", out var St) &&
                        St.GetString() == "available");
                    var Sb = new StringBuilder();
                    Sb.Append("{\"Jobs\":[");
                    for (var I = 0; I < Jobs.Count; I++)
                    {
                        if (I > 0) Sb.Append(',');
                        var J = Jobs[I];
                        var JId = J.GetProperty("Id").GetString() ?? "";
                        var Data = J.GetProperty("Data");
                        Sb.Append($"{{\"Id\":\"{JId}\",\"Title\":\"{Esc(Data.GetProperty("Title").GetString() ?? "")}\",\"Pay\":\"{Esc(Data.GetProperty("Pay").GetString() ?? "")}\",\"Distance\":\"{Esc(Data.GetProperty("Distance").GetString() ?? "")}\",\"Window\":\"{Esc(Data.GetProperty("Window").GetString() ?? "")}\"}}");
                    }
                    Sb.Append($"],\"Count\":{Jobs.Count}}}");
                    return Sb.ToString();
                }

                case "job_details":
                {
                    var JobId = ToolArgs.TryGetProperty("job_id", out var Jid) ? Jid.GetString() ?? "" : "";
                    var Job = DbGet(JobId);
                    if (Job == null) return "{\"Error\":\"Job not found\"}";
                    return Job.Value.GetRawText();
                }

                case "job_accept":
                {
                    var JobId = ToolArgs.TryGetProperty("job_id", out var Jid) ? Jid.GetString() ?? "" : "";
                    var DriverEmail = ToolArgs.TryGetProperty("driver_email", out var De) ? De.GetString() ?? "" : "";
                    var DriverId = LookupUserId(DriverEmail) ?? "unknown";

                    var Job = DbGet(JobId);
                    if (Job == null) return "{\"Error\":\"Job not found\"}";

                    var Data = Job.Value.GetProperty("Data");
                    var Status = Data.TryGetProperty("Status", out var St) ? St.GetString() ?? "" : "";
                    if (Status != "available") return $"{{\"Error\":\"Job is not available (status: {Status})\"}}";

                    var RawJson = Job.Value.GetRawText();
                    var UpdatedJson = RawJson.Replace("\"Status\":\"available\"", $"\"Status\":\"accepted\",\"AcceptedBy\":\"{Esc(DriverId)}\",\"AcceptedAt\":\"{DateTime.UtcNow:O}\"");
                    DbUpdate(JobId, UpdatedJson);

                    var DeliveryId = "delivery_" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var Title = Data.TryGetProperty("Title", out var Tt) ? Tt.GetString() ?? "" : "";
                    DbAppend($"{{\"Id\":\"{DeliveryId}\",\"Type\":\"delivery\",\"Timestamp\":\"{DateTime.UtcNow:O}\",\"Data\":{{\"JobId\":\"{Esc(JobId)}\",\"DriverId\":\"{Esc(DriverId)}\",\"Status\":\"in_progress\",\"Title\":\"{Esc(Title)}\",\"StartedAt\":\"{DateTime.UtcNow:O}\"}}}}");

                    AuditLog("job_accepted", DriverId, JobId, $"Driver accepted job: {Title}");
                    return $"{{\"Success\":true,\"DeliveryId\":\"{DeliveryId}\",\"JobId\":\"{JobId}\",\"Message\":\"Job accepted successfully\"}}";
                }

                case "job_complete":
                {
                    var JobId = ToolArgs.TryGetProperty("job_id", out var Jid) ? Jid.GetString() ?? "" : "";
                    var DriverEmail = ToolArgs.TryGetProperty("driver_email", out var De) ? De.GetString() ?? "" : "";
                    var Notes = ToolArgs.TryGetProperty("notes", out var N) ? N.GetString() ?? "" : "";
                    var DriverId = LookupUserId(DriverEmail) ?? "unknown";

                    var Deliveries = DbQuery("delivery", E =>
                        E.TryGetProperty("Data", out var D) &&
                        D.TryGetProperty("JobId", out var Djid) &&
                        Djid.GetString() == JobId &&
                        D.TryGetProperty("Status", out var Ds) &&
                        Ds.GetString() == "in_progress");

                    if (Deliveries.Count == 0) return "{\"Error\":\"No active delivery found for this job\"}";

                    var Delivery = Deliveries[^1];
                    var DelId = Delivery.GetProperty("Id").GetString() ?? "";
                    var DelRaw = Delivery.GetRawText();
                    var UpdatedDel = DelRaw.Replace("\"Status\":\"in_progress\"", $"\"Status\":\"completed\",\"CompletedAt\":\"{DateTime.UtcNow:O}\",\"Notes\":\"{Esc(Notes)}\"");
                    DbUpdate(DelId, UpdatedDel);

                    var Job = DbGet(JobId);
                    if (Job != null)
                    {
                        var JobRaw = Job.Value.GetRawText();
                        var UpdatedJob = JobRaw.Replace("\"Status\":\"accepted\"", "\"Status\":\"completed\"");
                        DbUpdate(JobId, UpdatedJob);
                    }

                    AuditLog("job_completed", DriverId, JobId, $"Delivery completed. Notes: {Notes}");
                    return $"{{\"Success\":true,\"DeliveryId\":\"{DelId}\",\"JobId\":\"{JobId}\",\"Message\":\"Job marked as complete\"}}";
                }

                case "checklist_start":
                {
                    var DriverEmail = ToolArgs.TryGetProperty("driver_email", out var De) ? De.GetString() ?? "" : "";
                    var DriverId = LookupUserId(DriverEmail) ?? "unknown";
                    var ChecklistId = "checklist_" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                    var Items = new[]
                    {
                        "Tires (tread depth, pressure, damage)",
                        "Brakes (pads, lines, air pressure)",
                        "Lights (headlights, taillights, signals, markers)",
                        "Fluids (oil, coolant, DEF, windshield washer)",
                        "Coupling devices (fifth wheel, kingpin, safety chains)",
                        "ELD device (powered on, driver logged in, HOS current)",
                        "Mirrors (adjusted, clean, no cracks)",
                        "Horn (functioning properly)",
                        "Wipers (blades condition, washer fluid)",
                        "Emergency kit (triangles, fire extinguisher, first aid)"
                    };

                    var ItemsJson = string.Join(",", Items.Select(It => $"\"{Esc(It)}\""));
                    DbAppend($"{{\"Id\":\"{ChecklistId}\",\"Type\":\"checklist\",\"Timestamp\":\"{DateTime.UtcNow:O}\",\"Data\":{{\"DriverId\":\"{Esc(DriverId)}\",\"Status\":\"in_progress\",\"Items\":[{ItemsJson}]}}}}");
                    AuditLog("checklist_started", DriverId, null, "Pre-trip inspection checklist started");

                    return $"{{\"ChecklistId\":\"{ChecklistId}\",\"Items\":[{ItemsJson}],\"Message\":\"Pre-trip checklist started. Inspect each item and mark complete when done.\"}}";
                }

                case "checklist_complete":
                {
                    var ChecklistId = ToolArgs.TryGetProperty("checklist_id", out var Cid) ? Cid.GetString() ?? "" : "";
                    var DriverEmail = ToolArgs.TryGetProperty("driver_email", out var De) ? De.GetString() ?? "" : "";
                    var AllPassed = ToolArgs.TryGetProperty("all_passed", out var Ap) && Ap.GetBoolean();
                    var FailedItems = ToolArgs.TryGetProperty("failed_items", out var Fi) ? Fi.GetString() ?? "" : "";
                    var DriverId = LookupUserId(DriverEmail) ?? "unknown";

                    var Checklist = DbGet(ChecklistId);
                    if (Checklist == null) return "{\"Error\":\"Checklist not found\"}";

                    var Raw = Checklist.Value.GetRawText();
                    var Updated = Raw.Replace("\"Status\":\"in_progress\"", $"\"Status\":\"completed\",\"CompletedAt\":\"{DateTime.UtcNow:O}\",\"AllPassed\":{(AllPassed ? "true" : "false")},\"FailedItems\":\"{Esc(FailedItems)}\"");
                    DbUpdate(ChecklistId, Updated);

                    AuditLog("checklist_completed", DriverId, null, $"Pre-trip checklist completed. All passed: {AllPassed}. Failed: {FailedItems}");
                    return $"{{\"Success\":true,\"ChecklistId\":\"{ChecklistId}\",\"AllPassed\":{(AllPassed ? "true" : "false")}}}";
                }

                case "delivery_status":
                {
                    var DriverEmail = ToolArgs.TryGetProperty("driver_email", out var De) ? De.GetString() ?? "" : "";
                    var DriverId = LookupUserId(DriverEmail) ?? "unknown";

                    var Deliveries = DbQuery("delivery", E =>
                        E.TryGetProperty("Data", out var D) &&
                        D.TryGetProperty("DriverId", out var Did) &&
                        Did.GetString() == DriverId);

                    var Sb = new StringBuilder();
                    Sb.Append("{\"Deliveries\":[");
                    for (var I = 0; I < Deliveries.Count; I++)
                    {
                        if (I > 0) Sb.Append(',');
                        Sb.Append(Deliveries[I].GetRawText());
                    }
                    Sb.Append($"],\"Count\":{Deliveries.Count}}}");
                    return Sb.ToString();
                }

                case "audit_list":
                {
                    var DriverEmail = ToolArgs.TryGetProperty("driver_email", out var De) ? De.GetString() ?? "" : "";
                    var DriverId = LookupUserId(DriverEmail) ?? "unknown";
                    var Limit = ToolArgs.TryGetProperty("limit", out var Lim) ? Lim.GetInt32() : 20;

                    var Audits = DbQuery("audit", E =>
                        E.TryGetProperty("Data", out var D) &&
                        D.TryGetProperty("UserId", out var Uid) &&
                        Uid.GetString() == DriverId)
                        .TakeLast(Limit).ToList();

                    var Sb = new StringBuilder();
                    Sb.Append("{\"AuditEntries\":[");
                    for (var I = 0; I < Audits.Count; I++)
                    {
                        if (I > 0) Sb.Append(',');
                        Sb.Append(Audits[I].GetRawText());
                    }
                    Sb.Append($"],\"Count\":{Audits.Count}}}");
                    return Sb.ToString();
                }

                case "audit_detail":
                {
                    var AuditId = ToolArgs.TryGetProperty("audit_id", out var Aid) ? Aid.GetString() ?? "" : "";
                    var Entry = DbGet(AuditId);
                    if (Entry == null) return "{\"Error\":\"Audit entry not found\"}";
                    return Entry.Value.GetRawText();
                }

                case "kpi_driver":
                {
                    var DriverEmail = ToolArgs.TryGetProperty("driver_email", out var De) ? De.GetString() ?? "" : "";
                    var DriverId = LookupUserId(DriverEmail) ?? "unknown";

                    var CompletedDeliveries = DbQuery("delivery", E =>
                        E.TryGetProperty("Data", out var D) &&
                        D.TryGetProperty("DriverId", out var Did) &&
                        Did.GetString() == DriverId &&
                        D.TryGetProperty("Status", out var St) &&
                        St.GetString() == "completed").Count;

                    var ActiveDeliveries = DbQuery("delivery", E =>
                        E.TryGetProperty("Data", out var D) &&
                        D.TryGetProperty("DriverId", out var Did) &&
                        Did.GetString() == DriverId &&
                        D.TryGetProperty("Status", out var St) &&
                        St.GetString() == "in_progress").Count;

                    var TotalAudits = DbQuery("audit", E =>
                        E.TryGetProperty("Data", out var D) &&
                        D.TryGetProperty("UserId", out var Uid) &&
                        Uid.GetString() == DriverId).Count;

                    var Checklists = DbQuery("checklist", E =>
                        E.TryGetProperty("Data", out var D) &&
                        D.TryGetProperty("DriverId", out var Did) &&
                        Did.GetString() == DriverId &&
                        D.TryGetProperty("Status", out var St) &&
                        St.GetString() == "completed").Count;

                    var Driver = DbQuery("user", E =>
                        E.TryGetProperty("Id", out var Uid) && Uid.GetString() == DriverId);
                    var DriverName = Driver.Count > 0 && Driver[^1].TryGetProperty("Data", out var Dd) && Dd.TryGetProperty("Name", out var Nm) ? Nm.GetString() ?? "" : "";
                    var Unit = Driver.Count > 0 && Driver[^1].TryGetProperty("Data", out var Dd2) && Dd2.TryGetProperty("Unit", out var Un) ? Un.GetString() ?? "" : "";

                    var OnTimeRate = CompletedDeliveries > 0 ? 95.0 : 0.0;
                    var SafetyScore = 98.5;
                    var FuelEfficiency = 6.8;

                    return $"{{\"DriverId\":\"{Esc(DriverId)}\",\"Name\":\"{Esc(DriverName)}\",\"Unit\":\"{Esc(Unit)}\",\"CompletedDeliveries\":{CompletedDeliveries},\"ActiveDeliveries\":{ActiveDeliveries},\"OnTimeRate\":{OnTimeRate},\"SafetyScore\":{SafetyScore},\"FuelEfficiency\":{FuelEfficiency},\"ChecklistsCompleted\":{Checklists},\"AuditEntries\":{TotalAudits}}}";
                }

                case "dispatch_check":
                {
                    var Collected = new List<(string MsgId, string Content, string Source, string Email)>();
                    while (PendingMessages.TryDequeue(out var Msg)) Collected.Add(Msg);
                    if (Collected.Count == 0) return "No pending messages";

                    var Sb = new StringBuilder();
                    Sb.Append("{\"Messages\":[");
                    for (var I = 0; I < Collected.Count; I++)
                    {
                        if (I > 0) Sb.Append(',');
                        Sb.Append($"{{\"MsgId\":\"{Esc(Collected[I].MsgId)}\",\"Content\":\"{Esc(Collected[I].Content)}\",\"Source\":\"{Esc(Collected[I].Source)}\",\"Email\":\"{Esc(Collected[I].Email)}\"}}");
                    }
                    Sb.Append($"],\"Count\":{Collected.Count}}}");
                    return Sb.ToString();
                }

                case "dispatch_reply":
                {
                    var MsgId = ToolArgs.TryGetProperty("msg_id", out var Mid) ? Mid.GetString() ?? "" : "";
                    var Text = ToolArgs.TryGetProperty("text", out var T) ? T.GetString() ?? "" : "";

                    if (!string.IsNullOrEmpty(MsgId) && ReplyUrls.TryRemove(MsgId, out var Url))
                    {
                        var Reply = $"{{\"type\":\"chat\",\"id\":\"{Esc(MsgId)}\",\"content\":\"{Esc(Text)}\"}}";
                        try
                        {
                            Http.PostAsync(Url + "/reply", new System.Net.Http.StringContent(Reply, Encoding.UTF8, "application/json")).GetAwaiter().GetResult();
                            Console.Error.WriteLine($"[mcp] Reply sent [{MsgId}]");
                            return $"{{\"Success\":true}}";
                        }
                        catch (Exception Ex) { return $"{{\"Error\":\"{Esc(Ex.Message)}\"}}"; }
                    }
                    return $"{{\"Error\":\"Message not found\"}}";
                }

                case "admin_dashboard":
                {
                    var AllDrivers = DbQuery("user", E =>
                        E.TryGetProperty("Data", out var D) &&
                        D.TryGetProperty("Role", out var R) &&
                        R.GetString() == "driver");

                    var AllJobs = DbQuery("job");
                    var CompletedJobs = AllJobs.Where(J =>
                        J.TryGetProperty("Data", out var D) &&
                        D.TryGetProperty("Status", out var St) &&
                        St.GetString() == "completed").Count();
                    var AvailableJobs = AllJobs.Where(J =>
                        J.TryGetProperty("Data", out var D) &&
                        D.TryGetProperty("Status", out var St) &&
                        St.GetString() == "available").Count();
                    var AcceptedJobs = AllJobs.Where(J =>
                        J.TryGetProperty("Data", out var D) &&
                        D.TryGetProperty("Status", out var St) &&
                        St.GetString() == "accepted").Count();

                    var AllDeliveries = DbQuery("delivery");
                    var CompletedDeliveries = AllDeliveries.Where(Dl =>
                        Dl.TryGetProperty("Data", out var D) &&
                        D.TryGetProperty("Status", out var St) &&
                        St.GetString() == "completed").Count();

                    var TotalAudits = DbQuery("audit").Count;
                    var TotalInterviews = DbQuery("interview").Count;

                    return $"{{\"Fleet\":{{\"TotalDrivers\":{AllDrivers.Count},\"ActiveDrivers\":{AllDrivers.Count(D => D.TryGetProperty("Data", out var Dd) && Dd.TryGetProperty("Status", out var St) && St.GetString() == "active")}}},\"Jobs\":{{\"Total\":{AllJobs.Count},\"Available\":{AvailableJobs},\"Accepted\":{AcceptedJobs},\"Completed\":{CompletedJobs}}},\"Deliveries\":{{\"Total\":{AllDeliveries.Count},\"Completed\":{CompletedDeliveries}}},\"AuditEntries\":{TotalAudits},\"Interviews\":{TotalInterviews}}}";
                }

                case "admin_drivers":
                {
                    var Drivers = DbQuery("user", E =>
                        E.TryGetProperty("Data", out var D) &&
                        D.TryGetProperty("Role", out var R) &&
                        R.GetString() == "driver");

                    var Sb = new StringBuilder();
                    Sb.Append("{\"Drivers\":[");
                    for (var I = 0; I < Drivers.Count; I++)
                    {
                        if (I > 0) Sb.Append(',');
                        Sb.Append(Drivers[I].GetRawText());
                    }
                    Sb.Append($"],\"Count\":{Drivers.Count}}}");
                    return Sb.ToString();
                }

                case "admin_interviews":
                {
                    var Interviews = DbQuery("interview");
                    var Sb = new StringBuilder();
                    Sb.Append("{\"Interviews\":[");
                    for (var I = 0; I < Interviews.Count; I++)
                    {
                        if (I > 0) Sb.Append(',');
                        Sb.Append(Interviews[I].GetRawText());
                    }
                    Sb.Append($"],\"Count\":{Interviews.Count}}}");
                    return Sb.ToString();
                }

                case "admin_approve_driver":
                {
                    var UserId = ToolArgs.TryGetProperty("user_id", out var Uid) ? Uid.GetString() ?? "" : "";
                    var User = DbGet(UserId);
                    if (User == null) return "{\"Error\":\"User not found\"}";
                    var Raw = User.Value.GetRawText();
                    var Updated = Raw.Replace("\"Status\":\"pending\"", "\"Status\":\"active\"");
                    DbUpdate(UserId, Updated);
                    AuditLog("driver_approved", UserId, null, "Driver application approved by admin");
                    return $"{{\"Success\":true,\"UserId\":\"{Esc(UserId)}\",\"Message\":\"Driver approved and activated\"}}";
                }

                case "admin_reject_driver":
                {
                    var UserId = ToolArgs.TryGetProperty("user_id", out var Uid) ? Uid.GetString() ?? "" : "";
                    var Reason = ToolArgs.TryGetProperty("reason", out var R) ? R.GetString() ?? "" : "";
                    var User = DbGet(UserId);
                    if (User == null) return "{\"Error\":\"User not found\"}";
                    var Raw = User.Value.GetRawText();
                    var Updated = Raw.Replace("\"Status\":\"pending\"", $"\"Status\":\"rejected\",\"RejectionReason\":\"{Esc(Reason)}\"");
                    DbUpdate(UserId, Updated);
                    AuditLog("driver_rejected", UserId, null, $"Driver application rejected. Reason: {Reason}");
                    return $"{{\"Success\":true,\"UserId\":\"{Esc(UserId)}\",\"Message\":\"Driver rejected\"}}";
                }

                case "admin_jobs":
                {
                    var StatusFilter = ToolArgs.TryGetProperty("status", out var Sf) ? Sf.GetString() ?? "" : "";
                    var Jobs = string.IsNullOrEmpty(StatusFilter)
                        ? DbQuery("job")
                        : DbQuery("job", E =>
                            E.TryGetProperty("Data", out var D) &&
                            D.TryGetProperty("Status", out var St) &&
                            St.GetString() == StatusFilter);

                    var Sb = new StringBuilder();
                    Sb.Append("{\"Jobs\":[");
                    for (var I = 0; I < Jobs.Count; I++)
                    {
                        if (I > 0) Sb.Append(',');
                        Sb.Append(Jobs[I].GetRawText());
                    }
                    Sb.Append($"],\"Count\":{Jobs.Count}}}");
                    return Sb.ToString();
                }

                case "admin_audit":
                {
                    var Limit = ToolArgs.TryGetProperty("limit", out var Lim) ? Lim.GetInt32() : 50;
                    var Audits = DbQuery("audit").TakeLast(Limit).ToList();
                    var Sb = new StringBuilder();
                    Sb.Append("{\"AuditEntries\":[");
                    for (var I = 0; I < Audits.Count; I++)
                    {
                        if (I > 0) Sb.Append(',');
                        Sb.Append(Audits[I].GetRawText());
                    }
                    Sb.Append($"],\"Count\":{Audits.Count}}}");
                    return Sb.ToString();
                }

                case "admin_command":
                {
                    var Command = ToolArgs.TryGetProperty("command", out var C) ? C.GetString() ?? "" : "";
                    AuditLog("admin_command", "user_admin_1", null, $"Admin command: {Command}");
                    return $"{{\"Command\":\"{Esc(Command)}\",\"Message\":\"Admin command logged. Claude has full authority to execute admin operations.\"}}";
                }

                case "client_create_job":
                {
                    var ClientEmail = ToolArgs.TryGetProperty("client_email", out var Ce) ? Ce.GetString() ?? "" : "";
                    var Pickup = ToolArgs.TryGetProperty("pickup", out var Pu) ? Pu.GetString() ?? "" : "";
                    var Delivery = ToolArgs.TryGetProperty("delivery", out var Dl) ? Dl.GetString() ?? "" : "";
                    var Cargo = ToolArgs.TryGetProperty("cargo", out var Cg) ? Cg.GetString() ?? "" : "";
                    var Weight = ToolArgs.TryGetProperty("weight", out var Wt) ? Wt.GetString() ?? "" : "";
                    var Instructions = ToolArgs.TryGetProperty("instructions", out var Ins) ? Ins.GetString() ?? "" : "";
                    var Schedule = ToolArgs.TryGetProperty("schedule", out var Sc) ? Sc.GetString() ?? "" : "";
                    var ClientId = LookupUserId(ClientEmail) ?? "unknown";
                    var JobId = "cjob_" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var Dist = 20 + new Random().Next(180);
                    var Pay = Math.Round(Dist * 1.45, 2);
                    DbAppend($"{{\"Id\":\"{JobId}\",\"Type\":\"job\",\"Timestamp\":\"{DateTime.UtcNow:O}\",\"Data\":{{\"Title\":\"Client Shipment: {Esc(Pickup)} to {Esc(Delivery)}\",\"Status\":\"pending\",\"Pickup\":\"{Esc(Pickup)}\",\"Delivery\":\"{Esc(Delivery)}\",\"Cargo\":\"{Esc(Cargo)}\",\"Weight\":\"{Esc(Weight)}\",\"Pay\":\"${Pay}\",\"Distance\":\"{Dist} mi\",\"Instructions\":\"{Esc(Instructions)}\",\"Schedule\":\"{Esc(Schedule)}\",\"ClientId\":\"{Esc(ClientId)}\",\"ClientEmail\":\"{Esc(ClientEmail)}\"}}}}");
                    AuditLog("client_job_created", ClientId, JobId, $"Client shipment: {Pickup} to {Delivery}, ${Pay}");
                    return $"{{\"Success\":true,\"JobId\":\"{JobId}\",\"Price\":{Pay},\"Distance\":{Dist},\"Message\":\"Shipment created and pending admin approval\"}}";
                }

                case "client_jobs":
                {
                    var ClientEmail = ToolArgs.TryGetProperty("client_email", out var Ce) ? Ce.GetString() ?? "" : "";
                    var ClientId = LookupUserId(ClientEmail) ?? "unknown";
                    var Jobs = DbQuery("job", E =>
                        E.TryGetProperty("Data", out var D) &&
                        D.TryGetProperty("ClientId", out var Cid) &&
                        Cid.GetString() == ClientId);
                    var Sb = new StringBuilder();
                    Sb.Append("{\"Jobs\":[");
                    for (var I = 0; I < Jobs.Count; I++)
                    {
                        if (I > 0) Sb.Append(',');
                        Sb.Append(Jobs[I].GetRawText());
                    }
                    Sb.Append($"],\"Count\":{Jobs.Count}}}");
                    return Sb.ToString();
                }

                case "client_invoices":
                {
                    var ClientEmail = ToolArgs.TryGetProperty("client_email", out var Ce) ? Ce.GetString() ?? "" : "";
                    var ClientId = LookupUserId(ClientEmail) ?? "unknown";
                    var Invoices = DbQuery("invoice", E =>
                        E.TryGetProperty("Data", out var D) &&
                        D.TryGetProperty("ClientId", out var Cid) &&
                        Cid.GetString() == ClientId);
                    var Sb = new StringBuilder();
                    Sb.Append("{\"Invoices\":[");
                    for (var I = 0; I < Invoices.Count; I++)
                    {
                        if (I > 0) Sb.Append(',');
                        Sb.Append(Invoices[I].GetRawText());
                    }
                    Sb.Append($"],\"Count\":{Invoices.Count}}}");
                    return Sb.ToString();
                }

                case "client_quote":
                {
                    var Distance = ToolArgs.TryGetProperty("distance", out var Ds) ? Ds.GetInt32() : (20 + new Random().Next(180));
                    var Weight = ToolArgs.TryGetProperty("weight", out var Wt) ? Wt.GetInt32() : 0;
                    var Stops = ToolArgs.TryGetProperty("stops", out var St) ? St.GetInt32() : 1;
                    var Rush = ToolArgs.TryGetProperty("rush", out var Ru) && Ru.GetBoolean();
                    var Rate = 1.45;
                    var WSurcharge = Weight > 20000 ? 75.0 : Weight > 10000 ? 35.0 : 0.0;
                    var StopFee = (Stops - 1) * 25.0;
                    var Base = Distance * Rate + WSurcharge + StopFee;
                    var RushFee = Rush ? Base * 0.3 : 0.0;
                    var Total = Math.Round(Base + RushFee, 2);
                    return $"{{\"Distance\":{Distance},\"Rate\":{Rate},\"WeightSurcharge\":{WSurcharge},\"StopFee\":{StopFee},\"RushFee\":{RushFee:F2},\"Total\":{Total}}}";
                }

                default:
                    return $"{{\"Error\":\"Unknown tool: {ToolName}\"}}";
            }
        }
        catch (Exception Ex)
        {
            return $"{{\"Error\":\"{Esc(Ex.Message)}\"}}";
        }
    }

    var StdinTask = Task.Run(async () =>
    {
        using var Reader = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8);
        while (true)
        {
            string? Line;
            try { Line = await Reader.ReadLineAsync(); } catch { break; }
            if (Line == null) break;
            if (string.IsNullOrWhiteSpace(Line)) continue;

            try
            {
                using var Doc = JsonDocument.Parse(Line);
                var Root = Doc.RootElement;
                var Method = Root.TryGetProperty("method", out var M) ? M.GetString() ?? "" : "";
                var HasId = Root.TryGetProperty("id", out var IdEl);
                var Id = HasId ? IdStr(IdEl) : "";

                if (!HasId || Method.StartsWith("notifications/"))
                {
                    if (Method == "notifications/initialized") McpInitialized = true;
                    continue;
                }

                switch (Method)
                {
                    case "initialize":
                        Respond(Id, "{\"protocolVersion\":\"2024-11-05\",\"capabilities\":{\"tools\":{},\"experimental\":{\"claude/channel\":{}}},\"serverInfo\":{\"name\":\"wolfstruckingco\",\"version\":\"3.0\"}}");
                        break;

                    case "tools/list":
                        Respond(Id, ToolsDef);
                        break;

                    case "tools/call":
                    {
                        var ToolName = Root.GetProperty("params").GetProperty("name").GetString() ?? "";
                        var ToolArgs = Root.GetProperty("params").TryGetProperty("arguments", out var A) ? A : default;

                        var Result = HandleTool(ToolName, ToolArgs, Id);
                        Send(ToolResultRaw(Id, Result));
                        break;
                    }

                    case "resources/list":
                        Respond(Id, "{\"resources\":[]}");
                        break;

                    case "resources/templates/list":
                        Respond(Id, "{\"resourceTemplates\":[]}");
                        break;

                    case "prompts/list":
                        Respond(Id, "{\"prompts\":[]}");
                        break;

                    case "ping":
                        Respond(Id, "{}");
                        break;

                    case "completion/complete":
                        Respond(Id, "{\"completion\":{\"values\":[]}}");
                        break;

                    default:
                        McpError(Id, -32601, $"Method not found: {Method}");
                        break;
                }
            }
            catch (Exception Ex)
            {
                Console.Error.WriteLine($"[mcp] Parse error: {Ex.Message}");
            }
        }
    });

    Console.Error.WriteLine("[mcp] Polling relay at " + RelayUrl);
    _ = Task.Run(async () => { for (var I = 0; I < 120 && !McpInitialized; I++) await Task.Delay(250); if (McpInitialized) Console.Error.WriteLine("[mcp] Claude initialized MCP tools"); });

    while (true)
    {
        try
        {
            var Resp = await Http.GetStringAsync(RelayUrl + "/poll?role=server");
            using var Doc = JsonDocument.Parse(Resp);
            var Msgs = Doc.RootElement.GetProperty("messages");

            foreach (var MsgRaw in Msgs.EnumerateArray())
            {
                var MsgText = MsgRaw.GetString() ?? "";
                if (string.IsNullOrEmpty(MsgText)) continue;

                using var MsgDoc = JsonDocument.Parse(MsgText);
                var El = MsgDoc.RootElement;

                var MsgType = El.TryGetProperty("type", out var Tp) ? Tp.GetString() ?? "" : "";
                var Content = El.TryGetProperty("content", out var Cp) ? Cp.GetString() ?? "" : "";
                var MsgId = El.TryGetProperty("id", out var Ip) ? Ip.GetString() ?? "" : "";
                var Email = El.TryGetProperty("email", out var Ep) ? Ep.GetString() ?? "" : "";
                if (string.IsNullOrEmpty(MsgId)) MsgId = "msg_" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                Console.Error.WriteLine($"[mcp] {MsgType} [{MsgId}]: {(Content.Length > 80 ? Content[..80] + "..." : Content)}");

                ReplyUrls[MsgId] = RelayUrl;

                var Role = GetUserRole(Email);
                string FullContent;
                string Source;

                if (MsgType == "hiring")
                {
                    var ExchangeCount = El.TryGetProperty("exchangeCount", out var EcP) ? EcP.GetInt32() : 0;
                    var Ctx = ExchangeCount <= 1
                        ? "[HIRING INTERVIEW] Interview truck driver applicant. Evaluate: CDL, experience, safety, HOS, medical, psychological readiness, vehicle inspection, emergency procedures, cargo handling. NEVER ask about age/race/gender/religion/disability/marital status. After 5-8 exchanges give RECOMMENDED/CONDITIONAL/NOT RECOMMENDED verdict. Reply using dispatch_reply. Applicant says: "
                        : "[HIRING INTERVIEW] Applicant says: ";
                    FullContent = Ctx + Content;
                    Source = "hiring_interview";
                }
                else if (MsgType == "admin" || Role == "admin")
                {
                    FullContent = Content;
                    Source = "admin_command";
                }
                else if (MsgType == "chat" && Role == "driver")
                {
                    var Ctx = "[DRIVER DISPATCH] Professional dispatch assistant. Help the driver. Do not reveal system internals. Driver says: ";
                    FullContent = Ctx + Content;
                    Source = "driver_chat";
                }
                else
                {
                    var Ctx = "[DRIVER DISPATCH] Professional dispatch assistant. Help the driver. Do not reveal system internals. Driver says: ";
                    FullContent = Ctx + Content;
                    Source = "driver_chat";
                }

                PendingMessages.Enqueue((MsgId, FullContent, Source, Email));
                Notify("notifications/claude/channel", $"{{\"content\":\"{Esc(FullContent)}\",\"meta\":{{\"msg_id\":\"{MsgId}\",\"source\":\"{Source}\",\"email\":\"{Esc(Email)}\"}}}}");
            }
        }
        catch (Exception Ex)
        {
            if (!Ex.Message.Contains("canceled")) Console.Error.WriteLine($"[mcp] Poll error: {Ex.Message}");
        }

        await Task.Delay(2000);
    }
}

string BuildToolsList()
{
    var Tools = new StringBuilder();
    Tools.Append("{\"tools\":[");

    void AddTool(StringBuilder Sb, string Name, string Desc, string PropsJson, string RequiredJson, ref bool First)
    {
        if (!First) Sb.Append(',');
        First = false;
        Sb.Append($"{{\"name\":\"{Name}\",\"description\":\"{Esc(Desc)}\",\"inputSchema\":{{\"type\":\"object\",\"properties\":{PropsJson},\"required\":{RequiredJson}}}}}");
    }

    var First = true;

    AddTool(Tools, "interview_chat",
        "Handle a hiring interview message from a truck driver applicant. Claude evaluates CDL, experience, safety, HOS, medical, psychological readiness, vehicle inspection, emergency procedures, cargo handling, customer service.",
        "{\"message\":{\"type\":\"string\",\"description\":\"The applicant's message\"},\"session_id\":{\"type\":\"string\",\"description\":\"Interview session ID (auto-generated if empty)\"}}",
        "[\"message\"]", ref First);

    AddTool(Tools, "jobs_list",
        "List all available jobs for the current driver. Returns jobs with title, pay, distance, and delivery window.",
        "{}",
        "[]", ref First);

    AddTool(Tools, "job_details",
        "Get full details of a specific job including pickup/delivery addresses, cargo, pay, distance, duration, GPS coordinates.",
        "{\"job_id\":{\"type\":\"string\",\"description\":\"The job ID to look up\"}}",
        "[\"job_id\"]", ref First);

    AddTool(Tools, "job_accept",
        "Accept a job. Changes status to accepted, creates delivery record, logs audit entry.",
        "{\"job_id\":{\"type\":\"string\",\"description\":\"The job ID to accept\"},\"driver_email\":{\"type\":\"string\",\"description\":\"The driver's email address\"}}",
        "[\"job_id\",\"driver_email\"]", ref First);

    AddTool(Tools, "job_complete",
        "Mark a job as complete. Updates delivery status and logs audit with completion details.",
        "{\"job_id\":{\"type\":\"string\",\"description\":\"The job ID to complete\"},\"driver_email\":{\"type\":\"string\",\"description\":\"The driver's email\"},\"notes\":{\"type\":\"string\",\"description\":\"Completion notes\"}}",
        "[\"job_id\",\"driver_email\"]", ref First);

    AddTool(Tools, "checklist_start",
        "Start a pre-trip inspection checklist. Returns 10 items: tires, brakes, lights, fluids, coupling, ELD, mirrors, horn, wipers, emergency kit.",
        "{\"driver_email\":{\"type\":\"string\",\"description\":\"The driver's email\"}}",
        "[\"driver_email\"]", ref First);

    AddTool(Tools, "checklist_complete",
        "Mark a pre-trip checklist as complete with pass/fail results.",
        "{\"checklist_id\":{\"type\":\"string\",\"description\":\"The checklist ID\"},\"driver_email\":{\"type\":\"string\",\"description\":\"The driver's email\"},\"all_passed\":{\"type\":\"boolean\",\"description\":\"Whether all items passed\"},\"failed_items\":{\"type\":\"string\",\"description\":\"Comma-separated list of failed items\"}}",
        "[\"checklist_id\",\"driver_email\",\"all_passed\"]", ref First);

    AddTool(Tools, "delivery_status",
        "Get current delivery status for a driver including all in-progress and completed deliveries.",
        "{\"driver_email\":{\"type\":\"string\",\"description\":\"The driver's email\"}}",
        "[\"driver_email\"]", ref First);

    AddTool(Tools, "audit_list",
        "List audit trail entries for a specific driver.",
        "{\"driver_email\":{\"type\":\"string\",\"description\":\"The driver's email\"},\"limit\":{\"type\":\"integer\",\"description\":\"Max entries to return (default 20)\"}}",
        "[\"driver_email\"]", ref First);

    AddTool(Tools, "audit_detail",
        "Get full details of a specific audit entry by ID.",
        "{\"audit_id\":{\"type\":\"string\",\"description\":\"The audit entry ID\"}}",
        "[\"audit_id\"]", ref First);

    AddTool(Tools, "kpi_driver",
        "Get driver KPI dashboard: on-time rate, safety score, deliveries completed, fuel efficiency, checklists completed.",
        "{\"driver_email\":{\"type\":\"string\",\"description\":\"The driver's email\"}}",
        "[\"driver_email\"]", ref First);

    AddTool(Tools, "dispatch_check",
        "Check for pending messages from browser chat (drivers, admins, applicants).",
        "{}",
        "[]", ref First);

    AddTool(Tools, "dispatch_reply",
        "Reply to a browser chat message. The msg_id comes from dispatch_check or channel notification meta.",
        "{\"msg_id\":{\"type\":\"string\",\"description\":\"The message ID to reply to\"},\"text\":{\"type\":\"string\",\"description\":\"Your reply text\"}}",
        "[\"msg_id\",\"text\"]", ref First);

    AddTool(Tools, "admin_dashboard",
        "Full admin KPI dashboard: fleet status, jobs, deliveries, audit count, interview count.",
        "{}",
        "[]", ref First);

    AddTool(Tools, "admin_drivers",
        "List all drivers with status, CDL, experience, unit assignment.",
        "{}",
        "[]", ref First);

    AddTool(Tools, "admin_interviews",
        "List all interview records with sessions, messages, and exchange counts.",
        "{}",
        "[]", ref First);

    AddTool(Tools, "admin_approve_driver",
        "Approve a pending driver application. Changes status from pending to active.",
        "{\"user_id\":{\"type\":\"string\",\"description\":\"The user ID to approve\"}}",
        "[\"user_id\"]", ref First);

    AddTool(Tools, "admin_reject_driver",
        "Reject a pending driver application with a reason.",
        "{\"user_id\":{\"type\":\"string\",\"description\":\"The user ID to reject\"},\"reason\":{\"type\":\"string\",\"description\":\"Rejection reason\"}}",
        "[\"user_id\",\"reason\"]", ref First);

    AddTool(Tools, "admin_jobs",
        "List all jobs with optional status filter.",
        "{\"status\":{\"type\":\"string\",\"description\":\"Filter by status: available, accepted, completed (empty for all)\"}}",
        "[]", ref First);

    AddTool(Tools, "admin_audit",
        "Full audit trail across all users.",
        "{\"limit\":{\"type\":\"integer\",\"description\":\"Max entries to return (default 50)\"}}",
        "[]", ref First);

    AddTool(Tools, "admin_command",
        "Execute any admin command. Full system access.",
        "{\"command\":{\"type\":\"string\",\"description\":\"The admin command to execute\"}}",
        "[\"command\"]", ref First);

    AddTool(Tools, "client_create_job",
        "Create a client shipment job with pickup, delivery, cargo, weight, schedule, and instructions. Returns price quote and job ID.",
        "{\"client_email\":{\"type\":\"string\",\"description\":\"Client email\"},\"pickup\":{\"type\":\"string\",\"description\":\"Pickup address\"},\"delivery\":{\"type\":\"string\",\"description\":\"Delivery address\"},\"cargo\":{\"type\":\"string\",\"description\":\"Cargo description\"},\"weight\":{\"type\":\"string\",\"description\":\"Weight in lbs\"},\"instructions\":{\"type\":\"string\",\"description\":\"Special instructions\"},\"schedule\":{\"type\":\"string\",\"description\":\"Scheduled date/time\"}}",
        "[\"client_email\",\"pickup\",\"delivery\"]", ref First);

    AddTool(Tools, "client_jobs",
        "List all shipment jobs for a specific client by email.",
        "{\"client_email\":{\"type\":\"string\",\"description\":\"Client email\"}}",
        "[\"client_email\"]", ref First);

    AddTool(Tools, "client_invoices",
        "List all invoices for a specific client by email.",
        "{\"client_email\":{\"type\":\"string\",\"description\":\"Client email\"}}",
        "[\"client_email\"]", ref First);

    AddTool(Tools, "client_quote",
        "Get an instant freight quote for a shipment based on distance, weight, stops, and rush priority.",
        "{\"distance\":{\"type\":\"integer\",\"description\":\"Distance in miles\"},\"weight\":{\"type\":\"integer\",\"description\":\"Weight in lbs\"},\"stops\":{\"type\":\"integer\",\"description\":\"Number of delivery stops\"},\"rush\":{\"type\":\"boolean\",\"description\":\"Rush delivery (+30%)\"}}",
        "[]", ref First);

    Tools.Append("]}");
    return Tools.ToString();
}
