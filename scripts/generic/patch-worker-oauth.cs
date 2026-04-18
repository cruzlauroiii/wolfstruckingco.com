#:property TargetFramework=net11.0
const string Path = @"C:\repo\public\wolfstruckingco.com\main\worker\worker.js";
const string Anchor = "// Reserved API paths that must not be matched";

var OAuthRouteLines = new List<string>
{
    "    // === OAuth start: redirect to provider's authorize endpoint =====",
    "    if (url.pathname.startsWith('/oauth/') && url.pathname.endsWith('/start') && request.method === 'GET') {",
    "      const provider = url.pathname.slice('/oauth/'.length, -'/start'.length).toLowerCase();",
    "      const cfg = OAUTH_CFG[provider];",
    "      if (!cfg) return new Response('unknown provider: ' + provider, { status: 404, headers: h });",
    "      const clientId = env[cfg.idKey];",
    "      if (!clientId) {",
    "        return new Response(",
    "          '<html><body style=\"font-family:system-ui;padding:40px;max-width:600px;margin:auto\"><h1>SSO not configured</h1>' +",
    "          '<p>The Cloudflare worker needs <code>' + cfg.idKey + '</code> set in the Secrets Store binding.</p>' +",
    "          '<p>Register the OAuth app at <a href=\"' + cfg.consoleUrl + '\">' + cfg.consoleUrl + '</a> with redirect URI:</p>' +",
    "          '<pre>https://wolfstruckingco.nbth.workers.dev/oauth/' + provider + '/callback</pre>' +",
    "          '<p><a href=\"https://cruzlauroiii.github.io/wolfstruckingco.com/Login/\">&larr; Back to login</a></p></body></html>',",
    "          { status: 503, headers: { ...h, 'Content-Type': 'text/html;charset=utf-8' } }",
    "        );",
    "      }",
    "      const state = rnd();",
    "      const params = new URLSearchParams({",
    "        client_id: clientId,",
    "        redirect_uri: 'https://wolfstruckingco.nbth.workers.dev/oauth/' + provider + '/callback',",
    "        response_type: 'code',",
    "        scope: cfg.scope,",
    "        state,",
    "      });",
    "      return Response.redirect(cfg.authUrl + '?' + params.toString(), 302);",
    "    }",
    "    if (url.pathname.startsWith('/oauth/') && url.pathname.endsWith('/callback') && request.method === 'GET') {",
    "      const provider = url.pathname.slice('/oauth/'.length, -'/callback'.length).toLowerCase();",
    "      const cfg = OAUTH_CFG[provider];",
    "      if (!cfg) return new Response('unknown provider', { status: 404, headers: h });",
    "      const code = url.searchParams.get('code');",
    "      if (!code) return new Response('missing code', { status: 400, headers: h });",
    "      const clientId = env[cfg.idKey];",
    "      const clientSecret = env[cfg.secretKey] && (typeof env[cfg.secretKey] === 'string' ? env[cfg.secretKey] : await env[cfg.secretKey].get());",
    "      if (!clientId || !clientSecret) return new Response('OAuth not fully configured', { status: 503, headers: h });",
    "      const tokenResp = await fetch(cfg.tokenUrl, {",
    "        method: 'POST',",
    "        headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'Accept': 'application/json' },",
    "        body: new URLSearchParams({",
    "          client_id: clientId,",
    "          client_secret: clientSecret,",
    "          code,",
    "          redirect_uri: 'https://wolfstruckingco.nbth.workers.dev/oauth/' + provider + '/callback',",
    "          grant_type: 'authorization_code',",
    "        }).toString(),",
    "      });",
    "      if (!tokenResp.ok) return new Response('token exchange failed: ' + tokenResp.status, { status: 502, headers: h });",
    "      const tokenJson = await tokenResp.json();",
    "      const accessToken = tokenJson.access_token;",
    "      const userResp = await fetch(cfg.userUrl, { headers: { Authorization: 'Bearer ' + accessToken, Accept: 'application/json' } });",
    "      const userJson = await userResp.json();",
    "      const email = userJson.email || (userJson.emails && userJson.emails[0] && userJson.emails[0].value) || (userJson.userPrincipalName) || '';",
    "      const session = 'sso_' + provider + '_' + Date.now() + '_' + rnd();",
    "      try { await env.R2.put('sessions/' + session, JSON.stringify({ provider, email, role: 'user', issuedAt: Date.now() })); } catch {}",
    "      const html = '<html><body><script>' +",
    "        'try{localStorage.setItem(\\'wolfs_session\\',\\'' + session + '\\');' +",
    "        'localStorage.setItem(\\'wolfs_role\\',\\'user\\');' +",
    "        'localStorage.setItem(\\'wolfs_email\\',' + JSON.stringify(email) + ');}catch(e){}' +",
    "        'location.replace(\\'https://cruzlauroiii.github.io/wolfstruckingco.com/Marketplace/\\');' +",
    "        '</script>Signed in as ' + email + '. Redirecting&hellip;</body></html>';",
    "      return new Response(html, { headers: { ...h, 'Content-Type': 'text/html;charset=utf-8' } });",
    "    }",
    "    const OAUTH_CFG_DECLARED = true;",
    string.Empty,
};
var OAuthRoutes = string.Join("\n", OAuthRouteLines);

