# wolfstruckingco worker

Cloudflare Worker + R2-backed REST API that powers wolfstruckingco.com's Hiring Hall, Career Agent, Employer view, and AI proxy. Deployed to:

- **Live URL:** https://wolfstruckingco.nbth.workers.dev
- **R2 bucket:** `wolfstruckingco-relay` (binding: `env.R2`)

## Endpoints

| Path | Method | Notes |
|------|--------|-------|
| `/health` | GET | liveness probe |
| `/api/{collection}` | GET | list (open read) |
| `/api/{collection}/{id}` | GET | get one |
| `/api/{collection}` | POST | create — requires `X-Wolfs-Session` header |
| `/api/{collection}/{id}` | PUT | update — requires session |
| `/api/{collection}/{id}` | DELETE | delete — requires session |
| `/ai` | POST | Claude proxy (Opus 4.7 default) |
| `/send`, `/poll`, `/reply`, `/status` | — | legacy message relay |

### Permissions

Writes require the `X-Wolfs-Session` header. Admin-only collections (`badges`, `roles`, `customers`, `audit`) also require `X-Wolfs-Role: admin`. Reads are currently public.

### `/ai` routing

Every AI call hits Anthropic's Messages API via **Cloudflare AI Gateway** at:
```
https://gateway.ai.cloudflare.com/v1/<account>/wolfs/anthropic/v1/messages
```
Direct `api.anthropic.com` is blocked from Cloudflare Workers by Anthropic's bot protection — AI Gateway is the whitelisted path. The Anthropic key still does auth at the origin; the gateway just routes.

### Key resolution (in order)

1. `X-Anthropic-Key` request header — BYO key per request (browser UI)
2. `env.ANTHROPIC_API_KEY` — **Cloudflare Secrets Store** binding declared in `wrangler.toml`:
   ```toml
   [[secrets_store_secrets]]
   binding = "ANTHROPIC_API_KEY"
   store_id = "712018f9c5a94cdd8db341e70442e6ea"
   secret_name = "ANTHROPIC_API_KEY"
   ```
   Secrets Store bindings return a `SecretValue` object, not a string — the code does `await env.ANTHROPIC_API_KEY.get()` to unwrap.
3. If neither: `503 ANTHROPIC_API_KEY not configured`

## Deploy

```bash
cd main/worker
# Env vars come from your Cloudflare account
export CLOUDFLARE_EMAIL=you@example.com
export CLOUDFLARE_API_KEY=...              # Global API key
export CLOUDFLARE_ACCOUNT_ID=...
npx -y wrangler@4 deploy --name wolfstruckingco
```

No secrets are committed to the repo. Full setup from scratch:

```bash
# 1. Create the Cloudflare Secrets Store (one per account, so this may already exist)
npx wrangler@4 secrets-store store create default --remote

# 2. Store the Anthropic API key in the vault (prompts for the value)
npx wrangler@4 secrets-store secret create <store-id> \
  --name ANTHROPIC_API_KEY --scopes workers --remote

# 3. Create the AI Gateway
curl -X POST "https://api.cloudflare.com/client/v4/accounts/<account-id>/ai-gateway/gateways" \
  -H "X-Auth-Email: you@example.com" -H "X-Auth-Key: <global-api-key>" \
  -H "Content-Type: application/json" \
  -d '{"id":"wolfs","authentication":false,"collect_logs":true,"cache_ttl":0}'

# 4. Reference the secret in wrangler.toml + deploy
npx wrangler@4 deploy --name wolfstruckingco
```

## R2 layout

- `db/{collection}.json` — per-collection JSON arrays (workers, badges, roles, customers, schedules, timesheets, chatSessions, agentProfiles, jobs, users, invoices, audit, …)
- `inbox/{ts}_{rnd}` and `outbox/{ts}_{rnd}` — legacy message relay queue
