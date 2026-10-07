import { clsx } from "clsx";

export function Avatar({
  initials,
  src,
  online,
  size = "md",
}: {
  initials: string;
  /** Photo URL; the initials show when there is none. */
  src?: string | null;
  online?: boolean;
  size?: "sm" | "md" | "lg";
}) {
  const dims = size === "sm" ? "h-9 w-9 text-xs" : size === "lg" ? "h-24 w-24 text-2xl" : "h-12 w-12 text-sm";
  return (
    <span className={clsx("relative inline-flex flex-shrink-0 items-center justify-center rounded-full bg-seal-tint font-semibold text-seal-strong", dims)}>
      {src ? <img src={src} alt="" className="h-full w-full rounded-full object-cover" /> : initials}
      {online && (
        <span className="absolute end-0 bottom-0 h-2.5 w-2.5 rounded-full border-2 border-surface bg-seal" />
      )}
    </span>
  );
}
