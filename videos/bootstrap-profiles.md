# Wolfs walkthrough — one-time persona profile bootstrap

The real-OAuth pipeline (`real-render.cs` / `run-real-pipeline.cs`) drives a real signed-in Chrome session per persona. Chrome 136+ blocks remote debugging on the default profile, so each persona gets a dedicated `--user-data-dir` directory pre-authenticated to its identity provider.

Do this **once** per machine before the first pipeline run. Re-do it only if you wipe the profile directory or the IdP cookie expires.

## Personas

From `workflow.md` Phase 1:

| # | Persona | Provider | Account | Profile dir |
|---|---------|----------|---------|-------------|
| 1 | Car seller | Google | cruzlauroiii@gmail.com | `C:\chrome-profiles\car-seller-google` |
| 2 | Car buyer | Microsoft | cruzlauroiii@gmail.com | `C:\chrome-profiles\car-buyer-microsoft` |
| 3 | Admin | GitHub | (your GitHub admin account) | `C:\chrome-profiles\admin-github` |
| 4 | Driver from China | Okta | (your Okta dev account) | `C:\chrome-profiles\driver-china-okta` |
| 5 | Driver from LA | Google | noahblesse@gmail.com | `C:\chrome-profiles\driver-la-google` |
| 6 | Team driver Phoenix | Microsoft | noahblesse@gmail.com | `C:\chrome-profiles\driver-phoenix-microsoft` |
| 7 | Driver in Wilmington | Google | analynrcastillo@gmail.com | `C:\chrome-profiles\driver-wilmington-google` |

## Per-persona setup

For each row in the table above, in PowerShell:

```powershell
& "C:\Program Files\Google\Chrome\Application\chrome.exe" --user-data-dir="C:\chrome-profiles\car-seller-google" --no-first-run --no-default-browser-check
```

This launches a brand-new isolated Chrome window. In that window:

1. Navigate to `https://cruzlauroiii.github.io/wolfstruckingco.com/Login/`
2. Click the persona's SSO button (Google for row 1, Microsoft for row 2, etc.)
3. Complete the IdP sign-in — enter password, approve any 2FA, complete consent screen.
4. Wait for the OAuth callback to land back on the home page (`?wsso=<provider>&email=...&session=...`)
5. Confirm the top-right header shows `Log off (<email>)` — that's the proof the session cookie + localStorage are set.
6. Close the Chrome window. The profile directory now retains the auth state.

Repeat for each of the 7 personas, swapping the `--user-data-dir` path and the provider button each time.

## Verifying a profile is ready

```powershell
& "C:\Program Files\Google\Chrome\Application\chrome.exe" --user-data-dir="C:\chrome-profiles\car-seller-google" --remote-debugging-port=9222 --no-first-run https://cruzlauroiii.github.io/wolfstruckingco.com/Login/
```

If you see `Log off (cruzlauroiii)` in the header without re-prompting for a password, the profile is bootstrapped. Close the window after verifying.

## During pipeline runs

`run-real-pipeline.cs` will launch Chrome with the right `--user-data-dir` per scene. If a session has expired (cookies cleared, IdP forced re-auth, 2FA challenge), the renderer detects the redirect to an IdP domain and exits **42** (HUMAN_NEEDED). The orchestrator then:

1. Sounds a laptop alarm via `request-human.cs` (Console.Beep + system-tray balloon)
2. Fires a Claude Code mobile push from the agent (your phone pings)
3. Blocks until you re-sign-in in the open Chrome window AND drop `ok` into `C:\Users\user1\AppData\Local\Temp\wolfs-alarm-ack.txt`

Once the ack lands, the pipeline retries the scene from where it stopped.

## Why 7 separate profiles

Chrome can only be signed in to one Google account at a time per profile (without using the profile picker). Two Google personas (`cruzlauroiii@gmail.com` as car seller + `noahblesse@gmail.com` as LA driver + `analynrcastillo@gmail.com` as Wilmington driver) cannot share a single profile. Microsoft personas are similar — even though `cruzlauroiii@gmail.com` is the same account for car-seller-Google and car-buyer-Microsoft, the persona stores different role/wsso state, so they get different profiles.

## Disk usage

Each Chrome profile is ~80–200 MB after one sign-in. Total ~1–2 GB across 7 profiles. Stored at `C:\chrome-profiles\*` — safe to delete and re-bootstrap any time.
