import { useState, type ReactNode } from "react";
import { Link, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { MapPin } from "lucide-react";
import { Avatar, Button, Card, Chip, CredentialCard, Ltr, VerifiedBadge } from "@law-portal/ui";
import { formatCurrency, formatDate, useTranslation } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { getLawyerProfile } from "../lib/api";

type Tab = "info" | "qualifications";

export default function LawyerProfile() {
  const { slug } = useParams<{ slug: string }>();
  const { i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const [tab, setTab] = useState<Tab>("info");

  const query = useQuery({
    queryKey: ["lawyers", "profile", slug],
    queryFn: () => getLawyerProfile(slug!),
    enabled: !!slug,
  });

  if (query.isLoading) {
    return (
      <AppShell>
        <p className="text-sm text-ink-faint">{isAr ? "جارٍ التحميل…" : "Loading…"}</p>
      </AppShell>
    );
  }

  if (query.isError || !query.data) {
    return (
      <AppShell>
        <p className="text-sm text-rubric">{isAr ? "لم يتم العثور على الملف." : "Profile not found."}</p>
      </AppShell>
    );
  }

  const lawyer = query.data;
  const specialties = isAr ? lawyer.specialtyNamesAr : lawyer.specialtyNamesEn;
  const languages = isAr ? lawyer.languageNamesAr : lawyer.languageNamesEn;
  const city = isAr ? lawyer.cityNameAr : lawyer.cityNameEn;
  const bio = (isAr ? lawyer.bioAr : lawyer.bioEn) ?? lawyer.bioAr ?? lawyer.bioEn;

  return (
    <AppShell>
      <div className="grid gap-8 lg:grid-cols-[1fr_20rem]">
        <div>
          <div className="relative mb-10 h-28 rounded-xl bg-ink">
            <div className="absolute -bottom-6 start-4">
              <Avatar initials={initialsOf(lawyer.fullName)} size="md" online />
            </div>
          </div>

          <div className="flex items-center gap-2">
            <h1 className="font-display text-xl font-bold">{lawyer.fullName}</h1>
            {lawyer.isVerified && <VerifiedBadge />}
          </div>
          <div className="mt-1 flex flex-wrap gap-2 text-xs text-ink-faint">
            {city && (
              <span className="flex items-center gap-1">
                <MapPin className="h-3 w-3" /> {city}
              </span>
            )}
            <span>{isAr ? "محامي مرخص" : "Licensed lawyer"}</span>
          </div>

          <div className="mt-4 grid grid-cols-3 gap-3 rounded-xl border border-border bg-surface p-4 text-center">
            <Stat label={isAr ? "سنوات الخبرة" : "Experience"} value={lawyer.experienceDisplay ?? "—"} />
            <Stat label={isAr ? "عدد الطلبات" : "Completed"} value={String(lawyer.completedRequestCount)} />
            <Stat
              label={isAr ? "التقييم العام" : "Rating"}
              value={lawyer.avgRating != null ? lawyer.avgRating.toFixed(1) : (isAr ? "جديد" : "New")}
            />
          </div>

          <div className="mt-6 flex gap-6 border-b border-rule text-sm font-medium">
            <TabButton active={tab === "info"} onClick={() => setTab("info")}>
              {isAr ? "المعلومات الشخصية" : "Personal Information"}
            </TabButton>
            <TabButton active={tab === "qualifications"} onClick={() => setTab("qualifications")}>
              {isAr ? "الخبرات والمؤهلات" : "Experience & Qualifications"}
            </TabButton>
          </div>

          {tab === "info" && (
            <div className="mt-4 flex flex-col gap-4">
              {bio && (
                <Card>
                  <h2 className="mb-2 text-sm font-semibold">{isAr ? "نبذة عن المحامي" : "About"}</h2>
                  <p className="text-sm text-ink-soft">{bio}</p>
                </Card>
              )}
              <Card>
                <h2 className="mb-2 text-sm font-semibold">{isAr ? "التخصصات" : "Specialties"}</h2>
                <div className="flex flex-wrap gap-1.5">
                  {specialties.map((s) => (
                    <Chip key={s}>{s}</Chip>
                  ))}
                </div>
              </Card>
              {languages.length > 0 && (
                <Card>
                  <h2 className="mb-2 text-sm font-semibold">{isAr ? "اللغات" : "Languages"}</h2>
                  <div className="flex flex-wrap gap-1.5">
                    {languages.map((l) => (
                      <Chip key={l}>{l}</Chip>
                    ))}
                  </div>
                </Card>
              )}
            </div>
          )}

          {tab === "qualifications" && (
            <Card className="mt-4">
              {lawyer.qualifications.length === 0 ? (
                <p className="text-sm text-ink-faint">
                  {isAr ? "لا توجد مؤهلات مضافة بعد." : "No qualifications added yet."}
                </p>
              ) : (
                <ul className="flex flex-col gap-3">
                  {lawyer.qualifications.map((q, i) => (
                    <li key={i} className="text-sm">
                      <div className="font-semibold">{isAr ? q.titleAr : q.titleEn}</div>
                      {q.institution && <div className="text-ink-faint">{q.institution}</div>}
                    </li>
                  ))}
                </ul>
              )}
            </Card>
          )}
        </div>

        <aside className="flex flex-col gap-4">
          {lawyer.pricing && (
            <Card>
              <h2 className="mb-3 text-sm font-semibold">{isAr ? "أسعار الاستشارات" : "Consultation Prices"}</h2>
              <div className="flex flex-col gap-2 text-sm">
                <PriceRow label={isAr ? "استشارة كتابية" : "Written"} value={lawyer.pricing.writtenPrice} />
                <PriceRow label={isAr ? "15 دقيقة" : "15 min"} value={lawyer.pricing.price15} />
                <PriceRow label={isAr ? "30 دقيقة" : "30 min"} value={lawyer.pricing.price30} />
                <PriceRow label={isAr ? "45 دقيقة" : "45 min"} value={lawyer.pricing.price45} />
              </div>
            </Card>
          )}

          <CredentialCard
            nameAr={lawyer.fullName}
            nameEn={lawyer.fullName}
            licenceNumber={lawyer.license.licenseNumber}
            issuedOn={formatDate(lawyer.license.issueDate)}
            expiresOn={formatDate(lawyer.license.expiryDate)}
            locale={isAr ? "ar" : "en"}
          />

          {lawyer.pricing && lawyer.acceptingNewRequests && (
            <Link to={`/lawyers/${lawyer.slug}/consult`}>
              <Button className="w-full justify-center">{isAr ? "استشر الآن" : "Consult Now"}</Button>
            </Link>
          )}
        </aside>
      </div>
    </AppShell>
  );
}

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <div className="font-mono text-lg font-bold">
        <Ltr>{value}</Ltr>
      </div>
      <div className="text-xs text-ink-faint">{label}</div>
    </div>
  );
}

function PriceRow({ label, value }: { label: string; value: number }) {
  return (
    <div className="flex items-center justify-between">
      <span className="text-ink-soft">{label}</span>
      <Ltr className="font-mono font-medium">{formatCurrency(value)}</Ltr>
    </div>
  );
}

function TabButton({ active, onClick, children }: { active: boolean; onClick: () => void; children: ReactNode }) {
  return (
    <button
      onClick={onClick}
      className={
        "-mb-px border-b-2 pb-2 " + (active ? "border-seal text-seal" : "border-transparent text-ink-faint")
      }
    >
      {children}
    </button>
  );
}

function initialsOf(fullName: string): string {
  const parts = fullName.trim().split(/\s+/);
  return parts.length >= 2 ? `${parts[0][0]}.${parts[1][0]}` : fullName.slice(0, 2);
}
