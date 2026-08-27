import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { Scale, Gavel, FileCheck2, Briefcase, FolderOpen, Send, Search } from "lucide-react";
import { Button, Card } from "@law-portal/ui";
import { useTranslation } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { getServiceCategories } from "../lib/api";

const ICONS: Record<string, typeof Scale> = {
  scale: Scale,
  gavel: Gavel,
  document: FileCheck2,
  briefcase: Briefcase,
  folder: FolderOpen,
};

export default function ServicesHub() {
  const { i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const navigate = useNavigate();
  const [activeId, setActiveId] = useState<number | null>(null);

  const query = useQuery({
    queryKey: ["catalog", "categories"],
    queryFn: getServiceCategories,
  });

  const categories = query.data ?? [];
  const active = categories.find((c) => c.id === activeId) ?? categories[0];

  return (
    <AppShell>
      <h1 className="mb-6 font-display text-2xl font-bold">{isAr ? "خدماتنا" : "Our Services"}</h1>

      {query.isLoading && <p className="text-sm text-ink-faint">{isAr ? "جارٍ التحميل…" : "Loading…"}</p>}

      {categories.length > 0 && (
        <>
          <div className="mb-6 flex flex-wrap gap-2 border-b border-rule pb-4">
            {categories.map((c) => {
              const Icon = (c.iconKey && ICONS[c.iconKey]) || FolderOpen;
              const isActive = active?.id === c.id;
              return (
                <button
                  key={c.id}
                  onClick={() => setActiveId(c.id)}
                  className={
                    "flex items-center gap-2 rounded-full px-4 py-2 text-sm font-medium transition-colors " +
                    (isActive ? "bg-seal text-seal-on" : "bg-surface text-ink-soft border border-border")
                  }
                >
                  <Icon className="h-4 w-4" />
                  {isAr ? c.nameAr : c.nameEn}
                </button>
              );
            })}
          </div>

          {active && (
            <div>
              <h2 className="mb-4 font-display text-lg font-bold">{isAr ? active.nameAr : active.nameEn}</h2>
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
                {active.services.map((s) => (
                  <Card key={s.id} className="flex flex-col">
                    <h3 className="mb-1 text-sm font-semibold">{isAr ? s.nameAr : s.nameEn}</h3>
                    {(isAr ? s.descriptionAr : s.descriptionEn) && (
                      <p className="mb-3 text-xs text-ink-soft">{isAr ? s.descriptionAr : s.descriptionEn}</p>
                    )}
                    {s.pricingModel === "CompetitiveBidding" && (
                      <Button
                        variant="secondary"
                        className="mt-auto self-start"
                        onClick={() => navigate(`/bidding/new?serviceId=${s.id}`)}
                      >
                        <Send className="h-4 w-4" />
                        {isAr ? "اطلب عروض أسعار" : "Request offers"}
                      </Button>
                    )}
                    {s.pricingModel === "PerLawyerFixed" && (
                      <Button variant="secondary" className="mt-auto self-start" onClick={() => navigate("/lawyers")}>
                        <Search className="h-4 w-4" />
                        {isAr ? "ابحث عن محامٍ" : "Find a lawyer"}
                      </Button>
                    )}
                  </Card>
                ))}
              </div>
            </div>
          )}
        </>
      )}
    </AppShell>
  );
}
