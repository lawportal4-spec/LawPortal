import { useState, type FormEvent, type ReactNode } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Plus } from "lucide-react";
import { Button, Card, Chip, Input, Ltr, SectionHeading, StatusTag } from "@law-portal/ui";
import { formatCurrency, formatDateTime, useTranslation } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import {
  DISCOUNT_SCOPES,
  getDiscountCodes,
  getDiscountRedemptions,
  saveDiscountCode,
  type AdminDiscountCodeDto,
  type DiscountCodeInput,
} from "../lib/discountCodesApi";

/** Raw status for StatusTag's coloring, plus the i18n key of its label. */
function statusOf(c: AdminDiscountCodeDto): { status: string; key: string } {
  const now = Date.now();
  if (!c.isActive) return { status: "Inactive", key: "inactive" };
  if (c.endsAtUtc && Date.parse(c.endsAtUtc) <= now) return { status: "Expired", key: "expired" };
  if (c.usageLimit != null && c.used >= c.usageLimit) return { status: "Withdrawn", key: "usedUp" };
  if (c.startsAtUtc && Date.parse(c.startsAtUtc) > now) return { status: "Pending", key: "scheduled" };
  return { status: "Active", key: "active" };
}

const EMPTY: DiscountCodeInput = {
  code: "",
  descriptionAr: null,
  descriptionEn: null,
  kind: "Percentage",
  value: 10,
  maxDiscountAmount: null,
  minAmount: null,
  scopes: [],
  usageLimit: null,
  perUserLimit: 1,
  firstPaymentOnly: false,
  isActive: true,
  startsAtUtc: null,
  endsAtUtc: null,
};

export default function DiscountCodes() {
  const { t } = useTranslation();
  // null = list only; "new" = create form; otherwise the code being edited.
  const [editing, setEditing] = useState<AdminDiscountCodeDto | "new" | null>(null);
  const query = useQuery({ queryKey: ["discountCodes"], queryFn: getDiscountCodes });

  return (
    <AppShell>
      <div className="mb-6 flex items-center justify-between gap-4">
        <SectionHeading level={2}>{t("discount.admin.title")}</SectionHeading>
        {editing === null && (
          <Button onClick={() => setEditing("new")}>
            <Plus className="h-4 w-4" />
            {t("discount.admin.new")}
          </Button>
        )}
      </div>

      {editing !== null && (
        <CodeForm
          key={editing === "new" ? "new" : editing.id}
          existing={editing === "new" ? null : editing}
          onDone={() => setEditing(null)}
        />
      )}

      {query.isError && <p className="text-sm text-rubric">{t("discount.admin.loadFailed")}</p>}
      {query.data?.length === 0 && <p className="text-sm text-ink-faint">{t("discount.admin.empty")}</p>}

      <div className="flex flex-col gap-2">
        {query.data?.map((c) => {
          const s = statusOf(c);
          return (
            <button key={c.id} type="button" onClick={() => setEditing(c)} className="text-start">
              <Card className="flex flex-wrap items-center justify-between gap-4 transition-colors hover:border-seal">
                <div className="min-w-0">
                  <p className="flex items-center gap-3">
                    <Ltr className="font-mono font-semibold text-ink">{c.code}</Ltr>
                    <Ltr className="font-mono text-sm text-seal">
                      {c.kind === "Percentage" ? `${c.value}%` : formatCurrency(c.value)}
                    </Ltr>
                  </p>
                  <div className="mt-1 flex flex-wrap gap-1">
                    {c.scopes.map((scope) => (
                      <Chip key={scope}>{t(`discount.scopes.${scope}`)}</Chip>
                    ))}
                  </div>
                </div>
                <div className="flex flex-wrap items-center gap-4 text-xs text-ink-faint">
                  <span>
                    {t("discount.admin.used")}:{" "}
                    <Ltr className="font-mono text-ink">
                      {c.used} / {c.usageLimit ?? "∞"}
                    </Ltr>
                  </span>
                  {(c.startsAtUtc || c.endsAtUtc) && (
                    <span>
                      {t("discount.admin.dates")}:{" "}
                      <Ltr className="font-mono text-ink">
                        {c.startsAtUtc ? formatDateTime(c.startsAtUtc) : "…"} → {c.endsAtUtc ? formatDateTime(c.endsAtUtc) : "…"}
                      </Ltr>
                    </span>
                  )}
                  <StatusTag status={s.status} label={t(`discount.admin.status.${s.key}`)} />
                </div>
              </Card>
            </button>
          );
        })}
      </div>
    </AppShell>
  );
}

