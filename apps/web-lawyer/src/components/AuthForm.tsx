import { cloneElement, useId, useState, type InputHTMLAttributes, type ReactElement, type ReactNode, type SelectHTMLAttributes } from "react";
import { Eye, EyeOff, ChevronDown } from "lucide-react";
import { Input } from "@law-portal/ui";
import { useTranslation } from "@law-portal/i18n";
import { AppShell } from "./AppShell";

/** Centred column under the brand wordmark — the layout every lawyer sign-up/sign-in screen shares. */
export function AuthLayout({ title, hideNav, children }: { title?: string; hideNav?: boolean; children: ReactNode }) {
  const { t, i18n } = useTranslation();
  return (
    <AppShell hideNav={hideNav}>
      <div className="mx-auto flex max-w-md flex-col items-center py-6">
        <img src="/favicon.svg" alt="" className="mb-3 h-24 w-20" />
        <p className="font-display text-4xl font-bold text-seal-strong">{i18n.language === "ar" ? t("app.nameAr") : t("app.nameEn")}</p>
        {title && <h1 className="mt-3 text-xl text-ink-soft">{title}</h1>}
        <div className="mt-8 w-full">{children}</div>
      </div>
    </AppShell>
  );
}

/** Label above one control. The control gets the label as its accessible name — `Input` already
 * renders its own <label> wrapper, so a second, outer <label> would be invalid nesting. */
export function Field({ label, required, error, children }: { label: string; required?: boolean; error?: string | null; children: ReactElement<{ "aria-label"?: string; "aria-describedby"?: string }> }) {
  const errorId = useId();
  return (
    <div className="flex flex-col gap-1.5">
      <span aria-hidden className="text-xs font-medium text-ink-soft">
        {label}
        {required && <span className="text-rubric"> *</span>}
      </span>
      {cloneElement(children, { "aria-label": label, "aria-describedby": error ? errorId : undefined })}
      {error && (
        <p id={errorId} className="text-xs text-rubric">
          {error}
        </p>
      )}
    </div>
  );
}

export function Select({ children, ...props }: SelectHTMLAttributes<HTMLSelectElement>) {
  return (
    <div className="relative">
      <select
        className="w-full appearance-none rounded-md border border-border bg-surface-raised py-2.5 pe-10 ps-4 text-sm text-ink focus:border-seal focus:outline-none focus:ring-2 focus:ring-seal/30 disabled:opacity-50"
        {...props}
      >
        {children}
      </select>
      <ChevronDown className="pointer-events-none absolute end-3 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-faint" />
    </div>
  );
}

export function PasswordInput(props: Omit<InputHTMLAttributes<HTMLInputElement>, "type">) {
  const { t } = useTranslation();
  const [visible, setVisible] = useState(false);
  return (
    <div className="flex items-center gap-2">
      <Input {...props} type={visible ? "text" : "password"} dir="ltr" className="flex-1" />
      <button
        type="button"
        onClick={() => setVisible((v) => !v)}
        aria-label={visible ? t("lawyerAuth.register.hidePassword") : t("lawyerAuth.register.showPassword")}
        className="p-1 text-ink-faint hover:text-ink"
      >
        {visible ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
      </button>
    </div>
  );
}

/** Fixed +966 prefix; the lawyer types the 9 local digits (5XXXXXXXX). */
export function SaudiPhoneInput(props: Omit<InputHTMLAttributes<HTMLInputElement>, "type">) {
  return (
    <div dir="ltr" className="flex items-center gap-2">
      <span className="flex items-center gap-1.5 rounded-md border border-border bg-surface px-3 py-2.5 font-mono text-sm text-ink-soft">
        <span aria-hidden>🇸🇦</span>+966
      </span>
      <Input {...props} type="tel" inputMode="numeric" autoComplete="tel-national" maxLength={10} placeholder="5XXXXXXXX" className="flex-1 font-mono" />
    </div>
  );
}
