import type { ReactNode } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { useTranslation } from "@law-portal/i18n";
import { useLocale } from "../lib/useLocale";
import { useAuth } from "../lib/authContext";

export function AppShell({ children }: { children: ReactNode }) {
  const { t } = useTranslation();
  const { locale, setLocale } = useLocale();
  const isAr = locale === "ar";
  const location = useLocation();
  const navigate = useNavigate();
  const { isAuthenticated, logout } = useAuth();

  const navItems = [
    { to: "/", label: t("nav.home") },
    { to: "/services", label: isAr ? "خدمات بينه" : "Services" },
    { to: "/lawyers", label: isAr ? "دليل المحامين" : "Lawyer Directory" },
    ...(isAuthenticated ? [{ to: "/orders", label: t("nav.orders") }] : []),
  ];

  function handleAuthClick() {
    if (isAuthenticated) {
      logout();
      navigate("/");
    } else {
      navigate("/login");
    }
  }

  return (
    <div className="min-h-screen bg-paper">
      <header className="border-b border-border bg-surface-raised">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
          <Link to="/" className="flex items-baseline gap-2 font-display text-xl font-bold">
            <span>{isAr ? "بوابة القانون" : "Law Portal"}</span>
            <span className="text-sm font-normal text-ink-faint">
              {isAr ? "Law Portal" : "بوابة القانون"}
            </span>
          </Link>

          <nav className="hidden items-center gap-6 text-sm font-medium text-ink-soft sm:flex">
            {navItems.map((item) => (
              <Link
                key={item.to}
                to={item.to}
                className={location.pathname === item.to ? "text-seal" : "hover:text-ink"}
              >
                {item.label}
              </Link>
            ))}
          </nav>

          <div className="flex items-center gap-3">
            <div className="inline-flex items-center gap-1 rounded-full border border-border bg-surface p-1">
              {(["ar", "en"] as const).map((code) => (
                <button
                  key={code}
                  onClick={() => setLocale(code)}
                  className={
                    "rounded-full px-3 py-1.5 text-sm font-medium transition-colors " +
                    (locale === code ? "bg-seal text-seal-on" : "text-ink-soft")
                  }
                >
                  {code === "ar" ? "العربية" : "English"}
                </button>
              ))}
            </div>

            <button
              onClick={handleAuthClick}
              className="rounded-md border border-border px-3 py-2 text-sm font-medium text-ink-soft hover:border-seal hover:text-seal"
            >
              {isAuthenticated ? t("nav.logout") : isAr ? "تسجيل الدخول" : "Sign in"}
            </button>
          </div>
        </div>
      </header>

      <main className="mx-auto max-w-6xl px-6 py-10">{children}</main>
    </div>
  );
}
