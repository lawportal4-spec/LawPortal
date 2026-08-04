import { useEffect, useState } from "react";
import { useTranslation, directionFor, type SupportedLocale } from "@law-portal/i18n";

/** Owns the document-level dir/lang attributes so Tailwind logical properties flip for free. */
export function useLocale() {
  const { i18n } = useTranslation();
  const [locale, setLocaleState] = useState<SupportedLocale>(i18n.language as SupportedLocale);

  useEffect(() => {
    document.documentElement.lang = locale;
    document.documentElement.dir = directionFor(locale);
  }, [locale]);

  function setLocale(next: SupportedLocale) {
    void i18n.changeLanguage(next);
    setLocaleState(next);
  }

  return { locale, setLocale };
}
