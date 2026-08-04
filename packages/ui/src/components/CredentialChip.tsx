import { Ltr } from "./Ltr";

export interface CredentialChipProps {
  licenceNumber: string;
  labelAr: string;
  labelEn: string;
  locale?: "ar" | "en";
}

/**
 * The signature element: a miniature facsimile of a lawyer's Ministry of Justice licence
 * card, rendered on `vellum` stock. This is the ONLY place `vellum` is used — it is reserved
 * exclusively for credential artifacts (see packages/design-tokens).
 */
export function CredentialChipMini({ licenceNumber, labelAr, labelEn, locale = "ar" }: CredentialChipProps) {
  return (
    <span className="inline-flex items-center gap-2 rounded-full border border-vellum-line bg-vellum py-1.5 pe-3 ps-1.5 text-vellum-ink">
      <SealMark size="sm" />
      <strong className="text-sm font-semibold">{locale === "ar" ? labelAr : labelEn}</strong>
      <Ltr className="font-mono text-sm opacity-85">{licenceNumber}</Ltr>
    </span>
  );
}

export interface CredentialCardProps {
  nameAr: string;
  nameEn: string;
  licenceNumber: string;
  issuedOn: string;
  expiresOn: string;
  locale?: "ar" | "en";
  sample?: boolean;
}

/** The expanded, full-fidelity licence-card facsimile used on a lawyer's profile. */
export function CredentialCard({
  nameAr,
  nameEn,
  licenceNumber,
  issuedOn,
  expiresOn,
  locale = "ar",
  sample = false,
}: CredentialCardProps) {
  return (
    <div className="relative w-76 max-w-full rounded-xl border border-vellum-line bg-vellum p-5 text-vellum-ink shadow-[0_10px_24px_-12px_rgba(14,31,26,0.28)]">
      <div className="mb-4 flex items-center gap-3">
        <SealMark size="lg" />
        <div>
          <div className="text-[0.68rem] uppercase tracking-wide opacity-75">
            {locale === "ar" ? "ترخيص مزاولة المهنة" : "Licence to Practice"}
          </div>
          <div className="font-display mt-0.5 text-lg font-bold">{locale === "ar" ? nameAr : nameEn}</div>
        </div>
      </div>
      <div className="grid grid-cols-2 gap-x-4 gap-y-2 border-t border-dashed border-vellum-line pt-3">
        <Field label={locale === "ar" ? "رقم الترخيص" : "Licence no."} value={licenceNumber} />
        <Field label={locale === "ar" ? "الإصدار" : "Issued"} value={issuedOn} />
        <Field label={locale === "ar" ? "الانتهاء" : "Expires"} value={expiresOn} />
      </div>
      {sample && (
        <span className="mt-3 inline-block rounded-full border border-current px-2.5 py-0.5 text-[0.62rem] uppercase tracking-wide text-rubric opacity-85">
          {locale === "ar" ? "نموذج توضيحي" : "Sample data"}
        </span>
      )}
    </div>
  );
}

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex flex-col gap-0.5">
      <span className="text-[0.66rem] opacity-70">{label}</span>
      <Ltr className="font-mono text-sm">{value}</Ltr>
    </div>
  );
}

function SealMark({ size = "sm" }: { size?: "sm" | "lg" }) {
  const dims = size === "sm" ? "h-6 w-6 text-xs" : "h-10 w-10 text-lg";
  return (
    <span
      className={`flex ${dims} flex-shrink-0 items-center justify-center rounded-full border border-vellum-line bg-vellum font-display font-bold text-vellum-ink outline outline-1 outline-offset-2 outline-vellum-line`}
    >
      ق
    </span>
  );
}
