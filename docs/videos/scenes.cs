#:property TargetFramework=net11.0
#:property RunAnalyzersDuringBuild=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false

// scenes.cs — short, non-technical, user-facing scenes only. Workflow order:
// seller → drivers → admin (hires) → drivers accept jobs → buyer (browses + buys)
// → drivers do legs → delivery → KPIs.
//
// === SSO-only first-time login (no separate sign-up scene) ===
// Every Login scene below carries an `["sso"] = "<provider>"` field consumed
// by run-crud-pipeline.cs ResolveSsoProvider (primary path). Narration still
// says "with Google/GitHub/Microsoft/Okta" so the regex fallback also works
// (defense in depth).
//
// Why no SignUp scenes:
//   1. The Wolfs worker (worker/worker.cs → worker.js, lines 317-353) issues a
//      session unconditionally on a successful OAuth token+userinfo exchange.
//      It never reads the `users` collection, so first-time SSO users get a
//      working session on the first OAuth callback. Public /api/signup was
//      removed in task #209 — SSO is the only sign-in path.
//   2. Each IdP also auto-handles "first-time" on its side:
//        - Google: consent screen on first request; no separate app account
//          (https://developers.google.com/identity/protocols/oauth2).
//        - GitHub: first-time user grants requested scopes on the authorize
//          form; uses their existing GitHub account
//          (https://docs.github.com/en/apps/oauth-apps/using-oauth-apps/authorizing-oauth-apps).
//        - Microsoft Entra: first sign-in from a new tenant creates a service
//          principal in that tenant on consent
//          (https://learn.microsoft.com/en-us/entra/identity/enterprise-apps/create-service-principal-cross-tenant).
//        - Okta: standards-compliant OAuth 2.0 / OIDC; users are provisioned
//          on first successful login (just-in-time)
//          (https://developer.okta.com/docs/concepts/oauth-openid/).
//
// Persona → provider + account mapping (task #218).
// Seven SSO accounts are currently signed into Chrome (3 Google + 2 Entra + 1 Okta + 1 GitHub),
// 1:1 with the 7 personas. Each Login scene below carries a structural `sso` field, and
// (where the provider has more than one signed-in account) an `account` field so the renderer
// can pick the matching tile on Chrome's "Choose an account" screen.
//
//   Persona              | Provider  | Account
//   ---------------------|-----------|-------------------------
//   Car seller           | Google    | cruzlauroiii@gmail.com
//   Car buyer            | Microsoft | cruzlauroiii@gmail.com
//   Admin                | GitHub    | (only signed-in account)
//   Driver from China    | Okta      | (only signed-in account)
//   Driver from LA       | Google    | noahblesse@gmail.com
//   Team driver Phoenix  | Microsoft | noahblesse@gmail.com
//   Driver in Wilmington | Google    | analynrcastillo@gmail.com

using System.Text.Json.Nodes;

const string Base = Environment.GetEnvironmentVariable("WOLFS_SCENE_BASE") ?? "https://cruzlauroiii.github.io/wolfstruckingco.com";
var SellerAccount = Environment.GetEnvironmentVariable("WOLFS_SELLER_ACCOUNT") ?? "cruzlauroiii@gmail.com";
var BuyerAccount = Environment.GetEnvironmentVariable("WOLFS_BUYER_ACCOUNT") ?? "cruzlauroiii@gmail.com";
var LaDriverAccount = Environment.GetEnvironmentVariable("WOLFS_LA_DRIVER_ACCOUNT") ?? "noahblesse@gmail.com";
var PhoenixTeamAccount = Environment.GetEnvironmentVariable("WOLFS_PHOENIX_TEAM_ACCOUNT") ?? "noahblesse@gmail.com";
var WilmingtonAccount = Environment.GetEnvironmentVariable("WOLFS_WILMINGTON_ACCOUNT") ?? "analynrcastillo@gmail.com";
const double Wait = 3;

var Out = new JsonArray();
void Add(string Path, string N, string? Sso = null, string? Account = null)
{
    var Obj = new JsonObject {
        ["action"] = "navigate",
        ["target"] = $"{Base}{Path}?cb=" + (Out.Count + 1).ToString("000"),
        ["narration"] = N,
        ["wait"] = Wait,
    };
    if (Sso is not null) { Obj["sso"] = Sso; }
    if (Account is not null) { Obj["account"] = Account; }
    Out.Add(Obj);
}

// Seller — lands on home page, signs in via SSO, posts job through agent chat
Add("/",               "Car seller lands on the Wolfs home page.");
Add("/Login/",         "Car seller signs in with Google to post a car for sale.", "google", SellerAccount);
Add("/",               "Car seller is back on the home page and taps Sell to talk to the agent.");
Add("/Sell/Chat/",     "Car seller starts a chat with the agent to post the car.");
Add("/Sell/Chat/",     "Agent asks what car and where it is picked up.");
Add("/Sell/Chat/",     "Agent asks how much cash the driver should bring to the factory.");
Add("/Sell/Chat/",     "Agent asks who the buyer is and where the car goes.");
Add("/Sell/Chat/",     "Agent asks the price and how the buyer pays.");
Add("/Sell/Chat/",     "Agent writes the job and seller publishes it.");
Add("/Marketplace/",   "Seller sees the published BYD Han EV listing live in the marketplace.");

