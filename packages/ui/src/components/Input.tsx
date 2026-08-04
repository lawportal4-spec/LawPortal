import { clsx } from "clsx";
import type { InputHTMLAttributes, ReactNode } from "react";

export function Input({
  icon,
  className,
  ...props
}: InputHTMLAttributes<HTMLInputElement> & { icon?: ReactNode }) {
  return (
    <label
      className={clsx(
        "flex items-center gap-2 rounded-md border border-border bg-surface-raised px-4 py-2.5",
        // The visible focus indicator lives on this wrapper, not the (borderless) input itself —
        // :focus-within is what makes that work. Without it, focusing the input showed no focus
        // indicator anywhere at all (WCAG 2.4.7).
        "focus-within:border-seal focus-within:ring-2 focus-within:ring-seal/30",
        className,
      )}
    >
      {icon}
      <input
        className="w-full border-none bg-transparent text-sm text-ink placeholder:text-ink-faint focus:outline-none"
        {...props}
      />
    </label>
  );
}
