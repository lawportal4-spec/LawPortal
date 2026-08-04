# Deployment Runbook

Real commands for this actual stack. There is no CI/CD pipeline and no staging/production
environment yet — this runbook describes the procedure to follow whenever one exists, written
against the real tooling this repo already has (Docker Compose services, EF Core migrations, the
`LawPortal.DbMigrator` console app built in P13).

## Topology (per `docker-compose.yml`)

| Service | Container | Purpose |
|---|---|---|
| `mysql` | `law-portal-mysql` | Primary datastore, named volume `law_portal_mysql_data` |
| `redis` | `law-portal-redis` | Presence tracking only (P5) — no persistent data, safe to lose |
| `rabbitmq` | `law-portal-rabbitmq` | Bidding broadcast fan-out (P9) |
| `minio` | `law-portal-minio` | Request attachments, named volume `law_portal_minio_data` |
| `clamav` | `law-portal-clamav` | Attachment virus scanning |
| `livekit` | `law-portal-livekit` | Voice/video (P8), `--dev` mode only — a real deployment needs TURN/public UDP, not this compose file's config |

Plus the .NET API (`LawPortal.Api`) and four frontends (`web-client`, `web-lawyer`, `web-admin`,
`web-marketing`), none of which are containerized yet — no `Dockerfile` exists for any of them.
**That is real, undone work before a first deployment**, not an oversight this runbook can paper
over.

## Standard deploy sequence

1. **Apply database migrations first, separately from starting the API.**
   ```bash
   cd api/src/LawPortal.DbMigrator
   dotnet run
   ```
   This runs `Database.MigrateAsync()` against whatever `ConnectionStrings:Default` resolves to
   (via `appsettings.json`, overridable by environment variables — e.g.
   `ConnectionStrings__Default=...`). It does **not** start any background service (no RabbitMQ
   consumer, no subscription-renewal poller) — see the file's own comment for why that's true by
   construction, not by convention.
   - First deploy to a brand-new environment: add `--seed` to also load reference/catalog data
     (regions, cities, specialties, service categories, commission policy, subscription plans,
     RBAC roles/permissions, the bootstrap admin).
   - **Never** pass `--with-demo-data` outside a demo/staging environment — it generates 550+
     synthetic lawyer profiles (`DevLawyerSeeder`) for pagination-testing purposes only.

2. **Verify readiness before routing traffic to a new instance.**
   ```bash
   curl -s https://<host>/health/ready
   ```
   Must show `"status": "Healthy"` with all three dependency checks (`database`, `redis`,
   `rabbitmq`) individually `Healthy`. A `503` here means don't cut traffic over yet — see the
   incident-response runbook for what to do if it stays unhealthy.

3. **Start (or roll) the API process.** Whatever the real deploy mechanism ends up being
   (systemd unit, container orchestrator, etc.), it must set `ASPNETCORE_ENVIRONMENT=Production`
   explicitly. **This bit us once already, for real**: restarting with `dotnet run
   --no-launch-profile` and no explicit environment variable silently defaulted to `Production`,
   which skips the `if (app.Environment.IsDevelopment())` block in `Program.cs` entirely — no
   migration, no seeding — and the very next request that touched a table created by a recent
   migration failed with `Table '...' doesn't exist`. In a real deployment this is *correct*
   behavior (migrations should never be a side effect of an API process starting in prod) — it
   only bit us because step 1 above hadn't been run yet. Always run step 1 first.

4. **Restart the frontends** (or redeploy their static builds — all four are Vite/Astro static
   output, `pnpm --filter <app> build` then serve `dist/`). None require a database migration
   step themselves.

## Configuration that must change before this is a real deployment

Every one of these is a placeholder today, flagged consistently since the phase that introduced
it — grep `appsettings.json` for `REPLACE_WITH` and `dev_only` to find them all:

- `Jwt:Key` — currently a placeholder string; must be a real 32+ char secret, injected via
  environment variable or a real secrets manager, never committed.
- `Payments:Moyasar:SecretKey` — no real Moyasar merchant account exists yet (flagged since P0).
- `Payments:WebhookSecret` — currently `dev_only_webhook_secret_replace_before_launch`, literally.
- SMS/OTP vendor — `LoggingOtpSender` logs to console; no real vendor is wired in (P1).
- reCAPTCHA — `NoOpRecaptchaVerifier` is a no-op; no real account exists (P1).
- `LiveKit:ApiKey`/`ApiSecret` — dev placeholders; a real deployment also needs LiveKit run
  without `--dev` mode, which needs a TURN server and public UDP reachability (P8's own caveat).
- CORS origins in `Program.cs` (`ClientOrigins` policy) — currently hardcoded to the four local
  dev ports. Must become the real `https://app.lawportal.sa`, `https://lawyers.lawportal.sa`,
  etc. domains before launch. (Fixed during P13: the `web-marketing` entry pointed at Astro's own
  default dev port, 4321, rather than the 5176 this monorepo actually runs it on — harmless today
  since `web-marketing`'s catalog fetch runs server-side at build time, not from the browser, so
  it was never actually subject to CORS, but would have silently broken the first client-side
  fetch anyone ever added to that app.)

## Rollback

No automated rollback tooling exists. Manually:
1. Redeploy the previous known-good API build/container.
2. **Do not** run `dotnet ef database update <previous-migration>` reflexively — check whether
   the migration being rolled back is destructive (dropped a column, etc.) first; EF Core
   migrations in this project have not been audited for down-migration safety.
3. Re-check `/health/ready`.
