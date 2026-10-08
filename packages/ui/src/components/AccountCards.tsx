import { useEffect, useState, type FormEvent, type ReactNode } from "react";
import { useTranslation } from "@law-portal/i18n";
import { Button } from "./Button";
import { Card } from "./Card";
import { Input } from "./Input";
import { Ltr } from "./Ltr";
import { SectionHeading } from "./SectionHeading";

// The "حسابي" building blocks shared by the client, lawyer and admin apps. Each app passes its own
// API calls (they carry that app's auth), so these stay free of any HTTP or query library.

export interface MyProfile {
  userType: string;
  name: string | null;
  email: string | null;
  phoneE164: string | null;
  regionId: number | null;
  cityId: number | null;
  hasPassword: boolean;
  canEditName: boolean;
  memberSinceUtc: string;
}

export interface RegionOption {
  id: number;
  nameAr: string;
  nameEn: string;
  cities: { id: number; nameAr: string; nameEn: string }[];
}

const selectClass =
  "w-full rounded-md border border-border bg-surface-raised px-4 py-2.5 text-sm text-ink focus:border-seal focus:outline-none focus:ring-2 focus:ring-seal/30";

function Label({ text, children }: { text: string; children: ReactNode }) {
  return (
    <label className="flex flex-col gap-1.5">
      <span className="text-xs font-medium text-ink-soft">{text}</span>
      {children}
    </label>
  );
}

function FieldError({ text }: { text: string | null }) {
  return text ? <p className="-mt-2 text-xs text-rubric">{text}</p> : null;
}

function Status({ state, okText, failText }: { state: "idle" | "ok" | "fail"; okText: string; failText: string }) {
  if (state === "idle") return null;
  return <p className={"text-sm " + (state === "ok" ? "text-seal-strong" : "text-rubric")}>{state === "ok" ? okText : failText}</p>;
}

