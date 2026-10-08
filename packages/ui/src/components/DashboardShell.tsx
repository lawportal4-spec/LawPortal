import { clsx } from "clsx";
import { useState, type ReactNode } from "react";

/** Top bar + start-side sidebar for the lawyer and admin portals. The app passes its own router
 * links as `nav` (styled with `sidebarLinkClass`); on small screens they fold into a menu. */
export function DashboardShell({
  brand,
  nav,
  actions,
  menuLabel,
  children,
}: {
  brand: ReactNode;
  /** Omit to hide the sidebar (signed out, or a gated screen). */
  nav?: ReactNode;
  actions: ReactNode;
  menuLabel: string;
  children: ReactNode;
}) {
  const [open, setOpen] = useState(false);
  return (
    <div className="min-h-screen bg-paper">
      <header className="sticky top-0 z-30 border-b border-border bg-surface-raised">
        {/* On a phone the brand gives way first, so the menu button always stays on screen. */}
        <div className="flex h-16 items-center gap-2 px-3 sm:gap-4 sm:px-6">
          <div className="min-w-0 overflow-hidden whitespace-nowrap">{brand}</div>
          <div className="ms-auto flex shrink-0 items-center gap-2 sm:gap-3">{actions}</div>
          {nav && (
            <button
              type="button"
              onClick={() => setOpen((o) => !o)}
              aria-label={menuLabel}
              aria-expanded={open}
              className="flex h-10 w-10 shrink-0 items-center justify-center rounded-md border border-border text-ink-soft lg:hidden"
            >
              <svg viewBox="0 0 24 24" className="h-5 w-5" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
                <path d={open ? "M6 6l12 12M18 6L6 18" : "M4 7h16M4 12h16M4 17h16"} />
              </svg>
            </button>
          )}
        </div>
      </header>

      <div className={clsx(nav && "lg:grid lg:grid-cols-[15rem_1fr]")}>
        {nav && (
          <aside
            className={clsx(
              "border-b border-border bg-surface lg:sticky lg:top-16 lg:block lg:h-[calc(100vh-4rem)] lg:overflow-y-auto lg:border-b-0 lg:border-e",
              open ? "block" : "hidden",
            )}
            onClick={() => setOpen(false)}
          >
            <nav className="flex flex-col gap-1 p-3">{nav}</nav>
          </aside>
        )}
        <main className="mx-auto w-full min-w-0 max-w-6xl px-4 py-8 sm:px-6">{children}</main>
      </div>
    </div>
  );
}

export function sidebarLinkClass(active: boolean) {
  return clsx(
    "flex items-center gap-3 rounded-md px-3 py-2.5 text-sm font-medium transition-colors [&>svg]:h-[18px] [&>svg]:w-[18px] [&>svg]:shrink-0",
    active ? "bg-seal-tint text-seal-strong" : "text-ink-soft hover:bg-surface-raised hover:text-ink",
  );
}

/** العربية / English pill used in every app's header. */
export function LocaleToggle({ locale, onChange }: { locale: "ar" | "en"; onChange: (code: "ar" | "en") => void }) {
  return (
    <div className="inline-flex items-center gap-1 rounded-full border border-border bg-surface p-1">
      {(["ar", "en"] as const).map((code) => (
        <button
          key={code}
          type="button"
          onClick={() => onChange(code)}
          className={clsx(
            "rounded-full px-2.5 py-1 text-xs font-medium transition-colors sm:px-3",
            locale === code ? "bg-seal text-seal-on" : "text-ink-soft hover:text-ink",
          )}
        >
          {code === "ar" ? "العربية" : "English"}
        </button>
      ))}
    </div>
  );
}
