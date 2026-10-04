import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { CheckCircle2, XCircle } from "lucide-react";
import { Button, Card, Ltr, StatusTag } from "@law-portal/ui";
import { useTranslation, formatDate } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { getPendingLawyers, rejectLawyer, verifyLawyer } from "../lib/adminApi";

export default function LawyerVerification() {
  const { t, i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const queryClient = useQueryClient();
  const [rejectingId, setRejectingId] = useState<string | null>(null);
  const [reason, setReason] = useState("");

  const query = useQuery({ queryKey: ["pendingLawyers"], queryFn: getPendingLawyers });

  function invalidate() {
    void queryClient.invalidateQueries({ queryKey: ["pendingLawyers"] });
    void queryClient.invalidateQueries({ queryKey: ["adminDashboard"] });
  }

  const verifyMutation = useMutation({ mutationFn: (id: string) => verifyLawyer(id), onSuccess: invalidate });
  const rejectMutation = useMutation({
    mutationFn: (id: string) => rejectLawyer(id, reason),
    onSuccess: () => {
      setRejectingId(null);
      setReason("");
      invalidate();
    },
  });

  return (
    <AppShell>
      <h1 className="mb-1 font-display text-2xl font-bold">{isAr ? "توثيق المحامين" : "Lawyer Verification"}</h1>
      <p className="mb-6 text-sm text-ink-faint">
        {isAr ? `${query.data?.length ?? "…"} طلب بانتظار المراجعة` : `${query.data?.length ?? "…"} pending review`}
      </p>

      {query.isLoading && <p className="text-sm text-ink-faint">{isAr ? "جارٍ التحميل…" : "Loading…"}</p>}

      <div className="flex flex-col gap-3">
        {query.data?.map((lawyer) => (
          <Card key={lawyer.lawyerProfileId}>
            <div className="mb-3 flex items-start justify-between gap-4">
              <div>
                <p className="font-medium text-ink">{lawyer.fullName}</p>
                <p className="text-xs text-ink-faint">
                  <Ltr className="font-mono">{lawyer.email}</Ltr>
                </p>
                {lawyer.phoneE164 && (
                  <p className="text-xs text-ink-faint">
                    <Ltr className="font-mono">{lawyer.phoneE164}</Ltr>
                  </p>
                )}
                <p className="mt-1 text-xs text-ink-soft">
                  {t(`lawyerAuth.register.licenseTypes.${lawyer.licenseType}`)}
                  {lawyer.licenseDocumentUrl && (
                    <>
                      {" · "}
                      <a href={lawyer.licenseDocumentUrl} target="_blank" rel="noreferrer" className="font-medium text-seal hover:underline">
                        {t("lawyerAuth.admin.viewLicense")}
                      </a>
                    </>
                  )}
                </p>
              </div>
              <StatusTag status={lawyer.verificationStatus} />
            </div>
            <div className="mb-3 grid grid-cols-3 gap-3 text-sm">
              <div>
                <p className="text-xs text-ink-faint">{isAr ? "رقم الترخيص" : "Licence number"}</p>
                <p className="font-mono">
                  <Ltr>{lawyer.licenseNumber}</Ltr>
                </p>
              </div>
              <div>
                <p className="text-xs text-ink-faint">{isAr ? "تاريخ الإصدار" : "Issued"}</p>
                <p className="font-mono">
                  <Ltr>{formatDate(lawyer.issueDate)}</Ltr>
                </p>
              </div>
              <div>
                <p className="text-xs text-ink-faint">{isAr ? "تاريخ الانتهاء" : "Expires"}</p>
                <p className="font-mono">
                  <Ltr>{formatDate(lawyer.expiryDate)}</Ltr>
                </p>
              </div>
            </div>

            {rejectingId === lawyer.lawyerProfileId ? (
              <div className="flex flex-col gap-2">
                <input
                  value={reason}
                  onChange={(e) => setReason(e.target.value)}
                  placeholder={isAr ? "سبب الرفض" : "Reason for rejection"}
                  className="rounded-md border border-border bg-surface-raised px-3 py-2 text-sm"
                />
                <div className="flex gap-2">
                  <Button variant="danger" disabled={!reason.trim() || rejectMutation.isPending} onClick={() => rejectMutation.mutate(lawyer.lawyerProfileId)}>
                    {isAr ? "تأكيد الرفض" : "Confirm reject"}
                  </Button>
                  <Button variant="ghost" onClick={() => setRejectingId(null)}>
                    {isAr ? "إلغاء" : "Cancel"}
                  </Button>
                </div>
              </div>
            ) : (
              <div className="flex gap-2">
                <Button onClick={() => verifyMutation.mutate(lawyer.lawyerProfileId)} disabled={verifyMutation.isPending}>
                  <CheckCircle2 className="h-4 w-4" />
                  {isAr ? "توثيق" : "Verify"}
                </Button>
                <Button variant="danger" onClick={() => setRejectingId(lawyer.lawyerProfileId)}>
                  <XCircle className="h-4 w-4" />
                  {isAr ? "رفض" : "Reject"}
                </Button>
              </div>
            )}
          </Card>
        ))}

        {query.data?.length === 0 && (
          <p className="text-sm text-ink-faint">{isAr ? "لا توجد طلبات توثيق معلّقة." : "No pending verifications."}</p>
        )}
      </div>
    </AppShell>
  );
}
