import { useState, type FormEvent } from "react";
import { useQuery } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { Link } from "react-router-dom";
import { Mail, Lock, User, BadgeCheck } from "lucide-react";
import { Button, Card, Input } from "@law-portal/ui";
import { useTranslation } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { useAuth } from "../lib/authContext";
import { getRegions, registerLawyer } from "../lib/authApi";

/**
 * The documented 3-step lawyer registration wizard (personal data → region/city → licence)
 * collapsed into one form submitting to the one call `RegisterLawyerCommand` has always
 * accepted since P1 — the same "one backend call, present it however makes sense on the
 * frontend" pattern used throughout this project. Never built until P12, when the marketing
 * site's "join as lawyer" funnel needed a real destination to send lawyers to.
 */
export default function Register() {
  const { i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const navigate = useNavigate();
  const { login } = useAuth();

  const regionsQuery = useQuery({ queryKey: ["regions"], queryFn: getRegions });

  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [regionId, setRegionId] = useState<number | null>(null);
  const [cityId, setCityId] = useState<number | null>(null);
  const [licenseNumber, setLicenseNumber] = useState("");
  const [issueDate, setIssueDate] = useState("");
  const [expiryDate, setExpiryDate] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const selectedRegion = regionsQuery.data?.find((r) => r.id === regionId);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const result = await registerLawyer({
        fullName,
        email,
        password,
        regionId,
        cityId,
        licenseNumber,
        issueDate,
        expiryDate,
      });
      login(result);
      navigate("/", { replace: true });
    } catch {
      setError(
        isAr
          ? "تعذّر إنشاء الحساب — تأكد من صحة البيانات أو أن البريد/رقم الترخيص غير مُستخدم من قبل."
          : "Could not create the account — check your details, or the email/licence number may already be registered.",
      );
    } finally {
      setBusy(false);
    }
  }

  return (
    <AppShell>
      <div className="mx-auto max-w-lg">
        <h1 className="mb-1 font-display text-2xl font-bold">{isAr ? "التسجيل كمحامٍ" : "Register as a Lawyer"}</h1>
        <p className="mb-6 text-sm text-ink-faint">
          {isAr
            ? "أنشئ حسابك المهني — سيقوم فريقنا بمراجعة ترخيصك قبل ظهور ملفك في الدليل."
            : "Create your professional account — our team reviews your licence before your profile goes live in the directory."}
        </p>

        <Card>
          <form className="flex flex-col gap-5" onSubmit={handleSubmit}>
            <div>
              <h2 className="mb-3 text-xs font-semibold uppercase tracking-wide text-ink-faint">{isAr ? "البيانات الشخصية" : "Personal details"}</h2>
              <div className="flex flex-col gap-3">
                <Input icon={<User className="h-4 w-4" />} placeholder={isAr ? "الاسم الكامل" : "Full name"} value={fullName} onChange={(e) => setFullName(e.target.value)} required />
                <Input
                  icon={<Mail className="h-4 w-4" />}
                  type="email"
                  dir="ltr"
                  className="font-mono"
                  placeholder={isAr ? "البريد الإلكتروني" : "Email"}
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  required
                />
                <Input
                  icon={<Lock className="h-4 w-4" />}
                  type="password"
                  dir="ltr"
                  placeholder={isAr ? "كلمة المرور (8 أحرف على الأقل، حرف كبير ورقم)" : "Password (8+ chars, one uppercase, one digit)"}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                />
              </div>
            </div>

            <div>
              <h2 className="mb-3 text-xs font-semibold uppercase tracking-wide text-ink-faint">{isAr ? "الموقع" : "Location"}</h2>
              <div className="grid grid-cols-2 gap-3">
                <select
                  className="rounded-md border border-border bg-surface-raised px-3 py-2.5 text-sm"
                  value={regionId ?? ""}
                  onChange={(e) => {
                    setRegionId(e.target.value ? Number(e.target.value) : null);
                    setCityId(null);
                  }}
                >
                  <option value="">{isAr ? "المنطقة" : "Region"}</option>
                  {regionsQuery.data?.map((r) => (
                    <option key={r.id} value={r.id}>
                      {isAr ? r.nameAr : r.nameEn}
                    </option>
                  ))}
                </select>
                <select
                  className="rounded-md border border-border bg-surface-raised px-3 py-2.5 text-sm disabled:opacity-50"
                  value={cityId ?? ""}
                  onChange={(e) => setCityId(e.target.value ? Number(e.target.value) : null)}
                  disabled={!selectedRegion}
                >
                  <option value="">{isAr ? "المدينة" : "City"}</option>
                  {selectedRegion?.cities.map((c) => (
                    <option key={c.id} value={c.id}>
                      {isAr ? c.nameAr : c.nameEn}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            <div>
              <h2 className="mb-3 flex items-center gap-1.5 text-xs font-semibold uppercase tracking-wide text-ink-faint">
                <BadgeCheck className="h-3.5 w-3.5" />
                {isAr ? "ترخيص المحاماة" : "Law licence"}
              </h2>
              <div className="flex flex-col gap-3">
                <Input placeholder={isAr ? "رقم الترخيص" : "Licence number"} className="font-mono" dir="ltr" value={licenseNumber} onChange={(e) => setLicenseNumber(e.target.value)} required />
                <div className="grid grid-cols-2 gap-3">
                  <label className="text-xs text-ink-faint">
                    {isAr ? "تاريخ الإصدار" : "Issue date"}
                    <input
                      type="date"
                      value={issueDate}
                      onChange={(e) => setIssueDate(e.target.value)}
                      className="mt-1 w-full rounded-md border border-border bg-surface-raised px-3 py-2 text-sm"
                      required
                    />
                  </label>
                  <label className="text-xs text-ink-faint">
                    {isAr ? "تاريخ الانتهاء" : "Expiry date"}
                    <input
                      type="date"
                      value={expiryDate}
                      onChange={(e) => setExpiryDate(e.target.value)}
                      className="mt-1 w-full rounded-md border border-border bg-surface-raised px-3 py-2 text-sm"
                      required
                    />
                  </label>
                </div>
              </div>
            </div>

            {error && <p className="text-sm text-rubric">{error}</p>}

            <Button type="submit" disabled={busy || !fullName || !email || !password || !licenseNumber || !issueDate || !expiryDate}>
              {busy ? (isAr ? "جارٍ الإنشاء…" : "Creating account…") : isAr ? "إنشاء الحساب" : "Create account"}
            </Button>

            <p className="text-center text-xs text-ink-faint">
              {isAr ? "لديك حساب؟" : "Already have an account?"}{" "}
              <Link to="/login" className="font-medium text-seal">
                {isAr ? "تسجيل الدخول" : "Sign in"}
              </Link>
            </p>
          </form>
        </Card>
      </div>
    </AppShell>
  );
}
