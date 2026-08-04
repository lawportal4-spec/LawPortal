import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { CheckCircle2, TriangleAlert } from "lucide-react";
import { Button, Card, Ltr } from "@law-portal/ui";
import { useTranslation, formatCurrency, formatDateTime } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { createCommissionPolicy, getCommissionPolicies, getLedgerEntries, getLedgerSummary } from "../lib/financeApi";

export default function Ledger() {
  const { i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const queryClient = useQueryClient();
  const [newSlug, setNewSlug] = useState("");
  const [newPercentage, setNewPercentage] = useState("");

  const summaryQuery = useQuery({ queryKey: ["ledgerSummary"], queryFn: getLedgerSummary });
  const entriesQuery = useQuery({ queryKey: ["ledgerEntries"], queryFn: () => getLedgerEntries(1, 20) });
  const policiesQuery = useQuery({ queryKey: ["commissionPolicies"], queryFn: getCommissionPolicies });

  const createPolicyMutation = useMutation({
    mutationFn: () => createCommissionPolicy(newSlug.trim() || null, Number(newPercentage)),
    onSuccess: () => {
      setNewSlug("");
      setNewPercentage("");
      void queryClient.invalidateQueries({ queryKey: ["commissionPolicies"] });
    },
  });

  const summary = summaryQuery.data;

  return (
    <AppShell>
      <h1 className="mb-6 font-display text-2xl font-bold">{isAr ? "التسوية المحاسبية" : "Reconciliation"}</h1>

      {summary && (
        <Card className="mb-6">
          <div className="mb-4 flex items-center gap-3">
            {summary.isBalanced ? (
              <>
                <CheckCircle2 className="h-5 w-5 shrink-0 text-seal" />
                <p className="text-sm text-ink-soft">{isAr ? "دفتر الأستاذ متوازن." : "The ledger is balanced."}</p>
              </>
            ) : (
              <>
                <TriangleAlert className="h-5 w-5 shrink-0 text-rubric" />
                <p className="text-sm text-rubric">{isAr ? "تحذير: دفتر الأستاذ غير متوازن!" : "Warning: the ledger is not balanced!"}</p>
              </>
            )}
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-border text-start text-xs text-ink-faint">
                  <th className="pb-2 text-start">{isAr ? "الحساب" : "Account"}</th>
                  <th className="pb-2 text-end">{isAr ? "إجمالي المدين" : "Total debits"}</th>
                  <th className="pb-2 text-end">{isAr ? "إجمالي الدائن" : "Total credits"}</th>
                  <th className="pb-2 text-end">{isAr ? "الرصيد الصافي" : "Net balance"}</th>
                </tr>
              </thead>
              <tbody>
                {summary.accounts.map((a) => (
                  <tr key={a.account} className="border-b border-border last:border-0">
                    <td className="py-2">{a.account}</td>
                    <td className="py-2 text-end font-mono">
                      <Ltr>{formatCurrency(a.totalDebits)}</Ltr>
                    </td>
                    <td className="py-2 text-end font-mono">
                      <Ltr>{formatCurrency(a.totalCredits)}</Ltr>
                    </td>
                    <td className="py-2 text-end font-mono">
                      <Ltr>{formatCurrency(a.netBalance)}</Ltr>
                    </td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr className="border-t border-ink font-semibold">
                  <td className="py-2">{isAr ? "الإجمالي" : "Grand total"}</td>
                  <td className="py-2 text-end font-mono">
                    <Ltr>{formatCurrency(summary.grandTotalDebits)}</Ltr>
                  </td>
                  <td className="py-2 text-end font-mono">
                    <Ltr>{formatCurrency(summary.grandTotalCredits)}</Ltr>
                  </td>
                  <td className="py-2 text-end font-mono">
                    <Ltr>{formatCurrency(summary.grandTotalDebits - summary.grandTotalCredits)}</Ltr>
                  </td>
                </tr>
              </tfoot>
            </table>
          </div>
        </Card>
      )}

      <Card className="mb-6">
        <h2 className="mb-3 text-sm font-semibold text-ink-soft">{isAr ? "سياسات العمولة" : "Commission Policies"}</h2>
        <div className="mb-4 flex flex-col gap-2">
          {policiesQuery.data?.map((p) => (
            <div key={p.id} className="flex items-center justify-between text-sm">
              <span className="text-ink-soft">
                {p.serviceCategorySlug ?? (isAr ? "عام (جميع الفئات)" : "Global (all categories)")}
                {!p.isActive && <span className="ms-2 text-xs text-ink-faint">({isAr ? "غير نشطة" : "inactive"})</span>}
              </span>
              <span className="font-mono">
                <Ltr>{p.percentage}%</Ltr>
              </span>
            </div>
          ))}
        </div>
        <div className="flex flex-wrap items-center gap-2 border-t border-border pt-4">
          <input
            value={newSlug}
            onChange={(e) => setNewSlug(e.target.value)}
            placeholder={isAr ? "معرّف الفئة (اتركه فارغًا للسياسة العامة)" : "Category slug (blank for global)"}
            className="rounded-md border border-border bg-surface-raised px-3 py-2 text-sm"
          />
          <input
            type="number"
            value={newPercentage}
            onChange={(e) => setNewPercentage(e.target.value)}
            placeholder={isAr ? "النسبة %" : "Percentage %"}
            className="w-32 rounded-md border border-border bg-surface-raised px-3 py-2 text-sm"
          />
          <Button onClick={() => createPolicyMutation.mutate()} disabled={!newPercentage || createPolicyMutation.isPending}>
            {isAr ? "إضافة سياسة جديدة" : "Add new policy"}
          </Button>
        </div>
      </Card>

      <Card>
        <h2 className="mb-3 text-sm font-semibold text-ink-soft">{isAr ? "أحدث حركات دفتر الأستاذ" : "Recent Ledger Entries"}</h2>
        <div className="flex flex-col gap-2">
          {entriesQuery.data?.items.map((e) => (
            <div key={e.id} className="flex items-center justify-between border-b border-border py-2 text-sm last:border-0">
              <div>
                <span className="font-medium text-ink">{e.account}</span>
                <span className="ms-2 text-xs text-ink-faint">{e.description}</span>
              </div>
              <div className="flex items-center gap-3">
                <span className={"font-mono " + (e.isDebit ? "text-ink" : "text-seal")}>
                  <Ltr>{(e.isDebit ? "Dr " : "Cr ") + formatCurrency(e.amount)}</Ltr>
                </span>
                <span className="text-xs text-ink-faint">
                  <Ltr>{formatDateTime(e.createdAtUtc)}</Ltr>
                </span>
              </div>
            </div>
          ))}
        </div>
      </Card>
    </AppShell>
  );
}
