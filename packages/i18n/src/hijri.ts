/**
 * Saudi lawyer licences carry Umm al-Qura (Hijri) dates, so the registration wizard takes them
 * that way and converts to the Gregorian ISO date the API stores. `Intl` only converts
 * Gregorian → Hijri, so the reverse is an estimate refined against `Intl` until it matches.
 */
const hijriFormat = new Intl.DateTimeFormat("en-u-ca-islamic-umalqura", {
  day: "numeric",
  month: "numeric",
  year: "numeric",
  timeZone: "UTC",
});

const DAY_MS = 86_400_000;
// 1 Muharram 1 AH ≈ 19 July 622 (proleptic Gregorian); only the starting guess, so ± a few days is fine.
const EPOCH_MS = Date.UTC(622, 6, 19);
const YEAR_DAYS = 354.367;
const MONTH_DAYS = 29.53;

interface HijriParts {
  day: number;
  month: number;
  year: number;
}

function toHijri(utcMs: number): HijriParts {
  const parts = Object.fromEntries(hijriFormat.formatToParts(new Date(utcMs)).map((p) => [p.type, p.value]));
  return { day: Number(parts.day), month: Number(parts.month), year: Number.parseInt(parts.year, 10) };
}

const serial = ({ day, month, year }: HijriParts) => (year - 1) * YEAR_DAYS + (month - 1) * MONTH_DAYS + day;

/** "22/10/1443" → "2022-05-23", or null if it isn't a real Umm al-Qura date. */
export function hijriToIsoDate(input: string): string | null {
  const match = /^\s*(\d{1,2})\s*\/\s*(\d{1,2})\s*\/\s*(\d{4})\s*$/.exec(input);
  if (!match) return null;
  const target = { day: Number(match[1]), month: Number(match[2]), year: Number(match[3]) };
  // Umm al-Qura data in Intl is only reliable across this span.
  if (target.month < 1 || target.month > 12 || target.day < 1 || target.day > 30 || target.year < 1300 || target.year > 1600) {
    return null;
  }

  let ms = EPOCH_MS + Math.round(serial(target)) * DAY_MS;
  for (let i = 0; i < 6; i++) {
    const got = toHijri(ms);
    if (got.day === target.day && got.month === target.month && got.year === target.year) {
      return new Date(ms).toISOString().slice(0, 10);
    }
    const diff = Math.round(serial(target) - serial(got));
    // A day-30 that doesn't exist in a 29-day month lands on day 1 of the next and never converges.
    if (diff === 0) return null;
    ms += diff * DAY_MS;
  }
  return null;
}
