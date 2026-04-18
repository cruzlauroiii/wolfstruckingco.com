#:property TargetFramework=net11.0
using System.Text.Json.Nodes;

if (args.Length == 0) { await Console.Error.WriteLineAsync("usage: dotnet run scripts/rewrite-narrations.cs -- <scenes.json>"); return 1; }
var Path = args[0];
if (!File.Exists(Path)) { await Console.Error.WriteLineAsync($"missing: {Path}"); return 1; }

var Updates = new Dictionary<int, string>
{
    [1] = "Welcome to Wolfs Trucking. Logistics that actually talks back. Three roles share one platform — user, driver, admin. The home page hero invites you to browse the marketplace. Wolfs Trucking About Services Pricing Marketplace.",
    [2] = "We reset the platform to an empty slate. Every number for the rest of this tour comes from real action, not seeded fixtures. Data fills live as we walk each role — user, driver, admin. Wolfs Trucking real-time tracking voice navigation dispatcher freight marketplace.",
    [3] = "Six capabilities show on the home page. Freight marketplace — customers post jobs, pay by card or cash on delivery, drivers accept dispatch. Voice navigation. Real-time tracking. Dispatcher you can call. Inline credential intake. Live numbers. Wolfs Trucking About Services Pricing Marketplace.",
    [4] = "About — built for owner-operators and brokers. Wolfs Trucking ships freight, hires drivers, runs dispatch, all on one platform with a marketplace, real-time tracking, voice navigation, and a dispatcher you can call. Four cards: Freight marketplace, Real-time tracking, Voice navigation, Dispatcher.",
    [5] = "Sign in to your account. Access your dashboard, dispatch system, and earnings. Email and Password fields with a Sign in button. New here, Create an account. Or use single sign-on through Google, GitHub, Microsoft, or Okta. Three demo accounts — user, driver, admin — sign in without a password.",
    [6] = "We type the user demo email at wolfstruckingco-dot-com and leave the password blank. The Sign in to your account form recognizes the demo account and skips the password check. Access your dashboard, dispatch system, and earnings. Wolfs Trucking About Services Pricing Marketplace Sign In Google GitHub Microsoft Okta.",
    [7] = "User lands on the Marketplace. Buy items posted by employers, pay by card or cash on delivery. Every purchase auto-creates a delivery job for a Wolfs driver. Want to drive for Wolfs? Apply to drive. Browse, Sell an item, My orders.",
    [8] = "Three listings show on the Marketplace. Refurbished Office Chair, In stock, pickup from seller, Herman Miller Aeron, mesh seat, lumbar support, lightly used. Cold-pressed orange juice, local farm, thirty-two ounce bottle. Sparkling water case, twelve cans, citrus blend. Browse, Buy now.",
    [9] = "Driver application — chat with the Dispatcher. Your Dispatcher runs a short intake interview, captures your experience, and collects credential scans for staff review. Hi, let's get your driver profile started. Type or call.",
    [10] = "Jordan Vega types his summary. Eight years Class A CDL. TWIC, hazmat, medical exam current. Wilmington NC. Targeting operations supervisor within two years. Driver application chat with Dispatcher. Type, send.",
    [11] = "The dispatcher attaches each credential scan inline — CDL front and back, TWIC card, medical exam, hazmat endorsement, drug screening, motor vehicle record, defensive driving, plus references. Driver application chat captures experience, collects credential scans for staff review.",
    [12] = "Wolfs Hiring Hall — Review applicants, approve with badges, hire as workers. Jordan Vega, jordan at example dot com, Wilmington NC, eight year exp. CDL-A-front, TWIC-card, medical-exam attached. Approve plus assign badges, or Reject.",
    [13] = "Wolfs Hiring Hall — Review applicants, approve with badges, hire as workers. Jordan Vega, jordan at example dot com, Wilmington NC, eight year exp. CDL-A-front, TWIC-card, medical-exam. Admin can Approve plus assign badges, or Reject.",
    [14] = "Wolfs Hiring Hall — Review applicants, approve with badges, hire workers. Jordan Vega, Wilmington NC, eight year exp. Approve plus assign badges, or Reject. Admin reviews documents and writes the audit row. Wolfs Trucking About Services Pricing Marketplace.",
    [15] = "Global KPIs — everything on the platform. Earnings, drivers, employers, posted jobs, completed deliveries, live, not pre-seeded. Platform Revenue. Driver Earnings Paid. Active Drivers. Employers. Open Jobs. Completed Deliveries. Applicants Pending Review. Average Job Value posted.",
    [16] = "Driver Dashboard — Live job board, accept dispatch, navigate with voice. On-time rate, This week, Completed loads, Rating four point nine. Live map. Available jobs lists Drayage from Port of Los Angeles to Long Beach, regular, thirty-five per hour, Accept.",
    [17] = "Driver Dashboard — Live job board, accept dispatch, navigate with voice. On-time rate, This week, Completed loads, Rating. Live map. Available jobs Drayage Port of Los Angeles to Long Beach, regular, thirty-five per hour, Accept.",
    [18] = "Driver Dashboard — Live job board, accept dispatch, navigate with voice. Available jobs Drayage from Port of Los Angeles to Long Beach, regular, thirty-five per hour. Tap Accept to open a timesheet. On-time rate, This week, Completed loads, Rating.",
    [19] = "Map — Live LA-area map. Drop a pin, view a route, see the fleet. Pickup, Delivery, Fuel stop, Driver break, Depot — color-coded markers every Map view uses. Wolfs Trucking About Services Pricing Marketplace Dashboard.",
    [20] = "Every role gets the dispatcher chat. The same component every page uses, scoped to what the signed-in role is allowed to see. Drivers see their own profile, jobs, and earnings. Customers see jobs they posted. Admins see the whole platform. Hold the call button.",
    [21] = "Chat with Dispatcher. Same dispatcher chat every page uses. Signed in as driver. Two-sentence platform status update. Driver hired, open drayage job at thirty-five an hour, forty-nine dollars in posting revenue. Map streaming live. Call button.",
    [22] = "Settings — Account, theme, and notification preferences. Email driver-at-wolfstruckingco-dot-com. Role driver. Theme set to Auto. Sign out at the bottom clears the session and bounces the user back to the login page. Wolfs Trucking About Services Pricing Marketplace Dashboard.",
    [23] = "Settings — Account, theme, notification preferences. Email driver-at-wolfstruckingco-dot-com. Role driver. Theme Auto. The Theme chip cycles between Auto, Dark, and Light. Every page re-renders the same instant. Sign out clears the session.",
    [24] = "That is Wolfs Trucking end to end. Three roles — user, driver, admin. Freight marketplace with cash on delivery. Real-time tracking. Voice navigation. Dispatcher you can call. Inline credential uploads. Live KPIs. Every page same shared design. Every number live, no seeds.",
};

var Body = await File.ReadAllTextAsync(Path);
var Arr = JsonNode.Parse(Body)!.AsArray();
var Touched = 0;
foreach (var (Idx, NewNarration) in Updates)
{
    if (Idx < 1 || Idx > Arr.Count) { await Console.Error.WriteLineAsync($"scene {Idx.ToString(System.Globalization.CultureInfo.InvariantCulture)} out of range"); continue; }
    Arr[Idx - 1]!.AsObject()["narration"] = NewNarration;
    Touched++;
}

await File.WriteAllTextAsync(Path, Arr.ToJsonString(new() { WriteIndented = true }));
if (Touched > 0) { await Console.Out.WriteLineAsync($"rewrote {Touched.ToString(System.Globalization.CultureInfo.InvariantCulture)}"); }
return 0;
