import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { Card } from "@law-portal/ui";
import { useTranslation, formatCurrency } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { getDashboard } from "../lib/lawyerApi";
import { getAwardedBiddingRequests, getBiddingFeed } from "../lib/biddingApi";

function StatCard({ label, value, to }: { label: string; value: string; to?: string }) {
  const content = (
    <Card className="flex flex-col gap-1">
      <span className="text-xs text-ink-faint">{label}</span>
      <span className="font-mono text-2xl font-bold text-ink">{value}</span>
    </Card>
  );
  return to ? <Link to={to}>{content}</Link> : content;
}

export default function Dashboard() {
  const { i18n } = useTranslation();
  const isAr = i18n.language === "ar";

  const query = useQuery({ queryKey: ["lawyerDashboard"], queryFn: getDashboard });
  // Lightweight counts only (page size 1) — just enough for a stat card, not a full fetch.
  const biddingFeedQuery = useQuery({ queryKey: ["biddingFeed", "count"], queryFn: () => getBiddingFeed(1, 1) });
  const biddingAwardedQuery = useQuery({
    queryKey: ["biddingAwarded", "count"],
    queryFn: () => getAwardedBiddingRequests(undefined, 1, 1),
  });

  return (
    <AppShell>
      <h1 className="mb-6 font-display text-2xl font-bold">{isAr ? "لوحة التحكم" : "Dashboard"}</h1>

      {query.isLoading && <p className="text-sm text-ink-faint">{isAr ? "جارٍ التحميل…" : "Loading…"}</p>}

      {query.data && (
        <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
          <StatCard label={isAr ? "بانتظار القبول" : "Awaiting acceptance"} value={String(query.data.awaitingAcceptance)} to="/requests?status=Paid" />
          <StatCard label={isAr ? "قيد التنفيذ" : "In progress"} value={String(query.data.inProgress)} to="/requests?status=InProgress" />
          <StatCard label={isAr ? "مكتملة هذا الشهر" : "Completed this month"} value={String(query.data.completedThisMonth)} />
          <StatCard label={isAr ? "رسائل غير مقروءة" : "Unread message threads"} value={String(query.data.unreadMessageThreads)} />
          <StatCard label={isAr ? "أرباح محتجزة" : "Earnings held"} value={formatCurrency(query.data.earningsHeld)} to="/earnings" />
          <StatCard label={isAr ? "أرباح مُحرَّرة هذا الشهر" : "Released this month"} value={formatCurrency(query.data.earningsReleasedThisMonth)} to="/earnings" />
          <StatCard label={isAr ? "متوسط التقييم" : "Average rating"} value={query.data.avgRating.toFixed(1)} to="/earnings" />
          <StatCard label={isAr ? "عدد التقييمات" : "Rating count"} value={String(query.data.ratingCount)} />
          <StatCard
            label={isAr ? "طلبات عروض أسعار مفتوحة" : "Open bidding requests"}
            value={String(biddingFeedQuery.data?.totalCount ?? "…")}
            to="/bidding"
          />
          <StatCard
            label={isAr ? "طلبات فائزة" : "Awarded requests"}
            value={String(biddingAwardedQuery.data?.totalCount ?? "…")}
            to="/bidding/awarded"
          />
        </div>
      )}
    </AppShell>
  );
}
