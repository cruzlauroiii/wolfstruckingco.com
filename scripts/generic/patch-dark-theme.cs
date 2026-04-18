#:property TargetFramework=net11.0
#:property ExperimentalFileBasedProgramEnableIncludeDirective=true
#:include script-paths.cs

// patch-dark-theme.cs - Specific. Expands dark-theme CSS so all surfaces
// (forms, modals, tables, hero gradients, login card, ssoBtn, stat cards,
// pill, iframe wrappers, chat bubbles) flip on [data-theme="dark"]. (Item #26)
// Owns ALL the find/replace tuples; delegates to GENERIC patch-file.cs --batch.
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Scripts;

const string Css = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\wwwroot\css\app.css";

const string OldDarkRules = "html[data-theme=\"dark\"]{--bg:#0a0d12;--card:#1e293b;--text:#f8fafc;--text-muted:#cbd5e1;--border:#334155;--accent:#fb923c;--accent-hover:#ea580c}html[data-theme=\"dark\"] .TopBar{background:#0f172a}html[data-theme=\"dark\"] .Field input,html[data-theme=\"dark\"] .Field textarea,html[data-theme=\"dark\"] .Field select,html[data-theme=\"dark\"] .CredForm input{background:#1e293b;color:#f8fafc}html[data-theme=\"dark\"] .Listing,html[data-theme=\"dark\"] .Card,html[data-theme=\"dark\"] .Stat,html[data-theme=\"dark\"] .ModalBody,html[data-theme=\"dark\"] .LoginCard{background:#1e293b;color:#f8fafc}html[data-theme=\"light\"]{--bg:#ffffff;--card:#f3f6fa;--text:#0a0d12;--text-muted:#334155;--border:#cbd5e1}";

