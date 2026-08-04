import { clsx } from "clsx";

export function Avatar({
  initials,
  online,
  size = "md",
}: {
  initials: string;
  online?: boolean;
  size?: "sm" | "md";
}) {
  const dims = size === "sm" ? "h-9 w-9 text-xs" : "h-12 w-12 text-sm";
  return (
    <span className={clsx("relative inline-flex flex-shrink-0 items-center justify-center rounded-full bg-seal-tint font-semibold text-seal-strong", dims)}>
      {initials}
      {online && (
        <span className="absolute end-0 bottom-0 h-2.5 w-2.5 rounded-full border-2 border-surface bg-seal" />
      )}
    </span>
  );
}
