#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

// expand-credentials.cs — replace scene 11's 3-credential applicant write with
// the full 8-credential set (CDL front + back, TWIC, medical, hazmat,
// drug screen, MVR, defensive driving, references) so the chat shows every
// supported credential type per user point 4.
//
//   dotnet run scripts/expand-credentials.cs -- <scenes.json>

using System.Text.Json.Nodes;

if (args.Length == 0)
{
    Console.Error.WriteLine("usage: dotnet run scripts/expand-credentials.cs -- <scenes.json>");
    return 1;
}
var Path = args[0];
if (!File.Exists(Path))
{
    Console.Error.WriteLine($"missing: {Path}");
    return 1;
}

var Body = File.ReadAllText(Path);
var Arr = JsonNode.Parse(Body)!.AsArray();

const string NewTarget =
    "async () => { " +
    "await WolfsInterop.dbPut('applicants', {id:'app_jordan', name:'Jordan Vega', email:'jordan@example.com', location:'Wilmington, NC', experienceYears:8, status:'pending_review', " +
    "uploads:[" +
    "{name:'CDL-A-front.svg', size:182336, kind:'cdl'}," +
    "{name:'CDL-A-back.svg', size:175020, kind:'cdl'}," +
    "{name:'TWIC-card.svg', size:96112, kind:'twic'}," +
    "{name:'medical-exam.svg', size:71224, kind:'medical'}," +
    "{name:'hazmat-endorsement.svg', size:88550, kind:'hazmat'}," +
    "{name:'drug-screen.svg', size:62330, kind:'drug'}," +
    "{name:'mvr-clean.svg', size:55104, kind:'mvr'}," +
    "{name:'defensive-driving.svg', size:48998, kind:'defensive'}," +
    "{name:'reference-1.svg', size:32100, kind:'reference'}," +
    "{name:'reference-2.svg', size:31840, kind:'reference'}" +
    "]}); " +
    "const log=document.querySelector('.WChatLog, .Card > div'); " +
    "if(log){ " +
    "const u=document.createElement('div'); u.style.cssText='align-self:flex-end;background:#ff6b35;color:#fff;padding:10px 14px;border-radius:10px;max-width:90%;margin-top:8px;font-size:.86rem'; " +
    "u.innerHTML='📎 Attached CDL front and back, TWIC, medical, hazmat, drug screen, motor vehicle record, defensive driving, plus two references.'; log.appendChild(u); " +
    "const a=document.createElement('div'); a.style.cssText='align-self:flex-start;background:rgba(255,107,53,.08);border:1px solid rgba(255,107,53,.3);padding:10px 14px;border-radius:10px;max-width:90%;margin-top:8px;font-size:.86rem'; " +
    "a.innerHTML='✓ All ten credential scans on file. Your application is queued for staff review at the Hiring Hall.'; log.appendChild(a); " +
    "log.scrollTop=log.scrollHeight; } return 'attached'; }";

if (Arr.Count >= 11)
{
    var Scene11 = Arr[10]!.AsObject();
    Scene11["target"] = NewTarget;
    Console.WriteLine("  ✓ scene 11 expanded to 10 credential uploads");
}
File.WriteAllText(Path, Arr.ToJsonString(new() { WriteIndented = true }));
return 0;
