return 0;

namespace Scripts
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Indexed config field by design")]
    internal static class PatchScriptsMdSsoConfig
    {
        public const string Path = @"C:\repo\public\wolfstruckingco.com\main\SCRIPTS.md";
        public const string Anchor = "| `scripts/git-commit-push.cs` |";

        public const string Marker_01 = "scripts/fetch-url.cs";
        public const string Row_01 = "| `scripts/fetch-url.cs` | `scripts/` | Generic. Reads a sibling specific .cs (`const string BaseUrl` + `Probes` 6-tuple table: Label/Path/Mode/Pattern/Method/Follow). Modes: body, head, grep, redirect. Used by every verify-* specific. |";

        public const string Marker_02 = "scripts/verify-sso.cs";
        public const string Row_02 = "| `scripts/verify-sso.cs` | `scripts/` | Specific. Probe data only. Confirms /Login/ has worker SSO anchors + the pre-hydration localStorage redirect snippet. |";

        public const string Marker_03 = "scripts/verify-sso-live.cs";
        public const string Row_03 = "| `scripts/verify-sso-live.cs` | `scripts/` | Specific. Probe data only. Live-deploy version of verify-sso with broader checks across /Login/ and /Applicant/. |";

        public const string Marker_04 = "scripts/verify-oauth.cs";
        public const string Row_04 = "| `scripts/verify-oauth.cs` | `scripts/` | Specific. Probe data only. Probes /oauth/<provider>/start on the worker for all four providers (Google, GitHub, Microsoft, Okta). 503 = secret missing; 302 to provider authorize URL = configured. |";

        public const string Marker_05 = "scripts/verify-google-redirect.cs";
        public const string Row_05 = "| `scripts/verify-google-redirect.cs` | `scripts/` | Specific. Decisive single probe: /oauth/google/start MUST 302 to https://accounts.google.com/o/oauth2/v2/auth?... |";

        public const string Marker_06 = "scripts/inspect-login-sso-buttons.cs";
        public const string Row_06 = "| `scripts/inspect-login-sso-buttons.cs` | `scripts/` | Specific. Probe data only. Wide-net grep on deployed /Login/ to inspect SSO button markup (anchors vs buttons, sso= refs, oauth refs). |";

        public const string Marker_07 = "scripts/patch-worker-oauth.cs";
        public const string Row_07 = "| `scripts/patch-worker-oauth.cs` | `scripts/` | Specific. Patch worker.js to add /oauth/<provider>/start + /oauth/<provider>/callback routes plus an OAUTH_CFG table for Google/GitHub/Microsoft/Okta. |";

        public const string Marker_08 = "scripts/set-worker-secrets.cs";
        public const string Row_08 = "| `scripts/set-worker-secrets.cs` | `scripts/` | Generic. Reads a sibling specific .cs (Provider, ClientIdKey, ClientSecretKey, SecretsJsonPath, IdJsonKey, SecretJsonKey) and pushes the two values via `wrangler secret put`. |";

        public const string Marker_09 = "scripts/google-oauth-secrets.cs";
        public const string Row_09 = "| `scripts/google-oauth-secrets.cs` | `scripts/` | Specific. Constants only. GOOGLE_CLIENT_ID + GOOGLE_CLIENT_SECRET source paths; consumed by set-worker-secrets.cs. |";

        public const string Marker_10 = "scripts/patch-scripts-md-sso.cs";
        public const string Row_10 = "| `scripts/patch-scripts-md-sso.cs` | `scripts/` | Specific (one-off). Append SSO/OAuth verify scripts to SCRIPTS.md (idempotent). |";
    }
}
