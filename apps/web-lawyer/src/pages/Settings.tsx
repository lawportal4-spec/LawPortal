import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Button, Card, Input } from "@law-portal/ui";
import { useTranslation } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import {
  getLanguages,
  getMyProfile,
  getSpecialties,
  renewLicense,
  updateMyProfile,
  updatePricing,
} from "../lib/lawyerApi";

export default function Settings() {
  const { i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const queryClient = useQueryClient();

  const specialtiesQuery = useQuery({ queryKey: ["specialties"], queryFn: getSpecialties });
  const languagesQuery = useQuery({ queryKey: ["languages"], queryFn: getLanguages });
  const profileQuery = useQuery({ queryKey: ["myLawyerProfile"], queryFn: getMyProfile });

  const [bioAr, setBioAr] = useState("");
  const [bioEn, setBioEn] = useState("");
  const [accepting, setAccepting] = useState(true);
  const [specialtyIds, setSpecialtyIds] = useState<number[]>([]);
  const [languageIds, setLanguageIds] = useState<number[]>([]);

  useEffect(() => {
    if (!profileQuery.data) return;
    setBioAr(profileQuery.data.bioAr ?? "");
    setBioEn(profileQuery.data.bioEn ?? "");
    setAccepting(profileQuery.data.acceptingNewRequests);
    setSpecialtyIds(profileQuery.data.specialtyIds);
    setLanguageIds(profileQuery.data.languageIds);
  }, [profileQuery.data]);

  const profileMutation = useMutation({
    mutationFn: () => updateMyProfile({ bioAr: bioAr || null, bioEn: bioEn || null, acceptingNewRequests: accepting, specialtyIds, languageIds }),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ["myLawyerProfile"] }),
  });

  const [writtenPrice, setWrittenPrice] = useState("");
  const [price15, setPrice15] = useState("");
  const [price30, setPrice30] = useState("");
  const [price45, setPrice45] = useState("");
  const pricingMutation = useMutation({
    mutationFn: () =>
      updatePricing({
        writtenPrice: Number(writtenPrice),
        price15: Number(price15),
        price30: Number(price30),
        price45: Number(price45),
      }),
  });

  const [licenseNumber, setLicenseNumber] = useState("");
  const [issueDate, setIssueDate] = useState("");
  const [expiryDate, setExpiryDate] = useState("");
  const licenseMutation = useMutation({
    mutationFn: () => renewLicense({ licenseNumber, issueDate, expiryDate }),
  });

  function toggleId(list: number[], setList: (v: number[]) => void, id: number) {
    setList(list.includes(id) ? list.filter((x) => x !== id) : [...list, id]);
  }

  return (
    <AppShell>
      <h1 className="mb-6 font-display text-2xl font-bold">{isAr ? "الملف والتسعير" : "Profile & Pricing"}</h1>

      <div className="mx-auto flex max-w-2xl flex-col gap-6">
        <Card>
          <h2 className="mb-4 text-sm font-semibold text-ink-soft">{isAr ? "الملف الشخصي" : "Profile"}</h2>
          <div className="flex flex-col gap-3">
            <label className="text-sm font-medium text-ink-soft">{isAr ? "نبذة (عربي)" : "Bio (Arabic)"}</label>
            <textarea
              value={bioAr}
              onChange={(e) => setBioAr(e.target.value)}
              rows={3}
              className="rounded-md border border-border bg-surface-raised px-4 py-2.5 text-sm focus:border-seal focus:outline-none focus:ring-2 focus:ring-seal/30"
            />
            <label className="text-sm font-medium text-ink-soft">{isAr ? "نبذة (إنجليزي)" : "Bio (English)"}</label>
            <textarea
              value={bioEn}
              onChange={(e) => setBioEn(e.target.value)}
              dir="ltr"
              rows={3}
              className="rounded-md border border-border bg-surface-raised px-4 py-2.5 text-sm focus:border-seal focus:outline-none focus:ring-2 focus:ring-seal/30"
            />

            <label className="mt-2 text-sm font-medium text-ink-soft">{isAr ? "التخصصات" : "Specialties"}</label>
            <div className="flex flex-wrap gap-2">
              {specialtiesQuery.data?.map((s) => (
                <button
                  key={s.id}
                  type="button"
                  onClick={() => toggleId(specialtyIds, setSpecialtyIds, s.id)}
                  className={
                    "rounded-full border px-3 py-1.5 text-xs font-medium transition-colors " +
                    (specialtyIds.includes(s.id) ? "border-seal bg-seal text-seal-on" : "border-border text-ink-soft")
                  }
                >
                  {isAr ? s.nameAr : s.nameEn}
                </button>
              ))}
            </div>

            <label className="mt-2 text-sm font-medium text-ink-soft">{isAr ? "اللغات" : "Languages"}</label>
            <div className="flex flex-wrap gap-2">
              {languagesQuery.data?.map((l) => (
                <button
                  key={l.id}
                  type="button"
                  onClick={() => toggleId(languageIds, setLanguageIds, l.id)}
                  className={
                    "rounded-full border px-3 py-1.5 text-xs font-medium transition-colors " +
                    (languageIds.includes(l.id) ? "border-seal bg-seal text-seal-on" : "border-border text-ink-soft")
                  }
                >
                  {isAr ? l.nameAr : l.nameEn}
                </button>
              ))}
            </div>

            <label className="mt-2 flex items-center gap-2 text-sm text-ink-soft">
              <input type="checkbox" checked={accepting} onChange={(e) => setAccepting(e.target.checked)} />
              {isAr ? "أستقبل طلبات جديدة حالياً" : "Currently accepting new requests"}
            </label>

            <Button onClick={() => profileMutation.mutate()} disabled={profileMutation.isPending} className="mt-2 self-start">
              {isAr ? "حفظ الملف الشخصي" : "Save profile"}
            </Button>
            {profileMutation.isSuccess && <p className="text-sm text-seal">{isAr ? "تم الحفظ." : "Saved."}</p>}
          </div>
        </Card>

        <Card>
          <h2 className="mb-4 text-sm font-semibold text-ink-soft">{isAr ? "أسعار الاستشارات" : "Consultation pricing"}</h2>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="text-sm text-ink-soft">{isAr ? "استشارة كتابية" : "Written"}</label>
              <Input dir="ltr" className="font-mono" value={writtenPrice} onChange={(e) => setWrittenPrice(e.target.value)} />
            </div>
            <div>
              <label className="text-sm text-ink-soft">{isAr ? "15 دقيقة" : "15 min"}</label>
              <Input dir="ltr" className="font-mono" value={price15} onChange={(e) => setPrice15(e.target.value)} />
            </div>
            <div>
              <label className="text-sm text-ink-soft">{isAr ? "30 دقيقة" : "30 min"}</label>
              <Input dir="ltr" className="font-mono" value={price30} onChange={(e) => setPrice30(e.target.value)} />
            </div>
            <div>
              <label className="text-sm text-ink-soft">{isAr ? "45 دقيقة" : "45 min"}</label>
              <Input dir="ltr" className="font-mono" value={price45} onChange={(e) => setPrice45(e.target.value)} />
            </div>
          </div>
          <Button onClick={() => pricingMutation.mutate()} disabled={pricingMutation.isPending} className="mt-4">
            {isAr ? "حفظ الأسعار" : "Save pricing"}
          </Button>
          {pricingMutation.isSuccess && <p className="mt-2 text-sm text-seal">{isAr ? "تم الحفظ." : "Saved."}</p>}
        </Card>

        <Card>
          <h2 className="mb-4 text-sm font-semibold text-ink-soft">{isAr ? "تجديد الترخيص" : "Renew licence"}</h2>
          <div className="flex flex-col gap-3">
            <Input
              placeholder={isAr ? "رقم الترخيص" : "Licence number"}
              dir="ltr"
              className="font-mono"
              value={licenseNumber}
              onChange={(e) => setLicenseNumber(e.target.value)}
            />
            <div className="grid grid-cols-2 gap-3">
              <input
                type="date"
                value={issueDate}
                onChange={(e) => setIssueDate(e.target.value)}
                className="rounded-md border border-border bg-surface-raised px-4 py-2.5 text-sm"
              />
              <input
                type="date"
                value={expiryDate}
                onChange={(e) => setExpiryDate(e.target.value)}
                className="rounded-md border border-border bg-surface-raised px-4 py-2.5 text-sm"
              />
            </div>
            <Button
              onClick={() => licenseMutation.mutate()}
              disabled={!licenseNumber || !issueDate || !expiryDate || licenseMutation.isPending}
              className="self-start"
            >
              {isAr ? "إرسال للمراجعة" : "Submit for review"}
            </Button>
            {licenseMutation.isSuccess && (
              <p className="text-sm text-seal">{isAr ? "تم الإرسال، بانتظار مراجعة الإدارة." : "Submitted, pending admin review."}</p>
            )}
          </div>
        </Card>
      </div>
    </AppShell>
  );
}