// Drivers — each one signs in via SSO, taps Apply, chats with Agent, uploads docs, then waits
Add("/Login/",         "Driver from China signs in with Okta.", "okta");
Add("/Apply/",         "Driver from China taps Apply to be a driver.");
Add("/Applicant/",     "Driver from China chats with the Agent. Agent asks his name and years driving.");
Add("/Applicant/",     "Agent asks for his license and China export pass.");
Add("/Applicant/",     "Driver from China sends both scans.");
Add("/Documents/",     "Driver from China uploads his driver's license and China export pass.");
Add("/Apply/",         "Driver from China sees his application is pending admin approval.");

Add("/Login/",         "Driver from Los Angeles signs in with Google.", "google", LaDriverAccount);
Add("/Apply/",         "Driver from Los Angeles taps Apply to be a driver.");
Add("/Applicant/",     "Driver from Los Angeles chats with the Agent and shares his details.");
Add("/Applicant/",     "Agent asks for his TWIC port pass and drayage card.");
Add("/Applicant/",     "Driver from Los Angeles sends both scans.");
Add("/Documents/",     "Driver from Los Angeles uploads his license, port pass, and drayage card.");
Add("/Apply/",         "Driver from Los Angeles sees his application is pending admin approval.");

Add("/Login/",         "Team driver in Phoenix signs in with Microsoft.", "microsoft", PhoenixTeamAccount);
Add("/Apply/",         "Team driver in Phoenix taps Apply to be a driver.");
Add("/Applicant/",     "Team driver in Phoenix chats with the Agent and shares his details.");
Add("/Applicant/",     "Agent asks for the team-driver papers.");
Add("/Applicant/",     "Team driver in Phoenix sends the papers.");
Add("/Documents/",     "Team driver in Phoenix uploads team papers and both licenses.");
Add("/Apply/",         "Team driver in Phoenix sees his application is pending admin approval.");

Add("/Login/",         "Driver in Wilmington signs in with Google.", "google", WilmingtonAccount);
Add("/Apply/",         "Driver in Wilmington taps Apply to be a driver.");
Add("/Applicant/",     "Driver in Wilmington chats with the Agent and shares his details.");
Add("/Applicant/",     "Agent asks for the auto-handling cert.");
Add("/Applicant/",     "Driver in Wilmington sends the cert.");
Add("/Documents/",     "Driver in Wilmington uploads his license and auto-handling cert.");
Add("/Apply/",         "Driver in Wilmington sees his application is pending admin approval.");

// Admin — approves all four drivers in one batch
Add("/Login/",         "Admin signs in with GitHub to approve the new drivers.", "github");
Add("/Admin/",         "Admin lands on the home page and sees four pending applicants.");
Add("/HiringHall/",    "Admin sees all four drivers in the list.");
Add("/HiringHall/",    "Admin clicks Approve all and assigns badges in one batch.");
Add("/HiringHall/",    "All four drivers are hired at the same time.");

// Drivers see they're hired, then land on the driver home
Add("/Apply/",         "Driver from China sees he is hired and goes to the driver home.");
Add("/Dashboard/",     "Driver from China lands on the driver home page.");
Add("/Apply/",         "Driver from Los Angeles sees he is hired.");
Add("/Dashboard/",     "Driver from Los Angeles lands on the driver home page.");
Add("/Apply/",         "Team driver in Phoenix sees he is hired.");
Add("/Dashboard/",     "Team driver in Phoenix lands on the driver home page.");
Add("/Apply/",         "Driver in Wilmington sees he is hired.");
Add("/Dashboard/",     "Driver in Wilmington lands on the driver home page.");

// Buyer — lands on home, signs in via SSO, browses marketplace, sees the posted car
Add("/",               "Car buyer lands on the Wolfs home page.");
Add("/Login/",         "Car buyer signs in with Microsoft to find a car.", "microsoft", BuyerAccount);
Add("/Marketplace/",   "Car buyer lands on the marketplace and sees the car the seller posted.");

// Buyer checkout — five dedicated pages
Add("/Buy/ShipTo/",    "Car buyer enters his shipping address.");
Add("/Buy/Contact/",   "Car buyer enters his contact for delivery.");
Add("/Buy/Window/",    "Car buyer picks his delivery day and time.");
Add("/Buy/Pay/",       "Car buyer picks pay on delivery.");
Add("/Buy/Notes/",     "Car buyer adds special instructions and confirms the order.");

