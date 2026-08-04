import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { CheckCircle2, TriangleAlert } from "lucide-react";
import { Card, Ltr } from "@law-portal/ui";
import { useTranslation, formatCurrency } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { getDashboard } from "../lib/adminApi";

const STATUS_LABEL: Record<string, { ar: string; en: string }> = {
  Draft: { ar: "مسودة", en: "Draft" },
  Submitted: { ar: "مُقدَّم", en: "Submitted" },
  Awarded: { ar: "تم الترسية", en: "Awarded" },
  Paid: { ar: "مدفوع", en: "Paid" },
  InProgress: { ar: "قيد التنفيذ", en: "In progress" },
  Completed: { ar: "مكتمل", en: "Completed" },
  Cancelled: { ar: "ملغى", en: "Cancelled" },
  Refunded: { ar: "مُسترجَع", en: "Refunded" },
};

function StatCard({ label, value, to }: { label: string; value: string; to?: string }) {
  const content = (
    <Card className="flex flex-col gap-1">
      <span className="text-xs text-ink-faint">{label}</span>
      <span className="font-mono text-2xl font-bold text-ink">
        <Ltr>{value}</Ltr>
      </span>
    </Card>
  );
  return to ? <Link to={to}>{content}</Link> : content;
}

export default function Dashboard() {
  const { i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const query = useQuery({ queryKey: ["adminDashboard"], queryFn: getDashboard });
  const d = query.data;

  return (
    <AppShell>
      <h1 className="mb-6 font-display text-2xl font-bold">{isAr ? "لوحة التحكم" : "Dashboard"}</h1>

      {query.isLoading && <p className="text-sm text-ink-faint">{isAr ? "جارٍ التحميل…" : "Loading…"}</p>}

      {d && (
        <>
          <div className="mb-6 grid grid-cols-2 gap-4 sm:grid-cols-4">
            <StatCard label={isAr ? "العملاء" : "Clients"} value={String(d.totalClients)} />
            <StatCard label={isAr ? "المحامون الموثّقون" : "Verified lawyers"} value={String(d.totalVerifiedLawyers)} />
            <StatCard label={isAr ? "طلبات توثيق معلّقة" : "Pending verifications"} value={String(d.pendingLawyerVerifications)} to="/lawyers" />
            <StatCard label={isAr ? "عدد المسؤولين" : "Admins"} value={String(d.totalAdmins)} to="/users" />
            <StatCard label={isAr ? "عمولة هذا الشهر" : "Commission this month"} value={formatCurrency(d.commissionRevenueThisMonth)} to="/ledger" />
            <StatCard label={isAr ? "إيراد الاشتراكات هذا الشهر" : "Subscription revenue this month"} value={formatCurrency(d.subscriptionRevenueThisMonth)} to="/subscriptions" />
          </div>

          <Card className="mb-6 flex items-center gap-3">
            {d.ledgerIsBalanced ? (
              <>
                <CheckCircle2 className="h-5 w-5 shrink-0 text-seal" />
                <p className="text-sm text-ink-soft">
                  {isAr ? "دفتر الأستاذ متوازن — إجمالي المدين يطابق إجمالي الدائن." : "The ledger is balanced — total debits match total credits."}
                </p>
              </>
            ) : (
              <>
                <TriangleAlert className="h-5 w-5 shrink-0 text-rubric" />
                <p className="text-sm text-rubric">
                  {isAr ? "تحذير: دفتر الأستاذ غير متوازن. راجع صفحة التسوية فورًا." : "Warning: the ledger is not balanced. Check the reconciliation page immediately."}
                </p>
              </>
            )}
            <Link to="/ledger" className="ms-auto text-sm font-medium text-seal">
              {isAr ? "عرض التفاصيل" : "View details"}
            </Link>
          </Card>

          <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
            <Card>
              <h2 className="mb-3 text-sm font-semibold text-ink-soft">{isAr ? "الطلبات حسب الحالة" : "Requests by status"}</h2>
              <div className="flex flex-col gap-2">
                {d.requestsByStatus.map((r) => (
                  <div key={r.status} className="flex items-center justify-between text-sm">
                    <span className="text-ink-soft">{STATUS_LABEL[r.status] ? (isAr ? STATUS_LABEL[r.status].ar : STATUS_LABEL[r.status].en) : r.status}</span>
                    <span className="font-mono font-medium text-ink">
                      <Ltr>{r.count}</Ltr>
                    </span>
                  </div>
                ))}
              </div>
            </Card>

            <Card>
              <h2 className="mb-3 text-sm font-semibold text-ink-soft">{isAr ? "الاشتراكات النشطة حسب الخطة" : "Active subscriptions by plan"}</h2>
              {d.activeSubscriptionsByPlan.length === 0 ? (
                <p className="text-sm text-ink-faint">{isAr ? "لا توجد اشتراكات مدفوعة نشطة حاليًا." : "No active paid subscriptions right now."}</p>
              ) : (
                <div className="flex flex-col gap-2">
                  {d.activeSubscriptionsByPlan.map((s) => (
                    <div key={s.planNameEn} className="flex items-center justify-between text-sm">
                      <span className="text-ink-soft">{s.planNameEn}</span>
                      <span className="font-mono font-medium text-ink">
                        <Ltr>{s.count}</Ltr>
                      </span>
                    </div>
                  ))}
                </div>
              )}
            </Card>
          </div>
        </>
      )}
    </AppShell>
  );
}
