import { useState } from "react";
import { CircleCheck, MailCheck } from "lucide-react";
import { Button } from "@law-portal/ui";
import { useTranslation } from "@law-portal/i18n";
import { resendVerificationEmail, type LawyerMeDto } from "../lib/authApi";
import { useCountdown } from "../lib/useCountdown";

const RESEND_SECONDS = 60;

/** Approved, email not confirmed yet: the link went to their inbox. */
export function CheckEmail({ me }: { me: LawyerMeDto }) {
  const { t } = useTranslation();
  const [secondsLeft, restart] = useCountdown(RESEND_SECONDS, 0);
  const [notice, setNotice] = useState<{ ok: boolean; text: string } | null>(null);

  async function resend() {
    setNotice(null);
    try {
      await resendVerificationEmail();
      setNotice({ ok: true, text: t("lawyerOnboarding.resent") });
      restart();
    } catch {
      setNotice({ ok: false, text: t("lawyerOnboarding.resendFailed") });
    }
  }

  return (
    <div className="flex flex-col items-center gap-4 text-center">
      <MailCheck className="h-12 w-12 text-seal" />
      <p className="text-lg text-ink">{t("lawyerAuth.pending.greeting", { name: me.fullName })}</p>
      <p className="font-display text-2xl text-seal">{t("lawyerOnboarding.checkEmailTitle")}</p>
      <p className="text-sm text-ink-soft">{t("lawyerOnboarding.checkEmailBody")}</p>
      {me.email && <bdi className="font-mono text-sm font-semibold text-ink">{me.email}</bdi>}
      <Button variant="secondary" onClick={resend} disabled={secondsLeft > 0}>
        {secondsLeft > 0 ? t("lawyerOnboarding.resendIn", { seconds: secondsLeft }) : t("lawyerOnboarding.resend")}
      </Button>
      {notice && <p className={"text-sm " + (notice.ok ? "text-seal" : "text-rubric")}>{notice.text}</p>}
    </div>
  );
}

/** "تم قبول طلب الانضمام…" — shown once the email is confirmed, before the fee. */
export function Accepted({ name, onStart }: { name: string; onStart: () => void }) {
  const { t } = useTranslation();
  return (
    <div className="flex flex-col items-center gap-4 text-center">
      <CircleCheck className="h-14 w-14 text-seal" />
      <p className="font-display text-2xl text-ink">{t("lawyerOnboarding.acceptedTitle")}</p>
      <p className="text-lg text-ink">{t("lawyerAuth.pending.greeting", { name })}</p>
      <p className="text-seal">{t("lawyerAuth.pending.thanks")}</p>
      <Button className="mt-4 w-full max-w-xs justify-center" onClick={onStart}>
        {t("lawyerOnboarding.start")}
      </Button>
    </div>
  );
}
