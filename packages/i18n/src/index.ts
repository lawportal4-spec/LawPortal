import i18next from "i18next";
import { initReactI18next } from "react-i18next";
import ar from "./locales/ar.json";
import en from "./locales/en.json";

export * from "./format";
export * from "./hijri";

// Re-exported so app code never takes a direct dependency on react-i18next/i18next —
// one workspace package owns the i18n vendor choice.
export { useTranslation, Trans } from "react-i18next";

export const SUPPORTED_LOCALES = ["ar", "en"] as const;
export type SupportedLocale = (typeof SUPPORTED_LOCALES)[number];

export function initI18n(defaultLocale: SupportedLocale = "ar") {
  void i18next.use(initReactI18next).init({
    resources: { ar: { translation: ar }, en: { translation: en } },
    lng: defaultLocale,
    fallbackLng: "ar",
    interpolation: { escapeValue: false },
  });
  return i18next;
}

export function directionFor(locale: SupportedLocale): "rtl" | "ltr" {
  return locale === "ar" ? "rtl" : "ltr";
}
