import { useRef, type ClipboardEvent, type KeyboardEvent } from "react";
import { Ltr } from "@law-portal/ui";
import { useTranslation } from "@law-portal/i18n";

/** One box per digit: typing advances, Backspace on an empty box goes back, pasting fills all.
 * Used for account activation and password reset. */
export function OtpCodeInput({ digits, onChange }: { digits: string[]; onChange: (digits: string[]) => void }) {
  const { t } = useTranslation();
  const boxes = useRef<(HTMLInputElement | null)[]>([]);
  const length = digits.length;

  function setDigit(index: number, value: string) {
    const digit = value.replace(/\D/g, "").slice(-1);
    onChange(digits.map((d, i) => (i === index ? digit : d)));
    if (digit && index < length - 1) boxes.current[index + 1]?.focus();
  }

  function onKeyDown(index: number, e: KeyboardEvent<HTMLInputElement>) {
    if (e.key === "Backspace" && !digits[index] && index > 0) boxes.current[index - 1]?.focus();
  }

  function onPaste(e: ClipboardEvent<HTMLInputElement>) {
    const pasted = e.clipboardData.getData("text").replace(/\D/g, "").slice(0, length);
    if (!pasted) return;
    e.preventDefault();
    onChange(Array.from({ length }, (_, i) => pasted[i] ?? ""));
    boxes.current[Math.min(pasted.length, length - 1)]?.focus();
  }

  return (
    <div dir="ltr" className="flex gap-2">
      {digits.map((d, i) => (
        <input
          key={i}
          ref={(el) => {
            boxes.current[i] = el;
          }}
          value={d}
          onChange={(e) => setDigit(i, e.target.value)}
          onKeyDown={(e) => onKeyDown(i, e)}
          onPaste={onPaste}
          inputMode="numeric"
          autoComplete={i === 0 ? "one-time-code" : "off"}
          maxLength={1}
          aria-label={t("lawyerAuth.otp.digit", { n: i + 1 })}
          className="h-12 w-11 rounded-md border border-border bg-surface-raised text-center font-mono text-lg text-ink focus:border-seal focus:outline-none focus:ring-2 focus:ring-seal/30"
        />
      ))}
    </div>
  );
}

/** "00:42" then an enabled "Resend" once it reaches zero. */
export function ResendTimer({ secondsLeft, onResend }: { secondsLeft: number; onResend: () => void }) {
  const { t } = useTranslation();
  const mm = String(Math.floor(secondsLeft / 60)).padStart(2, "0");
  const ss = String(secondsLeft % 60).padStart(2, "0");
  return (
    <>
      <Ltr className="font-mono text-sm text-ink-faint">
        {mm}:{ss}
      </Ltr>
      <button
        type="button"
        onClick={onResend}
        disabled={secondsLeft > 0}
        className="text-sm text-ink-soft underline disabled:cursor-not-allowed disabled:no-underline disabled:opacity-50"
      >
        {t("lawyerAuth.otp.resend")}
      </button>
    </>
  );
}
