import { useEffect, useRef, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { Search } from "lucide-react";
import { Ltr } from "@law-portal/ui";
import { useTranslation } from "@law-portal/i18n";
import { searchAll, type SearchHit } from "../../lib/directoryApi";

const PATH: Record<SearchHit["kind"], string> = { Client: "/clients", Lawyer: "/lawyers", Request: "/requests", Payment: "/payments" };

/** One box that finds any client, lawyer, request or payment and opens it. */
export function GlobalSearch() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [text, setText] = useState("");
  const [term, setTerm] = useState("");
  const [open, setOpen] = useState(false);
  const box = useRef<HTMLDivElement>(null);
  useEffect(() => {
    const id = setTimeout(() => setTerm(text.trim()), 250);
    return () => clearTimeout(id);
  }, [text]);
  useEffect(() => {
    const close = (e: MouseEvent) => { if (!box.current?.contains(e.target as Node)) setOpen(false); };
    document.addEventListener("mousedown", close);
    return () => document.removeEventListener("mousedown", close);
  }, []);
  const hits = useQuery({ queryKey: ["adminSearch", term], queryFn: () => searchAll(term), enabled: term.length >= 2 });

  function go(h: SearchHit) {
    setOpen(false);
    setText("");
    navigate(`${PATH[h.kind]}/${h.id}`);
  }

  return (
    <div ref={box} className="relative hidden md:block">
      <label className="flex items-center gap-2 rounded-md border border-border bg-surface px-3 py-1.5 focus-within:border-seal">
        <Search className="h-4 w-4 text-ink-faint" />
        <input id="global-search" value={text} onFocus={() => setOpen(true)} onChange={(e) => { setText(e.target.value); setOpen(true); }}
          placeholder={t("directory.searchAll")} aria-label={t("directory.searchAll")} className="w-56 bg-transparent text-sm text-ink placeholder:text-ink-faint focus:outline-none lg:w-72" />
      </label>
      {open && term.length >= 2 && (
        <div className="absolute end-0 top-full z-40 mt-1 w-80 overflow-hidden rounded-md border border-border bg-surface-raised shadow-raised">
          {hits.data?.length === 0 && <p className="px-3 py-3 text-sm text-ink-faint">{t("directory.noResults")}</p>}
          {hits.data?.map((h) => (
            <button key={`${h.kind}-${h.id}`} type="button" onClick={() => go(h)}
              className="flex w-full items-center justify-between gap-3 border-b border-rule px-3 py-2 text-start text-sm last:border-0 hover:bg-surface">
              <span className="min-w-0">
                <span className="block truncate">{h.kind === "Request" || h.kind === "Payment" ? <Ltr className="font-mono">{h.title}</Ltr> : h.title}</span>
                {h.subtitle && <span className="block truncate text-xs text-ink-faint"><Ltr className="font-mono">{h.subtitle}</Ltr></span>}
              </span>
              <span className="shrink-0 rounded-full bg-paper px-2 py-0.5 text-[11px] text-ink-soft">{t(`directory.kinds.${h.kind}`)}</span>
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
