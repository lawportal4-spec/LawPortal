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