/** Name (and city, when `loadRegions` is given) plus the read-only sign-in details. */
export function ProfileCard({
  profile,
  onSave,
  loadRegions,
}: {
  profile: MyProfile;
  onSave: (input: { name: string; cityId: number | null }) => Promise<void>;
  loadRegions?: () => Promise<RegionOption[]>;
}) {
  const { t, i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const [name, setName] = useState(profile.name ?? "");
  const [regions, setRegions] = useState<RegionOption[]>([]);
  const [regionId, setRegionId] = useState(profile.regionId ?? 0);
  const [cityId, setCityId] = useState(profile.cityId ?? 0);
  const [state, setState] = useState<"idle" | "ok" | "fail">("idle");
  const [busy, setBusy] = useState(false);
  const [tried, setTried] = useState(false);
  const nameError = tried && !name.trim() ? t("form.required") : null;

  useEffect(() => {
    loadRegions?.().then(setRegions).catch(() => setRegions([]));
  }, [loadRegions]);

  const cities = regions.find((r) => r.id === regionId)?.cities ?? [];

  async function submit(e: FormEvent) {
    e.preventDefault();
    setTried(true);
    if (!name.trim()) return;
    setBusy(true);
    try {
      await onSave({ name: name.trim(), cityId: cityId || null });
      setState("ok");
    } catch {
      setState("fail");
    } finally {
      setBusy(false);
    }
  }

  return (
    <Card>
      <SectionHeading level={3} className="mb-4">
        {t("account.profile")}
      </SectionHeading>
      <form className="flex flex-col gap-4" onSubmit={submit}>
        <Label text={t("account.name")}>
          <Input id="account-name" value={name} disabled={!profile.canEditName} maxLength={150} aria-invalid={!!nameError}
            className={nameError ? "border-rubric!" : undefined}
            onChange={(e) => { setName(e.target.value); setState("idle"); }} />
        </Label>
        <FieldError text={nameError} />
        {!profile.canEditName && <p className="-mt-2 text-xs text-ink-faint">{t("account.nameFromLicence")}</p>}

        {loadRegions && (
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <Label text={t("account.region")}>
              <select id="account-region" className={selectClass} value={regionId || ""}
                onChange={(e) => { setRegionId(Number(e.target.value)); setCityId(0); setState("idle"); }}>
                <option value="">—</option>
                {regions.map((r) => <option key={r.id} value={r.id}>{isAr ? r.nameAr : r.nameEn}</option>)}
              </select>
            </Label>
            <Label text={t("account.city")}>
              <select id="account-city" className={selectClass} value={cityId || ""}
                onChange={(e) => { setCityId(Number(e.target.value)); setState("idle"); }}>
                <option value="">—</option>
                {cities.map((c) => <option key={c.id} value={c.id}>{isAr ? c.nameAr : c.nameEn}</option>)}
              </select>
            </Label>
          </div>
        )}

        <dl className="grid grid-cols-1 gap-3 rounded-md border border-border bg-paper p-4 text-sm sm:grid-cols-3">
          {profile.phoneE164 && (
            <div><dt className="text-xs text-ink-faint">{t("account.phone")}</dt><dd><Ltr className="font-mono">{profile.phoneE164}</Ltr></dd></div>
          )}
          {profile.email && (
            <div><dt className="text-xs text-ink-faint">{t("account.email")}</dt><dd><Ltr className="font-mono">{profile.email}</Ltr></dd></div>
          )}
          <div><dt className="text-xs text-ink-faint">{t("account.memberSince")}</dt>
            <dd><Ltr className="font-mono">{new Date(profile.memberSinceUtc).toLocaleDateString("en-GB")}</Ltr></dd></div>
        </dl>

        {profile.canEditName && (
          <div className="flex items-center gap-3">
            <Button type="submit" disabled={busy}>{t("account.save")}</Button>
            <Status state={state} okText={t("account.saved")} failText={t("account.saveFailed")} />
          </div>
        )}
      </form>
    </Card>
  );
}

const PASSWORD_RULE = /^(?=.*[A-Z])(?=.*\d).{8,}$/;

export function ChangePasswordCard({ onChange }: { onChange: (currentPassword: string, newPassword: string) => Promise<void> }) {
  const { t } = useTranslation();
  const [current, setCurrent] = useState("");
  const [next, setNext] = useState("");
  const [confirm, setConfirm] = useState("");
  const [state, setState] = useState<"idle" | "ok" | "fail">("idle");
  const [busy, setBusy] = useState(false);

  const [tried, setTried] = useState(false);
  const required = (v: string) => (tried && !v ? t("form.required") : null);
  const currentError = required(current);
  const ruleError = required(next) ?? (next && !PASSWORD_RULE.test(next) && tried ? t("account.passwordRule") : null);
  const matchError = required(confirm) ?? (confirm && confirm !== next ? t("account.passwordMismatch") : null);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setTried(true);
    if (!current || !next || !confirm || !PASSWORD_RULE.test(next) || confirm !== next) return;
    setBusy(true);
    try {
      await onChange(current, next);
      setState("ok");
      setCurrent(""); setNext(""); setConfirm(""); setTried(false);
    } catch {
      setState("fail");
    } finally {
      setBusy(false);
    }
  }

  return (
    <Card>
      <SectionHeading level={3} className="mb-4">
        {t("account.password")}
      </SectionHeading>
      <form className="flex flex-col gap-4" onSubmit={submit}>
        <Label text={t("account.currentPassword")}>
          <Input id="account-current-password" type="password" autoComplete="current-password" value={current}
            aria-invalid={!!currentError} className={currentError ? "border-rubric!" : undefined}
            onChange={(e) => { setCurrent(e.target.value); setState("idle"); }} />
        </Label>
        <FieldError text={currentError} />
        <Label text={t("account.newPassword")}>
          <Input id="account-new-password" type="password" autoComplete="new-password" value={next}
            aria-invalid={!!ruleError} className={ruleError ? "border-rubric!" : undefined}
            onChange={(e) => { setNext(e.target.value); setState("idle"); }} />
        </Label>
        {ruleError ? <FieldError text={ruleError} /> : <p className="-mt-2 text-xs text-ink-faint">{t("account.passwordRule")}</p>}
        <Label text={t("account.confirmPassword")}>
          <Input id="account-confirm-password" type="password" autoComplete="new-password" value={confirm}
            aria-invalid={!!matchError} className={matchError ? "border-rubric!" : undefined}
            onChange={(e) => { setConfirm(e.target.value); setState("idle"); }} />
        </Label>
        <FieldError text={matchError} />
        <div className="flex items-center gap-3">
          <Button type="submit" disabled={busy}>{t("account.changePassword")}</Button>
          <Status state={state} okText={t("account.passwordChanged")} failText={t("account.passwordFailed")} />
        </div>
      </form>
    </Card>
  );
}
