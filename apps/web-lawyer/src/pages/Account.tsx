import { useEffect, useRef, useState, type FormEvent, type ReactNode } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Lock, Plus, Trash2 } from "lucide-react";
import { Avatar, Button, Card, Input, Ltr, SectionHeading } from "@law-portal/ui";
import { useTranslation } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { Field, SaudiPhoneInput, Select } from "../components/AuthForm";
import { OtpCodeInput } from "../components/OtpCodeInput";
import { getRegions, toSaudiE164 } from "../lib/authApi";
import {
  CONTACT_KINDS,
  confirmPhoneChange,
  getAccount,
  removePhoto,
  requestEmailChange,
  requestPhoneChange,
  saveContactNumbers,
  toLocalNumber,
  toSaudiAnyE164,
  updateLocation,
  uploadPhoto,
  type ContactNumberKind,
  type LawyerAccountDto,
} from "../lib/accountApi";

const ACCOUNT_KEY = ["lawyerAccount"];

/** "البيانات الشخصية": photo, sign-in mobile and email (each confirmed before it applies),
 * location, and contact numbers only the platform team sees. */
export default function Account() {
  const { t } = useTranslation();
  const account = useQuery({ queryKey: ACCOUNT_KEY, queryFn: getAccount });

  return (
    <AppShell>
      <SectionHeading level={2} className="mb-6">
        {t("lawyerAccount.title")}
      </SectionHeading>
      {account.data && (
        <div className="flex max-w-2xl flex-col gap-4">
          <PhotoCard account={account.data} />
          <Card>
            <SectionHeading level={3} className="mb-4">
              {t("lawyerAccount.signIn")}
            </SectionHeading>
            <div className="flex flex-col gap-6">
              <MobileSection current={account.data.phoneE164} />
              <EmailSection current={account.data.email} pending={account.data.pendingEmail} />
            </div>
          </Card>
          <LocationCard account={account.data} />
          <ContactsCard account={account.data} />
        </div>
      )}
    </AppShell>
  );
}

function useRefreshAccount() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ACCOUNT_KEY });
}

function Notice({ ok, children }: { ok: boolean; children: ReactNode }) {
  return <p className={"text-sm " + (ok ? "text-seal" : "text-rubric")}>{children}</p>;
}

function PhotoCard({ account }: { account: LawyerAccountDto }) {
  const { t } = useTranslation();
  const refresh = useRefreshAccount();
  const fileRef = useRef<HTMLInputElement>(null);
  const upload = useMutation({ mutationFn: uploadPhoto, onSuccess: refresh });
  const remove = useMutation({ mutationFn: removePhoto, onSuccess: refresh });
  const initials = account.fullName.split(" ").filter(Boolean).slice(0, 2).map((w) => w[0]).join("");

  return (
    <Card>
      <SectionHeading level={3} className="mb-4">
        {t("lawyerAccount.photo")}
      </SectionHeading>
      <div className="flex flex-wrap items-center gap-5">
        <Avatar initials={initials} src={account.photoUrl} size="lg" />
        <div className="flex flex-col gap-2">
          <p className="text-xs text-ink-faint">{t("lawyerAccount.photoHint")}</p>
          <div className="flex flex-wrap gap-2">
            <Button variant="secondary" disabled={upload.isPending} onClick={() => fileRef.current?.click()}>
              {account.photoUrl ? t("lawyerAccount.changePhoto") : t("lawyerAccount.uploadPhoto")}
            </Button>
            {account.photoUrl && (
              <Button variant="ghost" disabled={remove.isPending} onClick={() => remove.mutate()}>
                {t("lawyerAccount.removePhoto")}
              </Button>
            )}
          </div>
          {upload.isError && <Notice ok={false}>{t("lawyerAccount.photoFailed")}</Notice>}
        </div>
        <input
          ref={fileRef}
          type="file"
          accept="image/jpeg,image/png"
          hidden
          onChange={(e) => {
            const file = e.target.files?.[0];
            e.target.value = "";
            if (file) upload.mutate(file);
          }}
        />
      </div>
    </Card>
  );
}

