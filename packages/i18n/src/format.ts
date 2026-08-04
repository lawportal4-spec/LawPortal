/**
 * Centralized number/date/currency formatting. Every value that mixes digits with
 * Arabic text MUST go through here — never call `.toLocaleString()` / `Intl.*` directly
 * in app code (enforced by the `no-raw-intl-formatting` ESLint rule in ./eslint).
 *
 * These values are always Western-numeral, Gregorian, and LTR regardless of UI language, so
 * they are formatted with a neutral Western locale (`en-GB`) rather than `ar-SA` with
 * numbering/calendar overrides. That distinction matters: `ar-SA` — even pinned to
 * `-u-nu-latn-ca-gregory` — still embeds invisible RTL-mark characters (U+200F) around date
 * separators, which silently reorders "2022/08/03" into garbled output once placed in an RTL
 * paragraph. `en-GB` produces the same DD/MM/YYYY shape with no hidden bidi characters. This is
 * exactly the class of bug the mono/&lt;bdi&gt; convention exists to prevent — one instance of it
 * slipped through here despite that convention, because the bug lived inside the formatter
 * itself, not in how its output was wrapped.
 */

const LOCALE_NEUTRAL = "en-GB";

export function formatNumber(value: number): string {
  return new Intl.NumberFormat(LOCALE_NEUTRAL).format(value);
}

export function formatCurrency(amountSar: number, opts?: { showSymbol?: boolean }): string {
  const amount = new Intl.NumberFormat(LOCALE_NEUTRAL, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(amountSar);
  return opts?.showSymbol === false ? amount : `${amount} SAR`;
}

export function formatDate(date: Date | string): string {
  const d = typeof date === "string" ? new Date(date) : date;
  return new Intl.DateTimeFormat(LOCALE_NEUTRAL, {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    timeZone: "Asia/Riyadh",
  }).format(d);
}

export function formatDateTime(date: Date | string): string {
  const d = typeof date === "string" ? new Date(date) : date;
  return new Intl.DateTimeFormat(LOCALE_NEUTRAL, {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
    timeZone: "Asia/Riyadh",
  }).format(d);
}

/** e.g. formatExperienceRange(1, 3) -> "1-3", formatExperienceRange(10, null) -> "10+" */
export function formatExperienceRange(minYears: number, maxYears: number | null): string {
  return maxYears == null
    ? `${formatNumber(minYears)}+`
    : `${formatNumber(minYears)}-${formatNumber(maxYears)}`;
}
