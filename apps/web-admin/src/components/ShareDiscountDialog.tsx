import { useEffect, useId, useState, type ReactNode } from "react";
import { Check, Copy, Facebook, Link as LinkIcon, Linkedin, Mail, Send, Share2, Twitter, X as Close } from "lucide-react";
import { Button, Chip, Ltr } from "@law-portal/ui";
import { formatCurrency, formatDate, useTranslation } from "@law-portal/i18n";
import type { AdminDiscountCodeDto } from "../lib/discountCodesApi";
import { buildShareMessage, buildShortMessage, type ShareLanguage } from "../lib/discountShare";

/** WhatsApp's mark — lucide has no WhatsApp icon. */
function WhatsAppIcon() {
  return (
    <svg viewBox="0 0 24 24" className="h-5 w-5" fill="currentColor" aria-hidden>
      <path d="M17.47 14.38c-.3-.15-1.75-.86-2.02-.96-.27-.1-.47-.15-.67.15-.2.3-.77.96-.94 1.16-.17.2-.35.22-.64.07-.3-.15-1.25-.46-2.38-1.47-.88-.78-1.48-1.75-1.65-2.05-.17-.3-.02-.46.13-.6.13-.14.3-.35.45-.52.15-.17.2-.3.3-.5.1-.2.05-.37-.03-.52-.07-.15-.67-1.6-.92-2.2-.24-.58-.49-.5-.67-.51h-.57c-.2 0-.52.07-.8.37-.27.3-1.04 1.02-1.04 2.48s1.07 2.88 1.21 3.08c.15.2 2.1 3.2 5.08 4.49.71.31 1.26.49 1.7.63.71.22 1.36.19 1.87.12.57-.09 1.75-.72 2-1.41.25-.69.25-1.29.17-1.41-.07-.12-.27-.2-.57-.35zM12.05 21.5h-.01a9.4 9.4 0 0 1-4.8-1.31l-.34-.2-3.57.94.95-3.48-.22-.36a9.43 9.43 0 1 1 7.99 4.41zm8.03-17.46A11.32 11.32 0 0 0 12.05.72C5.8.72.72 5.8.72 12.05c0 2 .52 3.95 1.52 5.67L.62 23.6l6.03-1.58a11.3 11.3 0 0 0 5.4 1.38h.01c6.25 0 11.33-5.08 11.33-11.33 0-3.03-1.18-5.87-3.31-8.01z" />
    </svg>
  );
}

const open = (url: string) => window.open(url, "_blank", "noopener,noreferrer");
// WhatsApp: api.whatsapp.com directly — the wa.me short link's redirect garbles emoji into "�".

