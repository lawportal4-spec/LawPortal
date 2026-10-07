import type { ReactNode } from "react";
import { useQuery } from "@tanstack/react-query";
import { Input, Ltr, SectionHeading, StatusTag } from "@law-portal/ui";
import { formatCurrency, formatDateTime, useTranslation } from "@law-portal/i18n";
import { DISCOUNT_SCOPES, getDiscountRedemptions, type DiscountCodeInput } from "../lib/discountCodesApi";

/** ISO (UTC) ⇄ the browser-local "YYYY-MM-DDTHH:mm" a datetime-local input takes. */
function toLocalInput(iso: string | null): string {
  if (!iso) return "";
  const d = new Date(iso);
  return new Date(d.getTime() - d.getTimezoneOffset() * 60_000).toISOString().slice(0, 16);
}
const fromLocalInput = (value: string) => (value ? new Date(value).toISOString() : null);
const numberOrNull = (value: string) => (value.trim() === "" ? null : Number(value));

/** Every rule of a discount code, shared by the create and edit pages. */
export function DiscountCodeFields({
  form,
  onChange,
  codeLocked = false,
}: {
  form: DiscountCodeInput;
  onChange: (form: DiscountCodeInput) => void;
  /** Edit page: the code text is fixed once created. */
  codeLocked?: boolean;
}) {
  const { t } = useTranslation();
  const set = <K extends keyof DiscountCodeInput>(key: K, value: DiscountCodeInput[K]) => onChange({ ...form, [key]: value });

  return (
    <div className="grid gap-4 sm:grid-cols-2">
      <Field label={t("discount.admin.code")} hint={codeLocked ? t("discount.admin.codeLocked") : t("discount.admin.codeHint")}>
        {codeLocked ? (
          <Ltr className="rounded-md border border-border bg-paper px-4 py-2.5 font-mono font-semibold text-ink">{form.code}</Ltr>
        ) : (
          <Input dir="ltr" required value={form.code} onChange={(e) => set("code", e.target.value.toUpperCase())} />
        )}
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
                onChange={(e) => set("scopes", e.target.checked ? [...form.scopes, scope] : form.scopes.filter((s) => s !== scope))}
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
    </div>
  );
}

export function Field({ label, hint, children }: { label: string; hint?: string; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-1">
      <span className="text-xs text-ink-faint">{label}</span>
      {children}
      {hint && <span className="text-xs text-ink-faint">{hint}</span>}
    </div>
  );
}

export function Redemptions({ codeId }: { codeId: string }) {
  const { t } = useTranslation();
  const query = useQuery({ queryKey: ["discountRedemptions", codeId], queryFn: () => getDiscountRedemptions(codeId) });

  return (
    <section>
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
