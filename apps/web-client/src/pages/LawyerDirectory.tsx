import { useState } from "react";
import { useQuery, keepPreviousData } from "@tanstack/react-query";
import { Search, ChevronLeft, ChevronRight } from "lucide-react";
import { Input } from "@law-portal/ui";
import { useTranslation } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { LawyerCard } from "../components/LawyerCard";
import { searchLawyers, type LawyerSortOption } from "../lib/api";

const SORTS: { value: LawyerSortOption; ar: string; en: string }[] = [
  { value: "Newest", ar: "الأحدث", en: "Newest" },
  { value: "MostRequested", ar: "الأنسب", en: "Most requested" },
  { value: "Rating", ar: "التقييمات", en: "Ratings" },
  { value: "City", ar: "المدينة", en: "City" },
  { value: "Experience", ar: "المؤهلات", en: "Experience" },
  { value: "Price", ar: "سعر الاستشارة", en: "Price" },
];

const GENDERS = [
  { value: "", ar: "الكل", en: "All" },
  { value: "Male", ar: "محامي", en: "Male" },
  { value: "Female", ar: "محامية", en: "Female" },
] as const;

export default function LawyerDirectory() {
  const { i18n } = useTranslation();
  const isAr = i18n.language === "ar";

  const [q, setQ] = useState("");
  const [gender, setGender] = useState<"" | "Male" | "Female">("");
  const [sort, setSort] = useState<LawyerSortOption>("Newest");
  const [page, setPage] = useState(1);
  const pageSize = 10;

  const query = useQuery({
    queryKey: ["lawyers", { q, gender, sort, page, pageSize }],
    queryFn: () =>
      searchLawyers({
        q: q || undefined,
        gender: gender || undefined,
        sort,
        page,
        pageSize,
      }),
    placeholderData: keepPreviousData,
  });

  function resetToFirstPage<T>(setter: (v: T) => void) {
    return (value: T) => {
      setter(value);
      setPage(1);
    };
  }

  return (
    <AppShell>
      <h1 className="mb-1 font-display text-2xl font-bold">{isAr ? "دليل المحامين" : "Lawyer Directory"}</h1>
      <p className="mb-6 text-sm text-ink-faint">
        {isAr
          ? `${query.data?.totalCount ?? "…"} محامٍ ومحامية مرخّصون`
          : `${query.data?.totalCount ?? "…"} licensed lawyers`}
      </p>

      <div className="mb-6 flex flex-wrap items-center gap-3">
        <Input
          icon={<Search className="h-4 w-4" />}
          placeholder={isAr ? "ابحث بالاسم" : "Search by name"}
          className="max-w-xs"
          value={q}
          onChange={(e) => resetToFirstPage(setQ)(e.target.value)}
        />

        <select
          className="rounded-md border border-border bg-surface-raised px-3 py-2.5 text-sm"
          value={gender}
          onChange={(e) => resetToFirstPage(setGender)(e.target.value as typeof gender)}
        >
          {GENDERS.map((g) => (
            <option key={g.value} value={g.value}>
              {isAr ? g.ar : g.en}
            </option>
          ))}
        </select>

        <select
          className="rounded-md border border-border bg-surface-raised px-3 py-2.5 text-sm"
          value={sort}
          onChange={(e) => resetToFirstPage(setSort)(e.target.value as LawyerSortOption)}
        >
          {SORTS.map((s) => (
            <option key={s.value} value={s.value}>
              {isAr ? s.ar : s.en}
            </option>
          ))}
        </select>
      </div>

      {query.isLoading && <p className="text-sm text-ink-faint">{isAr ? "جارٍ التحميل…" : "Loading…"}</p>}
      {query.isError && (
        <p className="text-sm text-rubric">
          {isAr ? "تعذّر تحميل الدليل." : "Could not load the directory."}
        </p>
      )}

      {query.data && (
        <>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {query.data.items.map((lawyer) => (
              <LawyerCard key={lawyer.id} lawyer={lawyer} />
            ))}
          </div>

          {query.data.items.length === 0 && (
            <p className="mt-8 text-center text-sm text-ink-faint">
              {isAr ? "لا توجد نتائج مطابقة." : "No matching results."}
            </p>
          )}

          <div className="mt-8 flex items-center justify-center gap-4">
            <button
              disabled={page <= 1}
              onClick={() => setPage((p) => p - 1)}
              className="flex items-center gap-1 rounded-md border border-border px-3 py-2 text-sm disabled:opacity-40"
            >
              <ChevronLeft className="h-4 w-4 rtl:hidden" />
              <ChevronRight className="h-4 w-4 ltr:hidden" />
              {isAr ? "السابق" : "Previous"}
            </button>
            <span className="font-mono text-sm text-ink-soft">
              {page} / {query.data.totalPages || 1}
            </span>
            <button
              disabled={page >= query.data.totalPages}
              onClick={() => setPage((p) => p + 1)}
              className="flex items-center gap-1 rounded-md border border-border px-3 py-2 text-sm disabled:opacity-40"
            >
              {isAr ? "التالي" : "Next"}
              <ChevronRight className="h-4 w-4 rtl:hidden" />
              <ChevronLeft className="h-4 w-4 ltr:hidden" />
            </button>
          </div>
        </>
      )}
    </AppShell>
  );
}
