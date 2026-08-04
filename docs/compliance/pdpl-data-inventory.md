# PDPL Data Inventory & Processing Record

**Status: internal engineering draft, not a substitute for legal review.** This document maps
what the Law Portal codebase actually collects, stores, and processes today, against Saudi
Arabia's Personal Data Protection Law (PDPL, Royal Decree M/19 of 1443H and its Implementing
Regulation). It is written from the code, not from a legal brief — every claim below is
traceable to a specific entity, command, or config file in this repository. **A licensed PDPL
counsel must review this before it is treated as a compliance artifact rather than an
engineering reference.**

Last generated: 2026-08-04, against the codebase as of P13.

---

## 1. Why this law plainly applies

Legal case data — a client's dispute details, court documents, chat transcripts with a lawyer —
is about as sensitive as personal data gets. The plan's own architecture section flagged this
from the start (`Domain events`/self-hosted LiveKit for exactly this reason). This inventory
exists so that claim can be backed by an actual accounting of what's stored where, not just
asserted.

## 2. Data categories, by source entity

| Category | Entities | Sensitivity |
|---|---|---|
| Identity & contact | `User` (phone/email), `ClientProfile`, `LawyerProfile`, `AdminProfile` | Personal data |
| Government-issued credential | `LawyerLicense` (MoJ licence number, issue/expiry dates) | Personal data, verification-critical |
| Case content | `ServiceRequest` + subtypes, `RequestAttachment`, `Message` | **Sensitive** — this is the legal-matter content itself |
| Financial | `Payment`, `Refund`, `Payout`, `Invoice`, `WalletTransaction`, `SubscriptionInvoice` | Personal data, financial |
| Communications metadata | `MessageThread`, `ThreadParticipant`, `Notification` | Personal data |
| Behavioral / trust | `Review`, `Report`, `Block`, `AuditLog` | Personal data |
| Security artifacts | `RefreshToken`, `OtpChallenge` | Personal data, short-lived |

## 3. Legal basis per processing purpose

| Purpose | Basis | Notes |
|---|---|---|
| Delivering the requested legal service | Contract performance | The core purpose — matching client to lawyer, moving money, hosting the conversation |
| Licence verification | Legal obligation / legitimate interest | The entire trust model rests on this; see P1's admin verification queue |
| Tax invoicing (VAT/ZATCA) | Legal obligation | 15% VAT, ZATCA Phase 1 QR — see `ZatcaQrCodeBuilder` |
| Fraud/abuse prevention (rate limiting, audit log) | Legitimate interest | Added in P13 — see §7 |
| Free-minutes entitlement, reviews | Contract performance | Directly tied to service delivery |

No processing in this codebase relies on consent as its basis except OTP/reCAPTCHA-gated actions
implicitly assuming the user initiated them — **no marketing consent flow exists because no
marketing communication feature exists** (P11 explicitly deferred promotions/campaigns).

## 4. Data subject rights — implementation status

| PDPL right | Implementation | Status |
|---|---|---|
| Right to know / access | `GET /api/v1/account/export` (`ExportMyDataQuery`, added P13) | **Implemented** — returns profile, requests, payments, messages, reviews as JSON |
| Right to erasure | `DELETE /api/v1/account` (`DeleteAccountCommand`, P1) | **Implemented** — soft-delete + immediate PII nulling (phone/email/password hash) |
| Right to correction | Profile update endpoints (`UpdateLawyerProfileCommand`, client profile fields) | **Partially implemented** — lawyers can self-correct; clients have no profile-edit UI yet |
| Right to restrict/object to processing | None | **Not implemented** — no processing-restriction flag exists on any entity |
| Right to data portability | Same export endpoint as "right to know" — JSON is machine-readable | **Implemented** in substance, not in a named/dedicated format |

**A real gap, stated plainly**: erasure nulls identifying fields but does not purge financial
records, which are retained for statutory tax-record-keeping reasons (see §6). This is very
likely the legally correct outcome, but "very likely correct" is an engineering guess — a real
PDPL counsel needs to confirm the retention override is actually justified under the law's own
exceptions, not just assumed.

