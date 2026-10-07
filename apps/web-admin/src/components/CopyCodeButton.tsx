import { useState, type MouseEvent } from "react";
import { Check, Copy } from "lucide-react";
import { useTranslation } from "@law-portal/i18n";

/** Copies a code to the clipboard, with a short "copied" confirmation. */
export function CopyCodeButton({ code, className = "" }: { code: string; className?: string }) {
  const { t } = useTranslation();
  const [copied, setCopied] = useState(false);

  async function copy(e: MouseEvent) {
    // The button sits on a clickable row — don't open the code page as well.
    e.preventDefault();
    e.stopPropagation();
    await navigator.clipboard.writeText(code);
    setCopied(true);
    setTimeout(() => setCopied(false), 1500);
  }

  return (
    <button
      type="button"
      onClick={copy}
      aria-label={copied ? t("discount.share.copied") : t("discount.share.copy")}
      title={copied ? t("discount.share.copied") : t("discount.share.copy")}
      className={"relative z-10 inline-flex items-center gap-1 rounded-md p-1.5 text-xs text-ink-faint transition-colors hover:bg-seal-tint hover:text-seal " + className}
    >
      {copied ? <Check className="h-4 w-4 text-seal" /> : <Copy className="h-4 w-4" />}
      {copied && <span className="text-seal">{t("discount.share.copied")}</span>}
    </button>
  );
}
