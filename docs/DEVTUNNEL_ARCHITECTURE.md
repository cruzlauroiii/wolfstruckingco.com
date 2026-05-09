# DevTunnel WSS Pub/Sub Execution Architecture

## Goal

Make every `dotnet run main/scripts/generic/<verb>.cs main/scripts/specific/<verb>-config.cs` invocation publish a command over Microsoft DevTunnel WSS to one or more named clients (`tester`, `developer`, ...) that actually execute the work and stream results back. Orchestrator (this Claude session) becomes a coordinator; clients become workers.

## Why

- Decouple the harness (this session) from execution (which can be browser control on a different machine, builds on a Linux box, captures on a Windows GPU host, etc.).
- One-to-many fan-out: a single orchestrator can publish to multiple subscribers on the same topic for parallel execution.
- DevTunnel handles NAT traversal, TLS, and reconnection — no inbound firewall rules needed.
- Azure PAT authenticates tunnel ownership; clients authenticate separately via tunnel access token.

## Topology

```
[ orchestrator (Claude session) ]
         |  publishes commands
         v
[ DevTunnel WSS endpoint ]   <--- single persistent tunnel, owned by Azure PAT user
         ^
         |  many clients subscribe by topic
         |
  +------+------+------+
  |      |      |      |
[tester][developer][gpu-host][ci-runner]
```

## Topics

- `tester` — owns Chrome+CDP. Receives `chrome-devtools.cs` commands (navigate, snapshot, click, screenshot, etc.).
- `developer` — owns the source tree. Receives `patch-source`, `write-file`, `git-run`, etc.
- (future) `gpu-host` — TTS synthesis, ffmpeg encode, OCR.
- (future) `ci-runner` — verify-page-errors against the live site, lint, test.

A single client can subscribe to multiple topics. A topic can have multiple subscribers (parallel execution / hot-spare).

## Wire protocol (JSON over WSS)

Each message is a single JSON object. Direction = orchestrator → client unless noted.

### `cmd` (orchestrator → topic)
```
{ "type": "cmd", "id": "<uuid>", "topic": "tester", "action": "dotnet_run",
  "args": {
    "generic": "main/scripts/generic/chrome-devtools.cs",
    "config":  "main/scripts/specific/chrome-devtools-serve-config.cs",
    "workdir": "C:/repo/public/wolfstruckingco.com"
  } }
```

### `ack` (client → orchestrator)
```
{ "type": "ack", "id": "<uuid>", "client": "tester-1", "received": "2026-05-10T12:34:56Z" }
```

### `log` (client → orchestrator, streamed during execution)
```
{ "type": "log", "id": "<uuid>", "client": "tester-1", "stream": "stdout", "line": "..." }
```

### `result` (client → orchestrator on completion)
```
{ "type": "result", "id": "<uuid>", "client": "tester-1",
  "exit_code": 0, "duration_ms": 1234, "stdout": "...", "stderr": "..." }
```

### `error` (any direction)
```
{ "type": "error", "id": "<uuid>", "message": "..." }
```

## Components to build

### 1. `scripts/generic/tunnel-init.cs` (one-shot)
- Reads Azure PAT from user-secrets (`Azure:Pat`).
- Calls `devtunnel create wolfs-execution --allow-anonymous false`.
- Calls `devtunnel port create wolfs-execution --port 4444 --protocol https`.
- Stores tunnel ID + access token in user-secrets (`DevTunnel:Id`, `DevTunnel:AccessToken`).

### 2. `scripts/generic/tunnel-broker.cs` (long-running, started once)
- Connects to the tunnel WSS endpoint as the broker.
- Maintains an in-memory map of `topic → [client-id]`.
- Routes `cmd` messages from orchestrator to subscribed clients.
- Routes `ack`/`log`/`result` back to the originating orchestrator session.