// Driver from China leg — China factory to port
Add("/Map/",           "Driver from China starts the map.");
Add("/Map/",           "Voice says: head west to the highway.");
Add("/Map/",           "Voice says: take the exit toward Hefei.");
Add("/Map/",           "Voice says: continue for three hundred kilometers.");
Add("/Map/",           "Voice says: arrive at the BYD factory.");
Add("/Dispatcher/",    "Driver from China tells Agent he is at the factory.");
Add("/Dispatcher/",    "Agent confirms the cash payment to the factory.");
Add("/Dispatcher/",    "Driver from China places the GPS tracker inside the car.");
Add("/Map/",           "Voice says: head east to Shanghai port.");
Add("/Map/",           "Voice says: take the bridge to terminal four.");
Add("/Map/",           "Voice says: arrive at the port.");
Add("/Dispatcher/",    "Driver from China loads the car into the ship's container.");

// Ocean transit
Add("/Track/",         "The ship leaves Shanghai.");
Add("/Track/",         "Car buyer watches the ship cross the ocean.");
Add("/Track/",         "The ship is halfway to Los Angeles.");
Add("/Track/",         "The ship arrives at Los Angeles.");

// Driver from Los Angeles leg
Add("/Dispatcher/",    "Agent tells driver from Los Angeles the ship is here.");
Add("/Map/",           "Driver from Los Angeles starts the map.");
Add("/Map/",           "Voice says: head north to the port.");
Add("/Map/",           "Voice says: show the port pass at gate B.");
Add("/Map/",           "Voice says: pick up the car from the yard.");
Add("/Dispatcher/",    "Driver from Los Angeles tells Agent he picked up the car.");
Add("/Map/",           "Voice says: head east on the highway to Phoenix.");

// Auto delay + recompute — GPS telemetry detects congestion, system recomputes
Add("/Dispatcher/",    "GPS telemetry detects heavy congestion ahead on I-10. ETA recomputed automatically.");
Add("/Dispatcher/",    "Agent tells the buyer the new ETA.");
Add("/Schedule/",      "System recomputes downstream legs from live traffic.");
Add("/Map/",           "Voice says: arrive at Phoenix.");
Add("/Dispatcher/",    "Driver from Los Angeles finishes his leg.");

// Team driver in Phoenix leg
Add("/Dispatcher/",    "Agent tells team driver in Phoenix to start.");
Add("/Map/",           "Team driver in Phoenix starts the map.");
Add("/Map/",           "Voice says: head east on the highway.");
Add("/Map/",           "Voice says: continue east on I-40 through Albuquerque.");
Add("/Map/",           "Voice says: continue to Memphis.");
Add("/Map/",           "Voice says: arrive at the Memphis yard.");
Add("/Dispatcher/",    "Team driver in Phoenix finishes the leg.");

// Driver in Wilmington leg
Add("/Dispatcher/",    "Agent tells driver in Wilmington to start the last leg.");
Add("/Map/",           "Driver in Wilmington starts the map.");
Add("/Map/",           "Voice says: head east on the highway.");
Add("/Map/",           "Voice says: turn south to Wilmington.");
Add("/Map/",           "Voice says: turn onto Oak Street.");
Add("/Map/",           "Voice says: arrive at fourteen-eighteen Oak Street.");
Add("/Dispatcher/",    "Driver in Wilmington calls the buyer from the door.");

// Delivery
Add("/Dispatcher/",    "Car buyer comes to the door.");
Add("/Dispatcher/",    "Car buyer looks at the car.");
Add("/Dispatcher/",    "Car buyer pays at the door.");
Add("/Dispatcher/",    "Driver in Wilmington takes a delivery photo.");
Add("/Dispatcher/",    "Driver in Wilmington hands over the keys.");

// KPIs
Add("/Investors/KPI/", "Admin opens the dashboard.");
Add("/Investors/KPI/", "All four drivers were paid.");
Add("/Investors/KPI/", "Driver from China was paid for leg one.");
Add("/Investors/KPI/", "Driver from Los Angeles was paid for leg two.");
Add("/Investors/KPI/", "Team driver in Phoenix was paid for leg three.");
Add("/Investors/KPI/", "Driver in Wilmington was paid for leg four.");
Add("/Investors/KPI/", "Driver from China is paid back for the factory cash.");
Add("/Investors/KPI/", "All shipping costs are paid.");
Add("/Investors/KPI/", "Customs fees are paid.");
Add("/Investors/KPI/", "The buyer paid in full.");
Add("/Investors/KPI/", "The platform earned its share.");
Add("/Investors/KPI/", "Every delivery was on time.");
Add("/Investors/KPI/", "Every payment cleared.");
Add("/Marketplace/",   "The listing is closed.");
Add("/Track/",         "The order is delivered.");


if (args.Length > 0) { File.WriteAllText(args[0], Out.ToJsonString(new() { WriteIndented = true })); }
double Total = 0;
foreach (var S in Out) { Total += S!["wait"]!.GetValue<double>(); }
Console.WriteLine($"wrote {Out.Count} scenes — total {Total:0}s = {Total / 60:0.00} min");
return 0;


