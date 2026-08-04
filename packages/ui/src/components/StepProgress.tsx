import { clsx } from "clsx";
import { Ltr } from "./Ltr";

export interface Step {
  label: string;
}

/**
 * The request-wizard step tracker. Step numbers are ALWAYS Western numerals (wrapped in
 * <Ltr>), even in the Arabic UI — this is a genuinely sequential device (steps have a real
 * order), unlike decorative "01/02/03" numbering elsewhere.
 */
export function StepProgress({ steps, current }: { steps: Step[]; current: number }) {
  return (
    <ol className="flex max-w-lg items-center">
      {steps.map((step, i) => {
        const index = i + 1;
        const done = index < current;
        const active = index === current;
        return (
          <li key={step.label} className="relative flex flex-1 flex-col items-center gap-2">
            <span
              className={clsx(
                "flex h-8 w-8 items-center justify-center rounded-full border font-mono text-sm font-semibold",
                done && "border-seal bg-seal text-seal-on",
                active && "border-seal bg-seal-tint text-seal",
                !done && !active && "border-border bg-surface text-ink-faint",
              )}
            >
              <Ltr>{index}</Ltr>
            </span>
            <span className={clsx("text-xs", active ? "font-semibold text-ink" : "text-ink-faint")}>
              {step.label}
            </span>
            {i < steps.length - 1 && (
              <span
                className={clsx(
                  "absolute top-4 h-px w-full start-1/2 -z-10",
                  done ? "bg-seal" : "bg-border",
                )}
              />
            )}
          </li>
        );
      })}
    </ol>
  );
}