/** Share a discount code as a ready-made promotion: coupon preview, editable message, one-click channels. */
export function ShareDiscountDialog({ code, onClose }: { code: AdminDiscountCodeDto; onClose: () => void }) {
  const { t, i18n } = useTranslation();
  const titleId = useId();
  const [lang, setLang] = useState<ShareLanguage>(i18n.language === "en" ? "en" : "ar");
  const [message, setMessage] = useState(() => buildShareMessage(code, t, lang));
  const [notice, setNotice] = useState<string | null>(null);

  useEffect(() => setMessage(buildShareMessage(code, t, lang)), [code, t, lang]);
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => e.key === "Escape" && onClose();
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [onClose]);

  const flash = (text: string) => {
    setNotice(text);
    setTimeout(() => setNotice(null), 3000);
  };
  const copy = async (text: string, label: string) => {
    await navigator.clipboard.writeText(text);
    flash(label);
  };
  // Facebook and LinkedIn only accept a link — the text goes to the clipboard to paste into the post.
  const linkOnly = async (network: string, url: string) => {
    await navigator.clipboard.writeText(message);
    flash(t("discount.share.pastedHint", { network }));
    open(url);
  };
  const enc = encodeURIComponent;
  const url = code.shareUrl;
  const canNativeShare = typeof navigator !== "undefined" && "share" in navigator;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 px-4" onClick={onClose}>
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        onClick={(e) => e.stopPropagation()}
        className="max-h-[92vh] w-full max-w-4xl overflow-y-auto rounded-lg bg-surface p-6 shadow-raised"
      >
        <div className="mb-5 flex items-start justify-between gap-4">
          <div>
            <h2 id={titleId} className="font-display text-xl font-bold text-ink">{t("discount.share.title")}</h2>
            <p className="text-sm text-ink-faint">{t("discount.share.subtitle")}</p>
          </div>
          <button type="button" onClick={onClose} aria-label={t("discount.share.close")} className="rounded-md p-1.5 text-ink-faint hover:bg-paper hover:text-ink">
            <Close className="h-5 w-5" />
          </button>
        </div>

        <div className="grid gap-6 md:grid-cols-2">
          <div className="flex flex-col gap-5">
            <Coupon code={code} />
            <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
              <Channel label="WhatsApp" className="bg-[#25D366] text-white hover:opacity-90" onClick={() => open(`https://api.whatsapp.com/send?text=${enc(message)}`)}>
                <WhatsAppIcon />
              </Channel>
              <Channel label="X" className="bg-black text-white hover:opacity-90" onClick={() => open(`https://twitter.com/intent/tweet?text=${enc(buildShortMessage(code, t, lang))}`)}>
                <Twitter className="h-5 w-5" />
              </Channel>
              <Channel label="Facebook" className="bg-[#1877F2] text-white hover:opacity-90" onClick={() => linkOnly("Facebook", `https://www.facebook.com/sharer/sharer.php?u=${enc(url)}`)}>
                <Facebook className="h-5 w-5" />
              </Channel>
              <Channel label="LinkedIn" className="bg-[#0A66C2] text-white hover:opacity-90" onClick={() => linkOnly("LinkedIn", `https://www.linkedin.com/sharing/share-offsite/?url=${enc(url)}`)}>
                <Linkedin className="h-5 w-5" />
              </Channel>
              <Channel label="Telegram" className="bg-[#229ED9] text-white hover:opacity-90" onClick={() => open(`https://t.me/share/url?url=${enc(url)}&text=${enc(message.replace(url, "").trim())}`)}>
                <Send className="h-5 w-5" />
              </Channel>
              <Channel label="Email" className="border border-border bg-surface-raised text-ink hover:border-seal" onClick={() => open(`mailto:?subject=${enc(message.split("\n")[0])}&body=${enc(message)}`)}>
                <Mail className="h-5 w-5" />
              </Channel>
            </div>
          </div>

          <div className="flex flex-col gap-3">
            <div className="flex items-center justify-between gap-3">
              <span className="text-xs text-ink-faint">{t("discount.share.message")}</span>
              <div className="flex rounded-full border border-border p-0.5 text-xs" role="group" aria-label={t("discount.share.language")}>
                {(["ar", "en"] as const).map((l) => (
                  <button
                    key={l}
                    type="button"
                    onClick={() => setLang(l)}
                    aria-pressed={lang === l}
                    className={"rounded-full px-3 py-1 " + (lang === l ? "bg-seal text-seal-on" : "text-ink-soft")}
                  >
                    {l === "ar" ? t("discount.share.arabic") : t("discount.share.english")}
                  </button>
                ))}
              </div>
            </div>
            <textarea
              dir={lang === "ar" ? "rtl" : "ltr"}
              value={message}
              onChange={(e) => setMessage(e.target.value)}
              rows={14}
              className="w-full rounded-md border border-border bg-surface-raised p-3 text-sm leading-relaxed text-ink focus:border-seal focus:outline-none focus:ring-2 focus:ring-seal/30"
            />
            <div className="flex flex-wrap gap-2">
              <Button onClick={() => copy(message, t("discount.share.messageCopied"))}>
                <Copy className="h-4 w-4" />
                {t("discount.share.copyMessage")}
              </Button>
              <Button variant="secondary" onClick={() => copy(url, t("discount.share.copied"))}>
                <LinkIcon className="h-4 w-4" />
                {t("discount.share.copyLink")}
              </Button>
              {canNativeShare && (
                <Button variant="secondary" onClick={() => void navigator.share({ title: message.split("\n")[0], text: message })}>
                  <Share2 className="h-4 w-4" />
                  {t("discount.share.more")}
                </Button>
              )}
            </div>
            {notice && (
              <p role="status" className="flex items-center gap-1.5 text-sm text-seal">
                <Check className="h-4 w-4" />
                {notice}
              </p>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

function Channel({ label, className, onClick, children }: { label: string; className: string; onClick: () => void; children: ReactNode }) {
  return (
    <button type="button" onClick={onClick} className={"flex items-center justify-center gap-2 rounded-md px-3 py-2.5 text-sm font-medium transition " + className}>
      {children}
      {label}
    </button>
  );
}

/** The code as a ticket — what the promotion looks like at a glance. */
function Coupon({ code }: { code: AdminDiscountCodeDto }) {
  const { t, i18n } = useTranslation();
  return (
    <div className="relative overflow-hidden rounded-xl border-2 border-dashed border-seal bg-seal-tint p-5 text-center">
      <span className="absolute -start-3 top-1/2 h-6 w-6 -translate-y-1/2 rounded-full bg-surface" aria-hidden />
      <span className="absolute -end-3 top-1/2 h-6 w-6 -translate-y-1/2 rounded-full bg-surface" aria-hidden />
      <p className="text-xs font-semibold tracking-wide text-seal-strong">{i18n.language === "ar" ? t("app.nameAr") : t("app.nameEn")}</p>
      <p className="mt-2 font-mono text-4xl font-bold text-seal-strong">
        <Ltr>{code.kind === "Percentage" ? `${code.value}%` : formatCurrency(code.value)}</Ltr>
      </p>
      <p className="text-sm font-semibold uppercase text-seal-strong">{t("discount.share.coupon.off")}</p>
      {code.kind === "Percentage" && code.maxDiscountAmount && (
        <p className="text-xs text-ink-soft">
          {t("discount.share.coupon.upTo")}
          <Ltr className="font-mono">{formatCurrency(code.maxDiscountAmount)}</Ltr>
        </p>
      )}
      <div className="mt-3 flex flex-wrap justify-center gap-1">
        {code.scopes.map((s) => (
          <Chip key={s}>{t(`discount.scopes.${s}`)}</Chip>
        ))}
      </div>
      <p className="mt-4 text-xs text-ink-faint">{t("discount.share.coupon.useCode")}</p>
      <p className="mx-auto mt-1 inline-block rounded-md border border-seal bg-surface px-4 py-2 font-mono text-xl font-bold tracking-widest text-ink">
        <Ltr>{code.code}</Ltr>
      </p>
      {code.endsAtUtc && (
        <p className="mt-3 text-xs text-ink-soft">
          {t("discount.share.coupon.validUntil", { date: "" })}
          <Ltr className="font-mono">{formatDate(code.endsAtUtc)}</Ltr>
        </p>
      )}
    </div>
  );
}
