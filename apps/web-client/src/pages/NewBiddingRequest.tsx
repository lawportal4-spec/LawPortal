import { useMemo, useState } from "react";
import { useMutation, useQuery, keepPreviousData } from "@tanstack/react-query";
import { useNavigate, useSearchParams } from "react-router-dom";
import { ArrowLeft, ArrowRight, Check, Radio, Search, Users, Megaphone } from "lucide-react";
import { Button, Card, Chip, Input, StepProgress } from "@law-portal/ui";
import { useTranslation } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { api, getServiceCategories, getSpecialties, searchLawyers, type SpecialtyDto } from "../lib/api";
import { createBiddingDraft, updateBiddingDetails, type BidSendMethod } from "../lib/biddingApi";

/**
 * The 6-step bidding wizard the plan describes: service → specialty (+sub-specialty) →
 * title/details → send method → lawyer targeting → review+submit. Steps 1–2 and 4–5 each
 * collapse into one backend call (CreateBiddingDraftCommand) exactly like the consultation
 * wizard's own 2-step collapse — this component just renders them as separate screens.
 */
export default function NewBiddingRequest() {
  const { i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const preselectedServiceId = searchParams.get("serviceId");

  const [step, setStep] = useState(1);
  const [serviceId, setServiceId] = useState<number | null>(preselectedServiceId ? Number(preselectedServiceId) : null);
  const [specialtyId, setSpecialtyId] = useState<number | null>(null);
  const [subSpecialtyId, setSubSpecialtyId] = useState<number | null>(null);
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [sendMethod, setSendMethod] = useState<BidSendMethod>("Broadcast");
  const [targetedIds, setTargetedIds] = useState<string[]>([]);
  const [lawyerSearch, setLawyerSearch] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submittedRequestId, setSubmittedRequestId] = useState<string | null>(null);

  const categoriesQuery = useQuery({ queryKey: ["catalog", "categories"], queryFn: getServiceCategories });
  const specialtiesQuery = useQuery({ queryKey: ["catalog", "specialties"], queryFn: getSpecialties });

  const biddingServices = useMemo(
    () =>
      (categoriesQuery.data ?? []).flatMap((c) =>
        c.services
          .filter((s) => s.pricingModel === "CompetitiveBidding")
          .map((s) => ({ ...s, categoryNameAr: c.nameAr, categoryNameEn: c.nameEn })),
      ),
    [categoriesQuery.data],
  );
  const selectedService = biddingServices.find((s) => s.id === serviceId);
  const selectedSpecialty = specialtiesQuery.data?.find((s) => s.id === specialtyId);

  const lawyersQuery = useQuery({
    queryKey: ["lawyers", "targeting", lawyerSearch, specialtyId],
    queryFn: () => searchLawyers({ q: lawyerSearch || undefined, specialtyId: specialtyId ?? undefined, page: 1, pageSize: 12 }),
    enabled: step === 5 && sendMethod === "Targeted",
    placeholderData: keepPreviousData,
  });

  const submitMutation = useMutation({
    mutationFn: async () => {
      const requestId = await createBiddingDraft({
        serviceId: serviceId!,
        specialtyId: specialtyId!,
        subSpecialtyId,
        sendMethod,
        targetedLawyerProfileIds: sendMethod === "Targeted" ? targetedIds : null,
      });
      await updateBiddingDetails(requestId, title, description);
      return requestId;
    },
    onSuccess: async (requestId) => {
      setError(null);
      // The shared submit endpoint (SubmitRequestCommand) is the same one every request
      // shape uses — this is where a broadcast send publishes its one fan-out message.
      await api.post(`/api/v1/client/requests/${requestId}/submit`);
      setSubmittedRequestId(requestId);
    },
    onError: () => setError(isAr ? "تعذّر إرسال الطلب." : "Could not submit the request."),
  });

  const steps = [
    { label: isAr ? "الخدمة" : "Service" },
    { label: isAr ? "التخصص" : "Specialty" },
    { label: isAr ? "التفاصيل" : "Details" },
    { label: isAr ? "طريقة الإرسال" : "Send method" },
    { label: isAr ? "المحامون" : "Lawyers" },
    { label: isAr ? "المراجعة" : "Review" },
  ];

  const canProceed =
    (step === 1 && serviceId != null) ||
    (step === 2 && specialtyId != null) ||
    (step === 3 && title.trim().length > 0 && description.trim().length > 0) ||
    (step === 4 && !!sendMethod) ||
    (step === 5 && (sendMethod === "Broadcast" || targetedIds.length > 0)) ||
    step === 6;

  function next() {
    // Broadcast skips the lawyer-targeting screen entirely — there's nobody to pick.
    if (step === 4 && sendMethod === "Broadcast") return setStep(6);
    setStep((s) => Math.min(6, s + 1));
  }
  function back() {
    if (step === 6 && sendMethod === "Broadcast") return setStep(4);
    setStep((s) => Math.max(1, s - 1));
  }

  if (submittedRequestId) {
    return (
      <AppShell>
        <div className="mx-auto max-w-md text-center">
          <div className="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-full bg-seal-tint text-seal">
            <Check className="h-7 w-7" />
          </div>
          <h1 className="mb-2 font-display text-2xl font-bold">{isAr ? "أُرسل طلبك بنجاح" : "Your request is out for offers"}</h1>
          <p className="mb-6 text-sm text-ink-faint">
            {sendMethod === "Broadcast"
              ? isAr
                ? "سيصل طلبك إلى المحامين المتخصصين في هذا المجال، وستصلك عروضهم هنا."
                : "Your request has gone out to lawyers who specialize in this area — offers will appear here as they arrive."
              : isAr
                ? "أُرسل طلبك إلى المحامين الذين اخترتهم، وستصلك عروضهم هنا."
                : "Your request has gone to the lawyers you picked — their offers will appear here."}
          </p>
          <Button onClick={() => navigate(`/orders/${submittedRequestId}`)}>
            {isAr ? "عرض الطلب" : "View request"}
          </Button>
        </div>
      </AppShell>
    );
  }

  return (
    <AppShell>
      <h1 className="mb-2 font-display text-2xl font-bold">
        {isAr ? "طلب عرض أسعار من المحامين" : "Request offers from lawyers"}
      </h1>
      <p className="mb-6 text-sm text-ink-faint">
        {isAr
          ? "صف قضيتك، واختر من يراها، ثم فاوض على السعر مباشرة مع المحامين."
          : "Describe your case, choose who sees it, then negotiate price directly with lawyers."}
      </p>

      <div className="mb-8">
        <StepProgress steps={steps} current={step} />
      </div>

      <Card className="mx-auto max-w-xl">
        {step === 1 && (
          <div>
            <h2 className="mb-4 text-sm font-semibold text-ink-soft">
              {isAr ? "ما نوع الخدمة التي تحتاجها؟" : "What kind of service do you need?"}
            </h2>
            {categoriesQuery.isLoading && <p className="text-sm text-ink-faint">{isAr ? "جارٍ التحميل…" : "Loading…"}</p>}
            <div className="flex flex-col gap-2">
              {biddingServices.map((s) => (
                <button
                  key={s.id}
                  onClick={() => setServiceId(s.id)}
                  className={
                    "flex items-center justify-between rounded-md border px-4 py-3 text-start text-sm transition-colors " +
                    (serviceId === s.id ? "border-seal bg-seal-tint text-seal-strong" : "border-border hover:border-seal")
                  }
                >
                  <span>
                    <span className="block font-medium">{isAr ? s.nameAr : s.nameEn}</span>
                    <span className="block text-xs text-ink-faint">{isAr ? s.categoryNameAr : s.categoryNameEn}</span>
                  </span>
                  {serviceId === s.id && <Check className="h-4 w-4" />}
                </button>
              ))}
            </div>
          </div>
        )}

        {step === 2 && (
          <div>
            <h2 className="mb-4 text-sm font-semibold text-ink-soft">
              {isAr ? "ما التخصص القانوني المناسب؟" : "Which legal specialty fits best?"}
            </h2>
            <div className="mb-4 flex flex-wrap gap-2">
              {(specialtiesQuery.data ?? []).map((s: SpecialtyDto) => (
                <button key={s.id} onClick={() => { setSpecialtyId(s.id); setSubSpecialtyId(null); }}>
                  <Chip
                    className={specialtyId === s.id ? "!bg-seal !text-seal-on" : "cursor-pointer"}
                  >
                    {isAr ? s.nameAr : s.nameEn}
                  </Chip>
                </button>
              ))}
            </div>
            {selectedSpecialty && selectedSpecialty.subSpecialties.length > 0 && (
              <>
                <h3 className="mb-2 text-xs font-semibold text-ink-faint">
                  {isAr ? "تخصص فرعي (اختياري)" : "Sub-specialty (optional)"}
                </h3>
                <div className="flex flex-wrap gap-2">
                  {selectedSpecialty.subSpecialties.map((sub) => (
                    <button key={sub.id} onClick={() => setSubSpecialtyId(sub.id === subSpecialtyId ? null : sub.id)}>
                      <Chip className={subSpecialtyId === sub.id ? "!bg-seal !text-seal-on" : "cursor-pointer"}>
                        {isAr ? sub.nameAr : sub.nameEn}
                      </Chip>
                    </button>
                  ))}
                </div>
              </>
            )}
          </div>
        )}

        {step === 3 && (
          <div className="flex flex-col gap-4">
            <div>
              <label className="mb-1 block text-xs font-semibold text-ink-faint">
                {isAr ? "عنوان الطلب" : "Request title"}
              </label>
              <Input value={title} onChange={(e) => setTitle(e.target.value)} maxLength={300} />
            </div>
            <div>
              <label className="mb-1 block text-xs font-semibold text-ink-faint">
                {isAr ? "تفاصيل القضية" : "Case details"}
              </label>
              <textarea
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={6}
                className="w-full rounded-md border border-border bg-surface-raised p-3 text-sm text-ink placeholder:text-ink-faint focus:border-seal focus:outline-none focus:ring-2 focus:ring-seal/30"
                placeholder={
                  isAr
                    ? "صف قضيتك بالتفصيل — يقرأ المحامون هذا الوصف لتحديد عروضهم."
                    : "Describe your case in detail — lawyers price their offers from this description."
                }
              />
            </div>
          </div>
        )}

        {step === 4 && (
          <div>
            <h2 className="mb-4 text-sm font-semibold text-ink-soft">
              {isAr ? "كيف تريد إرسال طلبك؟" : "How should we send your request?"}
            </h2>
            <div className="flex flex-col gap-3">
              <button
                onClick={() => setSendMethod("Broadcast")}
                className={
                  "flex items-start gap-3 rounded-md border px-4 py-3 text-start text-sm transition-colors " +
                  (sendMethod === "Broadcast" ? "border-seal bg-seal-tint" : "border-border hover:border-seal")
                }
              >
                <Megaphone className="mt-0.5 h-4 w-4 shrink-0 text-seal" />
                <span>
                  <span className="block font-medium">{isAr ? "إرسال عام" : "Broadcast to all"}</span>
                  <span className="block text-xs text-ink-faint">
                    {isAr
                      ? "يصل طلبك لكل المحامين المتخصصين في هذا المجال."
                      : "Reaches every lawyer who specializes in this area."}
                  </span>
                </span>
              </button>
              <button
                onClick={() => setSendMethod("Targeted")}
                className={
                  "flex items-start gap-3 rounded-md border px-4 py-3 text-start text-sm transition-colors " +
                  (sendMethod === "Targeted" ? "border-seal bg-seal-tint" : "border-border hover:border-seal")
                }
              >
                <Users className="mt-0.5 h-4 w-4 shrink-0 text-seal" />
                <span>
                  <span className="block font-medium">{isAr ? "اختيار محامين محددين" : "Choose specific lawyers"}</span>
                  <span className="block text-xs text-ink-faint">
                    {isAr ? "أنت تحدد من يرى طلبك." : "You choose exactly who sees your request."}
                  </span>
                </span>
              </button>
            </div>
          </div>
        )}

        {step === 5 && sendMethod === "Targeted" && (
          <div>
            <h2 className="mb-3 text-sm font-semibold text-ink-soft">
              {isAr ? `اختر المحامين (${targetedIds.length} محدد)` : `Pick lawyers (${targetedIds.length} selected)`}
            </h2>
            <Input
              icon={<Search className="h-4 w-4" />}
              placeholder={isAr ? "ابحث بالاسم" : "Search by name"}
              value={lawyerSearch}
              onChange={(e) => setLawyerSearch(e.target.value)}
              className="mb-3"
            />
            <div className="flex max-h-72 flex-col gap-2 overflow-y-auto">
              {(lawyersQuery.data?.items ?? []).map((lawyer) => {
                const picked = targetedIds.includes(lawyer.id);
                return (
                  <button
                    key={lawyer.id}
                    onClick={() =>
                      setTargetedIds((ids) => (picked ? ids.filter((id) => id !== lawyer.id) : [...ids, lawyer.id]))
                    }
                    className={
                      "flex items-center justify-between rounded-md border px-3 py-2 text-start text-sm transition-colors " +
                      (picked ? "border-seal bg-seal-tint" : "border-border hover:border-seal")
                    }
                  >
                    <span className="font-medium">{lawyer.fullName}</span>
                    {picked && <Check className="h-4 w-4 text-seal" />}
                  </button>
                );
              })}
              {lawyersQuery.data?.items.length === 0 && (
                <p className="py-4 text-center text-sm text-ink-faint">
                  {isAr ? "لا توجد نتائج." : "No results."}
                </p>
              )}
            </div>
          </div>
        )}

        {step === 6 && (
          <div>
            <h2 className="mb-4 text-sm font-semibold text-ink-soft">
              {isAr ? "راجع طلبك قبل الإرسال" : "Review before sending"}
            </h2>
            <dl className="flex flex-col gap-3 text-sm">
              <div className="flex justify-between border-b border-border pb-2">
                <dt className="text-ink-faint">{isAr ? "الخدمة" : "Service"}</dt>
                <dd className="font-medium">{isAr ? selectedService?.nameAr : selectedService?.nameEn}</dd>
              </div>
              <div className="flex justify-between border-b border-border pb-2">
                <dt className="text-ink-faint">{isAr ? "التخصص" : "Specialty"}</dt>
                <dd className="font-medium">{isAr ? selectedSpecialty?.nameAr : selectedSpecialty?.nameEn}</dd>
              </div>
              <div className="flex justify-between border-b border-border pb-2">
                <dt className="text-ink-faint">{isAr ? "طريقة الإرسال" : "Send method"}</dt>
                <dd className="flex items-center gap-1.5 font-medium">
                  {sendMethod === "Broadcast" ? <Megaphone className="h-3.5 w-3.5" /> : <Radio className="h-3.5 w-3.5" />}
                  {sendMethod === "Broadcast"
                    ? isAr ? "إرسال عام" : "Broadcast to all"
                    : isAr ? `${targetedIds.length} محامٍ محدد` : `${targetedIds.length} lawyers picked`}
                </dd>
              </div>
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
          {step < 6 ? (
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
