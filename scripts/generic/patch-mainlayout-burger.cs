#:property TargetFramework=net11.0

// patch-mainlayout-burger.cs - replace JS-required @onclick burger with a CSS-only
// checkbox+label toggle so the menu works on statically prerendered pages
// (before Blazor WASM hydrates). Uses sibling selector .MenuToggle:checked ~ .TopActions
// in app.css to drive open/close.
const string Path = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Components\MainLayout.razor";
var Text = await File.ReadAllTextAsync(Path);

var Nl = Text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
var OldButton = "<button type=\"button\" class=\"BurgerBtn\" aria-label=\"Menu\" @onclick=\"ToggleMenu\"><span></span></button>";
var NewButton = "<input type=\"checkbox\" id=\"MenuToggle\" class=\"MenuToggle\" aria-hidden=\"true\" tabindex=\"-1\" />" + Nl + "    <label for=\"MenuToggle\" class=\"BurgerBtn\" aria-label=\"Menu\" role=\"button\"><span></span></label>";
var OldDiv = "<div class=\"TopActions @((MenuOpen || SharedUI.Services.WolfsRenderContext.MenuOpen) ? \"Open\" : \"\")\">";
var NewDiv = "<div class=\"TopActions\">";
var OldField = "    private bool Authed;" + Nl + "    private bool MenuOpen;" + Nl + "    private string? Email;";
var NewField = "    private bool Authed;" + Nl + "    private string? Email;";
var OldToggle = "    private void ToggleMenu() => MenuOpen = !MenuOpen;" + Nl + Nl + "    private async Task SignOutAsync()";
var NewToggle = "    private async Task SignOutAsync()";
var OldClear = "Authed = false; Email = null; Role = null; MenuOpen = false;";
var NewClear = "Authed = false; Email = null; Role = null;";

(string Old, string New)[] Patches =
[
    (OldButton, NewButton),
    (OldDiv, NewDiv),
    (OldField, NewField),
    (OldToggle, NewToggle),
    (OldClear, NewClear),
];
foreach (var (Old, New) in Patches)
{
    var IdxBefore = Text.IndexOf(Old, StringComparison.Ordinal);
    if (IdxBefore < 0) { await Console.Error.WriteLineAsync($"miss: '{Old[..Math.Min(60, Old.Length)]}...'"); return 1; }
    Text = Text.Replace(Old, New);
}
await File.WriteAllTextAsync(Path, Text);
await Console.Out.WriteLineAsync($"wrote {Path} ({Text.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)} chars)");
return 0;
