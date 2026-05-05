# Wolfs Trucking — 4-Driver International COD Workflow (121 atomic real-user-CRUD scenes, SSO-only auth)

## Architecture rules

- **No seeding, no demo, no relative captions.** All scene rows are reset at pipeline start via `dotnet run scripts/wipe-db.cs scripts/wipe-db-config.cs` (calls worker `/api-wipe`, returns `wiped: 20` collections to `[]`). The platform's stores (`applicants`, `listings`, `schedules`, `timesheets`, `charges`, `audit`) grow only by real CRUD actions performed during the workflow.
- **SSO-only auth.** SignUp page removed (#209). Worker `/api/signup` returns 404. Login page renders only the 4 SSO provider buttons (Google / GitHub / Microsoft / Okta). First-time SSO login auto-registers via the worker's OAuth callback (worker.cs:317-353 issues a session unconditionally on successful OAuth without consulting `users` collection). Each IdP also auto-handles first-time on its side.
- **Every scene is an actual real route page** — `/Login/`, `/Applicant/`, `/Interview/`, `/Documents/`, `/HiringHall/`, `/Marketplace/`, `/Employer/Post/`, `/Schedule/`, `/Map/`, `/Itinerary/`, `/Track/`, `/Investors/KPI/`, `/Dispatcher/`. No `/SignUp/`. No fake pages.
- **Every scene is a real user CRUD action.** Observational/system narrations excluded.
- **Per-scene CRUD with permissions:** `WolfsInteropService.DbPutAsync` enforced through `StubJsRuntime.Stub` permission gate. Each actor's role determines which permissions they can write. Dispatcher has full cross-store rights to act on behalf of any user.
- **Pages are DB-driven.** Each page reads its store via `WolfsInterop.dbAllJson(store)` and renders newest-first by id.
- **Working light-theme map with turn-by-turn voice nav** — `MapPage` renders an inline SVG light map (gradient background, light-blue water, gray road network, dashed orange route, green/orange pins, blue 🚚 driver position) plus per-step turn-by-turn cards with 🔊 voice transcript.
- **JS interop via C# code-behind.** Most browser interop in C# via `IJSRuntime` / `WolfsInteropService`. Migration from inline-JS-in-C# to proper Razor `[JSImport]` patterns is documented in `JS_TO_RAZOR_PLAN.md` (#207); WebRTC + IndexedDB + Leaflet stay JS by necessity.
- **Title in TopBar.** `MainLayout` brand renders `Wolfs · {Page Title}` from `WolfsRenderContext.CurrentRoute`; per-page H1 hidden via inline CSS.
- Mobile viewport 414×896, dsf=2, mobile=true. OCR text matches the narration of each scene.
- Pipeline: `dotnet run docs/videos/run-crud-pipeline.cs`. Video: `dotnet run docs/videos/build-video.cs`.

## Phases

### Phase 1 — SSO Login (7)
Each persona has exactly ONE Login scene. First-time SSO logins auto-register; no SignUp page, no email/password form. Per-persona provider+account bindings (#218):

| # | Persona | Provider | Account |
|---|---------|----------|---------|
| 1 | Car seller | Google | cruzlauroiii@gmail.com |
| 2 | Car buyer | Microsoft | cruzlauroiii@gmail.com |
| 3 | Admin | GitHub | (only signed-in account) |
| 4 | Driver from China | Okta | (only signed-in account) |
| 5 | Driver from LA | Google | noahblesse@gmail.com |
| 6 | Team driver Phoenix | Microsoft | noahblesse@gmail.com |
| 7 | Driver in Wilmington | Google | analynrcastillo@gmail.com |

Scene shape: `{"action":"navigate","target":".../Login/?cb=NNN","narration":"X signs in with <Provider>.","sso":"<provider>","account":"<email>"}`. Renderer (run-crud-pipeline.cs) reads `sso`, skips email/password fill, populates localhost localStorage via `Runtime.evaluate` to mimic the post-OAuth state (real OAuth callback redirects to github.io, not localhost — workaround documented at worker.js:351). Audit kind: `auth.sso.<provider>`.

### Phase 2 — Driver onboarding · 4 drivers × applicant + interview + documents (32)
17–24. `/Applicant/` `/Interview/` `/Documents/` — Driver 1 intake (name + years, location, payout-rail), DOT compliance, HOS, dispatch protocol, CDL upload, China export cert.
25–32. Driver 2 intake (San Pedro, RTP/Wells Fargo), TWIC + drayage Q&A + uploads.
33–40. Driver 3 team (Phoenix, Visa Direct shared), team-driver HOS + cross-country routing + interstate authority + team cert + both CDLs.
41–48. Driver 4 (Wilmington, Chase RTP), auto-handling + night-cutoff Q&A + interstate + auto-handling cert + vehicle inspection + insurance.

### Phase 3 — Admin hiring (8)
49–56. `/HiringHall/` — Admin reviews + assigns badges per driver, then approves all four.

### Phase 4 — Marketplace + employer setup (10)
57–66. `/Marketplace/` `/Employer/Post/` — Wei posts BYD Han EV listing (title, photos, price, COD, country, HTSUS, pickup/drop, ACH listing fee), configures multi-leg job (pay-per-leg, badges, payout rail per leg), publishes.

### Phase 5 — Buyer purchase + schedule (10)
67–76. `/Marketplace/` `/Schedule/` — Sam browses, opens detail, selects COD, pre-authorizes RTP, clicks Buy. Schedule legs 1–4 + ocean leg.

### Phase 6 — Driver 1 leg China to Yangshan (10)
77–86. `/Map/` `/Itinerary/` — Voice nav D1 turn-by-turn, factory pay $18,000 SWIFT T/T, vehicle inspection, GPS install, drive 480 mi, container load, ts_d1 closes $320.

### Phase 7 — Ocean transit + customs (8)
87–94. `/Schedule/` `/Track/` `/Investors/KPI/` — ocean carrier 6500 mi over 37 days, GPS milestones, customs hold released, ISF + ocean freight + factoring + Section 301 tariff.

### Phase 8 — Driver 2 LA leg (8)
95–102. `/Map/` `/Itinerary/` — Voice nav D2 turn-by-turn to Port of LA terminal 401, container pickup, drive to Phoenix, ts_d2 closes $420.

### Phase 9 — Realtime delay + recompute (4)
103–106. `/Track/` `/Schedule/` `/Itinerary/` — I-10 East mile 78 Boyle Heights flip 47-min stoppage, audit recompute fires, downstream legs re-timed, D2 instant payout via RTP.

### Phase 10 — Driver 3 cross-country (8)
107–114. `/Map/` `/Itinerary/` — Voice nav D3 I-10/I-20/I-30/I-40 corridor, team-driver split sleeper berth, fuel stop Pilot, Memphis 24/7 yard, ts_d3 closes $1,180, RTP team-account payout.

### Phase 11 — Driver 4 final mile (8)
115–122. `/Map/` `/Itinerary/` — Voice nav D4 I-40/I-26/I-95/US-117 to Wilmington, buyer cutoff confirmation, navigate Oak Street, arrive 21:30, parks, brings keys + factory documents to door.

### Phase 12 — Delivery + COD (8)
123–130. `/Itinerary/` — buyer inspection (VIN, factory seal, no damage), COD $48,500 confirmed, RTP push at door, delivered-plus-photo event, escrow released, codCollected = 48500, job settles, ts_d4 closes $600.

### Phase 13 — Settlement + KPIs (16)
131–146. `/Investors/KPI/` `/Marketplace/` `/Itinerary/` — driver payouts D1 SWIFT $320, D2 RTP $420, D3 RTP $1,180, D4 RTP $600 (total $2,520), reimburse D1 $18,000 factory advance, HMF $60.63, MPF $614.35, tariff base 25% $1,212.50, debt cleared from COD, net platform revenue $46,064.98, drivers earnings paid, completion stats, listing closed, purchase delivered.

### Phase 14 — Dispatcher control on behalf (8)
147–153. `/Dispatcher/` — Dispatcher dispatches D1 to Hefei plant + GPS confirm + dispatches D2 to Port of LA + reroutes D2 around I-10 flip + dispatches D3 cross-country + reroutes D3 via I-40 east + confirms D4 final-mile delivery + closes pur_byd job. All `schedule.recompute` / `nav.read` / `track.read` / `itinerary.write` / `purchase.write` / `listing.write` permissions granted to dispatcher role.

---

## Payment rails summary

- **RTP / FedNow** — buyer COD ($48,500), all US driver payouts (D2/D3/D4) — instant, 24/7, irrevocable, no card fees.
- **SWIFT wire (T/T)** — Driver 1 → factory ($18,000), platform → Driver 1 payout + reimbursement — China cross-border standard.
- **ACH / wire** — platform listing fees, ocean freight, ISF, factoring, HMF, MPF; **CBP ACE ACH** for tariffs — cheap B2B scheduled.

## Realtime recompute rule

Every leg's `startsAt`/`endsAt` is regenerated whenever upstream status changes, evaluated against:
1. GPS position
2. Live traffic
3. Destination yard/buyer working hours
4. FMCSA HOS remaining for the assigned driver

`recomputed:true` + `recomputeReason` written to the schedule; `audit.schedule.recompute` row appended.

## Dispatcher

The `dispatcher@wolfstruckingco.com` role has full cross-store permission grant in `StubJsRuntime.PermissionAllowed` — `schedule.*`, `nav.*`, `track.*`, `applicant.*`, `listing.*`, `purchase.*`, `itinerary.*`, `auth.*`, `kpi.*`, `audit.*`, `interview.*`, `documents.*`. Dispatcher actions are recorded in `audit` with `kind=dispatcher.action` + `onBehalfOf=<user-email>` so every operator override is auditable.
