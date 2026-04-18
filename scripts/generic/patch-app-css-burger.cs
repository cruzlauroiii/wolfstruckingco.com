#:property TargetFramework=net11.0

// patch-app-css-burger.cs - update app.css so the new MainLayout checkbox toggle
// drives the burger open/close. Adds .MenuToggle hide rule and changes the mobile
// .TopActions.Open opener to .MenuToggle:checked ~ .TopActions sibling selector.
const string Path = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\wwwroot\css\app.css";
var Text = await File.ReadAllTextAsync(Path);

const string OldBurger = ".BurgerBtn{display:none;width:44px;height:44px;border:1px solid var(--border);border-radius:8px;background:#fff;cursor:pointer;align-items:center;justify-content:center;padding:0}";
const string NewBurger = ".MenuToggle{position:absolute;left:-9999px;opacity:0;pointer-events:none;width:0;height:0}.BurgerBtn{display:none;width:44px;height:44px;border:1px solid var(--border);border-radius:8px;background:#fff;cursor:pointer;align-items:center;justify-content:center;padding:0;user-select:none}.MenuToggle:checked~.BurgerBtn{background:rgba(0,0,0,.05);border-color:var(--accent)}";

const string OldOpen = ".TopActions.Open{display:inline-flex}";
const string NewOpen = ".TopActions.Open,.MenuToggle:checked~.TopActions{display:inline-flex}";

if (!Text.Contains(OldBurger, StringComparison.Ordinal)) { await Console.Error.WriteLineAsync("burger anchor missing"); return 1; }
if (!Text.Contains(OldOpen, StringComparison.Ordinal)) { await Console.Error.WriteLineAsync("open anchor missing"); return 2; }

Text = Text.Replace(OldBurger, NewBurger);
Text = Text.Replace(OldOpen, NewOpen);
await File.WriteAllTextAsync(Path, Text);
await Console.Out.WriteLineAsync($"wrote {Path} ({Text.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)} chars)");
return 0;