function MobileSection({ current }: { current: string | null }) {
  const { t } = useTranslation();
  const refresh = useRefreshAccount();
  const [editing, setEditing] = useState(false);
  const [phone, setPhone] = useState("");
  const [sentTo, setSentTo] = useState<string | null>(null);
  const [digits, setDigits] = useState<string[]>(() => Array(6).fill(""));
  const [notice, setNotice] = useState<{ ok: boolean; text: string } | null>(null);

  const send = useMutation({
    mutationFn: (e164: string) => requestPhoneChange(e164),
    onSuccess: (_, e164) => setSentTo(e164),
    onError: () => setNotice({ ok: false, text: t("lawyerAccount.mobileFailed") }),
  });
  const confirm = useMutation({
    mutationFn: () => confirmPhoneChange(sentTo!, digits.join("")),
    onSuccess: () => {
      reset();
      setNotice({ ok: true, text: t("lawyerAccount.mobileChanged") });
      void refresh();
    },
    onError: () => setNotice({ ok: false, text: t("lawyerAccount.mobileFailed") }),
  });

  function reset() {
    setEditing(false);
    setPhone("");
    setSentTo(null);
    setDigits(Array(6).fill(""));
  }

  function submitPhone(e: FormEvent) {
    e.preventDefault();
    setNotice(null);
    const e164 = toSaudiE164(phone);
    if (!e164) return setNotice({ ok: false, text: t("lawyerAuth.register.errors.phone") });
    send.mutate(e164);
  }

  return (
    <section className="flex flex-col gap-3">
      <Row label={t("lawyerAccount.mobile")} value={current} onChange={editing ? undefined : () => { setNotice(null); setEditing(true); }} />
      {editing && !sentTo && (
        <form className="flex flex-col gap-3" onSubmit={submitPhone} noValidate>
          <Field label={t("lawyerAccount.newMobile")} required>
            <SaudiPhoneInput value={phone} onChange={(e) => setPhone(e.target.value)} />
          </Field>
          <div className="flex gap-2">
            <Button type="submit" disabled={!phone || send.isPending}>{t("lawyerAccount.sendCode")}</Button>
            <Button type="button" variant="ghost" onClick={reset}>{t("lawyerAccount.cancel")}</Button>
          </div>
        </form>
      )}
      {sentTo && (
        <form className="flex flex-col items-start gap-3" onSubmit={(e) => { e.preventDefault(); setNotice(null); confirm.mutate(); }}>
          <p className="text-sm text-ink-soft">
            {t("lawyerAccount.codeSentTo")} <Ltr className="font-mono text-ink">{toLocalNumber(sentTo)}</Ltr>
          </p>
          <OtpCodeInput digits={digits} onChange={setDigits} />
          <div className="flex gap-2">
            <Button type="submit" disabled={digits.some((d) => !d) || confirm.isPending}>{t("lawyerAccount.confirm")}</Button>
            <Button type="button" variant="ghost" onClick={reset}>{t("lawyerAccount.cancel")}</Button>
          </div>
        </form>
      )}
      {notice && <Notice ok={notice.ok}>{notice.text}</Notice>}
    </section>
  );
}

function EmailSection({ current, pending }: { current: string | null; pending: string | null }) {
  const { t } = useTranslation();
  const refresh = useRefreshAccount();
  const [editing, setEditing] = useState(false);
  const [email, setEmail] = useState("");
  const send = useMutation({
    mutationFn: () => requestEmailChange(email.trim()),
    onSuccess: () => {
      setEditing(false);
      setEmail("");
      void refresh();
    },
  });

  return (
    <section className="flex flex-col gap-3">
      <Row label={t("lawyerAccount.email")} value={current} onChange={editing ? undefined : () => setEditing(true)} />
      {pending && (
        <p className="text-sm text-warning">
          {t("lawyerAccount.pendingEmail")} <bdi className="font-mono">{pending}</bdi>
        </p>
      )}
      {editing && (
        <form className="flex flex-col gap-3" onSubmit={(e) => { e.preventDefault(); send.mutate(); }}>
          <Field label={t("lawyerAccount.newEmail")} required>
            <Input dir="ltr" type="email" value={email} onChange={(e) => setEmail(e.target.value)} />
          </Field>
          <div className="flex gap-2">
            <Button type="submit" disabled={!/^\S+@\S+\.\S+$/.test(email) || send.isPending}>{t("lawyerAccount.sendLink")}</Button>
            <Button type="button" variant="ghost" onClick={() => setEditing(false)}>{t("lawyerAccount.cancel")}</Button>
          </div>
        </form>
      )}
      {send.isError && <Notice ok={false}>{t("lawyerAccount.emailFailed")}</Notice>}
    </section>
  );
}

function Row({ label, value, onChange }: { label: string; value: string | null; onChange?: () => void }) {
  const { t } = useTranslation();
  return (
    <div className="flex flex-wrap items-center justify-between gap-2">
      <div>
        <p className="text-xs text-ink-faint">{label}</p>
        <bdi className="font-mono text-sm font-semibold text-ink">{value?.startsWith("+966") ? toLocalNumber(value) : value}</bdi>
      </div>
      {onChange && (
        <button type="button" onClick={onChange} className="text-sm font-medium text-seal hover:underline">
          {t("lawyerAccount.change")}
        </button>
      )}
    </div>
  );
}