/** ISO (UTC) ⇄ the browser-local "YYYY-MM-DDTHH:mm" a datetime-local input takes. */
function toLocalInput(iso: string | null): string {
  if (!iso) return "";
  const d = new Date(iso);
  return new Date(d.getTime() - d.getTimezoneOffset() * 60_000).toISOString().slice(0, 16);
}
const fromLocalInput = (value: string) => (value ? new Date(value).toISOString() : null);
const numberOrNull = (value: string) => (value.trim() === "" ? null : Number(value));

function CodeForm({ existing, onDone }: { existing: AdminDiscountCodeDto | null; onDone: () => void }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [form, setForm] = useState<DiscountCodeInput>(existing ?? EMPTY);
  const [error, setError] = useState<string | null>(null);
  const set = <K extends keyof DiscountCodeInput>(key: K, value: DiscountCodeInput[K]) => setForm((f) => ({ ...f, [key]: value }));

  const mutation = useMutation({
    mutationFn: () => saveDiscountCode(existing?.id ?? null, form),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["discountCodes"] });
      onDone();
    },
    onError: () => setError(t("discount.admin.saveFailed")),
  });

  function submit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    mutation.mutate();
  }

  return (
    <Card className="mb-6">
      <SectionHeading level={3} className="mb-4">
        {existing ? t("discount.admin.edit") : t("discount.admin.new")}
      </SectionHeading>
      <form className="grid gap-4 sm:grid-cols-2" onSubmit={submit}>
        <Field label={t("discount.admin.code")} hint={t("discount.admin.codeHint")}>
          <Input dir="ltr" required value={form.code} onChange={(e) => set("code", e.target.value.toUpperCase())} />
        </Field>
        <Field label={t("discount.admin.kind")}>
          <div className="flex gap-2">
            {(["Percentage", "Fixed"] as const).map((kind) => (
              <button
                key={kind}
                type="button"
                onClick={() => set("kind", kind)}
                className={
                  "flex-1 rounded-md border px-3 py-2.5 text-sm " +
                  (form.kind === kind ? "border-seal bg-seal-tint text-seal-strong" : "border-border hover:border-seal")
                }
              >
                {t(kind === "Percentage" ? "discount.admin.percentage" : "discount.admin.fixed")}
              </button>
            ))}
          </div>
        </Field>
        <Field label={`${t("discount.admin.value")} (${form.kind === "Percentage" ? "%" : "SAR"})`}>
          <Input dir="ltr" type="number" min="0.01" step="0.01" required value={form.value} onChange={(e) => set("value", Number(e.target.value))} />
        </Field>
        {form.kind === "Percentage" && (
          <Field label={`${t("discount.admin.maxDiscount")} (SAR)`}>
            <Input dir="ltr" type="number" min="0.01" step="0.01" placeholder={t("discount.admin.unlimited")}
              value={form.maxDiscountAmount ?? ""} onChange={(e) => set("maxDiscountAmount", numberOrNull(e.target.value))} />
          </Field>
        )}
        <Field label={`${t("discount.admin.minAmount")} (SAR)`}>
          <Input dir="ltr" type="number" min="0.01" step="0.01" placeholder="—"
            value={form.minAmount ?? ""} onChange={(e) => set("minAmount", numberOrNull(e.target.value))} />
        </Field>
        <Field label={t("discount.admin.usageLimit")}>
          <Input dir="ltr" type="number" min="1" step="1" placeholder={t("discount.admin.unlimited")}
            value={form.usageLimit ?? ""} onChange={(e) => set("usageLimit", numberOrNull(e.target.value))} />
        </Field>
        <Field label={t("discount.admin.perUserLimit")}>
          <Input dir="ltr" type="number" min="1" step="1" placeholder={t("discount.admin.unlimited")}
            value={form.perUserLimit ?? ""} onChange={(e) => set("perUserLimit", numberOrNull(e.target.value))} />
        </Field>
        <Field label={t("discount.admin.startsAt")}>
          <Input dir="ltr" type="datetime-local" value={toLocalInput(form.startsAtUtc)} onChange={(e) => set("startsAtUtc", fromLocalInput(e.target.value))} />
        </Field>
        <Field label={t("discount.admin.endsAt")}>
          <Input dir="ltr" type="datetime-local" value={toLocalInput(form.endsAtUtc)} onChange={(e) => set("endsAtUtc", fromLocalInput(e.target.value))} />
        </Field>
        <Field label={t("discount.admin.descriptionAr")}>
          <Input value={form.descriptionAr ?? ""} onChange={(e) => set("descriptionAr", e.target.value || null)} />
        </Field>
        <Field label={t("discount.admin.descriptionEn")}>
          <Input dir="ltr" value={form.descriptionEn ?? ""} onChange={(e) => set("descriptionEn", e.target.value || null)} />
        </Field>

        <fieldset className="sm:col-span-2">
          <legend className="mb-2 text-xs text-ink-faint">{t("discount.admin.scopes")}</legend>
          <div className="grid gap-2 sm:grid-cols-2">
            {DISCOUNT_SCOPES.map((scope) => (
              <label key={scope} className="flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={form.scopes.includes(scope)}
                  onChange={(e) =>
                    set("scopes", e.target.checked ? [...form.scopes, scope] : form.scopes.filter((s) => s !== scope))
                  }
                />
                {t(`discount.scopes.${scope}`)}
              </label>
            ))}
          </div>
        </fieldset>

        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" checked={form.firstPaymentOnly} onChange={(e) => set("firstPaymentOnly", e.target.checked)} />
          {t("discount.admin.firstPaymentOnly")}
        </label>
        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" checked={form.isActive} onChange={(e) => set("isActive", e.target.checked)} />
          {t("discount.admin.isActive")}
        </label>

        {error && <p className="text-sm text-rubric sm:col-span-2">{error}</p>}
        <div className="flex gap-2 sm:col-span-2">
          <Button type="submit" disabled={mutation.isPending || form.scopes.length === 0}>
            {t("discount.admin.save")}
          </Button>
          <Button type="button" variant="ghost" onClick={onDone}>
            {t("common.cancel")}
          </Button>
        </div>
      </form>

      {existing && <Redemptions codeId={existing.id} />}
    </Card>
  );
}

