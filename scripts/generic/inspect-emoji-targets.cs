#:property TargetFramework=net11.0

// inspect-emoji-targets.cs - Specific. Owns the page list to inspect for
// emoji anchors. Reads each file (project source, not a runtime artifact)
// and prints H1/H2 lines so the human can pick which to emoji-bump.
const string Pages = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Pages\";

string[] Targets =
[
    "ServicesPage.razor",
    "AboutPage.razor",
    "PricingPage.razor",
    "ApplyPage.razor",
    "TrackPage.razor",
    "DashboardPage.razor",
    "AdminPage.razor",
    "HiringHallPage.razor",
    "FAQPage.razor",
    "CareersPage.razor",
    "BlogPage.razor",
    "ContactPage.razor",
    "ReviewsPage.razor",
    "IndustriesPage.razor",
    "SignUpPage.razor",
];

foreach (var T in Targets)
{
    var Full = Path.Combine(Pages, T);
    if (!File.Exists(Full)) { await Console.Out.WriteLineAsync($"[skip] missing {T}"); continue; }
    await Console.Out.WriteLineAsync($"=== {T} ===");
    var Lines = await File.ReadAllLinesAsync(Full);
    for (var I = 0; I < Lines.Length; I++)
    {
        var L = Lines[I];
        if (L.Contains("<h1", StringComparison.Ordinal) || L.Contains("<h2", StringComparison.Ordinal) || L.Contains("class=\"Btn", StringComparison.Ordinal))
        {
            await Console.Out.WriteLineAsync($"  {(I + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)}\t{L.Trim()}");
        }
    }
}
return 0;