function LocationCard({ account }: { account: LawyerAccountDto }) {
  const { t, i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const refresh = useRefreshAccount();
  const regions = useQuery({ queryKey: ["regions"], queryFn: getRegions });
  const [regionId, setRegionId] = useState(account.regionId ?? 0);
  const [cityId, setCityId] = useState(account.cityId ?? 0);
  const save = useMutation({ mutationFn: () => updateLocation(regionId, cityId), onSuccess: refresh });
  const cities = regions.data?.find((r) => r.id === regionId)?.cities ?? [];

  return (
    <Card>
      <SectionHeading level={3} className="mb-4">
        {t("lawyerAccount.location")}
      </SectionHeading>
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <Field label={t("lawyerAccount.region")} required>
          <Select value={regionId || ""} onChange={(e) => { setRegionId(Number(e.target.value)); setCityId(0); save.reset(); }}>
            <option value="" disabled />
            {regions.data?.map((r) => (
              <option key={r.id} value={r.id}>{isAr ? r.nameAr : r.nameEn}</option>
            ))}
          </Select>
        </Field>
        <Field label={t("lawyerAccount.city")} required>
          <Select value={cityId || ""} onChange={(e) => { setCityId(Number(e.target.value)); save.reset(); }}>
            <option value="" disabled />
            {cities.map((c) => (
              <option key={c.id} value={c.id}>{isAr ? c.nameAr : c.nameEn}</option>
            ))}
          </Select>
        </Field>
      </div>
      <div className="mt-4 flex items-center gap-3">
        <Button
          disabled={!regionId || !cityId || save.isPending || (regionId === account.regionId && cityId === account.cityId)}
          onClick={() => save.mutate()}
        >
          {t("lawyerAccount.saveLocation")}
        </Button>
        {save.isSuccess && <Notice ok>{t("lawyerAccount.saved")}</Notice>}
        {save.isError && <Notice ok={false}>{t("lawyerAccount.saveFailed")}</Notice>}
      </div>
    </Card>
  );
}

interface ContactRow {
  kind: ContactNumberKind;
  contactName: string;
  number: string;
}

function ContactsCard({ account }: { account: LawyerAccountDto }) {
  const { t } = useTranslation();
  const refresh = useRefreshAccount();
  const [rows, setRows] = useState<ContactRow[]>([]);
  const [showErrors, setShowErrors] = useState(false);

  useEffect(() => {
    setRows(account.contactNumbers.map((c) => ({ kind: c.kind, contactName: c.contactName ?? "", number: toLocalNumber(c.phoneE164) })));
  }, [account.contactNumbers]);

  const invalid = rows.some((r) => !toSaudiAnyE164(r.number));
  const save = useMutation({
    mutationFn: () =>
      saveContactNumbers(rows.map((r) => ({ kind: r.kind, contactName: r.contactName.trim() || null, phoneE164: toSaudiAnyE164(r.number)! }))),
    onSuccess: refresh,
  });
  const update = (i: number, patch: Partial<ContactRow>) => {
    save.reset();
    setRows((all) => all.map((r, j) => (j === i ? { ...r, ...patch } : r)));
  };

  return (
    <Card>
      <SectionHeading level={3} className="mb-1">
        {t("lawyerAccount.contacts")}
      </SectionHeading>
      <p className="mb-4 flex items-center gap-1.5 text-xs text-ink-faint">
        <Lock className="h-3.5 w-3.5" />
        {t("lawyerAccount.contactsHint")}
      </p>
      <div className="flex flex-col gap-3">
        {rows.map((r, i) => (
          <div key={i} className="grid grid-cols-1 items-end gap-2 rounded-md border border-border p-3 sm:grid-cols-[9rem_1fr_1fr_auto]">
            <Field label={t("lawyerAccount.kind")}>
              <Select value={r.kind} onChange={(e) => update(i, { kind: e.target.value as ContactNumberKind })}>
                {CONTACT_KINDS.map((k) => (
                  <option key={k} value={k}>{t(`lawyerAccount.kinds.${k}`)}</option>
                ))}
              </Select>
            </Field>
            <Field label={t("lawyerAccount.number")} required>
              <Input dir="ltr" inputMode="tel" className="font-mono" value={r.number} onChange={(e) => update(i, { number: e.target.value })} />
            </Field>
            <Field label={t("lawyerAccount.contactName")}>
              <Input value={r.contactName} onChange={(e) => update(i, { contactName: e.target.value })} />
            </Field>
            <button
              type="button"
              aria-label={t("lawyerAccount.remove")}
              onClick={() => { save.reset(); setRows((all) => all.filter((_, j) => j !== i)); }}
              className="mb-2 justify-self-start text-ink-faint hover:text-rubric"
            >
              <Trash2 className="h-4 w-4" />
            </button>
          </div>
        ))}
      </div>
      {showErrors && invalid && <p className="mt-2 text-sm text-rubric">{t("lawyerAccount.numberInvalid")}</p>}
      <div className="mt-4 flex flex-wrap items-center gap-3">
        {rows.length < 5 && (
          <Button variant="secondary" onClick={() => setRows((all) => [...all, { kind: "Office", contactName: "", number: "" }])}>
            <Plus className="h-4 w-4" />
            {t("lawyerAccount.addNumber")}
          </Button>
        )}
        <Button disabled={save.isPending} onClick={() => (invalid ? setShowErrors(true) : save.mutate())}>
          {t("lawyerAccount.saveContacts")}
        </Button>
        {save.isSuccess && <Notice ok>{t("lawyerAccount.saved")}</Notice>}
        {save.isError && <Notice ok={false}>{t("lawyerAccount.saveFailed")}</Notice>}
      </div>
    </Card>
  );
}
