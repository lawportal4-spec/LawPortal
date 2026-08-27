import { clsx } from "clsx";
import type { HTMLAttributes } from "react";

export interface SectionHeadingProps extends HTMLAttributes<HTMLHeadingElement> {
  /** Heading level for the DOM tag — pick the one correct for this page's outline. */
  level?: 2 | 3;
}

const LEVEL_CLASSES: Record<2 | 3, string> = {
  2: "text-2xl",
  3: "text-lg",
};

/**
 * The one place a page reaches for a section title. Replaces the ad-hoc
 * `font-display text-lg font-bold` repeated across every page — that pattern put every
 * heading one step above body text with no real jump, which is why the UI read as flat.
 */
export function SectionHeading({ level = 2, className, ...props }: SectionHeadingProps) {
  const Tag = level === 2 ? "h2" : "h3";
  return (
    <Tag
      className={clsx("font-display font-bold text-ink", LEVEL_CLASSES[level], className)}
      {...props}
    />
  );
}
