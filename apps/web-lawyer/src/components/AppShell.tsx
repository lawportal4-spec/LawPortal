import type { ReactNode } from "react";
import { Link, NavLink, useNavigate } from "react-router-dom";
import { ChartLine, Gavel, Inbox, LayoutDashboard, Repeat, SlidersHorizontal, UserRound } from "lucide-react";
import { DashboardShell, LocaleToggle, sidebarLinkClass } from "@law-portal/ui";
import { useTranslation } from "@law-portal/i18n";
import { useLocale } from "../lib/useLocale";
import { useAuth } from "../lib/authContext";

/** `hideNav`: the portal sections are useless (and all gated) while a registration awaits review. */
export function AppShell({ children, hideNav = false }: { children: ReactNode; hideNav?: boolean }) {
  const { t } = useTranslation();
  const { locale, setLocale } = useLocale();
  const navigate = useNavigate();
  const { isAuthenticated, logout } = useAuth();

  const navItems = [
    { to: "/", label: t("shell.lawyer.nav.dashboard"), icon: LayoutDashboard },
    { to: "/requests", label: t("shell.lawyer.nav.incomingRequests"), icon: Inbox },
    { to: "/bidding", label: t("shell.lawyer.nav.biddingFeed"), icon: Gavel },
    { to: "/earnings", label: t("shell.lawyer.nav.earningsAndReviews"), icon: ChartLine },
    { to: "/subscription", label: t("shell.lawyer.nav.subscription"), icon: Repeat },
    { to: "/settings", label: t("shell.lawyer.nav.profileAndPricing"), icon: SlidersHorizontal },
    { to: "/account", label: t("shell.lawyer.nav.personalInfo"), icon: UserRound },
  ];

  function handleAuthClick() {
    if (isAuthenticated) logout();
    navigate("/login");
  }

  return (
    <DashboardShell
      menuLabel={t("nav.openMenu")}
      brand={
        <Link to="/" className="flex items-center gap-2 font-display text-xl font-bold">
          <img src="/favicon.svg" alt="" className="h-9 w-8" />
          <span className="text-seal-strong">{locale === "ar" ? "بوابة القانون" : "Law Portal"}</span>
          <span className="hidden text-sm font-normal text-ink-faint sm:inline">{t("shell.lawyer.tagline")}</span>
        </Link>
      }
      nav={
        isAuthenticated &&
        !hideNav &&
        navItems.map(({ to, label, icon: Icon }) => (
          <NavLink key={to} to={to} end={to === "/"} className={({ isActive }) => sidebarLinkClass(isActive)}>
            <Icon />
            {label}
          </NavLink>
        ))
      }
      actions={
        <>
          <LocaleToggle locale={locale} onChange={setLocale} />
          <button onClick={handleAuthClick} className="rounded-md border border-border px-3 py-1.5 text-sm font-medium text-ink-soft hover:border-seal hover:text-seal">
            {isAuthenticated ? t("nav.logout") : t("nav.login")}
          </button>
        </>
      }
    >
      {children}
    </DashboardShell>
  );
}
