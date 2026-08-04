import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ChevronDown, ChevronUp, Pencil } from "lucide-react";
import { Button, Card, Ltr } from "@law-portal/ui";
import { useTranslation, formatCurrency } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import {
  getCategories,
  updateCategory,
  updateService,
  updateVariant,
  type AdminServiceCatalogItemDto,
  type AdminServiceCategoryDto,
  type AdminServiceVariantDto,
} from "../lib/catalogApi";

function VariantRow({ variant, isAr }: { variant: AdminServiceVariantDto; isAr: boolean }) {
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState(false);
  const [basePrice, setBasePrice] = useState(String(variant.basePrice));
  const [isActive, setIsActive] = useState(variant.isActive);

  const mutation = useMutation({
    mutationFn: () =>
      updateVariant(variant.id, {
        nameAr: variant.nameAr,
        nameEn: variant.nameEn,
        basePrice: Number(basePrice),
        requiresQuantity: variant.requiresQuantity,
        includedQuantity: variant.includedQuantity,
        extraUnitPrice: variant.extraUnitPrice,
        quantityLabelAr: variant.quantityLabelAr,
        quantityLabelEn: variant.quantityLabelEn,
        minQuantity: variant.minQuantity,
        maxQuantity: variant.maxQuantity,
        sortOrder: variant.sortOrder,
        isActive,
      }),
    onSuccess: () => {
      setEditing(false);
      void queryClient.invalidateQueries({ queryKey: ["adminCatalog"] });
    },
  });

  return (
    <div className="flex items-center justify-between gap-3 border-b border-border py-2 text-sm last:border-0">
      <span className={variant.isActive ? "text-ink-soft" : "text-ink-faint line-through"}>{isAr ? variant.nameAr : variant.nameEn}</span>
      {editing ? (
        <div className="flex items-center gap-2">
          <input
            type="number"
            value={basePrice}
            onChange={(e) => setBasePrice(e.target.value)}
            className="w-24 rounded-md border border-border bg-surface-raised px-2 py-1 text-xs"
          />
          <label className="flex items-center gap-1 text-xs">
            <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />
            {isAr ? "نشط" : "Active"}
          </label>
          <Button onClick={() => mutation.mutate()} disabled={mutation.isPending}>
            {isAr ? "حفظ" : "Save"}
          </Button>
          <Button variant="ghost" onClick={() => setEditing(false)}>
            {isAr ? "إلغاء" : "Cancel"}
          </Button>
        </div>
      ) : (
        <div className="flex items-center gap-2">
          <span className="font-mono">
            <Ltr>{formatCurrency(variant.basePrice)}</Ltr>
          </span>
          <button onClick={() => setEditing(true)} className="text-ink-faint hover:text-seal">
            <Pencil className="h-3.5 w-3.5" />
          </button>
        </div>
      )}
    </div>
  );
}

