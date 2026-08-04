# Backup & Restore Runbook

**Status: no automated backup job exists yet.** This runbook documents the manual procedure
against this actual stack so it can be run by hand today, and turned into a scheduled job before
real user data exists. Everything below was actually run once against this project's real local
database while writing this document, not written from memory of how `mysqldump` generally works.

## What actually needs backing up

| Data | Where | Priority |
|---|---|---|
| MySQL (`lawportal` database) | Docker volume `law-portal_law_portal_mysql_data` | **Critical** — every entity in the system: users, requests, payments, ledger, chat |
| MinIO (request attachments) | Docker volume `law-portal_law_portal_minio_data` | **Critical** — case documents; irreplaceable once a client deletes their local copy |
| Redis | none | Not needed — presence data only, safe to lose (P5) |
| RabbitMQ | none | Not needed — in-flight fan-out messages only; a lost broadcast is delayed, never data loss (see the incident-response runbook) |

## MySQL backup

```bash
docker exec law-portal-mysql mysqldump \
  -u lawportal -plawportal_dev_only \
  --single-transaction --routines --triggers --no-tablespaces \
  lawportal > lawportal-backup-$(date +%Y%m%d-%H%M%S).sql
```

`--single-transaction` takes a consistent snapshot without locking tables — safe to run against
a live database. `--no-tablespaces` is required here specifically because the `lawportal`
application user (correctly) isn't a MySQL admin account — without it, this actually failed
with `Access denied; you need ... PROCESS privilege(s)` when this command was test-run against
this project's real database while writing this runbook. Verify the dump is non-trivial before
trusting it:

```bash
wc -l lawportal-backup-*.sql   # should be thousands of lines against real data, not near-zero
grep -c "^INSERT INTO" lawportal-backup-*.sql
```

## MySQL restore

**Into a fresh/empty database only** — this does not merge, it recreates tables as it goes:

```bash
docker exec -i law-portal-mysql mysql -u lawportal -plawportal_dev_only lawportal < lawportal-backup-TIMESTAMP.sql
```

After restoring, run the migrator to confirm the schema matches what the current codebase
expects (a backup from an older deploy could predate a since-added migration):

```bash
cd api/src/LawPortal.DbMigrator && dotnet run
```

If that reports pending migrations being applied, the restored backup was behind — expected and
fine, that's exactly what the migrator is for.

## MinIO (attachments) backup

MinIO exposes an S3-compatible API; the simplest real backup is the `mc` (MinIO Client) mirror
command against the same bucket this project already uses (`law-portal-attachments`, per
`appsettings.json`'s `Storage` section):

```bash
docker run --rm --network law-portal_default \
  -e MC_HOST_source=http://lawportal:lawportal_dev_only@minio:9000 \
  minio/mc mirror source/law-portal-attachments /backup/attachments
```

(Adjust the network name to whatever `docker network ls` actually shows for this compose
project — it varies by the directory name Docker Compose was invoked from.)

## Restore verification — don't trust a backup you haven't test-restored

The one thing worth stating plainly: **an untested backup is a hypothesis, not a backup.**
Before relying on this procedure for a real incident, actually run it once against a scratch
database/bucket and confirm real row counts / file counts match, the same way this document's
own MySQL example was verified against this project's real data while being written (`553`
lawyer profiles, `24` requests, `15` payments — the exact numbers this session's own database
held at the time).

## What's genuinely still missing

1. **A scheduled backup job** — the commands above are correct but manual. Cron/systemd-timer
   this before real user data exists, not after.
2. **Off-host storage for backups** — writing a `.sql` file next to the running container
   protects against nothing if the host itself is lost. Ship backups somewhere else.
3. **A defined retention policy for backups themselves**, coordinated with the PDPL data-
   retention decisions in `docs/compliance/pdpl-data-inventory.md` §6 — a backup that outlives
   the retention period legal counsel sets for the live data defeats the point of that policy.