const string NewDarkRules = "html[data-theme=\"dark\"]{--bg:#0a0d12;--card:#1e293b;--text:#f8fafc;--text-muted:#cbd5e1;--border:#334155;--accent:#fb923c;--accent-hover:#ea580c;color-scheme:dark}html[data-theme=\"dark\"] body{background:var(--bg);color:var(--text)}html[data-theme=\"dark\"] .TopBar{background:#0f172a;border-bottom-color:#1e293b}html[data-theme=\"dark\"] .Brand a{color:#f8fafc}html[data-theme=\"dark\"] .TopActions a,html[data-theme=\"dark\"] .TopActions .LinkBtn{color:#cbd5e1}html[data-theme=\"dark\"] .TopActions a:hover,html[data-theme=\"dark\"] .TopActions .LinkBtn:hover{color:#fb923c;background:#1e293b}html[data-theme=\"dark\"] .Field input,html[data-theme=\"dark\"] .Field textarea,html[data-theme=\"dark\"] .Field select,html[data-theme=\"dark\"] .CredForm input{background:#1e293b;color:#f8fafc;border-color:#334155}html[data-theme=\"dark\"] .Field input::placeholder,html[data-theme=\"dark\"] .CredForm input::placeholder{color:#64748b}html[data-theme=\"dark\"] .Listing,html[data-theme=\"dark\"] .Card,html[data-theme=\"dark\"] .Stat,html[data-theme=\"dark\"] .ModalBody,html[data-theme=\"dark\"] .LoginCard{background:#1e293b;color:#f8fafc;border-color:#334155}html[data-theme=\"dark\"] .Listing .Photo{background:#0f172a}html[data-theme=\"dark\"] .Hero{background:linear-gradient(135deg,#1e293b 0%,#0f172a 100%);color:#f8fafc}html[data-theme=\"dark\"] .Hero p{color:#cbd5e1}html[data-theme=\"dark\"] .Empty{background:#1e293b;color:#cbd5e1;border-color:#334155}html[data-theme=\"dark\"] .Table{background:#1e293b;color:#f8fafc}html[data-theme=\"dark\"] .Table th{background:#0f172a;color:#cbd5e1}html[data-theme=\"dark\"] .Table td,html[data-theme=\"dark\"] .Table th{border-bottom-color:#334155}html[data-theme=\"dark\"] .Tab{color:#94a3b8}html[data-theme=\"dark\"] .Tab.Active{color:#fb923c;border-bottom-color:#fb923c}html[data-theme=\"dark\"] .Btn.Ghost{color:#f8fafc;border-color:#334155}html[data-theme=\"dark\"] .Btn.Ghost:hover{color:#fb923c;border-color:#fb923c}html[data-theme=\"dark\"] .SsoBtn{background:#1e293b;color:#f8fafc;border-color:#334155}html[data-theme=\"dark\"] .SsoBtn:hover{background:#334155;color:#fb923c;border-color:#fb923c}html[data-theme=\"dark\"] .Pill{background:#334155;color:#cbd5e1}html[data-theme=\"dark\"] .Divider,html[data-theme=\"dark\"] .CredHint{color:#94a3b8}html[data-theme=\"dark\"] .Divider::before,html[data-theme=\"dark\"] .Divider::after{background:#334155}html[data-theme=\"dark\"] .CredForm button{background:#fb923c;color:#0a0d12}html[data-theme=\"dark\"] .CredForm button:hover{background:#ea580c}html[data-theme=\"dark\"] .ErrMsg{background:#3f1d1d;color:#fca5a5;border-color:#7f1d1d}html[data-theme=\"dark\"] .SuccessBanner{background:#0f3a26;color:#86efac;border-color:#14532d}html[data-theme=\"dark\"] .AuthWarn{background:#3a2410;color:#fdba74;border-color:#7c2d12}html[data-theme=\"dark\"] .Modal{background:rgba(0,0,0,.75)}html[data-theme=\"dark\"] .PayChoice label{background:#1e293b;color:#f8fafc;border-color:#334155}html[data-theme=\"dark\"] .PayChoice label.Active{background:rgba(251,146,60,.15);color:#fb923c;border-color:#fb923c}html[data-theme=\"dark\"] .MapWrap{background:#0f172a;border-color:#334155}html[data-theme=\"dark\"] .wt-theme-chip{background:#1e293b;color:#f8fafc;border-color:#334155}html[data-theme=\"dark\"] .wt-theme-chip:hover{background:#334155;color:#fb923c}html[data-theme=\"dark\"] .BurgerBtn{background:#1e293b;border-color:#334155}html[data-theme=\"dark\"] .BurgerBtn span,html[data-theme=\"dark\"] .BurgerBtn span::before,html[data-theme=\"dark\"] .BurgerBtn span::after{background:#f8fafc}html[data-theme=\"dark\"] [data-wolfs-banner=\"1\"]{background:#1e293b;color:#f8fafc;border:1px solid #334155}html[data-theme=\"light\"]{--bg:#ffffff;--card:#f3f6fa;--text:#0a0d12;--text-muted:#334155;--border:#cbd5e1}";

(string Path, string Find, string Replace, bool Idempotent)[] Patches =
[
    (Css, OldDarkRules, NewDarkRules, true),
];

var Tmp = Path.Combine(Path.GetTempPath(), $"wolfs-dark-{Guid.NewGuid():N}.jsonl");
var Opts = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
await using (var Sw = new StreamWriter(Tmp))
{
    foreach (var (P, F, R, I) in Patches)
    {
        var Obj = new JsonObject { ["path"] = P, ["find"] = F, ["replace"] = R, ["idempotent"] = I };
        await Sw.WriteLineAsync(Obj.ToJsonString(Opts));
    }
}
await Console.Out.WriteLineAsync($"wrote batch: {Tmp} ({Patches.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)} patches)");
var Psi = new ProcessStartInfo("dotnet", $"run scripts/patch-file.cs -- --batch \"{Tmp}\"") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, WorkingDirectory = Paths.Repo };
using var Proc = Process.Start(Psi)!;
await Console.Out.WriteAsync(await Proc.StandardOutput.ReadToEndAsync());
await Console.Error.WriteAsync(await Proc.StandardError.ReadToEndAsync());
await Proc.WaitForExitAsync();
try { File.Delete(Tmp); } catch (IOException) { }
return Proc.ExitCode;
