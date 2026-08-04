import type { ReactNode } from "react";

/**
 * Wraps a number, date, phone number, licence ID, filename, or URL so it renders correctly
 * inside an RTL paragraph. `<bdi>` isolates it from the surrounding bidi context; `dir="ltr"`
 * fixes its internal ordering. Pair with the mono font for anything tabular.
 *
 * This is the fix for the reversed-experience-range bug ("3:1" instead of "1-3") seen in
 * other Arabic legal-tech UIs — never inline raw numbers/IDs directly in Arabic text.
 */
export function Ltr({ children, className }: { children: ReactNode; className?: string }) {
  return (
    <bdi dir="ltr" className={className}>
      {children}
    </bdi>
  );
}
