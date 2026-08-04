import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { Scale, Gavel, FileCheck2, Briefcase, FolderOpen, Search } from "lucide-react";
import { Card, Input, StatusPill, StepProgress, Chip, type ChipCategory } from "@law-portal/ui";
import { useTranslation } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { LawyerCard } from "../components/LawyerCard";
import { api, searchLawyers, type SpecialtyDto } from "../lib/api";

const CATEGORIES: { key: ChipCategory; icon: typeof Scale; to: string }[] = [
  { key: "consult", icon: Scale, to: "/services" },
  { key: "judiciary", icon: Gavel, to: "/services" },
  { key: "notary", icon: FileCheck2, to: "/services" },
  { key: "business", icon: Briefcase, to: "/services" },
  { key: "other", icon: FolderOpen, to: "/services" },
];

export default function PortalHome() {
  const { t, i18n } = useTranslation();
  const isAr = i18n.language === "ar";

  const specialtiesQuery = useQuery({
    queryKey: ["catalog", "specialties"],
    queryFn: async () => (await api.get<SpecialtyDto[]>("/api/v1/catalog/specialties")).data,
  });

  const topLawyerQuery = useQuery({
    queryKey: ["lawyers", "top"],
    queryFn: () => searchLawyers({ sort: "Rating", page: 1, pageSize: 1 }),
  });

  return (
    <AppShell>
      {/* The page's own heading — every other page here has one, this dashboard-style home
          page didn't (its five sections all start at h2, with nothing above them). Visually
          hidden since the header already carries the brand name; screen-reader navigation-by-
          heading still needs exactly one h1 per page. */}
      <h1 className="sr-only">{isAr ? "الرئيسية" : "Home"}</h1>
      <Input
        icon={<Search className="h-4 w-4" />}
        placeholder={t("nav.search") ?? undefined}
        className="mb-8 max-w-md"
      />

      <section className="mb-10">
        <h2 className="mb-4 font-display text-lg font-bold">
          {isAr ? "كيف يمكننا مساعدتك؟" : "How can we help?"}
        </h2>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-5">
          {CATEGORIES.map(({ key, icon: Icon, to }) => (
            <Link key={key} to={to}>
              <Card className="flex h-full flex-col gap-2 hover:border-seal">
                <span
                  className="flex h-10 w-10 items-center justify-center rounded-lg"
                  style={{
                    background: `var(--color-cat-${key}-bg)`,
                    color: `var(--color-cat-${key}-fg)`,
                  }}
                >
                  <Icon className="h-5 w-5" />
                </span>
                <span className="text-sm font-semibold">{t(`categories.${categoryI18nKey(key)}`)}</span>
              </Card>
            </Link>
          ))}
        </div>
      </section>

      <section className="mb-10 grid gap-8 lg:grid-cols-2">
        <div>
          <div className="mb-4 flex items-center justify-between">
            <h2 className="font-display text-lg font-bold">
              {isAr ? "أعلى المحامين تقييمًا" : "Top-rated lawyer"}
            </h2>
            <Link to="/lawyers" className="text-sm text-seal hover:underline">
              {isAr ? "عرض الدليل ←" : "View directory →"}
            </Link>
          </div>
          {topLawyerQuery.data?.items[0] ? (
            <LawyerCard lawyer={topLawyerQuery.data.items[0]} />
          ) : (
            <p className="text-sm text-ink-faint">{isAr ? "جارٍ التحميل…" : "Loading…"}</p>
          )}
        </div>

        <div>
          <h2 className="mb-4 font-display text-lg font-bold">
            {isAr ? "مؤشّر خطوات الطلب" : "Request wizard steps"}
          </h2>
          <StepProgress
            current={2}
            steps={[
              { label: isAr ? "التخصص" : "Specialty" },
              { label: isAr ? "المحامي" : "Lawyer" },
              { label: isAr ? "التفاصيل" : "Details" },
            ]}
          />

          <h2 className="mt-8 mb-4 font-display text-lg font-bold">
            {isAr ? "حالات الطلب" : "Request statuses"}
          </h2>
          <div className="flex flex-wrap gap-2">
            <StatusPill status="draft" label={t("status.draft")} />
            <StatusPill status="pendingPayment" label={t("status.pendingPayment")} />
            <StatusPill status="inProgress" label={t("status.inProgress")} />
            <StatusPill status="completed" label={t("status.completed")} />
            <StatusPill status="disputed" label={t("status.disputed")} />
          </div>
        </div>
      </section>

      <section>
        <h2 className="mb-4 font-display text-lg font-bold">
          {isAr ? "التخصصات (من الـ API الحي)" : "Specialties (live from the API)"}
        </h2>
        {specialtiesQuery.isLoading && (
          <p className="text-sm text-ink-faint">{isAr ? "جارٍ التحميل…" : "Loading…"}</p>
        )}
        {specialtiesQuery.isError && (
          <p className="text-sm text-rubric">
            {isAr
              ? "تعذّر الاتصال بالـ API. تأكد من تشغيله على http://localhost:5280."
              : "Could not reach the API. Make sure it's running on http://localhost:5280."}
          </p>
        )}
        {specialtiesQuery.data && (
          <div className="flex flex-wrap gap-2">
            {specialtiesQuery.data.map((s) => (
              <Chip key={s.id}>{isAr ? s.nameAr : s.nameEn}</Chip>
            ))}
          </div>
        )}
      </section>
    </AppShell>
  );
}

function categoryI18nKey(key: ChipCategory): string {
  switch (key) {
    case "consult":
      return "consultations";
    case "judiciary":
      return "judiciary";
    case "notary":
      return "notarization";
    case "business":
      return "business";
    case "other":
      return "other";
  }
}
