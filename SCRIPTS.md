# `dotnet run *.cs` script reference

Every scripted/repeatable operation in this repo is a **.NET 11 file-based program** invoked with `dotnet run <file>.cs`. No PowerShell scripts, no bash one-liners — single language (C#) for app code and tooling.

> Invoke from the **PowerShell tool** only. Pass arguments after `--`. Example: `dotnet run scripts/publish-pages.cs -- --basePath /wolfstruckingco.com/app/`.

| Script | Folder | Purpose |
|--------|--------|---------|
| `wolfstruckingco.cs` | repo root | MCP server + launcher for Claude Code |
| `scripts/generic/publish-pages.cs` | `scripts/generic/` | Publish Blazor WASM client to `docs/app/` for GitHub Pages |
| `scripts/generic/build-all.cs` | `scripts/generic/` | Single-entry orchestrator: SCSS + WASM publish + static HTML in one shot |
| `scripts/generic/lint-strict.cs` | `scripts/generic/` | Strict linter — flags magic numbers, magic strings, non-PascalCase, domain words in `$"…"` |
| `scripts/generic/lint-each-script.cs` | `scripts/generic/` | Generic. Reads `Dir`/`Repo` from a config specific and runs `dotnet build` against every `*.cs` in `Dir`, skipping files included via `#:include` |
| `scripts/specific/lint-each-config.cs` | `scripts/specific/` | Specific. Config for `lint-each-script.cs` — points at `scripts/` for per-file build linting |
| `scripts/generic/lint-one-script.cs` | `scripts/generic/` | Generic. Reads `Target`/`Repo` from a config specific and runs `dotnet build` against ONE file. Prints `OK` or one error per line. Used by per-file fixer agents to validate their own file without colliding on the shared lint runfile. |
| `scripts/specific/lint-one-cdpcommands.cs` | `scripts/specific/` | Specific. Targets `chrome-devtools.cs` (its `#:include` chain pulls in CdpCommands.cs); used to validate CdpCommands.cs lint state. |
| `scripts/specific/lint-one-cdpsetup.cs` | `scripts/specific/` | Specific. Targets `chrome-devtools.cs`; used to validate CdpSetup.cs lint state. |
| `scripts/specific/lint-one-chromedevtools.cs` | `scripts/specific/` | Specific. Targets `chrome-devtools.cs` directly; used to validate the entry-point host file lint state. |
| `scripts/specific/lint-one-lintstrict.cs` | `scripts/specific/` | Specific. Targets `lint-strict.cs`. |
| `scripts/specific/lint-one-migrateclasses.cs` | `scripts/specific/` | Specific. Targets `migrate-classes.cs`. |
| `scripts/specific/lint-one-setworkersecrets.cs` | `scripts/specific/` | Specific. Targets `set-worker-secrets.cs`. |
| `scripts/specific/lint-one-wolfstruckingco.cs` | `scripts/specific/` | Specific. Targets root-level `wolfstruckingco.cs` MCP host/launcher; used to validate that file's lint state under the documented file-level NoWarn opt-outs. |
| `scripts/generic/rename-namespace.cs` | `scripts/generic/` | Strip the WolfsTruckingCo prefix from every project, folder, csproj, namespace |
| `scripts/generic/fix-datetime-kind.cs` | `scripts/generic/` | Append `, DateTimeKind.Local` to every `new DateTime(...)` (Sonar S6562) |
| `scripts/generic/tail-file.cs` | `scripts/generic/` | Print last N lines of a file (replaces `tail -N path`) |
| `scripts/generic/dump-file.cs` | `scripts/generic/` | Print full file / line range / byte range / regex-grep — replaces ad-hoc Read on generated artifacts |
| `scripts/generic/patch-file.cs` | `scripts/generic/` | Substring or regex replace on a file — replaces ad-hoc Edit on non-cs files |
| `scripts/generic/git-commit-push.cs` | `scripts/generic/` | git add -u + amend (or new) + force-push to main |
| `scripts/generic/fetch-url.cs` | `scripts/generic/` | Generic. Reads a sibling specific .cs (`const string BaseUrl` + `Probes` 6-tuple table: Label/Path/Mode/Pattern/Method/Follow). Modes: body, head, grep, redirect. Used by every verify-* specific. |
| `scripts/specific/verify-sso.cs` | `scripts/specific/` | Specific. Probe data only. Confirms /Login/ has worker SSO anchors + the pre-hydration localStorage redirect snippet. |
| `scripts/specific/verify-sso-live.cs` | `scripts/specific/` | Specific. Probe data only. Live-deploy version of verify-sso with broader checks across /Login/ and /Applicant/. |
| `scripts/specific/verify-oauth.cs` | `scripts/specific/` | Specific. Probe data only. Probes /oauth/<provider>/start on the worker for all four providers (Google, GitHub, Microsoft, Okta). 503 = secret missing; 302 to provider authorize URL = configured. |
| `scripts/specific/verify-google-redirect.cs` | `scripts/specific/` | Specific. Decisive single probe: /oauth/google/start MUST 302 to https://accounts.google.com/o/oauth2/v2/auth?... |
| `scripts/specific/inspect-login-sso-buttons.cs` | `scripts/specific/` | Specific. Probe data only. Wide-net grep on deployed /Login/ to inspect SSO button markup (anchors vs buttons, sso= refs, oauth refs). |
| `scripts/specific/patch-worker-oauth.cs` | `scripts/specific/` | Specific. Patch worker.js to add /oauth/<provider>/start + /oauth/<provider>/callback routes plus an OAUTH_CFG table for Google/GitHub/Microsoft/Okta. |
| `scripts/generic/set-worker-secrets.cs` | `scripts/generic/` | Generic. Reads a sibling specific .cs (Provider, ClientIdKey, ClientSecretKey, SecretsJsonPath, IdJsonKey, SecretJsonKey) and pushes the two values via `wrangler secret put`. |
| `scripts/specific/google-oauth-secrets.cs` | `scripts/specific/` | Specific. Constants only. GOOGLE_CLIENT_ID + GOOGLE_CLIENT_SECRET source paths; consumed by set-worker-secrets.cs. |
| `scripts/specific/github-oauth-secrets.cs` | `scripts/specific/` | Specific. Constants only. GITHUB_CLIENT_ID + GITHUB_CLIENT_SECRET source paths; consumed by set-worker-secrets.cs. |
| `scripts/specific/microsoft-oauth-secrets.cs` | `scripts/specific/` | Specific. Constants only. MICROSOFT_CLIENT_ID + MICROSOFT_CLIENT_SECRET source paths (Azure: keys); consumed by set-worker-secrets.cs. |
| `scripts/specific/okta-oauth-secrets.cs` | `scripts/specific/` | Specific. Constants only. OKTA_CLIENT_ID + OKTA_CLIENT_SECRET source paths; consumed by set-worker-secrets.cs. |
| `scripts/generic/wipe-db.cs` | `scripts/generic/` | Generic. POSTs the worker's admin-only `/api-wipe` endpoint declared in `wipe-db-config.cs` to clear every R2 collection, then GETs `/api/listings` to verify the wipe (`count=0` + empty `items`). Used by the video pipeline to start each run from an empty marketplace. |
| `scripts/specific/wipe-db-config.cs` | `scripts/specific/` | Specific. Constants only. WipeUrl + VerifyUrl + admin headers consumed by `wipe-db.cs`. |
| `scripts/specific/inspect-secrets-config.cs` | `scripts/specific/` | Specific. Config for `dump-file.cs` — points at the user-secrets `secrets.json` and greps for `ClientId`/`ClientSecret`/provider names. |
| `scripts/specific/patch-scripts-md-sso.cs` | `scripts/specific/` | Specific (one-off). Append SSO/OAuth verify scripts to SCRIPTS.md (idempotent). |
| `scripts/generic/git-status.cs` | `scripts/generic/` | Print `git status -sb`, last 3 commits, and origin/main..HEAD divergence |
| `scripts/specific/stage-amend-push.cs` | `scripts/specific/` | Specific. Auto-stage all `scripts/*.cs` + `wwwroot/app/` + `docs/`, amend single main commit, force-push |
| `scripts/specific/patch-gitignore.cs` | `scripts/specific/` | Specific. Append generated/runtime artifact rules to `.gitignore` (idempotent) |
| `scripts/generic/patch-trailing-ws.cs` | `scripts/generic/` | Generic. Strip trailing whitespace from a single file |
| `scripts/specific/patch-mainlayout-burger.cs` | `scripts/specific/` | Specific. Convert MainLayout burger menu from JS `@onclick` to CSS `:checked` toggle so it works on static prerendered pages |
| `scripts/specific/patch-app-css-burger.cs` | `scripts/specific/` | Specific. Drive burger open/close in `app.css` via the new `MenuToggle:checked ~ TopActions` sibling selector |
| `scripts/specific/patch-chat-rows.cs` | `scripts/specific/` | Specific. Replace single-mic input row with call+attach+send row on Sell/Applicant/Dispatcher chat pages |
| `scripts/specific/patch-login-sso.cs` | `scripts/specific/` | Specific. Swap prtask.com SSO redirects for inline-onclick stubs that explain the demo and keep users on /Login |
| `scripts/specific/patch-marketplace-photo.cs` | `scripts/specific/` | Specific. Add Listing.PhotoUrl + Seller, prefer `<img>` when set, overlay `📷 Photo by {Seller}` on SVG fallback |
| `scripts/specific/patch-theme-chip.cs` | `scripts/specific/` | Specific. Rewire ThemeChip to a pure-HTML cycle button + add dark/light CSS rules keyed off `[data-theme]` |
| `scripts/specific/patch-wolfs-call-js.cs` | `scripts/specific/` | Specific. Add `window.WolfsCall` to wolfs-interop.js so chat call button starts the existing WolfsChatVoice mic bridge and drops transcript into the input |
| `scripts/specific/patch-stage-padding.cs` | `scripts/specific/` | Specific. Increase top/bottom padding on `.Stage` and `.Hero` so video screenshot topic content lands closer to viewport center |
| `scripts/specific/patch-scripts-md-v2.cs` | `scripts/specific/` | Specific (one-off). Append this batch of patch scripts to SCRIPTS.md (idempotent) |
| `scripts/generic/proc-list.cs` | `scripts/generic/` | List recent dotnet/chrome/ffmpeg processes |
| `scripts/generic/kill-stale.cs` | `scripts/generic/` | Kill stale dotnet/chrome/ffmpeg processes |
| `scripts/generic/remove-orphans.cs` | `scripts/generic/` | Delete `docs/<folder>/` not matching a SharedUI route |
| `scripts/generic/rewrite-narrations.cs` | `scripts/generic/` | Bulk-rewrite specified scenes' narration text by index |
| `scripts/generic/expand-credentials.cs` | `scripts/generic/` | Replace scene 11's 3-credential write with full 10-credential set |
| `scripts/generic/run-lighthouse.cs` | `scripts/generic/` | Run Google Lighthouse against each `docs/<Page>/index.html` and report scores |
| `scripts/generic/rename-namespace.cs` | `scripts/generic/` | Strip `WolfsTruckingCo` prefix from every project, folder, csproj, namespace |
| `scripts/generic/build-blazor.cs` | `scripts/generic/` | Build + stage Blazor WASM into `wwwroot/app/` for local dev |
| `scripts/generic/migrate-classes.cs` | `scripts/generic/` | Bulk-rename legacy SharedUI class names to TopBar/Card/Btn/Stage/Stat |
| `scripts/generic/generate-statics.cs` | `scripts/generic/` | Prerender every SharedUI page to `docs/Generated/<Route>/index.html` (or `docs/<Route>/` with `--in-place`) |
| `scripts/generic/build-scss.cs` | `scripts/generic/` | Compile `scss/wolfs.scss` → `wwwroot/wolfs.css` (dart-sass) |
| `scripts/generic/compile-scss.cs` | `scripts/generic/` | Generic. Reads `Entry`/`Output`/`Style` consts from a scratch config and shells out to `sass.cmd` (dart-sass). Used to round-trip `src/SharedUI/scss/app.scss` → `src/SharedUI/wwwroot/css/app.css`. |
| `scripts/generic/build-razor-scss.cs` | `scripts/generic/` | Compile every `*.razor.scss` → `*.razor.css` for component-scoped styles |
| `scripts/generic/voice-sidecar.cs` | `scripts/generic/` | Local HTTP bridge: edge-tts TTS + Anthropic voice_stream STT |
| `docs/videos/serve-local.cs` | `docs/videos/` | Local HTTPS dev server on `:8443` (HTTP redirect on `:8080`) |
| `docs/videos/scenes.cs` | `docs/videos/` | Emit `scenes-final.json` — the 77 atomic real-user-CRUD scenes mirroring `workflow.md` |
| `docs/videos/run-crud-pipeline.cs` | `docs/videos/` | End-to-end pipeline: reset scene rows → per-scene CRUD via `WolfsInteropService.DbPutAsync` (permission-gated) → re-prerender real route page → drive chrome on `:9222` → screenshot → OCR via `Windows.Media.Ocr` → write `ocr.json` + `frame-references.md` |
| `docs/videos/verify-sso-render.cs` | `docs/videos/` | Specific. Single-scene smoke test for the SSO renderer path in `run-crud-pipeline.cs`. Exercises both the structural `["sso"]` field primary path and the narration regex fallback, inspects `LoginPage.razor` for SSO buttons / absence of email-password inputs, and (if chrome `:9222` is up) drives a navigation + `Runtime.evaluate` localStorage-populate cycle. |
| `scripts/generic/glob-files.cs` | `scripts/generic/` | Generic. Reads `Pattern`/`Root` from a config specific and prints relative paths of every file matching the glob under `Root` (recursive). Replaces ad-hoc `Glob`/`Get-ChildItem -Recurse`. |
| `scripts/specific/glob-files-example-config.cs` | `scripts/specific/` | Specific (example). Demonstrates `Pattern` + `Root` constants for `glob-files.cs`. |
| `scripts/generic/grep-content.cs` | `scripts/generic/` | Generic. Reads `Pattern`/`Root`/`FilePattern` from a config specific and prints `path:line:text` for every regex match across files matching `FilePattern` under `Root`. Replaces ad-hoc `Grep`/`Select-String`. |
| `scripts/specific/grep-content-example-config.cs` | `scripts/specific/` | Specific (example). Demonstrates `Pattern` + `Root` + `FilePattern` constants for `grep-content.cs`. |
| `scripts/generic/count-lines.cs` | `scripts/generic/` | Generic. Reads `FilePath`/`Root`/`Pattern` from a config specific and prints a line count for one file (`FilePath`) or every file matching `Pattern` under `Root`. |
| `scripts/specific/count-lines-example-config.cs` | `scripts/specific/` | Specific (example). Demonstrates `FilePath` + `Root` + `Pattern` constants for `count-lines.cs`. |
| `scripts/generic/dotnet-build.cs` | `scripts/generic/` | Generic. Reads `Target`/`Repo` from a config specific and runs `dotnet build` against `Target` (csproj or .cs) with `-nologo -v q`. Prints `OK` or one error per line. |
| `scripts/specific/dotnet-build-sharedui-config.cs` | `scripts/specific/` | Specific. Targets `src/SharedUI/SharedUI.csproj`; used to validate the shared UI project builds clean. |
| `scripts/generic/cdp-fix-google-uri.cs` | `scripts/generic/` | Generic. Reads `Needle`/`HeadingRegex`/`AddButtonRegex` from a config specific and drives Chrome via `chrome-devtools.cs` to repair a redirect-URI row on the matching tab. |
| `scripts/specific/cdp-fix-google-uri-config.cs` | `scripts/specific/` | Specific. Constants for `cdp-fix-google-uri.cs` — Google Cloud Console authorized-redirect-URI row. |
| `scripts/generic/cdp-snapshot-oauth.cs` | `scripts/generic/` | Generic. Reads `UrlNMatch` + `FilterX` constants from a config specific and snapshots OAuth admin tabs via `chrome-devtools.cs`, filtering DOM text to the configured needles. |
| `scripts/specific/cdp-snapshot-oauth-config.cs` | `scripts/specific/` | Specific. URL/filter constants for `cdp-snapshot-oauth.cs` covering Google, GitHub, Microsoft Entra, and Okta admin pages. |
| `scripts/generic/cdp-add-redirect-uri.cs` | `scripts/generic/` | Generic. Reads `PageNeedle`/`HeadingText`/`UriValue`/`SaveButtonText`/`AddButtonRegex` from a config specific and drives Chrome via `chrome-devtools.cs` to add a redirect URI on the matching admin page. |
| `scripts/specific/cdp-add-redirect-uri-microsoft-config.cs` | `scripts/specific/` | Specific. Constants for `cdp-add-redirect-uri.cs` — Microsoft Entra app registration redirect URI. |

---

## App lifecycle

### `wolfstruckingco.cs` — MCP server + launcher
```powershell
dotnet run wolfstruckingco.cs -- mcp      # run as MCP server over stdio (used by Claude Code)
dotnet run wolfstruckingco.cs -- launch   # open Claude Code terminal with MCP configured
```
Reads/writes `data/db.jsonl`. No external dependencies.

### `scripts/build-scss.cs` — SCSS compile
```powershell
dotnet run scripts/build-scss.cs              # one-shot
dotnet run scripts/build-scss.cs -- --watch   # rebuild on save
```
Requires `npm install -g sass` once. Output: `wwwroot/wolfs.css`.

### `scripts/build-blazor.cs` — local Blazor build
```powershell
dotnet run scripts/build-blazor.cs
```
Publishes `src/Client` and copies the WASM bundle into `wwwroot/app/`. Use for local dev. For GitHub Pages, use `publish-pages.cs` instead.

### `scripts/publish-pages.cs` — production publish
```powershell
dotnet run scripts/publish-pages.cs                                   # default → docs/app/, basePath /wolfstruckingco.com/app/
dotnet run scripts/publish-pages.cs -- --subdir app --basePath /wolfstruckingco.com/app/
dotnet run scripts/publish-pages.cs -- --repo C:\path\to\main         # alternate repo root
```
Idempotent. Removes prior `publish/` and `docs/app/_framework + _content`, re-mirrors, rewrites `<base href>` and writes `.nojekyll` + SPA `404.html`.

### `scripts/voice-sidecar.cs` — local TTS + STT bridge
```powershell
dotnet run scripts/voice-sidecar.cs
# POST http://localhost:5151/tts  {"text":"..."}      → audio/mpeg (edge-tts en-US-AriaNeural)
# POST http://localhost:5151/stt  <webm/ogg audio>    → {"text":"..."}
```
One-time secret setup (from any .NET project dir):
```powershell
dotnet user-secrets set "Voice:ClaudeOAuthAccessToken"  "<sk-ant-oat01-...>"
dotnet user-secrets set "Voice:ClaudeOAuthRefreshToken" "<sk-ant-ort01-...>"
```
Falls back to `%USERPROFILE%\.claude\.credentials.json` if user-secrets are unset.

---

## Local dev server

### `docs/videos/serve-local.cs` — HTTPS static server
```powershell
dotnet run docs/videos/serve-local.cs                                     # serves repo root on :8443
dotnet run docs/videos/serve-local.cs -- C:\repo\public\wolfstruckingco.com\main\wwwroot 8443 8080
```
First-time only:
```powershell
dotnet dev-certs https --trust
```
Maps `/wolfstruckingco.com/*` → root folder. SPA fallback rewrites unknown extensionless paths under `/wolfstruckingco.com/app/` to `index.html` (lets the Blazor router handle them).

---

## Walkthrough video pipeline (local-only, on-the-fly, no demo, no hardcode)

Seven generics chained by `build-walkthrough-local.cs`. Crawls the live deployed site, screenshots each route, generates per-scene narration via Claude Haiku from the actual page text, rotates across five anime AI TTS engines (English-only, ranked by GitHub stars), encodes per-scene clips, concatenates into `docs/videos/walkthrough.mp4`. Run locally only.

### TTS prerequisites — install all five engines locally before first run

The rotator dispatches scene `index % 5` to:

| Slot | Engine | Stars | Install |
|---|---|---|---|
| 0 | [Bark](https://github.com/suno-ai/bark) | ~36k | `pip install git+https://github.com/suno-ai/bark.git` |
| 1 | [GPT-SoVITS](https://github.com/RVC-Boss/GPT-SoVITS) | ~32k | `git clone … && pip install -r requirements.txt` (download English anime preset model) |
| 2 | [Coqui XTTS-v2](https://github.com/coqui-ai/TTS) | ~30k | `pip install TTS` (provide English anime reference WAV via `--speaker_wav`) |
| 3 | [OpenVoice](https://github.com/myshell-ai/OpenVoice) | ~30k | `git clone … && pip install -e .` |
| 4 | [Tortoise TTS](https://github.com/neonbjb/tortoise-tts) | ~14k | `pip install tortoise-tts` (uses built-in voice presets) |

Also install ffmpeg + ffprobe on PATH and have Chrome running with `--remote-debugging-port=9222` for CDP.

Set `ANTHROPIC_API_KEY` env var so `extract-narrations-claude.cs` can call your `/ai` worker endpoint with the `X-Anthropic-Key` header.

Edit `main/scripts/specific/tts-rotate-scratch-config.cs` so each `EngineNCmd` matches your local install paths and reference audio.

### `scripts/generic/build-walkthrough-local.cs` — single orchestrator
```powershell
dotnet run main/scripts/generic/build-walkthrough-local.cs main/scripts/specific/build-walkthrough-local-scratch-config.cs
```
Chains L1→L6 sequentially. Total runtime ~10–30 min depending on GPU and TTS engine speeds. Output: `docs/videos/walkthrough.mp4`.

### `scripts/generic/discover-live-routes.cs` — L1
Crawls `PAGES_ORIGIN` (set in scratch config). Follows internal `<a href>` links breadth-first up to `MaxDepth`. Outputs `docs/videos/live-routes.json`. No static `docs/` parse — true live state.

### `scripts/generic/capture-frames-readonly.cs` — L2
Reads `live-routes.json`, drives `chrome-devtools.cs` per scene: navigate → 2.5s hydration → screenshot. Frames at `%TEMP%\wolfs-walkthrough\frames\NNN.png`. Read-only — no DB writes, no R2 wipe.

### `scripts/generic/extract-narrations-claude.cs` — L3
For each route: GET HTML, strip tags, send innerText to `/ai` worker (Claude Haiku 4.5 via Cloudflare AI Gateway → Anthropic). Returns one descriptive sentence per page. Outputs `docs/videos/narrations.json`. Requires `ANTHROPIC_API_KEY` env.

### `scripts/generic/tts-rotate.cs` — L4
Reads `narrations.json`, rotates across 5 anime TTS engines by `index % 5`. Each engine invoked as configurable subprocess command template (`{text}` and `{out}` placeholders). Outputs `%TEMP%\wolfs-walkthrough\audio\NNN.wav` + `audio-index.json` mapping scene → engine.

### `scripts/generic/encode-clips.cs` — L5
Reads `audio-index.json`, runs ffmpeg per scene to combine `frames/NNN.png` + `audio/NNN.wav` → `clips/NNN.mp4` at 1920×1080 H.264 + AAC 192k. Holds image for audio duration via `-shortest`. Skips scenes whose TTS failed.

### `scripts/generic/concat-walkthrough.cs` — L6
Builds ffmpeg concat list from `clips/*.mp4`, runs `ffmpeg -f concat -c copy` (stream-copy, no re-encode), writes `docs/videos/walkthrough.mp4`. Prints final size + duration via ffprobe.

### `scripts/generic/delete-files.cs` — utility
Reads scratch config with `Path1`, `Path2`, … `PathN` consts; deletes each if it exists. Used by L8 to remove legacy pipeline files.
```powershell
dotnet run docs/videos/build-video.cs -- --scenes scenes-full.json --out walkthrough.mp4
dotnet run docs/videos/build-video.cs -- --scenes scenes-full.json --out walkthrough.mp4 --voice en-US-AriaNeural --rate +0%
dotnet run docs/videos/build-video.cs -- --scenes scenes-full.json --out walkthrough.mp4 --cli C:\repo\public\chrome-devtools-cli\chrome-devtools.cs
```
Requires `edge-tts` and `ffmpeg`/`ffprobe` on `PATH`. Drives Chrome via `chrome-devtools-cli`. Per-scene flow: navigate/evaluate → wait → screenshot → edge-tts narrate → ffmpeg encode 1920×1080 H.264. Final concat copies clips into `walkthrough.mp4`.

After a `navigate` whose URL contains `/app/`, the script polls until Blazor hydrates `#app` (Loading… disappears) before continuing — avoids capturing the loading skeleton.

### `docs/videos/generate-transcript.cs` — write transcript.md
```powershell
dotnet run docs/videos/generate-transcript.cs -- --scenes scenes-full.json --frames frames --out transcript.md --video walkthrough.mp4
```
One markdown row per scene with the captured frame, narration, and a `- [ ] Image matches narration` checkbox for QA.

### `docs/videos/build-status.cs` — quick status
```powershell
dotnet run docs/videos/build-status.cs                       # default dir = docs/videos
dotnet run docs/videos/build-status.cs -- C:\path\to\videos  # alternate dir
```
Prints `walkthrough.mp4` size + last write, then tails `build-video.log`. Use to check progress without piping shell commands.

### `docs/videos/frame-check.cs` — frame QA
```powershell
dotnet run docs/videos/frame-check.cs
```
Scans `%TEMP%\wolfs-video\{frames,clips,audio}` and prints a per-scene table with sizes plus a duplicate-frame warning when consecutive PNGs share a SHA1 hash (a sign that Blazor wasn't ready when the screenshot fired).

### `docs/videos/match-check.cs` — narration vs image text QA
```powershell
dotnet run docs/videos/match-check.cs
dotnet run docs/videos/match-check.cs -- --scenes scenes-full.json --frames C:\path\to\frames
```
For each scene, compares the narration against the page's `document.body.innerText` captured by `build-video.cs` at screenshot time (`frames/NNN.txt`). Reports a content-word overlap percentage per scene; `⚠ very weak` flags a frame that almost certainly doesn't match what the narrator says.

### `docs/videos/extract-frame-text-compare.cs` — local OCR vs narration markdown

```bash
dotnet run docs/videos/extract-frame-text-compare.cs -- --scenes docs/videos/scenes-final.json --frames %TEMP%\wolfs-video\frames
```

Runs Windows OCR locally on `NNN.png` frames, compares extracted text with each scene narration, and writes `docs/videos/ocr-narration-check.md`.

### `docs/videos/video-duration.cs` — duration probe
```powershell
dotnet run docs/videos/video-duration.cs                            # defaults to ./walkthrough.mp4
dotnet run docs/videos/video-duration.cs -- C:\path\to\video.mp4
```
Calls ffprobe and prints duration in mm:ss plus a flag if the runtime is outside the 7:00–8:00 target window.

### `docs/videos/trim-duration.cs` — speed-to-fit
```powershell
dotnet run docs/videos/trim-duration.cs                             # defaults to 1.085× speedup
dotnet run docs/videos/trim-duration.cs -- --speed 1.10
dotnet run docs/videos/trim-duration.cs -- --in walkthrough.mp4 --out walkthrough.mp4 --speed 1.10
```
Runs ffmpeg `setpts` + `atempo` to speed video and audio together. Lets the existing build land in the 7:00–8:00 window without a full rebuild. Pitch is preserved up to ~1.1×.

### `docs/videos/inspect-frames.cs` — text-capture debug
```powershell
dotnet run docs/videos/inspect-frames.cs
```
Prints the first six `frames/NNN.txt` files captured by `build-video.cs` so you can confirm the body-text capture is firing (zero-byte files mean the JS wrapper is wrong and `match-check.cs` will report 0% across the board).

---

## Generic + specific scratch helpers (Read/Edit/WebSearch replacements)

Per memory rule `feedback_replace_update_read.md`, harness Read/Edit/WebSearch are forbidden on code files — use these `dotnet run <generic>.cs <specific>-config.cs` pairs instead. The scratch configs are mutated via Write before each invocation.

| Script | Folder | Purpose |
|--------|--------|---------|
| `scripts/generic/patch-source.cs` | `scripts/generic/` | Generic. Reads TargetFile + Find_NN/Replace_NN const strings from the specific scratch config, applies each Find→Replace pair to TargetFile (terminated by `___UNUSED_SLOT___` or empty Find). Skips already-applied pairs. |
| `scripts/specific/patch-source-scratch-config.cs` | `scripts/specific/` | Specific. The current TargetFile + Find_NN/Replace_NN slots that patch-source operates on. Re-write before each invocation. |
| `scripts/generic/web-search.cs` | `scripts/generic/` | Generic. Reads Query + MaxResults from specific scratch config; runs an Anthropic-equivalent web search and prints the top N results. |
| `scripts/specific/web-search-scratch-config.cs` | `scripts/specific/` | Specific. Query string + MaxResults for web-search.cs. |
| `scripts/generic/dump-file.cs` | `scripts/generic/` | Generic. Reads Path + optional line/byte/regex range from a specific config and prints the slice. |
| `scripts/specific/dump-file-scratch-config.cs` | `scripts/specific/` | Specific. Path + range for dump-file.cs. |
| `scripts/specific/glob-files-scratch-config.cs` | `scripts/specific/` | Specific. Pattern/Root for glob-files.cs. |
| `scripts/specific/grep-content-scratch-config.cs` | `scripts/specific/` | Specific. Pattern/Root/Glob for grep-content.cs. |
| `scripts/specific/set-config.cs` | `scripts/specific/` | Generic. Mutate any const string in a specific config file by name. |
| `scripts/specific/set-config-scratch-config.cs` | `scripts/specific/` | Specific. ConfigFile + ConstName + ConstValue for set-config.cs. |
| `scripts/generic/delete-lines.cs` | `scripts/generic/` | Generic. Reads TargetFile + StartLine + EndLine from a specific config and removes that inclusive 1-indexed line range from the target — used for bulk removal of dead code blocks where patch-source's full-text Find/Replace would be impractically large. |
| `scripts/specific/delete-lines-scratch-config.cs` | `scripts/specific/` | Specific. TargetFile + StartLine + EndLine for delete-lines.cs. |

## Video-pipeline support helpers (Chrome 144+ approval-mode)

| Script | Folder | Purpose |
|--------|--------|---------|
| `scripts/generic/cdp-allow.cs` | `scripts/generic/` | Wrapper invoking `chrome-devtools.cs -- allow` (UIA Allow click + infobar dismiss). 4-token-pattern compliant. |
| `scripts/generic/click-at.cs` | `scripts/generic/` | Generic. Sends Win32 SendInput LEFTDOWN/UP at (X,Y) read from specific config. Pixel-fallback for UIA-unreachable buttons. |
| `scripts/specific/click-at-scratch-config.cs` | `scripts/specific/` | Specific. X,Y pixel coords for click-at.cs. |
| `scripts/generic/add-grid.cs` | `scripts/generic/` | Generic. Overlays magenta grid lines + coordinate labels on a PNG (System.Drawing). Used to debug Allow-popup screenshots. |
| `scripts/specific/add-grid-scratch-config.cs` | `scripts/specific/` | Specific. SourcePath + DestPath + GridStep for add-grid.cs. |
| `scripts/generic/run-with-watchdog.cs` | `scripts/generic/` | Generic. Spawns a target .cs subprocess; line-tails stdout/stderr; on regex match (errors/warnings) or stall (>StallSec without output), captures desktop screenshot + grid overlay + browser console; kills process tree. |
| `scripts/specific/run-with-watchdog-pipeline-config.cs` | `scripts/specific/` | Specific. TargetCs (run-crud-pipeline.cs) + ErrorPattern + StallSec for run-with-watchdog.cs. |
| `scripts/generic/dump-uia-buttons.cs` | `scripts/generic/` | Diagnostic. Lists all buttons within Y<250 of every Chrome window via UI Automation — used to identify infobar X-button selectors. |
| `docs/videos/pipeline-cdp.cs` | `docs/videos/` | Extracted CdpClient class (single-click Allow approval, Target.attachToTarget flatten:true, send/event multiplexing). Included from run-crud-pipeline.cs via `#:include`. |

## Adding a new script

1. Create `scripts/<name>.cs` (or wherever it logically belongs).
2. Header:
   ```csharp
   #:property TargetFramework=net11.0
   #:property PublishAot=false
   ```
3. Add a 1-line docstring at the top with the exact `dotnet run` invocation.
4. Append a row to the table in this file.
5. Invoke with `dotnet run scripts/<name>.cs -- <args>` from the **PowerShell tool**.

Do **not** author `.ps1` scripts. Do **not** wrap with bash. Do **not** call inline pipelines like `Get-Content | Measure-Object` — write a `*.cs` file instead.