## 5. Third parties data is shared with

| Party | Data shared | Contract status |
|---|---|---|
| Payment gateway (Moyasar, planned) | Payment amount, currency, a gateway reference — **not** full card numbers | **No real account exists yet** — `FakePaymentGateway` is the Development default; flagged since P0 |
| SMS/OTP vendor | Phone number, OTP code | **No real vendor contracted** — `LoggingOtpSender` logs to console only, since P1 |
| Cloud storage (self-hosted MinIO) | Request attachments | Self-hosted, not a third party in the legal sense — data stays on infrastructure we control |
| Antivirus scanning (self-hosted ClamAV) | Attachment file contents, transiently | Self-hosted, same as above |
| Voice/video (self-hosted LiveKit) | Call audio/video, session metadata | Self-hosted specifically for data-residency reasons — see the plan's own architecture rationale |
| Push/email notification vendor | None yet | **No vendor contracted** — `LoggingNotificationSender` since P5 |

**The pattern across every unconfigured vendor above is deliberate, not accidental**: this
project has repeatedly chosen "log to console, ship a real interface, wire the real vendor in
later" over guessing at a vendor integration with no real account to test against. That means
today, in this codebase's actual running state, **no personal data has ever left
infrastructure this project controls** — genuinely simpler PDPL posture than most real deployments
will have, precisely because the real integrations don't exist yet. Each of those integrations
becomes a real cross-border-transfer and processor-agreement question the moment it's contracted.

## 6. Retention

| Data | Retention | Why |
|---|---|---|
| Account PII (phone/email/password) | Until deletion request; nulled immediately on deletion | `DeleteAccountCommand` |
| Financial records (payments/invoices/ledger) | Retained after account deletion — no purge job exists | Saudi tax law requires retained accounting records; **the exact retention period has not been set by legal counsel and is not encoded anywhere in this codebase** |
| Chat messages | Retained indefinitely today | No retention job exists; legal correspondence being evidence (per the plan's own architecture note) argues for retention, but no explicit retention *policy* has been decided |
| OTP challenges / refresh tokens | Short-lived by design (OTP expires in minutes; refresh tokens rotate) | Security artifacts, not case data |
| Audit log | Retained indefinitely today | No purge job exists |

**Open item for legal counsel**: set an explicit retention period for financial records and
chat history, then build the purge job — neither currently exists as a scheduled process.

## 7. What P13 added specifically for compliance/security posture

- `ExportMyDataQuery` / `GET /api/v1/account/export` — the data-portability implementation above.
- Rate limiting (`Microsoft.AspNetCore.RateLimiting`, fixed-window, 10 req/min/IP) on every
  auth endpoint (client OTP, lawyer login/register, admin login, token refresh) — defense in
  depth against credential-stuffing, independent of whether a real reCAPTCHA account exists.
- `/health/ready` with real dependency probes (MySQL/Redis/RabbitMQ) — see the runbooks for how
  this is meant to be used operationally.
- Baseline OWASP security headers (`X-Content-Type-Options`, `X-Frame-Options`,
  `Referrer-Policy`, `Permissions-Policy`, `Content-Security-Policy`).
- A per-request correlation id (`X-Request-Id`) tying a caller's own error report to exact
  server log lines.

## 8. What genuinely still needs a human, not more code

1. **A licensed PDPL counsel must review this entire document.** Nothing here should be
   presented externally as "PDPL-compliant" on the strength of this file alone.
2. **A real Data Protection Impact Assessment (DPIA)** for the case-content processing path —
   this document is an inventory, not a DPIA; a DPIA weighs risk and mitigation, which is a
   judgment call this document deliberately doesn't make.
3. **Retention periods for financial records and chat history**, set by counsel, then encoded
   as an actual scheduled purge job (none exists).
4. **Processor agreements** with Moyasar and an SMS vendor, the moment either is actually
   contracted — this document can only describe today's state (no real vendor exists yet).
5. **A real third-party penetration test** — see `docs/compliance/security-hardening-notes.md`
   for what internal review this pass could and couldn't substitute for.
