return 0;

namespace Scripts
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Indexed config field by design")]
    internal static class PatchScriptsMdV2Config
    {
        public const string Path = @"C:\repo\public\wolfstruckingco.com\main\SCRIPTS.md";
        public const string Anchor = "| `scripts/git-commit-push.cs` |";

        public const string Marker_01 = "scripts/git-status.cs";
        public const string Row_01 = "| `scripts/git-status.cs` | `scripts/` | Print `git status -sb`, last 3 commits, and origin/main..HEAD divergence |";

        public const string Marker_02 = "scripts/stage-amend-push.cs";
        public const string Row_02 = "| `scripts/stage-amend-push.cs` | `scripts/` | Specific. Auto-stage all `scripts/*.cs` + `wwwroot/app/` + `docs/`, amend single main commit, force-push |";

        public const string Marker_03 = "scripts/patch-gitignore.cs";
        public const string Row_03 = "| `scripts/patch-gitignore.cs` | `scripts/` | Specific. Append generated/runtime artifact rules to `.gitignore` (idempotent) |";

        public const string Marker_04 = "scripts/patch-trailing-ws.cs";
        public const string Row_04 = "| `scripts/patch-trailing-ws.cs` | `scripts/` | Generic. Strip trailing whitespace from a single file |";

        public const string Marker_05 = "scripts/patch-mainlayout-burger.cs";
        public const string Row_05 = "| `scripts/patch-mainlayout-burger.cs` | `scripts/` | Specific. Convert MainLayout burger menu from JS `@onclick` to CSS `:checked` toggle so it works on static prerendered pages |";

        public const string Marker_06 = "scripts/patch-app-css-burger.cs";
        public const string Row_06 = "| `scripts/patch-app-css-burger.cs` | `scripts/` | Specific. Drive burger open/close in `app.css` via the new `MenuToggle:checked ~ TopActions` sibling selector |";

        public const string Marker_07 = "scripts/patch-chat-rows.cs";
        public const string Row_07 = "| `scripts/patch-chat-rows.cs` | `scripts/` | Specific. Replace single-mic input row with call+attach+send row on Sell/Applicant/Dispatcher chat pages |";

        public const string Marker_08 = "scripts/patch-login-sso.cs";
        public const string Row_08 = "| `scripts/patch-login-sso.cs` | `scripts/` | Specific. Swap prtask.com SSO redirects for inline-onclick stubs that explain the demo and keep users on /Login |";

        public const string Marker_09 = "scripts/patch-marketplace-photo.cs";
        public const string Row_09 = "| `scripts/patch-marketplace-photo.cs` | `scripts/` | Specific. Add Listing.PhotoUrl + Seller, prefer `<img>` when set, overlay `📷 Photo by {Seller}` on SVG fallback |";

        public const string Marker_10 = "scripts/patch-theme-chip.cs";
        public const string Row_10 = "| `scripts/patch-theme-chip.cs` | `scripts/` | Specific. Rewire ThemeChip to a pure-HTML cycle button + add dark/light CSS rules keyed off `[data-theme]` |";

        public const string Marker_11 = "scripts/patch-wolfs-call-js.cs";
        public const string Row_11 = "| `scripts/patch-wolfs-call-js.cs` | `scripts/` | Specific. Add `window.WolfsCall` to wolfs-interop.js so chat call button starts the existing WolfsChatVoice mic bridge and drops transcript into the input |";

        public const string Marker_12 = "scripts/patch-stage-padding.cs";
        public const string Row_12 = "| `scripts/patch-stage-padding.cs` | `scripts/` | Specific. Increase top/bottom padding on `.Stage` and `.Hero` so video screenshot topic content lands closer to viewport center |";

        public const string Marker_13 = "scripts/patch-scripts-md-v2.cs";
        public const string Row_13 = "| `scripts/patch-scripts-md-v2.cs` | `scripts/` | Specific (one-off). Append this batch of patch scripts to SCRIPTS.md (idempotent) |";
    }
}
