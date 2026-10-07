import { formatDate, type useTranslation } from "@law-portal/i18n";
import type { AdminDiscountCodeDto, DiscountScope } from "./discountCodesApi";

export type ShareLanguage = "ar" | "en";
type TFunction = ReturnType<typeof useTranslation>["t"];

const LAWYER_SCOPES: DiscountScope[] = ["LawyerSubscription", "LawyerRegistrationFee"];

/** Codes that only lawyers can use get the "invite lawyers" wording and link. */
export const isLawyerOffer = (c: AdminDiscountCodeDto) => c.scopes.every((s) => LAWYER_SCOPES.includes(s));

/** "20%" or "40 ر.س" in the message's language. */
function valueText(c: AdminDiscountCodeDto, t: TFunction, lng: ShareLanguage) {
  return c.kind === "Percentage" ? `${c.value}%` : t("discount.share.msg." + lng + ".sar", { amount: c.value, lng });
}

/** "A، B وC" / "A, B and C". */
function joinList(items: string[], and: string, lng: ShareLanguage) {
  if (items.length <= 1) return items.join("");
  const sep = lng === "ar" ? "، " : ", ";
  return items.slice(0, -1).join(sep) + and + items[items.length - 1];
}

/** A promotional post built from the code's actual rules, so it never promises more than the code gives. */
export function buildShareMessage(c: AdminDiscountCodeDto, t: TFunction, lng: ShareLanguage): string {
  const k = (key: string, vars: Record<string, unknown> = {}) => t(`discount.share.msg.${lng}.${key}`, { ...vars, lng });
  const lawyer = isLawyerOffer(c);
  const scopes = joinList(c.scopes.map((s) => k(`scopes.${s}`)), k("and"), lng);
  const offer = k("offer", { value: valueText(c, t, lng), scopes })
    + (c.kind === "Percentage" && c.maxDiscountAmount ? " " + k("cap", { cap: k("sar", { amount: c.maxDiscountAmount }) }) : "");

  const lines = [
    k(lawyer ? "headlineLawyer" : "headlineClient"),
    "",
    offer,
    k("code", { code: c.code }),
    c.endsAtUtc ? k("until", { date: formatDate(c.endsAtUtc) }) : null,
    c.minAmount ? k("min", { min: k("sar", { amount: c.minAmount }) }) : null,
    c.firstPaymentOnly ? k("first") : null,
    c.usageLimit ? k("limited") : null,
    "",
    k(lawyer ? "ctaLawyer" : "ctaClient"),
    c.shareUrl,
    "",
    k(lawyer ? "tagsLawyer" : "tagsClient"),
  ];
  return lines.filter((l) => l !== null).join("\n");
}

/** The short form for X (280 characters): headline, offer, code, link. */
export function buildShortMessage(c: AdminDiscountCodeDto, t: TFunction, lng: ShareLanguage): string {
  const full = buildShareMessage(c, t, lng).split("\n");
  return [full[0], full[2], full[3], c.shareUrl].join("\n");
}
