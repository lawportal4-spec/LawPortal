import { clsx } from "clsx";
import type { HTMLAttributes } from "react";

export type ChipCategory = "consult" | "judiciary" | "notary" | "business" | "other";

const CATEGORY_CLASSES: Record<ChipCategory, string> = {
  consult: "bg-cat-consult-bg text-cat-consult-fg",
  judiciary: "bg-cat-judiciary-bg text-cat-judiciary-fg",
  notary: "bg-cat-notary-bg text-cat-notary-fg",
  business: "bg-cat-business-bg text-cat-business-fg",
  other: "bg-cat-other-bg text-cat-other-fg",
};

export interface ChipProps extends HTMLAttributes<HTMLSpanElement> {
  category?: ChipCategory;
}

/** A small pill tag — service categories, specialties, request statuses. */
export function Chip({ category, className, ...props }: ChipProps) {
  return (
    <span
      className={clsx(
        "inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-xs font-medium",
        category ? CATEGORY_CLASSES[category] : "bg-paper text-ink-soft border border-border",
        className,
      )}
      {...props}
    />
  );
}
