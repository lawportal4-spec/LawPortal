# Consultation booking flow — design

## Context

The UI test cycle (see conversation history) found that `LawyerProfile.tsx`'s "Consult Now"
button has no `onClick` handler at all, and — after reading every page in `web-client` — that
there is **no consultation-booking UI anywhere in the app**. This blocks an entire transaction
type: a client browsing lawyer profiles has no way to start a consultation through the interface.
The only way a consultation request was created during testing was by calling the API directly.

The backend already fully supports this (`CreateConsultationDraftCommand`,
`UpdateConsultationDetailsCommand`, submit, checkout) — verified by driving a real consultation
end to end via direct API calls: draft → details → submit → checkout → payment → lawyer accept →
complete → payout release. The gap is purely a missing frontend flow, plus one small DTO gap
(see below). Intended outcome: the pricing card already shown on every lawyer's profile becomes
actually bookable, for all three consultation types the backend supports.

## Scope

All three consultation types, all four price tiers already displayed on the profile page:
Written, Instant call (15/30/45 min), Scheduled call (15/30/45 min + a chosen time). Confirmed
with the user rather than shipping a smaller slice, since the pricing card already advertises all
four options — shipping less would mean removing already-visible promises from the UI.

## Approach

A dedicated wizard page at `/lawyers/:slug/consult`, reached from `LawyerProfile.tsx`'s now-real
"Consult Now" button. This mirrors `NewBiddingRequest.tsx`'s existing pattern exactly — same
`StepProgress` + `Card` shell, same visual language — rather than introducing a second,
inline-panel interaction pattern the rest of the codebase doesn't use. Considered and rejected:
an inline expandable panel directly on the profile page (fewer navigations, but a new pattern
with no precedent, more surface for new bugs, and this codebase already has exactly one working
"how a multi-step booking works" pattern worth reusing rather than duplicating).

## Backend change (small, necessary)

`GetLawyerProfileQuery` / `LawyerProfileDetailDto`
(`api/src/LawPortal.Application/Lawyers/Queries/GetLawyerProfileQuery.cs`,
`api/src/LawPortal.Application/Lawyers/Dtos/LawyerDtos.cs`) currently project only
`SpecialtyNamesAr`/`SpecialtyNamesEn` — no specialty IDs. Booking requires a `specialtyId`.
Add `IReadOnlyList<LawyerSpecialtyDto>` (`Id`, `NameAr`, `NameEn`) alongside — additive, no
existing consumer of this DTO breaks. Mirror the shape into
`apps/web-client/src/lib/api.ts`'s `LawyerProfileDetailDto` TS interface and
`getLawyerProfile()`'s consumer in `LawyerProfile.tsx`.

## Flow

Route: `/lawyers/:slug/consult`. Header throughout: "Booking with [lawyer full name]" so identity
from the profile page isn't lost across the navigation — the one piece of context a generic
wizard page doesn't otherwise carry.

**Step 1 — Type.** Three cards: Written / Call now / Schedule a call. Each shows its real price
from the lawyer's `Pricing` (already fetched via the existing `getLawyerProfile` call, passed
through via route state or refetched by slug — refetch, to survive a page reload/direct link,
matching how `NewBiddingRequest.tsx` treats `?serviceId=` as a hint to refetch-and-preselect
rather than trusting passed-in state).

**Step 2 — Length** (skipped entirely for Written). 15 / 30 / 45 min, price shown per option from
`Pricing.price15/30/45`. If "Schedule a call" was picked in step 1, this step also asks for a
date + time via a native `<input type="datetime-local">` — matching the native date inputs
already used in `Register.tsx` and `Settings.tsx`, no new date-picker dependency.

**Step 3 — Details.** Title + description. Required — the backend's submit endpoint rejects
without both (confirmed directly: `"Title and details are required before submitting."`).

Out of scope: how an "Instant" call actually connects (voice/video mechanics, LiveKit
integration already present in `docker-compose.yml`) — this flow only submits the request with
the correct type/duration; whatever happens after acceptance is existing backend/infra behavior,
not something this page builds.

**Step 4 — Review → Submit.** Summarizes type/length/time/specialty/title/description exactly
like `NewBiddingRequest.tsx`'s own review step. On submit: `createConsultationDraft` →
`updateConsultationDetails` → the shared submit endpoint (`POST
/api/v1/client/requests/{id}/submit`, same one every request kind uses) → navigate to
`/orders/:id`.

**Specialty selection.** A lawyer may have more than one specialty. If exactly one, use it
directly with no extra prompt. If more than one, step 1 additionally asks which specialty this
consultation is about (chips, matching the specialty-picker already used in
`NewBiddingRequest.tsx`'s step 2) — using the new `LawyerSpecialtyDto.Id` values from the backend
change above.

**Payment — no new code.** After submit, the client lands on the existing `/orders/:id`
(`OrderDetail.tsx`), whose payment card already renders correctly for a `Submitted` consultation
request with a known `subtotal` — this was verified end-to-end during the test cycle without any
changes to that file. `OrderDetail.tsx` is touched only if a genuine display gap is found while
wiring this up; none is currently known.

## New frontend code

- `apps/web-client/src/lib/consultationApi.ts` — new file, mirrors `biddingApi.ts`'s shape:
  `createConsultationDraft(params)`, `updateConsultationDetails(id, title, description, scheduledStartUtc?)`.
- `apps/web-client/src/pages/NewConsultationRequest.tsx` — new page, the 4-step wizard above.
- Route addition in `App.tsx`: `/lawyers/:slug/consult`, wrapped in `RequireAuth` like
  `/bidding/new`.
- `LawyerProfile.tsx`: give "Consult Now" a real `onClick`/`Link` to the new route.
- Copy: inline `isAr ? … : …` ternaries, matching `NewBiddingRequest.tsx`'s own convention (that
  file doesn't use the shared `packages/i18n` locale files either — staying consistent with the
  sibling wizard rather than introducing a second copy convention for this one page).

## Reused as-is, no changes

`CreateConsultationDraftCommand`, `UpdateConsultationDetailsCommand`, the submit endpoint, the
checkout/payment flow, `StepProgress`, `Card`, `Button`, `Chip`, `Input` from `packages/ui`.

## Verification

- `pnpm --filter @law-portal/web-client build` and `lint` clean.
- Drive the full flow with `playwright-cli` exactly as the test cycle did for bidding: pick each
  of the three types at least once, confirm the length/scheduling step only appears when it
  should, submit, confirm landing on `/orders/:id` shows the same payment card already verified
  working, complete a payment, and confirm the lawyer sees it correctly in `Requests.tsx` /
  `RequestDetail.tsx` (already-verified accept/decline/complete buttons — this flow is what feeds
  them real data going forward, closing the gap that forced direct API calls during testing).
- Confirm the multi-specialty picker only appears for a lawyer with more than one specialty (spot
  check against real seeded data).
