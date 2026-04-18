#:property TargetFramework=net11.0

// patch-login-sso.cs - replace prtask.com SSO redirects (404 in this deploy)
// with internal stubs that explain SSO is coming soon and point users to
// email/password sign-in. Pure HTML onclick so it works pre-hydration.
// (Item #5)
const string Path = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Pages\LoginPage.razor";
var Text = await File.ReadAllTextAsync(Path);

const string DemoNote = "🔐 SSO is wired through the Cloudflare worker in the live build. For this demo, use the email/password form above — sam@buyer.com / demo, sara@hr.com / demo, or john@driver.com / demo.";
(string Provider, string Icon)[] Sso =
[
    ("Google", "🔍"),
    ("GitHub", "🐙"),
    ("Microsoft", "🪟"),
    ("Okta", "🔑"),
];

var Total = 0;
foreach (var (Provider, Icon) in Sso)
{
    var TextLabel = Provider == "Microsoft" ? "Azure" : Provider;
    var Old = $"<a class=\"SsoBtn\" href=\"https://prtask.com/Api/Auth/{TextLabel}?redirect=https://cruzlauroiii.github.io/wolfstruckingco.com/app/Login/\">{Provider}</a>";
    var New = $"<button type=\"button\" class=\"SsoBtn\" onclick=\"alert('{DemoNote}')\">{Icon} {Provider}</button>";
    if (!Text.Contains(Old, StringComparison.Ordinal)) { await Console.Error.WriteLineAsync($"miss {Provider}: {Old}"); continue; }
    Text = Text.Replace(Old, New);
    Total++;
}
await File.WriteAllTextAsync(Path, Text);
await Console.Out.WriteLineAsync($"replaced {Total.ToString(System.Globalization.CultureInfo.InvariantCulture)} SSO links");
return Total == Sso.Length ? 0 : 1;
