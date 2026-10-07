import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Button, Card, Input, Ltr } from "@law-portal/ui";
import { hijriToIsoDate, isoToHijriDate, useTranslation } from "@law-portal/i18n";
import { Field } from "./AuthForm";
import { HijriDateInput } from "./HijriDateInput";
import { getLawyerMe } from "../lib/authApi";
import { renewLicense } from "../lib/lawyerApi";

/** Renewal opens 3 months before the licence on file expires (the end date from registration
 * step 3) — the API enforces the same window. Dates are entered in Hijri, as at registration. */
export function LicenseRenewalCard() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const me = useQuery({ queryKey: ["lawyerMe"], queryFn: getLawyerMe, gcTime: 0 });
  const [licenseNumber, setLicenseNumber] = useState("");
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [showErrors, setShowErrors] = useState(false);

  const issueIso = hijriToIsoDate(startDate);
  const expiryIso = hijriToIsoDate(endDate);
  const currentExpiry = me.data?.expiryDate ?? "";
  const datesValid = !!issueIso && !!expiryIso && expiryIso > issueIso && expiryIso > currentExpiry;

  const renew = useMutation({
    mutationFn: () => renewLicense({ licenseNumber, issueDate: issueIso!, expiryDate: expiryIso! }),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ["lawyerMe"] }),
  });

  if (!me.data) return null;
  // Compared as ISO "YYYY-MM-DD" strings, in the browser's local calendar day.
  const today = new Date(Date.now() - new Date().getTimezoneOffset() * 60_000).toISOString().slice(0, 10);
  const isOpen = today >= me.data.renewalOpensOn;

  return (
    <Card>
      <h2 className="mb-4 text-sm font-semibold text-ink-soft">{t("licenseRenewal.title")}</h2>
      <p className="mb-4 text-sm text-ink-soft">
        {t("licenseRenewal.expiresOn")} <Ltr className="font-mono font-semibold text-ink">{isoToHijriDate(currentExpiry)}</Ltr>
      </p>

      {me.data.renewalPending ? (
        <p className="text-sm font-medium text-seal">{t("licenseRenewal.pending")}</p>
      ) : !isOpen ? (
        <p className="text-sm text-ink-faint">
          {t("licenseRenewal.notYet")} <Ltr className="font-mono text-ink">{isoToHijriDate(me.data.renewalOpensOn)}</Ltr>
        </p>
      ) : (
        <form
          className="flex flex-col gap-4"
          noValidate
          onSubmit={(e) => {
            e.preventDefault();
            if (!licenseNumber.trim() || !datesValid) return setShowErrors(true);
            renew.mutate();
          }}
        >
          <Field label={t("licenseRenewal.licenseNumber")} required>
            <Input dir="ltr" className="font-mono" value={licenseNumber} onChange={(e) => setLicenseNumber(e.target.value)} />
          </Field>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <Field label={t("lawyerAuth.register.startDate")} required>
              <HijriDateInput value={startDate} onChange={setStartDate} />
            </Field>
            <Field label={t("lawyerAuth.register.endDate")} required>
              <HijriDateInput value={endDate} onChange={setEndDate} align="end" />
            </Field>
          </div>
          {showErrors && !datesValid && <p className="text-sm text-rubric">{t("licenseRenewal.datesInvalid")}</p>}
          {renew.isError && <p className="text-sm text-rubric">{t("licenseRenewal.failed")}</p>}
          <Button type="submit" className="self-start" disabled={renew.isPending}>
            {t("licenseRenewal.submit")}
          </Button>
        </form>
      )}
    </Card>
  );
}
