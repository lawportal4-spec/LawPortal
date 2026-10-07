import { useState, type FormEvent } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Link, useNavigate } from "react-router-dom";
import { ArrowLeft, ArrowRight } from "lucide-react";
import { Button, Card, SectionHeading } from "@law-portal/ui";
import { useTranslation } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { DiscountCodeFields } from "../components/DiscountCodeFields";
import { createDiscountCode, EMPTY_DISCOUNT_CODE, type DiscountCodeInput } from "../lib/discountCodesApi";

/** Create a discount code. On success it opens the new code's own page. */
export default function DiscountCodeNew() {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [form, setForm] = useState<DiscountCodeInput>(EMPTY_DISCOUNT_CODE);

  const create = useMutation({
    mutationFn: () => createDiscountCode(form),
    onSuccess: (id) => {
      void queryClient.invalidateQueries({ queryKey: ["discountCodes"] });
      navigate(`/discount-codes/${id}`, { replace: true });
    },
  });

  function submit(e: FormEvent) {
    e.preventDefault();
    create.mutate();
  }

  return (
    <AppShell>
      <BackToList isAr={i18n.language === "ar"} label={t("discount.admin.backToList")} />
      <SectionHeading level={2} className="mb-6">
        {t("discount.admin.new")}
      </SectionHeading>
      <Card className="max-w-3xl">
        <form onSubmit={submit}>
          <DiscountCodeFields form={form} onChange={(next) => { create.reset(); setForm(next); }} />
          {create.isError && <p className="mt-4 text-sm text-rubric">{t("discount.admin.saveFailed")}</p>}
          <div className="mt-6 flex gap-2">
            <Button type="submit" disabled={create.isPending || !form.code.trim() || form.scopes.length === 0}>
              {t("discount.admin.create")}
            </Button>
            <Link to="/discount-codes">
              <Button type="button" variant="ghost">{t("common.cancel")}</Button>
            </Link>
          </div>
        </form>
      </Card>
    </AppShell>
  );
}

export function BackToList({ isAr, label }: { isAr: boolean; label: string }) {
  return (
    <Link to="/discount-codes" className="mb-4 inline-flex items-center gap-1.5 text-sm text-ink-faint hover:text-ink">
      {isAr ? <ArrowRight className="h-4 w-4" /> : <ArrowLeft className="h-4 w-4" />}
      {label}
    </Link>
  );
}
