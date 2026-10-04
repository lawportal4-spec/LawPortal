import { useState, type FormEvent } from "react";
import { useNavigate, useLocation, Link } from "react-router-dom";
import { Button } from "@law-portal/ui";
import { useTranslation } from "@law-portal/i18n";
import { AuthLayout, Field, PasswordInput, SaudiPhoneInput } from "../components/AuthForm";
import { useAuth } from "../lib/authContext";
import { lawyerLogin, toSaudiE164 } from "../lib/authApi";

export default function Login() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const location = useLocation();
  const { login } = useAuth();

  const [phone, setPhone] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const phoneE164 = toSaudiE164(phone);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (!phoneE164) {
      setError(t("lawyerAuth.register.errors.phone"));
      return;
    }
    setError(null);
    setBusy(true);
    try {
      const result = await lawyerLogin(phoneE164, password);
      login(result);
      const from = (location.state as { from?: string } | null)?.from ?? "/";
      navigate(from, { replace: true });
    } catch {
      setError(t("lawyerAuth.login.failed"));
    } finally {
      setBusy(false);
    }
  }

  return (
    <AuthLayout title={t("lawyerAuth.login.title")}>
      <form className="flex flex-col gap-4" onSubmit={handleSubmit} noValidate>
        <Field label={t("lawyerAuth.register.phone")} required>
          <SaudiPhoneInput value={phone} onChange={(e) => setPhone(e.target.value)} />
        </Field>
        <Field label={t("lawyerAuth.register.password")} required>
          <PasswordInput value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="current-password" />
        </Field>
        {error && <p className="text-sm text-rubric">{error}</p>}
        <Button type="submit" className="mt-2 w-full justify-center" disabled={busy || !phone || !password}>
          {t("lawyerAuth.login.submit")}
        </Button>
        <p className="text-center text-sm text-ink-faint">
          {t("lawyerAuth.login.noAccount")}{" "}
          <Link to="/register" className="font-semibold text-ink hover:text-seal">
            {t("lawyerAuth.login.register")}
          </Link>
        </p>
      </form>
    </AuthLayout>
  );
}
