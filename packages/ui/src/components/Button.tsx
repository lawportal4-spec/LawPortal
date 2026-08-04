import { clsx } from "clsx";
import type { ButtonHTMLAttributes } from "react";

type ButtonVariant = "primary" | "secondary" | "ghost" | "danger";

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
}

const VARIANT_CLASSES: Record<ButtonVariant, string> = {
  primary: "bg-seal text-seal-on hover:bg-seal-strong",
  secondary: "bg-surface text-ink border border-border hover:border-seal hover:text-seal",
  ghost: "bg-transparent text-ink-soft hover:text-ink",
  danger: "bg-transparent text-rubric border border-rubric-tint hover:bg-rubric-tint",
};

export function Button({ variant = "primary", className, ...props }: ButtonProps) {
  return (
    <button
      className={clsx(
        "inline-flex items-center gap-2 rounded-md px-5 py-2.5 text-sm font-semibold transition-colors",
        "focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-seal",
        VARIANT_CLASSES[variant],
        className,
      )}
      {...props}
    />
  );
}
