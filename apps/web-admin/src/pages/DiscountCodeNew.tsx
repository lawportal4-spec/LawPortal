import { useState, type FormEvent } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Link, useNavigate } from "react-router-dom";
import { ArrowLeft, ArrowRight } from "lucide-react";
import { Button, Card, SectionHeading } from "@law-portal/ui";
import { useTranslation } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { DiscountCodeFields } from "../components/DiscountCodeFields";
import {
  createDiscountCode,
  EMPTY_DISCOUNT_CODE,
  saveErrorKey,
  validateDiscountCode,
  type DiscountCodeInput,
  type DiscountFieldErrors,
} from "../lib/discountCodesApi";

/** Create a discount code. On success it returns to the list, which offers to share the new code. */
export default function DiscountCodeNew() {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [form, setForm] = useState<DiscountCodeInput>(EMPTY_DISCOUNT_CODE);
  const [errors, setErrors] = useState<DiscountFieldErrors>({});
  const [triedSubmit, setTriedSubmit] = useState(false);

  const create = useMutation({
    mutationFn: () => createDiscountCode(form),
    onSuccess: (id) => {
      void queryClient.invalidateQueries({ queryKey: ["discountCodes"] });
      navigate("/discount-codes", { replace: true, state: { savedId: id, action: "created" } });
    },
  });

  function submit(e: FormEvent) {
    e.preventDefault();
    const found = validateDiscountCode(form);
    setErrors(found);
    setTriedSubmit(true);
    if (Object.keys(found).length === 0) create.mutate();
  }

  function change(next: DiscountCodeInput) {
    create.reset();
    setForm(next);
    // Once the admin has tried to save, keep the messages in step with what they fix.
    if (triedSubmit) setErrors(validateDiscountCode(next));
  }

  return (
    <AppShell>
      <BackToList isAr={i18n.language === "ar"} label={t("discount.admin.backToList")} />
      <SectionHeading level={2} className="mb-6">
        {t("discount.admin.new")}
      </SectionHeading>
      <Card className="max-w-3xl">
        <form onSubmit={submit}>
          <DiscountCodeFields form={form} onChange={change} errors={errors} />
          {Object.keys(errors).length > 0 && <p className="mt-4 text-sm text-rubric">{t("discount.admin.errors.fixBelow")}</p>}
          {create.isError && <p className="mt-4 text-sm text-rubric">{t(`discount.admin.errors.${saveErrorKey(create.error)}`)}</p>}
          <div className="mt-6 flex gap-2">
            <Button type="submit" disabled={create.isPending}>
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
