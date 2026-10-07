import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Pencil } from "lucide-react";
import { Button, Card, Ltr } from "@law-portal/ui";
import { useTranslation, formatCurrency } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { RegistrationFeeCard } from "../components/RegistrationFeeCard";
import { getAdminSubscriptionPlans, updateSubscriptionPlan, type AdminSubscriptionPlanDto } from "../lib/subscriptionsApi";

function PlanCard({ plan, isAr }: { plan: AdminSubscriptionPlanDto; isAr: boolean }) {
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState(false);
  const [monthlyPrice, setMonthlyPrice] = useState(String(plan.monthlyPrice));
  const [commissionOverride, setCommissionOverride] = useState(plan.commissionPercentageOverride?.toString() ?? "");
  const [includesBroadcastBidding, setIncludesBroadcastBidding] = useState(plan.includesBroadcastBidding);
  const [isActive, setIsActive] = useState(plan.isActive);

  const mutation = useMutation({
    mutationFn: () =>
      updateSubscriptionPlan({
        ...plan,
        monthlyPrice: Number(monthlyPrice),
        commissionPercentageOverride: commissionOverride === "" ? null : Number(commissionOverride),
        includesBroadcastBidding,
        isActive,
      }),
    onSuccess: () => {
      setEditing(false);
      void queryClient.invalidateQueries({ queryKey: ["adminSubscriptionPlans"] });
    },
  });

  return (
    <Card className={plan.isActive ? undefined : "opacity-60"}>
      <div className="mb-3 flex items-center justify-between">
        <h2 className="font-display text-lg font-bold">{isAr ? plan.nameAr : plan.nameEn}</h2>
        {!editing && (
          <button onClick={() => setEditing(true)} className="text-ink-faint hover:text-seal">
            <Pencil className="h-4 w-4" />
          </button>
        )}
      </div>

      {editing ? (
        <div className="flex flex-col gap-3">
          <label className="text-xs text-ink-faint">
            {isAr ? "السعر الشهري (ر.س.)" : "Monthly price (SAR)"}
            <input
              type="number"
              value={monthlyPrice}
              onChange={(e) => setMonthlyPrice(e.target.value)}
              className="mt-1 w-full rounded-md border border-border bg-surface-raised px-3 py-2 text-sm"
            />
          </label>
          <label className="text-xs text-ink-faint">
            {isAr ? "نسبة العمولة البديلة (اتركها فارغة للسياسة العامة)" : "Commission override % (blank = global policy)"}
            <input
              type="number"
              value={commissionOverride}
              onChange={(e) => setCommissionOverride(e.target.value)}
              className="mt-1 w-full rounded-md border border-border bg-surface-raised px-3 py-2 text-sm"
            />
          </label>
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" checked={includesBroadcastBidding} onChange={(e) => setIncludesBroadcastBidding(e.target.checked)} />
            {isAr ? "يشمل عروض المزايدة العامة" : "Includes broadcast bidding leads"}
          </label>
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />
            {isAr ? "خطة نشطة (قابلة للبيع)" : "Active (sellable)"}
          </label>
          <div className="flex gap-2">
            <Button onClick={() => mutation.mutate()} disabled={mutation.isPending}>
              {isAr ? "حفظ" : "Save"}
            </Button>
            <Button variant="ghost" onClick={() => setEditing(false)}>
              {isAr ? "إلغاء" : "Cancel"}
            </Button>
          </div>
        </div>
      ) : (
        <>
          <p className="mb-3 font-mono text-xl font-bold text-ink">
            <Ltr>{plan.monthlyPrice === 0 ? (isAr ? "مجاني" : "Free") : formatCurrency(plan.monthlyPrice)}</Ltr>
          </p>
          <ul className="mb-3 flex flex-col gap-1 text-xs text-ink-soft">
            <li>{isAr ? `عمولة: ${plan.commissionPercentageOverride ?? "السياسة العامة"}${plan.commissionPercentageOverride ? "%" : ""}` : `Commission: ${plan.commissionPercentageOverride != null ? plan.commissionPercentageOverride + "%" : "global policy"}`}</li>
            <li>{plan.includesBroadcastBidding ? (isAr ? "يشمل عروض المزايدة العامة" : "Includes broadcast bidding") : isAr ? "عروض بالاسم فقط" : "Targeted only"}</li>
            <li>{isAr ? `${plan.activeSubscriberCount} مشترك نشط` : `${plan.activeSubscriberCount} active subscribers`}</li>
            {!plan.isActive && <li className="text-rubric">{isAr ? "غير نشطة — غير معروضة للمحامين" : "Inactive — not shown to lawyers"}</li>}
          </ul>
        </>
      )}
    </Card>
  );
}

export default function Subscriptions() {
  const { i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const query = useQuery({ queryKey: ["adminSubscriptionPlans"], queryFn: getAdminSubscriptionPlans });

  return (
    <AppShell>
      <RegistrationFeeCard />

      <h1 className="mb-6 font-display text-2xl font-bold">{isAr ? "خطط الاشتراك" : "Subscription Plans"}</h1>

      {query.isLoading && <p className="text-sm text-ink-faint">{isAr ? "جارٍ التحميل…" : "Loading…"}</p>}

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        {query.data?.map((p) => <PlanCard key={p.id} plan={p} isAr={isAr} />)}
      </div>
    </AppShell>
  );
}
