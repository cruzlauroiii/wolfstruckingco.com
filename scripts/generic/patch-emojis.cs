#:property TargetFramework=net11.0
const string Repo = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Pages\";

(string File, string Old, string New)[] Patches =
[
    ($"{Repo}MarketplacePage.razor",
     "<h1>Marketplace</h1>",
     "<h1>\U0001F6D2 Marketplace</h1>"),
    ($"{Repo}MarketplacePage.razor",
     "<h2>Post a listing</h2>",
     "<h2>\U0001F4CB Post a listing</h2>"),
    ($"{Repo}LoginPage.razor",
     "<h1>Sign in to your account</h1>",
     "<h1>\U0001F513 Sign in to your account</h1>"),
    ($"{Repo}LoginPage.razor",
     "<button type=\"submit\" disabled=\"@Busy\">Sign in</button>",
     "<button type=\"submit\" disabled=\"@Busy\">\U0001F6AA Sign in</button>"),
    ($"{Repo}SellChatPage.razor",
     "<h1 style=\"font-size:1.4rem;margin-bottom:6px\">Chat with Agent</h1>",
     "<h1 style=\"font-size:1.4rem;margin-bottom:6px\">\U0001F4AC Chat with Agent</h1>"),
    ($"{Repo}ApplicantPage.razor",
     "<h1 style=\"font-size:1.4rem;margin-bottom:6px\">Chat with Agent</h1>",
     "<h1 style=\"font-size:1.4rem;margin-bottom:6px\">\U0001F9D1\u200D\u2708\uFE0F Chat with Agent</h1>"),
    ($"{Repo}DispatcherPage.razor",
     "<h1 style=\"font-size:1.4rem;margin-bottom:6px\">Agent</h1>",
     "<h1 style=\"font-size:1.4rem;margin-bottom:6px\">\U0001F69A Dispatcher</h1>"),
];

var Total = 0;
foreach (var (Path, Old, New) in Patches)
{
    if (!File.Exists(Path)) { await Console.Error.WriteLineAsync($"missing: {Path}"); continue; }
    var Text = await File.ReadAllTextAsync(Path);
    if (Text.Contains(New, StringComparison.Ordinal)) { continue; }
    if (!Text.Contains(Old, StringComparison.Ordinal)) { await Console.Error.WriteLineAsync($"anchor missing in {Path}"); continue; }
    await File.WriteAllTextAsync(Path, Text.Replace(Old, New));
    Total++;
}
if (Total > 0) { await Console.Out.WriteLineAsync($"patched {Total.ToString(System.Globalization.CultureInfo.InvariantCulture)}"); }
return 0;