### 3. `scripts/generic/tunnel-client.cs` (long-running, runs on each worker machine)
- Reads `Topics` (CSV) and `ClientName` from specific config.
- Connects WSS, sends a `subscribe` message for each topic.
- For each `cmd` received: executes it (e.g., `Process.Start("dotnet", "run", generic, config)`), streams `log` lines, sends `result` on exit.
- Reconnect with exponential backoff on disconnect.

### 4. `scripts/generic/pub-exec.cs` (replaces direct `dotnet run` invocations)
- Reads the same generic+config args you'd pass to `dotnet run` directly.
- Publishes a `cmd` message tagged with `topic` (default = `developer`, override via specific config).
- Streams `log` lines to local stdout/stderr.
- Exits with the remote `result.exit_code`.
- The orchestrator's harness keeps the strict 4-token shape: `dotnet run main/scripts/generic/pub-exec.cs main/scripts/specific/<verb>-pub-config.cs` where the specific config encodes both the topic AND the original generic+config it wants executed.

### 5. Per-client launcher PowerShell preview windows
- Each client process is started in its own PowerShell preview pane: `dotnet run main/scripts/generic/tunnel-client.cs main/scripts/specific/tunnel-client-tester-config.cs`.
- Logs visible in the pane; killing the pane stops the client.
- One pane per topic = one worker per topic.

## Auth flow

1. User runs `tunnel-init.cs` once. Reads Azure PAT from `prtask-server-secrets`. Creates DevTunnel, stores tunnel ID + access token.
2. User runs `tunnel-broker.cs` once (in its own pane). Stays running for the session.
3. User runs `tunnel-client.cs` per worker machine, in their own panes.
4. Orchestrator (Claude) uses `pub-exec.cs` instead of direct `dotnet run` for actions that should execute remotely.

## Migration

Not all `dotnet run` calls need to go through the tunnel. Local-only utilities (read-file, cat-file, write-file on canonical scratch configs, git-run) stay direct. Only actions that benefit from remote execution route through `pub-exec.cs`:
- `chrome-devtools.cs` (browser on tester host)
- `interactive-captures.cs` (captures on a host with Chrome)
- `verify-page-errors.cs` (browser-required verification)
- `publish-pages.cs`, `generate-statics.cs` (build host)

## Open questions

1. Which exact Azure PAT scope? (DevTunnel.ReadWrite is needed; what's the minimum?)
2. Tunnel anonymous-allow off, but how do clients authenticate without sharing the access token in clear? Per-client tunnel scopes?
3. Reconnect/idempotency: if a `cmd` is in-flight when client disconnects, does the orchestrator retry? With which client? (could double-execute)
4. Log streaming back-pressure: `take_screenshot --fullPage true` returns large bytes — encode as base64 in `log.line`, or use HTTP side-channel?
5. Concurrency per client: if `tester` receives 4 `cmd`s simultaneously, does it serialize them or parallelize? (mirrors the chrome-devtools.cs serve-mode question)

## Phasing

- **Phase 1 (~1 day)**: tunnel-init + tunnel-broker + tunnel-client + pub-exec scaffolding. JSON wire protocol. Single topic (`tester`). Single command type (`dotnet_run`).
- **Phase 2**: streaming logs, multiple topics, multiple clients per topic.
- **Phase 3**: migration of existing scripts to route through `pub-exec` selectively.
- **Phase 4**: handle reconnect/idempotency, large-payload sidechannel, observability.

## Concrete next steps for Claude session to start

If you ✅ this design, the next session should:
1. Run `winget install Microsoft.devtunnel` (user-side; needs admin).
2. Add `Azure:Pat` to `prtask-server-secrets` user-secrets (user-side).
3. Author `tunnel-init.cs` and run it once to materialize a tunnel.
4. Author `tunnel-broker.cs` and start it in a dedicated pane.
5. Author `tunnel-client.cs` and start one client (`tester`) in another pane.
6. Author `pub-exec.cs` and route ONE existing call (e.g., `chrome-devtools.cs serve`) through it as the smoke test.

Until then, all dotnet runs continue to execute locally as before.
