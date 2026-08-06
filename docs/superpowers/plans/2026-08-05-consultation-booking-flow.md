# Consultation Booking Flow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the "Consult Now" button on a lawyer's profile actually book a consultation — the
only entry point into an entire transaction type that currently does nothing.

**Architecture:** One new client-side wizard page (`/lawyers/:slug/consult`, 4 steps: type →
length/schedule → details → review+submit) that calls the backend's already-working consultation
endpoints, then hands off to the existing `OrderDetail.tsx` payment flow unchanged. One small,
additive backend DTO change exposes the specialty IDs the wizard needs (today only names are
exposed).

**Tech Stack:** React + TypeScript + Vite (`apps/web-client`), `@tanstack/react-query`,
`react-router-dom`, `@law-portal/ui` component library. ASP.NET Core + EF Core + MediatR
(`api/src/LawPortal.Application`, `.Api`).

## Global Constraints

- Spec: `docs/superpowers/specs/2026-08-05-consultation-booking-flow-design.md` — read it before
  starting; this plan implements it exactly.
- Copy convention: inline `isAr ? "…" : "…"` ternaries throughout, matching
  `NewBiddingRequest.tsx` — **not** the shared `packages/i18n` locale files (that sibling wizard
  doesn't use them either; stay consistent with it, not with the rest of the app).
- Component reuse only: `StepProgress`, `Card`, `Button`, `Chip`, `Input` from `@law-portal/ui`.
  Do not add new dependencies (no date-picker library — use native
  `<input type="datetime-local">`, matching `Register.tsx`/`Settings.tsx`).
- **No test framework exists in this repo for either side** (both `api/tests/*` projects contain
  only the default scaffolded `UnitTest1.cs`; `web-client` has no test runner configured). Every
  "test cycle" step in this plan is therefore a **manual verification** (a `curl` command against
  the running API, or a browser walkthrough) rather than an automated test — this matches how
  every other feature in this codebase has actually been verified so far. Do not introduce a test
  framework as a side effect of this plan; that's a separate decision for the user to make.
- The backend change in Task 1 requires **restarting the running API process** for it to take
  effect (`dotnet run` does not hot-reload). If the API is already running (check with
  `lsof -nP -iTCP:5280 -sTCP:LISTEN`), confirm with the user before killing/restarting it — it may
  be attached to an IDE debugger.
- After every task: `pnpm --filter @law-portal/web-client build` and
  `pnpm --filter @law-portal/web-client lint` must both stay clean.

---

## File Structure

| File | Responsibility |
|---|---|
| `api/src/LawPortal.Application/Lawyers/Dtos/LawyerDtos.cs` | Modify — add `LawyerSpecialtyDto`, add a `Specialties` field to `LawyerProfileDetailDto` |
| `api/src/LawPortal.Application/Lawyers/Queries/GetLawyerProfileQuery.cs` | Modify — project the new field |
| `apps/web-client/src/lib/api.ts` | Modify — mirror the new field into the TS `LawyerProfileDetailDto` interface |
| `apps/web-client/src/lib/consultationApi.ts` | Create — thin API client for the two consultation-specific calls, mirrors `biddingApi.ts` |
| `apps/web-client/src/pages/NewConsultationRequest.tsx` | Create — the 4-step wizard, one file, matching `NewBiddingRequest.tsx`'s own single-file convention |
| `apps/web-client/src/App.tsx` | Modify — add the route |
| `apps/web-client/src/pages/LawyerProfile.tsx` | Modify — wire the button, gate it on pricing existing |

---

### Task 1: Backend — expose specialty IDs on the lawyer profile endpoint

**Files:**
- Modify: `api/src/LawPortal.Application/Lawyers/Dtos/LawyerDtos.cs`
- Modify: `api/src/LawPortal.Application/Lawyers/Queries/GetLawyerProfileQuery.cs`

**Interfaces:**
- Produces: `LawyerSpecialtyDto(int Id, string NameAr, string NameEn)`; a new
  `Specialties: IReadOnlyList<LawyerSpecialtyDto>` field on `LawyerProfileDetailDto`, positioned
  immediately after the existing `SpecialtyNamesEn` field. JSON-serializes as
  `specialties: [{ id, nameAr, nameEn }]` (camelCase, matching every other endpoint in this API).

- [ ] **Step 1: Confirm the field is currently absent (red)**

Find a verified, seeded lawyer's slug and confirm today's response has no `specialties` field:

```bash
docker exec law-portal-mysql mysql -ulawportal -plawportal_dev_only lawportal --default-character-set=utf8mb4 \
  -e "SELECT Slug FROM lawyer_profiles WHERE IsVerified = 1 LIMIT 1;"
curl -s "http://localhost:5280/api/v1/lawyers/<slug-from-above>" | python3 -m json.tool
```

Expected: valid JSON with `specialtyNamesAr`/`specialtyNamesEn`, but **no** `specialties` key.

- [ ] **Step 2: Add `LawyerSpecialtyDto` and the new field**

In `LawyerDtos.cs`, add the new record near `LawyerQualificationDto`:

```csharp
public record LawyerSpecialtyDto(int Id, string NameAr, string NameEn);
```

Then update `LawyerProfileDetailDto` to add `Specialties` right after `SpecialtyNamesEn`:

```csharp
public record LawyerProfileDetailDto(
    Guid Id,
    string Slug,
    string FullName,
    string? BioAr,
    string? BioEn,
    string? Gender,
    string? CityNameAr,
    string? CityNameEn,
    string? RegionNameAr,
    string? RegionNameEn,
    string? ExperienceDisplay,
    bool IsVerified,
    bool IsVatRegistered,
    decimal? AvgRating,
    int RatingCount,
    int CompletedRequestCount,
    LawyerLicenseDto License,
    LawyerPricingDto? Pricing,
    IReadOnlyList<string> SpecialtyNamesAr,
    IReadOnlyList<string> SpecialtyNamesEn,
    IReadOnlyList<LawyerSpecialtyDto> Specialties,
    IReadOnlyList<string> LanguageNamesAr,
    IReadOnlyList<string> LanguageNamesEn,
    IReadOnlyList<LawyerQualificationDto> Qualifications);
```

- [ ] **Step 3: Project the new field in the query handler**

In `GetLawyerProfileQuery.cs`, add one line to the `Select` projection, immediately after the
existing `SpecialtyNamesEn` line (`l.LawyerSpecialties.Select(ls => ls.Specialty!.NameEn).ToList(),`):

```csharp
l.LawyerSpecialties.Select(ls => new LawyerSpecialtyDto(ls.Specialty!.Id, ls.Specialty.NameAr, ls.Specialty.NameEn)).ToList(),
```

The full constructor call's argument order must match the record's declaration order exactly —
double-check against Step 2 before moving on.

- [ ] **Step 4: Rebuild and restart the API, then verify (green)**

```bash
cd api/src/LawPortal.Api && dotnet build
```

Restart the running `dotnet run --urls http://localhost:5280` process (see Global Constraints —
confirm with the user first if it's IDE-attached). Then re-run Step 1's `curl` command:

Expected: response now includes
`"specialties":[{"id":<int>,"nameAr":"...","nameEn":"..."}]` matching the count of
`specialtyNamesAr`.

- [ ] **Step 5: Commit**

```bash
git add api/src/LawPortal.Application/Lawyers/Dtos/LawyerDtos.cs \
        api/src/LawPortal.Application/Lawyers/Queries/GetLawyerProfileQuery.cs
git commit -m "Expose specialty IDs on the lawyer profile endpoint

Additive field alongside the existing name-only arrays — needed by the
consultation-booking wizard to submit a specialtyId, which the names alone
can't provide."
```

---

### Task 2: Frontend — API client for consultation creation

**Files:**
- Modify: `apps/web-client/src/lib/api.ts`
- Create: `apps/web-client/src/lib/consultationApi.ts`

**Interfaces:**
- Consumes: the `specialties` field from Task 1 (already live on the running API by this point).
- Produces:
  - `LawyerSpecialtyDto { id: number; nameAr: string; nameEn: string }` (in `api.ts`)
  - `ConsultationType = "Instant" | "Written" | "Scheduled"` (in `consultationApi.ts`)
  - `createConsultationDraft(params: { specialtyId: number; lawyerProfileId: string; consultationType: ConsultationType; durationMinutes: number | null }): Promise<string>`
  - `updateConsultationDetails(id: string, title: string, description: string, scheduledStartUtc: string | null): Promise<void>`

- [ ] **Step 1: Mirror the new DTO field into `api.ts`**

Add the interface and extend `LawyerProfileDetailDto`:

```typescript
export interface LawyerSpecialtyDto {
  id: number;
  nameAr: string;
  nameEn: string;
}
```

In the existing `LawyerProfileDetailDto` interface, add one line right after
`specialtyNamesEn: string[];`:

```typescript
  specialties: LawyerSpecialtyDto[];
```

- [ ] **Step 2: Verify the type change compiles and the field is really there at runtime**

```bash
pnpm --filter @law-portal/web-client build
```

Expected: no TypeScript errors (this is a pure additive interface change, nothing consumes the
new field yet, so nothing can break).

Then, with the web-client dev server running, open the browser console on any lawyer profile
page and confirm the live shape:

```bash
playwright-cli open http://localhost:5173/lawyers/<any-verified-lawyer-slug>
playwright-cli eval "fetch('http://localhost:5280/api/v1/lawyers/<slug>').then(r => r.json()).then(d => JSON.stringify(d.specialties))"
```

Expected: a JSON array of `{id, nameAr, nameEn}` objects, not `undefined`.

- [ ] **Step 3: Write `consultationApi.ts`**

```typescript
import { api } from "./api";

export type ConsultationType = "Instant" | "Written" | "Scheduled";

export interface CreateConsultationDraftParams {
  specialtyId: number;
  lawyerProfileId: string;
  consultationType: ConsultationType;
  durationMinutes: number | null;
}

export async function createConsultationDraft(params: CreateConsultationDraftParams): Promise<string> {
  const { data } = await api.post<string>("/api/v1/client/requests/consultations", params);
  return data;
}

export async function updateConsultationDetails(
  id: string,
  title: string,
  description: string,
  scheduledStartUtc: string | null,
): Promise<void> {
  await api.put(`/api/v1/client/requests/consultations/${id}/details`, {
    title,
    description,
    scheduledStartUtc,
    voiceNoteStorageKey: null,
    voiceNoteDurationSeconds: null,
  });
}
```

These two endpoints were already verified working end-to-end via direct `curl` calls during the
UI test cycle that found this bug — this step is pure plumbing around a proven contract, not new
backend behavior. No additional runtime check needed beyond the build in Step 4.

- [ ] **Step 4: Build and lint**

```bash
pnpm --filter @law-portal/web-client build
pnpm --filter @law-portal/web-client lint
```

Expected: both clean.

- [ ] **Step 5: Commit**

```bash
git add apps/web-client/src/lib/api.ts apps/web-client/src/lib/consultationApi.ts
git commit -m "Add consultation-draft API client, mirroring biddingApi.ts"
```

---

### Task 3: Frontend — the booking wizard, route, and button wiring

**Files:**
- Create: `apps/web-client/src/pages/NewConsultationRequest.tsx`
- Modify: `apps/web-client/src/App.tsx`
- Modify: `apps/web-client/src/pages/LawyerProfile.tsx`

**Interfaces:**
- Consumes: `getLawyerProfile` (existing, `api.ts`), `createConsultationDraft` /
  `updateConsultationDetails` / `ConsultationType` (Task 2), `api` (existing axios instance,
  `api.ts`), `StepProgress` / `Card` / `Button` / `Chip` / `Input` (`@law-portal/ui`).
- Produces: route `/lawyers/:slug/consult` (behind `RequireAuth`); `LawyerProfile.tsx`'s
  "Consult Now" button now navigates there instead of doing nothing.

- [ ] **Step 1: Write `NewConsultationRequest.tsx`**

```tsx
import { useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useNavigate, useParams } from "react-router-dom";
import { ArrowLeft, ArrowRight, Check, CalendarClock, FileText, Phone } from "lucide-react";
import { Button, Card, Chip, Input, StepProgress } from "@law-portal/ui";
import { useTranslation, formatCurrency } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { api, getLawyerProfile } from "../lib/api";
import { createConsultationDraft, updateConsultationDetails, type ConsultationType } from "../lib/consultationApi";

const DURATIONS = [15, 30, 45] as const;

/**
 * The consultation-booking wizard — the flow "Consult Now" always should have opened. Mirrors
 * NewBiddingRequest.tsx's shape (StepProgress + Card, skip-a-step-on-a-choice pattern) rather
 * than inventing a second wizard convention: type -> length/schedule (skipped for Written) ->
 * details -> review+submit. Submitting hands off to the existing OrderDetail payment flow —
 * unchanged, already proven to render correctly for a submitted consultation request.
 */
export default function NewConsultationRequest() {
  const { slug } = useParams<{ slug: string }>();
  const { i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const navigate = useNavigate();

  const [step, setStep] = useState(1);
  const [consultationType, setConsultationType] = useState<ConsultationType | null>(null);
  const [pickedSpecialtyId, setPickedSpecialtyId] = useState<number | null>(null);
  const [durationMinutes, setDurationMinutes] = useState<15 | 30 | 45 | null>(null);
  const [scheduledStart, setScheduledStart] = useState("");
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [error, setError] = useState<string | null>(null);

  const lawyerQuery = useQuery({
    queryKey: ["lawyers", "profile", slug],
    queryFn: () => getLawyerProfile(slug!),
    enabled: !!slug,
  });

  const submitMutation = useMutation({
    mutationFn: async () => {
      const lawyer = lawyerQuery.data!;
      const requestId = await createConsultationDraft({
        specialtyId: effectiveSpecialtyId!,
        lawyerProfileId: lawyer.id,
        consultationType: consultationType!,
        durationMinutes: consultationType === "Written" ? null : durationMinutes,
      });
      await updateConsultationDetails(
        requestId,
        title,
        description,
        consultationType === "Scheduled" && scheduledStart ? new Date(scheduledStart).toISOString() : null,
      );
      await api.post(`/api/v1/client/requests/${requestId}/submit`);
      return requestId;
    },
    onSuccess: (requestId) => navigate(`/orders/${requestId}`),
    onError: () => setError(isAr ? "تعذّر إرسال الطلب." : "Could not submit the request."),
  });

  if (lawyerQuery.isLoading) {
    return (
      <AppShell>
        <p className="text-sm text-ink-faint">{isAr ? "جارٍ التحميل…" : "Loading…"}</p>
      </AppShell>
    );
  }
  if (lawyerQuery.isError || !lawyerQuery.data) {
    return (
      <AppShell>
        <p className="text-sm text-rubric">{isAr ? "لم يتم العثور على الملف." : "Profile not found."}</p>
      </AppShell>
    );
  }

  const lawyer = lawyerQuery.data;

  if (!lawyer.pricing) {
    return (
      <AppShell>
        <p className="text-sm text-rubric">
          {isAr ? "هذا المحامي غير متاح للحجز حاليًا." : "This lawyer is not currently bookable."}
        </p>
      </AppShell>
    );
  }
  const pricing = lawyer.pricing;

  const effectiveSpecialtyId = lawyer.specialties.length === 1 ? lawyer.specialties[0].id : pickedSpecialtyId;

  const steps = [
    { label: isAr ? "نوع الاستشارة" : "Type" },
    { label: isAr ? "المدة" : "Length" },
    { label: isAr ? "التفاصيل" : "Details" },
    { label: isAr ? "المراجعة" : "Review" },
  ];

  const canProceed =
    (step === 1 && consultationType !== null && effectiveSpecialtyId !== null) ||
    (step === 2 &&
      durationMinutes !== null &&
      (consultationType !== "Scheduled" || scheduledStart.trim().length > 0)) ||
    (step === 3 && title.trim().length > 0 && description.trim().length > 0) ||
    step === 4;

  function next() {
    if (step === 1 && consultationType === "Written") return setStep(3);
    setStep((s) => Math.min(4, s + 1));
  }
  function back() {
    if (step === 3 && consultationType === "Written") return setStep(1);
    setStep((s) => Math.max(1, s - 1));
  }

  function durationPrice(minutes: 15 | 30 | 45) {
    return minutes === 15 ? pricing.price15 : minutes === 30 ? pricing.price30 : pricing.price45;
  }

  const typeLabel = (t: ConsultationType) =>
    t === "Written" ? (isAr ? "كتابية" : "Written") : t === "Instant" ? (isAr ? "فورية" : "Instant") : isAr ? "مجدولة" : "Scheduled";

  return (
    <AppShell>
      <h1 className="mb-2 font-display text-2xl font-bold">
        {isAr ? `استشارة مع ${lawyer.fullName}` : `Consultation with ${lawyer.fullName}`}
      </h1>
      <p className="mb-6 text-sm text-ink-faint">
        {isAr ? "اختر نوع الاستشارة، صف حالتك، ثم أكمل الدفع." : "Choose a consultation type, describe your case, then complete payment."}
      </p>

      <div className="mb-8">
        <StepProgress steps={steps} current={step} />
      </div>

      <Card className="mx-auto max-w-xl">
        {step === 1 && (
          <div>
            <h2 className="mb-4 text-sm font-semibold text-ink-soft">
              {isAr ? "كيف تريد الاستشارة؟" : "How do you want to consult?"}
            </h2>
            <div className="flex flex-col gap-2">
              <button
                onClick={() => setConsultationType("Written")}
                className={
                  "flex items-center justify-between rounded-md border px-4 py-3 text-start text-sm transition-colors " +
                  (consultationType === "Written" ? "border-seal bg-seal-tint text-seal-strong" : "border-border hover:border-seal")
                }
              >
                <span className="flex items-center gap-3">
                  <FileText className="h-4 w-4" />
                  <span>
                    <span className="block font-medium">{isAr ? "استشارة كتابية" : "Written consultation"}</span>
                    <span className="block text-xs text-ink-faint">{formatCurrency(pricing.writtenPrice)}</span>
                  </span>
                </span>
                {consultationType === "Written" && <Check className="h-4 w-4" />}
              </button>
              <button
                onClick={() => setConsultationType("Instant")}
                className={
                  "flex items-center justify-between rounded-md border px-4 py-3 text-start text-sm transition-colors " +
                  (consultationType === "Instant" ? "border-seal bg-seal-tint text-seal-strong" : "border-border hover:border-seal")
                }
              >
                <span className="flex items-center gap-3">
                  <Phone className="h-4 w-4" />
                  <span>
                    <span className="block font-medium">{isAr ? "اتصال فوري" : "Call now"}</span>
                    <span className="block text-xs text-ink-faint">
                      {isAr ? "من " : "From "}
                      {formatCurrency(pricing.price15)}
                    </span>
                  </span>
                </span>
                {consultationType === "Instant" && <Check className="h-4 w-4" />}
              </button>
              <button
                onClick={() => setConsultationType("Scheduled")}
                className={
                  "flex items-center justify-between rounded-md border px-4 py-3 text-start text-sm transition-colors " +
                  (consultationType === "Scheduled" ? "border-seal bg-seal-tint text-seal-strong" : "border-border hover:border-seal")
                }
              >
                <span className="flex items-center gap-3">
                  <CalendarClock className="h-4 w-4" />
                  <span>
                    <span className="block font-medium">{isAr ? "جدولة اتصال" : "Schedule a call"}</span>
                    <span className="block text-xs text-ink-faint">
                      {isAr ? "من " : "From "}
                      {formatCurrency(pricing.price15)}
                    </span>
                  </span>
                </span>
                {consultationType === "Scheduled" && <Check className="h-4 w-4" />}
              </button>
            </div>

            {lawyer.specialties.length > 1 && (
              <div className="mt-4">
                <h3 className="mb-2 text-xs font-semibold text-ink-faint">
                  {isAr ? "التخصص المتعلق بحالتك" : "Which specialty fits your case?"}
                </h3>
                <div className="flex flex-wrap gap-2">
                  {lawyer.specialties.map((s) => (
                    <button key={s.id} onClick={() => setPickedSpecialtyId(s.id)}>
                      <Chip className={pickedSpecialtyId === s.id ? "!bg-seal !text-seal-on" : "cursor-pointer"}>
                        {isAr ? s.nameAr : s.nameEn}
                      </Chip>
                    </button>
                  ))}
                </div>
              </div>
            )}
          </div>
        )}

        {step === 2 && (
          <div>
            <h2 className="mb-4 text-sm font-semibold text-ink-soft">{isAr ? "كم المدة؟" : "How long?"}</h2>
            <div className="flex flex-col gap-2">
              {DURATIONS.map((minutes) => (
                <button
                  key={minutes}
                  onClick={() => setDurationMinutes(minutes)}
                  className={
                    "flex items-center justify-between rounded-md border px-4 py-3 text-start text-sm transition-colors " +
                    (durationMinutes === minutes ? "border-seal bg-seal-tint text-seal-strong" : "border-border hover:border-seal")
                  }
                >
                  <span>{isAr ? `${minutes} دقيقة` : `${minutes} minutes`}</span>
                  <span className="font-mono text-xs">{formatCurrency(durationPrice(minutes))}</span>
                </button>
              ))}
            </div>

            {consultationType === "Scheduled" && (
              <div className="mt-4">
                <label className="mb-1 block text-xs font-semibold text-ink-faint">
                  {isAr ? "التاريخ والوقت" : "Date and time"}
                </label>
                <input
                  type="datetime-local"
                  value={scheduledStart}
                  onChange={(e) => setScheduledStart(e.target.value)}
                  className="w-full rounded-md border border-border bg-surface-raised px-4 py-2.5 text-sm"
                />
              </div>
            )}
          </div>
        )}

        {step === 3 && (
          <div className="flex flex-col gap-4">
            <div>
              <label className="mb-1 block text-xs font-semibold text-ink-faint">{isAr ? "عنوان الطلب" : "Request title"}</label>
              <Input value={title} onChange={(e) => setTitle(e.target.value)} maxLength={300} />
            </div>
            <div>
              <label className="mb-1 block text-xs font-semibold text-ink-faint">{isAr ? "تفاصيل القضية" : "Case details"}</label>
              <textarea
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={6}
                className="w-full rounded-md border border-border bg-surface-raised p-3 text-sm text-ink placeholder:text-ink-faint focus:border-seal focus:outline-none focus:ring-2 focus:ring-seal/30"
                placeholder={
                  isAr
                    ? "صف قضيتك بالتفصيل — يقرأ المحامي هذا الوصف قبل الاستشارة."
                    : "Describe your case in detail — the lawyer reads this before the consultation."
                }
              />
            </div>
          </div>
        )}

        {step === 4 && (
          <div>
            <h2 className="mb-4 text-sm font-semibold text-ink-soft">{isAr ? "راجع طلبك قبل الإرسال" : "Review before sending"}</h2>
            <dl className="flex flex-col gap-3 text-sm">
              <div className="flex justify-between border-b border-border pb-2">
                <dt className="text-ink-faint">{isAr ? "المحامي" : "Lawyer"}</dt>
                <dd className="font-medium">{lawyer.fullName}</dd>
              </div>
              <div className="flex justify-between border-b border-border pb-2">
                <dt className="text-ink-faint">{isAr ? "النوع" : "Type"}</dt>
                <dd className="font-medium">{typeLabel(consultationType!)}</dd>
              </div>
              {consultationType !== "Written" && (
                <div className="flex justify-between border-b border-border pb-2">
                  <dt className="text-ink-faint">{isAr ? "المدة" : "Length"}</dt>
                  <dd className="font-medium">{isAr ? `${durationMinutes} دقيقة` : `${durationMinutes} min`}</dd>
                </div>
              )}
              {consultationType === "Scheduled" && (
                <div className="flex justify-between border-b border-border pb-2">
                  <dt className="text-ink-faint">{isAr ? "الوقت" : "Time"}</dt>
                  <dd className="font-medium">{scheduledStart}</dd>
                </div>
              )}
              <div>
                <dt className="mb-1 text-ink-faint">{isAr ? "العنوان" : "Title"}</dt>
                <dd className="font-medium">{title}</dd>
              </div>
              <div>
                <dt className="mb-1 text-ink-faint">{isAr ? "التفاصيل" : "Details"}</dt>
                <dd className="whitespace-pre-wrap text-ink-soft">{description}</dd>
              </div>
            </dl>
            {error && <p className="mt-4 text-sm text-rubric">{error}</p>}
          </div>
        )}

        <div className="mt-6 flex items-center justify-between border-t border-border pt-4">
          <Button variant="ghost" onClick={back} disabled={step === 1}>
            {isAr ? <ArrowRight className="h-4 w-4" /> : <ArrowLeft className="h-4 w-4" />}
            {isAr ? "السابق" : "Back"}
          </Button>
          {step < 4 ? (
            <Button onClick={next} disabled={!canProceed}>
              {isAr ? "التالي" : "Next"}
              {isAr ? <ArrowLeft className="h-4 w-4" /> : <ArrowRight className="h-4 w-4" />}
            </Button>
          ) : (
            <Button onClick={() => submitMutation.mutate()} disabled={submitMutation.isPending}>
              {isAr ? "إرسال الطلب" : "Send request"}
            </Button>
          )}
        </div>
      </Card>
    </AppShell>
  );
}
```

- [ ] **Step 2: Wire the route in `App.tsx`**

Add the import near the other page imports:

```tsx
import NewConsultationRequest from "./pages/NewConsultationRequest";
```

Add the route immediately after the existing `/lawyers/:slug` route:

```tsx
<Route
  path="/lawyers/:slug/consult"
  element={
    <RequireAuth>
      <NewConsultationRequest />
    </RequireAuth>
  }
/>
```

- [ ] **Step 3: Wire the button in `LawyerProfile.tsx`**

Add `Link` to the existing `react-router-dom` import (currently only `useParams`):

```tsx
import { Link, useParams } from "react-router-dom";
```

Replace the dead button:

```tsx
<Button className="w-full justify-center">{isAr ? "استشر الآن" : "Consult Now"}</Button>
```

with a version gated on pricing existing (today it renders even when `lawyer.pricing` is null,
right below a pricing card that's already correctly hidden in that case — same guard, applied
consistently):

```tsx
{lawyer.pricing && (
  <Link to={`/lawyers/${lawyer.slug}/consult`}>
    <Button className="w-full justify-center">{isAr ? "استشر الآن" : "Consult Now"}</Button>
  </Link>
)}
```

- [ ] **Step 4: Build and lint**

```bash
pnpm --filter @law-portal/web-client build
pnpm --filter @law-portal/web-client lint
```

Expected: both clean.

- [ ] **Step 5: Walk through one full path in the browser**

With the web-client dev server and API running (API must already be restarted from Task 1), and
logged in as a client (see the OTP-login flow — the dev OTP is logged to the API's log file):

```bash
playwright-cli open http://localhost:5173/lawyers/<a-verified-lawyer-with-pricing-slug>
playwright-cli click <ref-of-Consult-Now-button>
```

Expected: navigates to `/lawyers/<slug>/consult`, shows "Consultation with <name>". Click
through: pick "Written consultation" → Next (should skip straight to step 3, "Details") → fill a
title and description → Next → step 4 review shows the lawyer name, type, title, and description
correctly → click "Send request" → lands on `/orders/<new-id>` and that page's existing payment
card is visible.

- [ ] **Step 6: Commit**

```bash
git add apps/web-client/src/pages/NewConsultationRequest.tsx apps/web-client/src/App.tsx apps/web-client/src/pages/LawyerProfile.tsx
git commit -m "Build the consultation-booking wizard

Fixes the dead 'Consult Now' button — LawyerProfile had no onClick handler
and web-client had no consultation-booking UI at all, despite the backend
fully supporting it. Mirrors NewBiddingRequest.tsx's wizard shape; hands off
to the existing OrderDetail payment flow unchanged."
```

---

### Task 4: Verify the remaining paths and the full cross-role handoff

**Files:** none (verification only).

**Interfaces:** none — this task exercises Tasks 1–3's surface, it doesn't add any.

- [ ] **Step 1: Instant call, with duration**

Repeat Task 3 Step 5's walkthrough but pick "Call now" at step 1. Expected: step 2 ("Length")
appears (not skipped); pick 30 minutes; step 4's review shows "Length: 30 min" and no "Time" row;
submit succeeds and lands on `/orders/<id>`.

- [ ] **Step 2: Scheduled call, with date/time**

Same walkthrough, pick "Schedule a call" at step 1, a duration at step 2, and fill the
`datetime-local` input that appears alongside it. Expected: step 4's review shows both "Length"
and "Time" rows with the values entered; submit succeeds.

Then confirm the scheduled time actually persisted:

```bash
curl -s "http://localhost:5280/api/v1/client/requests/<id>" -H "Authorization: Bearer <client-token>" | python3 -m json.tool
```

Expected: `scheduledStartUtc` matches what was entered (converted to UTC).

- [ ] **Step 3: Multi-specialty lawyer shows the specialty picker; single-specialty doesn't**

Find one seeded lawyer with more than one specialty and one with exactly one:

```bash
docker exec law-portal-mysql mysql -ulawportal -plawportal_dev_only lawportal --default-character-set=utf8mb4 -e "
  SELECT lp.Slug, COUNT(*) AS specialty_count
  FROM lawyer_profiles lp JOIN lawyer_specialties ls ON ls.LawyerProfileId = lp.Id
  WHERE lp.IsVerified = 1 GROUP BY lp.Id ORDER BY specialty_count DESC LIMIT 1;"
docker exec law-portal-mysql mysql -ulawportal -plawportal_dev_only lawportal --default-character-set=utf8mb4 -e "
  SELECT lp.Slug, COUNT(*) AS specialty_count
  FROM lawyer_profiles lp JOIN lawyer_specialties ls ON ls.LawyerProfileId = lp.Id
  WHERE lp.IsVerified = 1 GROUP BY lp.Id HAVING specialty_count = 1 LIMIT 1;"
```

Visit each one's `/consult` page. Expected: the multi-specialty lawyer's step 1 shows the chip
picker and "Next" stays disabled until one is picked; the single-specialty lawyer's step 1 has no
picker and "Next" enables as soon as a type is chosen.

- [ ] **Step 4: The lawyer actually receives it — the gap this whole feature closes**

Log in as the lawyer whose profile was booked in Step 1 (or any lawyer used above). Go to
`/requests` in `web-lawyer`. Expected: the new consultation request appears with status
"Awaiting acceptance" (this list and its Accept/Decline/Complete buttons were already fully
verified working in the original test cycle — this step confirms they now receive *real* data
from the UI instead of requiring a direct API call, which is the actual bug being fixed).

- [ ] **Step 5: Pay and confirm the full loop**

From the client's `/orders/<id>` page, pay (card, via the dev fake-gateway — "Pay successfully").
Then, as the lawyer, accept the request and mark it complete. Expected: identical behavior to
what was already verified for the API-seeded consultation request during the original test
cycle — accept moves status to "In progress" and shows the chat link; complete moves it to
"Completed" and releases the payout. No new code is involved in this step; it's confirming the
new entry point feeds the already-correct downstream flow.

- [ ] **Step 6: No commit needed**

This task is verification-only. If any step surfaces a real defect, fix it as part of the task
whose code is at fault, re-run that task's own verification, and only then continue here.