function ServiceRow({ service, isAr }: { service: AdminServiceCatalogItemDto; isAr: boolean }) {
  const queryClient = useQueryClient();
  const [expanded, setExpanded] = useState(false);
  const [editing, setEditing] = useState(false);
  const [isActive, setIsActive] = useState(service.isActive);
  const [sortOrder, setSortOrder] = useState(service.sortOrder);

  const mutation = useMutation({
    mutationFn: () =>
      updateService(service.id, {
        nameAr: service.nameAr,
        nameEn: service.nameEn,
        descriptionAr: service.descriptionAr,
        descriptionEn: service.descriptionEn,
        requiresSpecialty: service.requiresSpecialty,
        sortOrder,
        isActive,
      }),
    onSuccess: () => {
      setEditing(false);
      void queryClient.invalidateQueries({ queryKey: ["adminCatalog"] });
    },
  });

  return (
    <div className="border-b border-border py-2 last:border-0">
      <div className="flex items-center justify-between gap-3">
        <button onClick={() => setExpanded((e) => !e)} className="flex items-center gap-2 text-start text-sm">
          {service.variants.length > 0 && (expanded ? <ChevronUp className="h-3.5 w-3.5" /> : <ChevronDown className="h-3.5 w-3.5" />)}
          <span className={service.isActive ? "font-medium text-ink" : "font-medium text-ink-faint line-through"}>
            {isAr ? service.nameAr : service.nameEn}
          </span>
          <span className="rounded-full bg-paper px-2 py-0.5 text-[10px] text-ink-faint">{service.pricingModel}</span>
        </button>
        {editing ? (
          <div className="flex items-center gap-2">
            <input
              type="number"
              value={sortOrder}
              onChange={(e) => setSortOrder(Number(e.target.value))}
              className="w-16 rounded-md border border-border bg-surface-raised px-2 py-1 text-xs"
            />
            <label className="flex items-center gap-1 text-xs">
              <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />
              {isAr ? "نشط" : "Active"}
            </label>
            <Button onClick={() => mutation.mutate()} disabled={mutation.isPending}>
              {isAr ? "حفظ" : "Save"}
            </Button>
            <Button variant="ghost" onClick={() => setEditing(false)}>
              {isAr ? "إلغاء" : "Cancel"}
            </Button>
          </div>
        ) : (
          <button onClick={() => setEditing(true)} className="text-ink-faint hover:text-seal">
            <Pencil className="h-3.5 w-3.5" />
          </button>
        )}
      </div>
      {expanded && service.variants.length > 0 && (
        <div className="mt-2 ps-6">
          {service.variants.map((v) => (
            <VariantRow key={v.id} variant={v} isAr={isAr} />
          ))}
        </div>
      )}
    </div>
  );
}

function CategoryCard({ category, isAr }: { category: AdminServiceCategoryDto; isAr: boolean }) {
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState(false);
  const [isActive, setIsActive] = useState(category.isActive);

  const mutation = useMutation({
    mutationFn: () => updateCategory(category.id, { nameAr: category.nameAr, nameEn: category.nameEn, iconKey: category.iconKey, sortOrder: category.sortOrder, isActive }),
    onSuccess: () => {
      setEditing(false);
      void queryClient.invalidateQueries({ queryKey: ["adminCatalog"] });
    },
  });

  return (
    <Card className="mb-4">
      <div className="mb-3 flex items-center justify-between">
        <h2 className={"font-display text-lg font-bold " + (category.isActive ? "" : "text-ink-faint line-through")}>
          {isAr ? category.nameAr : category.nameEn}
        </h2>
        {editing ? (
          <div className="flex items-center gap-2">
            <label className="flex items-center gap-1 text-xs">
              <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />
              {isAr ? "نشط" : "Active"}
            </label>
            <Button onClick={() => mutation.mutate()} disabled={mutation.isPending}>
              {isAr ? "حفظ" : "Save"}
            </Button>
            <Button variant="ghost" onClick={() => setEditing(false)}>
              {isAr ? "إلغاء" : "Cancel"}
            </Button>
          </div>
        ) : (
          <button onClick={() => setEditing(true)} className="text-ink-faint hover:text-seal">
            <Pencil className="h-4 w-4" />
          </button>
        )}
      </div>
      <div>
        {category.services.map((s) => (
          <ServiceRow key={s.id} service={s} isAr={isAr} />
        ))}
      </div>
    </Card>
  );
}

export default function Catalog() {
  const { i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const query = useQuery({ queryKey: ["adminCatalog"], queryFn: getCategories });

  return (
    <AppShell>
      <h1 className="mb-1 font-display text-2xl font-bold">{isAr ? "الخدمات والتسعير" : "Catalog & Pricing"}</h1>
      <p className="mb-6 text-sm text-ink-faint">
        {isAr
          ? "إدارة الفئات والخدمات والخيارات المسعّرة. التبديل بين نمط التسعير غير متاح بعد الإنشاء."
          : "Manage categories, services, and priced variants. Pricing model can't change after a service is created."}
      </p>

      {query.isLoading && <p className="text-sm text-ink-faint">{isAr ? "جارٍ التحميل…" : "Loading…"}</p>}

      {query.data?.map((c) => <CategoryCard key={c.id} category={c} isAr={isAr} />)}
    </AppShell>
  );
}
