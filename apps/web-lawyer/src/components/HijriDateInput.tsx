import { useEffect, useRef, useState } from "react";
import { clsx } from "clsx";
import { CalendarDays, ChevronLeft, ChevronRight } from "lucide-react";
import { Input, Ltr } from "@law-portal/ui";
import { hijriMonthLayout, todayHijri, useTranslation } from "@law-portal/i18n";

// Licence dates reach back decades and forward a few years; the converter is reliable 1300–1600.
const FIRST_YEAR = 1380;
const YEARS_AHEAD = 15;

const pad = (n: number) => String(n).padStart(2, "0");

function parse(value: string) {
  const m = /^\s*(\d{1,2})\/(\d{1,2})\/(\d{4})\s*$/.exec(value);
  return m
    ? { day: Number(m[1]), month: Number(m[2]), year: Number(m[3]) }
    : null;
}

/**
 * A Hijri (Umm al-Qura) date field: type "dd/mm/yyyy" or pick from the calendar. The browser's
 * own date picker can't show the Hijri calendar, which is what Saudi licences are dated in.
 */
export function HijriDateInput({
  value,
  onChange,
  align = "start",
  ...aria
}: {
  value: string;
  onChange: (value: string) => void;
  /** Which field edge the popup hangs from — "end" for a field in the trailing column, so the
   * popup opens back over the form instead of past its edge (off-screen on a phone). */
  align?: "start" | "end";
  "aria-label"?: string;
  "aria-describedby"?: string;
}) {
  const { t, i18n } = useTranslation();
  const [open, setOpen] = useState(false);
  const today = todayHijri();
  const selected = parse(value);
  const [view, setView] = useState(() => ({
    year: selected?.year ?? today.year,
    month: selected?.month ?? today.month,
  }));
  const wrapper = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    const onPointer = (e: MouseEvent) => {
      if (!wrapper.current?.contains(e.target as Node)) setOpen(false);
    };
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") setOpen(false);
    };
    document.addEventListener("mousedown", onPointer);
    document.addEventListener("keydown", onKey);
    return () => {
      document.removeEventListener("mousedown", onPointer);
      document.removeEventListener("keydown", onKey);
    };
  }, [open]);

  function openCalendar() {
    if (open) return;
    // Re-centre on whatever is typed (or today) each time it opens.
    setView({
      year: selected?.year ?? today.year,
      month: selected?.month ?? today.month,
    });
    setOpen(true);
  }

  function shiftMonth(delta: number) {
    setView(({ year, month }) => {
      const index = year * 12 + (month - 1) + delta;
      return { year: Math.floor(index / 12), month: (index % 12) + 1 };
    });
  }

  const layout = hijriMonthLayout(view.year, view.month);
  const months = t("lawyerAuth.hijriCalendar.months", {
    returnObjects: true,
  }) as string[];
  const weekdays = t("lawyerAuth.hijriCalendar.weekdays", {
    returnObjects: true,
  }) as string[];
  const years = Array.from(
    { length: today.year + YEARS_AHEAD - FIRST_YEAR + 1 },
    (_, i) => FIRST_YEAR + i,
  );

  return (
    <div ref={wrapper} className="relative">
      {/* Icon inside the field as in the reference design. Input renders a <label> wrapper, so a
          click on the icon lands on the input too — one handler opens the calendar for both.
          The wrapper is LTR so the icon sits on the left beside the date, as dates read LTR. */}
      <div dir="ltr">
        <Input
          {...aria}
          icon={
            <CalendarDays
              aria-hidden
              className="h-4 w-4 shrink-0 text-ink-faint"
            />
          }
          dir="ltr"
          // Digits in the mono face; the Arabic placeholder in the body face — mono spaces Arabic letters apart.
          // Empty in Arabic: the placeholder sits at the far (right) end — the field is LTR, so "end" is right.
          className={clsx("cursor-pointer font-mono [&_input::placeholder]:font-body", !value && i18n.language === "ar" && "[&_input]:text-end")}
          inputMode="numeric"
          placeholder={t("lawyerAuth.register.hijriPlaceholder")}
          value={value}
          onChange={(e) => onChange(e.target.value)}
          onClick={openCalendar}
          onKeyDown={(e) => {
            if (e.key === "ArrowDown" || (e.key === "Enter" && !open)) {
              e.preventDefault();
              openCalendar();
            }
          }}
          aria-haspopup="dialog"
          aria-expanded={open}
        />
      </div>

      {open && layout && (
        <div
          role="dialog"
          aria-label={aria["aria-label"]}
          className={clsx(
            "absolute top-full z-20 mt-1 w-72 rounded-lg border border-border bg-surface-raised p-3 shadow-raised",
            align === "end" ? "end-0" : "start-0",
          )}
        >
          <div className="mb-2 flex items-center justify-between gap-1">
            <button
              type="button"
              onClick={() => shiftMonth(-1)}
              aria-label={t("lawyerAuth.hijriCalendar.prevMonth")}
              className="rounded p-1 text-ink-soft hover:bg-surface"
            >
              <ChevronLeft className="h-4 w-4 rtl:rotate-180" />
            </button>
            <div className="flex items-center gap-1">
              <span className="text-sm font-semibold text-ink">
                {months[view.month - 1]}
              </span>
              <select
                aria-label={t("lawyerAuth.hijriCalendar.year")}
                value={view.year}
                onChange={(e) =>
                  setView((v) => ({ ...v, year: Number(e.target.value) }))
                }
                className="rounded border border-border bg-surface px-1 py-0.5 font-mono text-sm text-ink"
              >
                {years.map((y) => (
                  <option key={y} value={y}>
                    {y}
                  </option>
                ))}
              </select>
            </div>
            <button
              type="button"
              onClick={() => shiftMonth(1)}
              aria-label={t("lawyerAuth.hijriCalendar.nextMonth")}
              className="rounded p-1 text-ink-soft hover:bg-surface"
            >
              <ChevronRight className="h-4 w-4 rtl:rotate-180" />
            </button>
          </div>

          <div className="grid grid-cols-7 gap-1 text-center">
            {weekdays.map((w) => (
              <span key={w} className="py-1 text-[11px] text-ink-faint">
                {w}
              </span>
            ))}
            {Array.from({ length: layout.firstWeekday }, (_, i) => (
              <span key={`blank-${i}`} />
            ))}
            {Array.from({ length: layout.days }, (_, i) => {
              const day = i + 1;
              const isSelected =
                selected?.day === day &&
                selected.month === view.month &&
                selected.year === view.year;
              const isToday =
                today.day === day &&
                today.month === view.month &&
                today.year === view.year;
              return (
                <button
                  key={day}
                  type="button"
                  aria-pressed={isSelected}
                  aria-label={`${day} ${months[view.month - 1]} ${view.year}`}
                  onClick={() => {
                    onChange(`${pad(day)}/${pad(view.month)}/${view.year}`);
                    setOpen(false);
                  }}
                  className={clsx(
                    "rounded py-1.5 font-mono text-sm",
                    isSelected
                      ? "bg-seal text-seal-on"
                      : "text-ink hover:bg-seal-tint",
                    isToday && !isSelected && "ring-1 ring-seal",
                  )}
                >
                  <Ltr>{day}</Ltr>
                </button>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
}
