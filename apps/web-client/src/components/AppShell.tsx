import { useState, type ReactNode } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { Menu, X } from "lucide-react";
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
  const [menuOpen, setMenuOpen] = useState(false);

  const navItems = [
    { to: "/", label: t("nav.home") },
    { to: "/services", label: t("nav.services") },
    { to: "/lawyers", label: t("nav.lawyerDirectory") },
    ...(isAuthenticated
      ? [
          { to: "/orders", label: t("nav.orders") },
          { to: "/wallet", label: t("nav.wallet") },
          { to: "/account", label: t("nav.account") },
        ]
      : []),
  ];

  function handleAuthClick() {
    setMenuOpen(false);
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
          <Link to="/" className="flex shrink-0 items-center gap-2 whitespace-nowrap font-display text-xl font-bold">
            <img src="/favicon.svg" alt="" className="h-9 w-8" />
            <span className="text-seal-strong">{isAr ? "بوابة القانون" : "Law Portal"}</span>
            <span className="hidden text-sm font-normal text-ink-faint lg:inline">
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

          <div className="hidden items-center gap-3 sm:flex">
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
              {isAuthenticated ? t("nav.logout") : t("nav.signIn")}
            </button>
          </div>

          <button
            onClick={() => setMenuOpen((open) => !open)}
            aria-label={menuOpen ? t("nav.closeMenu") : t("nav.openMenu")}
            aria-expanded={menuOpen}
            className="flex h-10 w-10 items-center justify-center rounded-md border border-border text-ink-soft sm:hidden"
          >
            {menuOpen ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
          </button>
        </div>

        {menuOpen && (
          <div className="border-t border-border bg-surface-raised px-6 py-4 sm:hidden">
            <nav className="flex flex-col gap-3 text-sm font-medium text-ink-soft">
              {navItems.map((item) => (
                <Link
                  key={item.to}
                  to={item.to}
                  onClick={() => setMenuOpen(false)}
                  className={location.pathname === item.to ? "text-seal" : "hover:text-ink"}
                >
                  {item.label}
                </Link>
              ))}
            </nav>

            <div className="mt-4 flex items-center justify-between border-t border-rule pt-4">
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
                {isAuthenticated ? t("nav.logout") : t("nav.signIn")}
              </button>
            </div>
          </div>
        )}
      </header>

      <main className="mx-auto max-w-6xl px-6 py-10">{children}</main>
    </div>
  );
}