var OAuthCfgLines = new List<string>
{
    "const OAUTH_CFG = {",
    "  google: {",
    "    idKey: 'GOOGLE_CLIENT_ID', secretKey: 'GOOGLE_CLIENT_SECRET',",
    "    authUrl: 'https://accounts.google.com/o/oauth2/v2/auth',",
    "    tokenUrl: 'https://oauth2.googleapis.com/token',",
    "    userUrl: 'https://www.googleapis.com/oauth2/v3/userinfo',",
    "    scope: 'openid email profile',",
    "    consoleUrl: 'https://console.cloud.google.com/apis/credentials',",
    "  },",
    "  github: {",
    "    idKey: 'GITHUB_CLIENT_ID', secretKey: 'GITHUB_CLIENT_SECRET',",
    "    authUrl: 'https://github.com/login/oauth/authorize',",
    "    tokenUrl: 'https://github.com/login/oauth/access_token',",
    "    userUrl: 'https://api.github.com/user',",
    "    scope: 'read:user user:email',",
    "    consoleUrl: 'https://github.com/settings/developers',",
    "  },",
    "  microsoft: {",
    "    idKey: 'MICROSOFT_CLIENT_ID', secretKey: 'MICROSOFT_CLIENT_SECRET',",
    "    authUrl: 'https://login.microsoftonline.com/common/oauth2/v2.0/authorize',",
    "    tokenUrl: 'https://login.microsoftonline.com/common/oauth2/v2.0/token',",
    "    userUrl: 'https://graph.microsoft.com/v1.0/me',",
    "    scope: 'openid email profile User.Read',",
    "    consoleUrl: 'https://entra.microsoft.com/',",
    "  },",
    "  okta: {",
    "    idKey: 'OKTA_CLIENT_ID', secretKey: 'OKTA_CLIENT_SECRET',",
    "    authUrl: 'https://example.okta.com/oauth2/default/v1/authorize',",
    "    tokenUrl: 'https://example.okta.com/oauth2/default/v1/token',",
    "    userUrl: 'https://example.okta.com/oauth2/default/v1/userinfo',",
    "    scope: 'openid email profile',",
    "    consoleUrl: 'https://developer.okta.com/',",
    "  },",
    "};",
    string.Empty,
};
var OAuthCfg = string.Join("\n", OAuthCfgLines);

if (!File.Exists(Path)) { await Console.Error.WriteLineAsync($"not found: {Path}"); return 1; }
var Body = await File.ReadAllTextAsync(Path);
if (Body.Contains("OAUTH_CFG", StringComparison.Ordinal)) { return 0; }
var AnchorIdx = Body.IndexOf(Anchor, StringComparison.Ordinal);
if (AnchorIdx < 0) { await Console.Error.WriteLineAsync($"anchor not found: {Anchor}"); return 2; }
#pragma warning disable MA0074
var Indent = Body.LastIndexOf('\n', AnchorIdx);
#pragma warning restore MA0074
Indent = Indent < 0 ? 0 : Indent + 1;
var Patched = Body[..Indent] + OAuthRoutes + "    " + Body[Indent..];
var ExportIdx = Patched.IndexOf("export default", StringComparison.Ordinal);
if (ExportIdx >= 0) { Patched = Patched[..ExportIdx] + OAuthCfg + Patched[ExportIdx..]; }
await File.WriteAllTextAsync(Path, Patched);
return 0;
