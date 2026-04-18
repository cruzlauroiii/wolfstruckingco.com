# `dotnet run *.cs` script reference

Every scripted/repeatable operation in this repo is a **.NET 11 file-based program** invoked with `dotnet run <file>.cs`. No PowerShell scripts, no bash one-liners — single language (C#) for app code and tooling.

> Invoke from the **PowerShell tool** only. Pass arguments after `--`. Example: `dotnet run scripts/publish-pages.cs -- --basePath /wolfstruckingco.com/app/`.

| Script | Folder | Purpose |
|--------|--------|---------|
| `wolfstruckingco.cs` | repo root | MCP server + launcher for Claude Code |
| `scripts/publish-pages.cs` | `scripts/` | Publish Blazor WASM client to `docs/app/` for GitHub Pages |
| `scripts/build-all.cs` | `scripts/` | Single-entry orchestrator: SCSS + WASM publish + static HTML in one shot |
| `scripts/lint-strict.cs` | `scripts/` | Strict linter — flags magic numbers, magic strings, non-PascalCase, domain words in `$"…"` |
| `scripts/rename-namespace.cs` | `scripts/` | Strip the WolfsTruckingCo prefix from every project, folder, csproj, namespace |
| `scripts/fix-datetime-kind.cs` | `scripts/` | Append `, DateTimeKind.Local` to every `new DateTime(...)` (Sonar S6562) |
| `scripts/tail-file.cs` | `scripts/` | Print last N lines of a file (replaces `tail -N path`) |
| `scripts/proc-list.cs` | `scripts/` | List recent dotnet/chrome/ffmpeg processes |
| `scripts/kill-stale.cs` | `scripts/` | Kill stale dotnet/chrome/ffmpeg processes |
| `scripts/remove-orphans.cs` | `scripts/` | Delete `docs/<folder>/` not matching a SharedUI route |
| `scripts/rewrite-narrations.cs` | `scripts/` | Bulk-rewrite specified scenes' narration text by index |
| `scripts/expand-credentials.cs` | `scripts/` | Replace scene 11's 3-credential write with full 10-credential set |
| `scripts/run-lighthouse.cs` | `scripts/` | Run Google Lighthouse against each `docs/<Page>/index.html` and report scores |
| `scripts/rename-namespace.cs` | `scripts/` | Strip `WolfsTruckingCo` prefix from every project, folder, csproj, namespace |
| `scripts/build-blazor.cs` | `scripts/` | Build + stage Blazor WASM into `wwwroot/app/` for local dev |
| `scripts/migrate-classes.cs` | `scripts/` | Bulk-rename legacy SharedUI class names to TopBar/Card/Btn/Stage/Stat |
| `scripts/generate-statics.cs` | `scripts/` | Prerender every SharedUI page to `docs/Generated/<Route>/index.html` (or `docs/<Route>/` with `--in-place`) |
| `scripts/build-scss.cs` | `scripts/` | Compile `scss/wolfs.scss` → `wwwroot/wolfs.css` (dart-sass) |
| `scripts/build-razor-scss.cs` | `scripts/` | Compile every `*.razor.scss` → `*.razor.css` for component-scoped styles |
| `scripts/voice-sidecar.cs` | `scripts/` | Local HTTP bridge: edge-tts TTS + Anthropic voice_stream STT |
| `docs/videos/serve-local.cs` | `docs/videos/` | Local HTTPS dev server on `:8443` (HTTP redirect on `:8080`) |
| `docs/videos/scenes.cs` | `docs/videos/` | Emit `scenes-final.json` — the 77 atomic real-user-CRUD scenes mirroring `workflow.md` |
| `docs/videos/run-crud-pipeline.cs` | `docs/videos/` | End-to-end pipeline: reset scene rows → per-scene CRUD via `WolfsInteropService.DbPutAsync` (permission-gated) → re-prerender real route page → drive chrome on `:9222` → screenshot → OCR via `Windows.Media.Ocr` → write `ocr.json` + `frame-references.md` |

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

## Walkthrough video pipeline

Run the four video scripts from inside `docs/videos/` so the relative paths resolve.

### `docs/videos/build-video.cs` — render narrated MP4
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
