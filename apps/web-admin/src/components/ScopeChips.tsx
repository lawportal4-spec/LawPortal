import { useLayoutEffect, useRef, useState } from "react";
import { Chip } from "@law-portal/ui";

const GAP = 4; // gap-1
const MORE_WIDTH = 44; // room kept for the "+N" chip

const toggleClass =
  "relative z-10 inline-flex items-center rounded-full border border-seal/60 px-3 py-1 text-xs font-medium text-seal hover:bg-seal-tint";

/** Chips on one line: as many as fit, then "+N" which expands to all of them (wrapping). */
export function ScopeChips({ labels, expanded, onToggle, onOverflow }: {
  labels: string[];
  expanded: boolean;
  onToggle: () => void;
  /** Reports how many chips are hidden, so the row can show its expand arrow. */
  onOverflow?: (hidden: number) => void;
}) {
  const box = useRef<HTMLDivElement>(null);
  const [visible, setVisible] = useState(labels.length);
  const key = labels.join("|");

  useLayoutEffect(() => {
    const el = box.current;
    if (expanded || !el) return;
    const measure = () => {
      const widths = Array.from(el.querySelectorAll<HTMLElement>("[data-measure] > *")).map((c) => c.offsetWidth + GAP);
      let used = 0;
      let n = 0;
      for (const w of widths) {
        const reserve = n + 1 < widths.length ? MORE_WIDTH : 0;
        if (used + w + reserve > el.clientWidth) break;
        used += w;
        n++;
      }
      const shown = Math.max(1, n);
      setVisible(shown);
      onOverflow?.(widths.length - shown);
    };
    measure();
    const observer = new ResizeObserver(measure);
    observer.observe(el);
    return () => observer.disconnect();
    // eslint-disable-next-line react-hooks/exhaustive-deps -- onOverflow is a fresh closure each render
  }, [expanded, key]);

  if (expanded)
    return (
      <div className="mt-1 flex flex-wrap gap-1">
        {labels.map((l) => <Chip key={l}>{l}</Chip>)}
      </div>
    );

  const hidden = labels.length - visible;
  return (
    <div ref={box} className="relative mt-1 min-w-0 overflow-hidden">
      {/* Invisible copy of every chip, only to measure widths. */}
      <div data-measure aria-hidden className="invisible absolute flex gap-1 whitespace-nowrap">
        {labels.map((l) => <Chip key={l}>{l}</Chip>)}
      </div>
      <div className="flex gap-1 whitespace-nowrap">
        {labels.slice(0, visible).map((l) => <Chip key={l}>{l}</Chip>)}
        {hidden > 0 && (
          <button type="button" onClick={onToggle} className={toggleClass} aria-expanded={false}>
            <bdi>+{hidden}</bdi>
          </button>
        )}
      </div>
    </div>
  );
}