function Field({ label, hint, children }: { label: string; hint?: string; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-1">
      <span className="text-xs text-ink-faint">{label}</span>
      {children}
      {hint && <span className="text-xs text-ink-faint">{hint}</span>}
    </div>
  );
}

function Redemptions({ codeId }: { codeId: string }) {
  const { t } = useTranslation();
  const query = useQuery({ queryKey: ["discountRedemptions", codeId], queryFn: () => getDiscountRedemptions(codeId) });

  return (
    <section className="mt-6 border-t border-border pt-4">
      <SectionHeading level={3} className="mb-3">
        {t("discount.admin.redemptions")}
      </SectionHeading>
      {query.data?.length === 0 && <p className="text-sm text-ink-faint">{t("discount.admin.noRedemptions")}</p>}
      <div className="flex flex-col">
        {query.data?.map((r) => (
          <div key={r.id} className="flex flex-wrap items-center justify-between gap-3 border-b border-border py-2.5 text-sm last:border-0">
            {/* bdi: the name falls back to a phone number, which must not mirror in RTL. */}
            <bdi className="font-medium text-ink">{r.userName}</bdi>
            <span className="text-xs text-ink-faint">{t(`discount.scopes.${r.scope}`)}</span>
            <Ltr className="font-mono text-ink">{formatCurrency(r.amount)}</Ltr>
            <Ltr className="font-mono text-xs text-ink-faint">{formatDateTime(r.createdAtUtc)}</Ltr>
            <StatusTag
              status={r.status === "Confirmed" ? "Paid" : r.status === "Released" ? "Cancelled" : "Pending"}
              label={t(`discount.admin.redemptionStatus.${r.status}`)}
            />
          </div>
        ))}
      </div>
    </section>
  );
}
