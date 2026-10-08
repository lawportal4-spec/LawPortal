import { useState, type ReactNode } from "react";
import { Search, SlidersHorizontal } from "lucide-react";
import { Card, Input } from "@law-portal/ui";
import { useTranslation } from "@law-portal/i18n";

export const filterSelectClass = "rounded-md border border-border bg-surface-raised px-3 py-2.5 text-sm";

/** The one filter layout every admin list uses: search box, «بحث متقدم» toggle opening a card of
 * extra fields (the children), and «مسح الفلاتر» once anything is set. */
export function FilterBar({ search, onSearch, placeholder, advancedActive, canClear, onClear, children }: {
  search: string;
  onSearch: (value: string) => void;
  placeholder: string;
  /** Highlights the toggle while a field inside the advanced card is set. */
  advancedActive?: boolean;
  canClear: boolean;
  onClear: () => void;
  children?: ReactNode;
}) {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  return (
    <>
      <div className="mb-4 flex flex-wrap items-center gap-3">
        <Input
          icon={<Search className="h-4 w-4 text-ink-faint" />}
          className="w-full max-w-xl"
          placeholder={placeholder}
          aria-label={placeholder}
          value={search}
          onChange={(e) => onSearch(e.target.value)}
        />
        {children && (
          <button
            type="button"
            aria-expanded={open}
            onClick={() => setOpen((v) => !v)}
            className={
              "flex items-center gap-1.5 rounded-md border px-3 py-2 text-sm " +
              (advancedActive ? "border-seal text-seal" : "border-border text-ink-soft hover:border-seal hover:text-seal")
            }
          >
            <SlidersHorizontal className="h-4 w-4" />
            {open ? t("discount.admin.hideAdvanced") : t("discount.admin.advanced")}
          </button>
        )}
        {canClear && (
          <button type="button" onClick={onClear} className="text-sm text-ink-faint hover:text-rubric">
            {t("discount.admin.clear")}
          </button>
        )}
      </div>
      {children && open && <Card className="mb-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">{children}</Card>}
    </>
  );
}

/** Pill tabs above a list for its main status. */
export function StatusTabs<T extends string>({ values, value, onChange, label }: {
  values: readonly T[];
  value: T;
  onChange: (value: T) => void;
  label: (value: T) => string;
}) {
  return (
    <div className="mb-4 flex flex-wrap gap-2" role="tablist">
      {values.map((s) => (
        <button
          key={s || "all"}
          type="button"
          role="tab"
          aria-selected={value === s}
          onClick={() => onChange(s)}
          className={
            value === s
              ? "rounded-full bg-seal px-4 py-1.5 text-sm font-medium text-seal-on"
              : "rounded-full border border-border px-4 py-1.5 text-sm text-ink-soft hover:border-seal hover:text-seal"
          }
        >
          {label(s)}
        </button>
      ))}
    </div>
  );
}
