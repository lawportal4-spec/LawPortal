import type { ReactNode } from "react";
import { Link, NavLink, useNavigate } from "react-router-dom";
import {
  BadgeCheck, BookOpenCheck, CreditCard, Files, HandCoins, UsersRound, LayoutDashboard, Repeat, ScrollText, Settings as SettingsIcon, Tags, TicketPercent, UserRound, Users,
} from "lucide-react";
import { DashboardShell, LocaleToggle, sidebarLinkClass } from "@law-portal/ui";
import { useTranslation } from "@law-portal/i18n";
import { useLocale } from "../lib/useLocale";
import { useAuth } from "../lib/authContext";
import { GlobalSearch } from "./directory/GlobalSearch";

export function AppShell({ children }: { children: ReactNode }) {
  const { t } = useTranslation();
  const { locale, setLocale } = useLocale();
  const navigate = useNavigate();
  const { isAuthenticated, logout } = useAuth();

  const navItems = [
    { to: "/", label: t("shell.admin.nav.dashboard"), icon: LayoutDashboard },
    { to: "/clients", label: t("shell.admin.nav.clients"), icon: UsersRound },
    { to: "/requests", label: t("shell.admin.nav.requests"), icon: Files },
    { to: "/lawyers", label: t("shell.admin.nav.lawyerVerification"), icon: BadgeCheck },
    { to: "/payments", label: t("shell.admin.nav.payments"), icon: CreditCard },
    { to: "/ledger", label: t("shell.admin.nav.reconciliation"), icon: BookOpenCheck },
    { to: "/lawyer-debts", label: t("shell.admin.nav.lawyerDebts"), icon: HandCoins },
    { to: "/catalog", label: t("shell.admin.nav.catalogAndPricing"), icon: Tags },
    { to: "/subscriptions", label: t("shell.admin.nav.subscriptionPlans"), icon: Repeat },
    { to: "/discount-codes", label: t("shell.admin.nav.discountCodes"), icon: TicketPercent },
    { to: "/users", label: t("shell.admin.nav.usersAndRoles"), icon: Users },
    { to: "/audit", label: t("shell.admin.nav.auditLog"), icon: ScrollText },
    { to: "/settings", label: t("shell.admin.nav.settings"), icon: SettingsIcon },
    { to: "/account", label: t("shell.admin.nav.account"), icon: UserRound },
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
          <span className="hidden text-sm font-normal text-ink-faint sm:inline">{t("shell.admin.tagline")}</span>
        </Link>
      }
      nav={
        isAuthenticated &&
        navItems.map(({ to, label, icon: Icon }) => (
          <NavLink key={to} to={to} end={to === "/"} className={({ isActive }) => sidebarLinkClass(isActive)}>
            <Icon />
            {label}
          </NavLink>
        ))
      }
      actions={
        <>
          {isAuthenticated && <GlobalSearch />}
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
