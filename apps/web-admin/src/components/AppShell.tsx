import type { ReactNode } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { useLocale } from "../lib/useLocale";
import { useAuth } from "../lib/authContext";

export function AppShell({ children }: { children: ReactNode }) {
  const { locale, setLocale } = useLocale();
  const isAr = locale === "ar";
  const location = useLocation();
  const navigate = useNavigate();
  const { isAuthenticated, logout } = useAuth();

  const navItems = isAuthenticated
    ? [
        { to: "/", label: isAr ? "لوحة التحكم" : "Dashboard" },
        { to: "/lawyers", label: isAr ? "توثيق المحامين" : "Lawyer Verification" },
        { to: "/payments", label: isAr ? "المدفوعات" : "Payments" },
        { to: "/ledger", label: isAr ? "التسوية المحاسبية" : "Reconciliation" },
        { to: "/catalog", label: isAr ? "الخدمات والتسعير" : "Catalog & Pricing" },
        { to: "/subscriptions", label: isAr ? "خطط الاشتراك" : "Subscription Plans" },
        { to: "/users", label: isAr ? "المستخدمون والصلاحيات" : "Users & Roles" },
        { to: "/audit", label: isAr ? "سجل التدقيق" : "Audit Log" },
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
            <span className="text-sm font-normal text-ink-faint">{isAr ? "لوحة الإدارة" : "Admin Backoffice"}</span>
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
              {isAuthenticated ? (isAr ? "خروج" : "Log out") : isAr ? "دخول" : "Log in"}
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
