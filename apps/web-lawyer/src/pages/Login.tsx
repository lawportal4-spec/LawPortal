import { useState, type FormEvent } from "react";
import { useNavigate, useLocation, Link } from "react-router-dom";
import { Mail, Lock } from "lucide-react";
import { Button, Card, Input } from "@law-portal/ui";
import { useTranslation } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { useAuth } from "../lib/authContext";
import { lawyerLogin } from "../lib/authApi";

export default function Login() {
  const { i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const navigate = useNavigate();
  const location = useLocation();
  const { login } = useAuth();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const result = await lawyerLogin(email, password);
      login(result);
      const from = (location.state as { from?: string } | null)?.from ?? "/";
      navigate(from, { replace: true });
    } catch {
      setError(isAr ? "البريد الإلكتروني أو كلمة المرور غير صحيحة." : "Incorrect email or password.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <AppShell>
      <div className="mx-auto max-w-md">
        <h1 className="mb-1 font-display text-2xl font-bold">
          {isAr ? "تسجيل دخول المحامي" : "Lawyer sign in"}
        </h1>
        <p className="mb-6 text-sm text-ink-faint">
          {isAr ? "الدخول إلى لوحة إدارة طلباتك وملفك المهني." : "Access your requests and professional profile."}
        </p>

        <Card>
          <form className="flex flex-col gap-4" onSubmit={handleSubmit}>
            <label className="text-sm font-medium text-ink-soft">
              {isAr ? "البريد الإلكتروني" : "Email"}
            </label>
            <Input
              icon={<Mail className="h-4 w-4" />}
              type="email"
              dir="ltr"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="font-mono"
              required
            />
            <label className="text-sm font-medium text-ink-soft">
              {isAr ? "كلمة المرور" : "Password"}
            </label>
            <Input
              icon={<Lock className="h-4 w-4" />}
              type="password"
              dir="ltr"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />
            {error && <p className="text-sm text-rubric">{error}</p>}
            <Button type="submit" disabled={busy || !email || !password}>
              {busy ? (isAr ? "جارٍ الدخول…" : "Signing in…") : isAr ? "تسجيل الدخول" : "Sign in"}
            </Button>
            <p className="text-center text-xs text-ink-faint">
              {isAr ? "جديد على بوابة القانون؟" : "New to Law Portal?"}{" "}
              <Link to="/register" className="font-medium text-seal">
                {isAr ? "سجّل كمحامٍ" : "Register as a lawyer"}
              </Link>
            </p>
          </form>
        </Card>
      </div>
    </AppShell>
  );
}
