import { clsx } from "clsx";
import type { HTMLAttributes } from "react";

export interface CardProps extends HTMLAttributes<HTMLDivElement> {
  /** Lifted treatment for interactive/floating surfaces — credential cards, popovers. */
  elevated?: boolean;
}

export function Card({ elevated = false, className, ...props }: CardProps) {
  return (
    <div
      className={clsx(
        "rounded-xl border border-border bg-surface p-5",
        elevated ? "shadow-raised" : "shadow-card",
        className,
      )}
      {...props}
    />
  );
}
