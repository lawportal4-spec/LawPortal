import { useState, type ReactNode } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useParams } from "react-router-dom";
import { ArrowLeft, ArrowRight, CheckCircle2, MessageCircle, XCircle } from "lucide-react";
import { Button, Card, StatusPill, Ltr, type RequestStatus } from "@law-portal/ui";
import { useTranslation, formatCurrency, formatDateTime } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { acceptRequest, completeRequest, declineRequest, getIncomingRequestDetail } from "../lib/lawyerApi";

const STATUS_TO_PILL: Record<string, RequestStatus> = {
  Paid: "pendingPayment",
  InProgress: "inProgress",
  Completed: "completed",
  Refunded: "disputed",
};

const STATUS_LABEL: Record<string, { ar: string; en: string }> = {
  Paid: { ar: "بانتظار القبول", en: "Awaiting acceptance" },
  InProgress: { ar: "قيد التنفيذ", en: "In progress" },
  Completed: { ar: "مكتمل", en: "Completed" },
  Refunded: { ar: "مُسترجَع", en: "Refunded" },
};

function Row({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="flex items-center justify-between border-b border-border py-3 text-sm last:border-0">
      <span className="text-ink-faint">{label}</span>
      <span className="font-medium text-ink">{children}</span>
    </div>
  );
}

export default function RequestDetail() {
  const { id } = useParams<{ id: string }>();
  const { i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const queryClient = useQueryClient();
  const [declineReason, setDeclineReason] = useState("");
  const [showDecline, setShowDecline] = useState(false);

  const query = useQuery({
    queryKey: ["incomingRequestDetail", id],
    queryFn: () => getIncomingRequestDetail(id!),
    enabled: !!id,
  });

  const acceptMutation = useMutation({
    mutationFn: () => acceptRequest(id!),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ["incomingRequestDetail", id] }),
  });
  const completeMutation = useMutation({
    mutationFn: () => completeRequest(id!),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ["incomingRequestDetail", id] }),
  });
  const declineMutation = useMutation({
    mutationFn: () => declineRequest(id!, declineReason),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ["incomingRequestDetail", id] }),
  });

  return (
    <AppShell>
      <Link to="/requests" className="mb-6 inline-flex items-center gap-1.5 text-sm text-ink-faint hover:text-ink">
        {isAr ? <ArrowRight className="h-4 w-4" /> : <ArrowLeft className="h-4 w-4" />}
        {isAr ? "الرجوع إلى الطلبات" : "Back to requests"}
      </Link>

      {query.isLoading && <p className="text-sm text-ink-faint">{isAr ? "جارٍ التحميل…" : "Loading…"}</p>}

      {query.data && (
        <div className="mx-auto max-w-2xl">
          <div className="mb-4 flex items-start justify-between gap-4">
            <div>
              <h1 className="font-display text-2xl font-bold">{isAr ? query.data.serviceNameAr : query.data.serviceNameEn}</h1>
              <p className="mt-1 text-sm text-ink-faint">
                <Ltr className="font-mono">{query.data.number}</Ltr>
              </p>
            </div>
            <StatusPill
              status={STATUS_TO_PILL[query.data.status] ?? "draft"}
              label={STATUS_LABEL[query.data.status] ? (isAr ? STATUS_LABEL[query.data.status].ar : STATUS_LABEL[query.data.status].en) : query.data.status}
            />
          </div>

          <Card className="mb-4">
            <Row label={isAr ? "العميل" : "Client"}>{query.data.clientName}</Row>
            <Row label={isAr ? "العنوان" : "Title"}>{query.data.title ?? "—"}</Row>
            {query.data.specialtyNameAr && (
              <Row label={isAr ? "التخصص" : "Specialty"}>{isAr ? query.data.specialtyNameAr : query.data.specialtyNameEn}</Row>
            )}
            {query.data.subtotal != null && (
              <Row label={isAr ? "المبلغ" : "Amount"}>
                <Ltr className="font-mono">{formatCurrency(query.data.subtotal)}</Ltr>
              </Row>
            )}
            <Row label={isAr ? "تاريخ الإنشاء" : "Created"}>
              <Ltr className="font-mono">{formatDateTime(query.data.createdAtUtc)}</Ltr>
            </Row>
          </Card>

          {query.data.description && (
            <Card className="mb-4">
              <h2 className="mb-2 text-sm font-semibold text-ink-soft">{isAr ? "التفاصيل" : "Details"}</h2>
              <p className="whitespace-pre-wrap text-sm text-ink">{query.data.description}</p>
            </Card>
          )}

          {query.data.status === "Paid" && (
            <Card className="mb-4">
              <h2 className="mb-3 text-sm font-semibold text-ink-soft">{isAr ? "إجراء" : "Action"}</h2>
              {!showDecline ? (
                <div className="flex flex-wrap gap-3">
                  <Button onClick={() => acceptMutation.mutate()} disabled={acceptMutation.isPending}>
                    <CheckCircle2 className="h-4 w-4" />
                    {isAr ? "قبول الطلب" : "Accept request"}
                  </Button>
                  <Button variant="danger" onClick={() => setShowDecline(true)}>
                    <XCircle className="h-4 w-4" />
                    {isAr ? "رفض الطلب" : "Decline request"}
                  </Button>
                </div>
              ) : (
                <div className="flex flex-col gap-3">
                  <input
                    value={declineReason}
                    onChange={(e) => setDeclineReason(e.target.value)}
                    placeholder={isAr ? "سبب الرفض (سيتم استرجاع كامل المبلغ للعميل)" : "Reason for declining (client is refunded in full)"}
                    className="rounded-md border border-border bg-surface-raised px-4 py-2.5 text-sm focus:border-seal focus:outline-none focus:ring-2 focus:ring-seal/30"
                  />
                  <div className="flex gap-3">
                    <Button variant="danger" disabled={!declineReason.trim() || declineMutation.isPending} onClick={() => declineMutation.mutate()}>
                      {isAr ? "تأكيد الرفض" : "Confirm decline"}
                    </Button>
                    <Button variant="ghost" onClick={() => setShowDecline(false)}>
                      {isAr ? "إلغاء" : "Cancel"}
                    </Button>
                  </div>
                </div>
              )}
            </Card>
          )}

          {query.data.status === "InProgress" && (
            <Card className="mb-4">
              <h2 className="mb-3 text-sm font-semibold text-ink-soft">{isAr ? "إجراء" : "Action"}</h2>
              <Button onClick={() => completeMutation.mutate()} disabled={completeMutation.isPending}>
                <CheckCircle2 className="h-4 w-4" />
                {isAr ? "تحديد الطلب كمكتمل" : "Mark as complete"}
              </Button>
            </Card>
          )}

          {(query.data.status === "InProgress" || query.data.status === "Completed") && (
            <Link to={`/chat/${query.data.id}`} className="mb-4 block">
              <Card className="flex items-center justify-center gap-2 text-sm font-medium text-seal transition-colors hover:border-seal">
                <MessageCircle className="h-4 w-4" />
                {isAr ? "الدردشة مع العميل" : "Chat with the client"}
              </Card>
            </Link>
          )}
        </div>
      )}
    </AppShell>
  );
}
