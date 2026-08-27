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

  const navItems = isAuthenticated
    ? [
        { to: "/", label: t("shell.admin.nav.dashboard") },
        { to: "/lawyers", label: t("shell.admin.nav.lawyerVerification") },
        { to: "/payments", label: t("shell.admin.nav.payments") },
        { to: "/ledger", label: t("shell.admin.nav.reconciliation") },
        { to: "/catalog", label: t("shell.admin.nav.catalogAndPricing") },
        { to: "/subscriptions", label: t("shell.admin.nav.subscriptionPlans") },
        { to: "/users", label: t("shell.admin.nav.usersAndRoles") },
        { to: "/audit", label: t("shell.admin.nav.auditLog") },
      ]
    : [];

  function handleAuthClick() {
    if (isAuthenticated) {
      logout();
      navigate("/login");
    } else {
      navigate("/login");
    }
  }

  return (
    <div className="min-h-screen bg-paper">
      <header className="border-b border-border bg-surface-raised">
        <div className="mx-auto flex max-w-7xl items-center justify-between px-6 py-4">
          <Link to="/" className="flex items-baseline gap-2 font-display text-xl font-bold">
            <span>{isAr ? "بوابة القانون" : "Law Portal"}</span>
            <span className="text-sm font-normal text-ink-faint">{t("shell.admin.tagline")}</span>
          </Link>

          <nav className="hidden flex-wrap items-center gap-5 text-sm font-medium text-ink-soft lg:flex">
            {navItems.map((item) => (
              <Link key={item.to} to={item.to} className={location.pathname === item.to ? "text-seal" : "hover:text-ink"}>
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
                    "rounded-full px-3 py-1 text-xs font-medium transition-colors " +
                    (locale === code ? "bg-seal text-seal-on" : "text-ink-faint")
                  }
                >
                  {code === "ar" ? "العربية" : "English"}
                </button>
              ))}
            </div>
            <button onClick={handleAuthClick} className="rounded-md border border-border px-3 py-2 text-sm font-medium">
              {isAuthenticated ? t("nav.logout") : t("nav.login")}
            </button>
          </div>
        </div>
        {isAuthenticated && (
          <nav className="flex flex-wrap gap-4 border-t border-border px-6 py-2 text-xs font-medium text-ink-soft lg:hidden">
            {navItems.map((item) => (
              <Link key={item.to} to={item.to} className={location.pathname === item.to ? "text-seal" : ""}>
                {item.label}
              </Link>
            ))}
          </nav>
        )}
      </header>
      <main className="mx-auto max-w-7xl px-6 py-8">{children}</main>
    </div>
  );
}
