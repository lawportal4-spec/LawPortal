import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { Scale, Gavel, FileCheck2, Briefcase, FolderOpen, Search } from "lucide-react";
import { Card, Input, Chip, SectionHeading, Button, type ChipCategory } from "@law-portal/ui";
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

  const topLawyersQuery = useQuery({
    queryKey: ["lawyers", "top"],
    queryFn: () => searchLawyers({ sort: "Rating", page: 1, pageSize: 3 }),
  });

  return (
    <AppShell>
      <section className="mb-12">
        <span className="font-mono text-xs font-semibold uppercase tracking-wide text-rubric">
          {isAr ? "ابحث بثقة" : "Search with confidence"}
        </span>
        <h1 className="mt-3 font-display text-3xl font-bold text-ink sm:text-4xl">
          {isAr ? "ابحث عن محامٍ متخصص في دقائق" : "Find the right licensed lawyer in minutes"}
        </h1>
        <p className="mt-3 max-w-2xl text-ink-soft">
          {isAr
            ? "استشر محامين مرخّصين من وزارة العدل، أو قدّم طلب توثيق وتفاوض على السعر قبل الالتزام."
            : "Consult MoJ-licensed lawyers, or submit a notarization request and negotiate price before committing."}
        </p>
        <Input
          icon={<Search className="h-4 w-4" />}
          placeholder={t("nav.search") ?? undefined}
          className="mt-6 max-w-md"
        />
        {specialtiesQuery.data && (
          <div className="mt-4 flex flex-wrap gap-2">
            {specialtiesQuery.data.slice(0, 6).map((s) => (
              <Link key={s.id} to="/lawyers">
                <Chip className="hover:border-seal hover:text-seal">{isAr ? s.nameAr : s.nameEn}</Chip>
              </Link>
            ))}
          </div>
        )}
        {specialtiesQuery.isError && (
          <p className="mt-4 text-sm text-rubric">
            {isAr
              ? "تعذّر الاتصال بالـ API. تأكد من تشغيله على http://localhost:5280."
              : "Could not reach the API. Make sure it's running on http://localhost:5280."}
          </p>
        )}
      </section>

      <section className="mb-12">
        <SectionHeading className="mb-4">
          {isAr ? "كيف يمكننا مساعدتك؟" : "How can we help?"}
        </SectionHeading>
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

      <section className="mb-12">
        <div className="mb-4 flex items-center justify-between">
          <SectionHeading className="mb-0">
            {isAr ? "أعلى المحامين تقييمًا" : "Top-rated lawyers"}
          </SectionHeading>
          <Link to="/lawyers" className="text-sm font-semibold text-seal hover:underline">
            {isAr ? "عرض الدليل ←" : "View directory →"}
          </Link>
        </div>
        {topLawyersQuery.data?.items.length ? (
          <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
            {topLawyersQuery.data.items.map((lawyer) => (
              <LawyerCard key={lawyer.id} lawyer={lawyer} />
            ))}
          </div>
        ) : (
          <p className="text-sm text-ink-faint">{isAr ? "جارٍ التحميل…" : "Loading…"}</p>
        )}
      </section>

      <section>
        <div className="flex flex-col items-center gap-4 rounded-xl bg-seal-tint py-10 text-center">
          <SectionHeading className="mb-0">
            {isAr ? "لديك طلب جاهز؟" : "Have a request ready?"}
          </SectionHeading>
          <p className="max-w-md text-ink-soft">
            {isAr
              ? "صف حالتك، واستقبل عروضًا من محامين مرخّصين خلال ساعات."
              : "Describe your case and receive offers from licensed lawyers within hours."}
          </p>
          <Link to="/bidding/new">
            <Button>{isAr ? "ابدأ طلبك الآن" : "Start your request"}</Button>
          </Link>
        </div>
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
