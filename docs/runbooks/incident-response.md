# Incident Response Runbook

Real diagnostic paths for this actual codebase — each section names the exact tool P13 built for
it, not a generic "check your monitoring dashboard" (no such dashboard exists yet; `/metrics` is
scraped by nothing today, since no Prometheus/Grafana deployment exists — see §5).

## 1. "The API is down / returning errors"

1. **Check liveness first**: `curl -s https://<host>/health/live`. If this doesn't return
   `Healthy`, the process itself is the problem (crashed, OOM, deadlocked) — restart it and check
   the Serilog console/file output for an unhandled exception around the time it stopped.
2. **If live but degraded, check readiness**: `curl -s https://<host>/health/ready`. The response
   names exactly which dependency is unhealthy (`database`/`redis`/`rabbitmq`), with a
   description and duration per check — go straight to that dependency's section below rather
   than guessing.
3. **Every response carries an `X-Request-Id` header.** If a user or client app reports a
   specific failed request, ask for that header's value (or read it from the client's own network
   tab) — it's the same value stamped into every Serilog line for that request via
   `CorrelationIdMiddleware`, so `grep <request-id>` against the log finds the exact server-side
   trace for that one request, no timestamp/path guessing needed.

## 2. Dependency-specific: MySQL down

Symptom: `/health/ready` shows `"database": "Unhealthy"`, description `"MySQL threw on
connect."` or `"MySQL not reachable."`

- `docker compose ps mysql` — confirm the container is actually running.
- `docker compose logs mysql --tail 50` — MySQL's own startup/crash log.
- **The named volume (`law_portal_mysql_data`) is what actually matters, not the container.**
  A container can be destroyed and recreated (`docker compose up -d mysql`) and all data
  survives as long as the volume itself wasn't removed (`docker compose down -v` would remove
  it — never run that against a real environment). Confirm the volume exists:
  `docker volume ls | grep mysql_data`.
- Once the container is back and passes its own `mysqladmin ping` healthcheck, `/health/ready`
  recovers on its own — no restart of the API is needed, since `DatabaseHealthCheck` probes a
  fresh connection on every call rather than caching a dead one.

## 3. Dependency-specific: RabbitMQ down

Symptom: `/health/ready` shows `"rabbitmq": "Unhealthy"`.

- **This is the least urgent of the three** by design: `RabbitMqBidFanOutQueue.EnqueueAsync`
  swallows-and-logs a publish failure rather than failing the request that triggered it (see its
  own doc comment — "broadcast fan-out is background convenience, not a synchronous part of
  submission"). A broadcast bidding request submitted while RabbitMQ is down is delayed, not
  lost — check the API log for `"Failed to publish"`-style warnings around the affected time,
  then manually verify no `RequestInvitation` rows are missing for that request once the broker
  recovers.
- Recovery is automatic on both sides once the broker comes back:
  `RabbitMqConnectionProvider` reopens lazily on next use, and `BidFanOutConsumer` reconnects
  with a 5-second backoff loop on its own (verified live during P13 — stopping and restarting
  the container while the API kept running recovered `/health/ready` to `Healthy` within ~15
  seconds once RabbitMQ itself finished booting, no API restart needed).

## 4. Dependency-specific: Redis down

Symptom: `/health/ready` shows `"redis": "Unhealthy"`.

- **Lowest-stakes of the three** — Redis backs presence tracking only (P5), which has no
  persistent volume by design (losing it just means every user briefly shows "offline" until
  they reconnect). `docker compose up -d redis` and it's a fresh, empty, fully-functional
  instance.

## 5. "A specific payment/webhook looks wrong"

- Every payment-affecting operation is designed to be replay-safe (webhook idempotency proven
  in P4; the subscription webhook path extended in P10). Before assuming corruption, check
  whether the same webhook fired twice — the ledger should show identical entries either way.
- **Reconciliation is a live view, not a report you have to generate**: `web-admin`'s Ledger
  page (`GET /api/v1/admin/finance/ledger/summary`, built in P11) shows whether debits equal
  credits across the *entire* database right now. If that's ever `false`, stop and investigate
  before anything else — every phase from P4 onward has relied on this balancing to exactly
  zero as its strongest correctness signal, and it has held throughout this project's history.

## 6. Rate limiting false positives ("a legitimate user is getting 429s")

- The `auth` policy is a *fixed* window (10 requests/minute per IP, not sliding) — a burst right
  at a window boundary can look tighter than 10/min in the worst case. If this is a genuine
  problem (e.g., an office/NAT sharing one IP), the fix is a higher per-partition limit or
  switching to a sliding-window limiter in `Program.cs`'s `AddRateLimiter` call — not disabling
  the policy, which was added specifically because no real reCAPTCHA account exists yet.

## 7. What this project does not have yet, honestly

- **No alerting.** `/health/ready` and `/metrics` exist and are correct, but nothing polls
  them and pages anyone — no Prometheus/Alertmanager/Grafana deployment exists. Standing up
  that stack (or pointing a hosted equivalent at `/metrics`) is real, undone infrastructure
  work, not a configuration toggle.
- **No on-call rotation** — there is one developer on this project. This section exists so that
  the *procedure* is written down before a second person ever needs it, not because a rotation
  exists today.
- **No real incident postmortem process** — write one down (what happened, root cause, what
  changed) the first time this runbook actually gets used for a real incident, and fold the
  lesson back into this file.
