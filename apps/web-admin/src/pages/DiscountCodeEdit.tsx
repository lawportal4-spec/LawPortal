import { useEffect, useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useNavigate, useParams } from "react-router-dom";
import { Share2 } from "lucide-react";
import { Button, Card, Ltr, SectionHeading, StatusTag } from "@law-portal/ui";
import { formatCurrency, formatDateTime, useTranslation } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { DiscountCodeFields, Redemptions } from "../components/DiscountCodeFields";
import { CopyCodeButton } from "../components/CopyCodeButton";
import { ShareDiscountDialog } from "../components/ShareDiscountDialog";
import {
  getDiscountCode,
  saveErrorKey,
  statusOf,
  updateDiscountCode,
  validateDiscountCode,
  type DiscountCodeInput,
  type DiscountFieldErrors,
} from "../lib/discountCodesApi";
import { BackToList } from "./DiscountCodeNew";

/** One code: its usage so far, its rules (editable, except the code text) and recent uses. */
export default function DiscountCodeEdit() {
  const { id } = useParams<{ id: string }>();
  const { t, i18n } = useTranslation();
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const query = useQuery({ queryKey: ["discountCode", id], queryFn: () => getDiscountCode(id!), enabled: !!id });
  const [form, setForm] = useState<DiscountCodeInput | null>(null);
  const [sharing, setSharing] = useState(false);
  const [errors, setErrors] = useState<DiscountFieldErrors>({});

  useEffect(() => {
    if (query.data) setForm(query.data);
  }, [query.data]);

  const save = useMutation({
    mutationFn: () => updateDiscountCode(id!, form!),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["discountCode", id] });
      void queryClient.invalidateQueries({ queryKey: ["discountCodes"] });
      // Back to the list, which confirms the save (see DiscountCodes).
      navigate("/discount-codes", { state: { savedId: id, action: "updated" } });
    },
  });

  function submit(e: FormEvent) {
    e.preventDefault();
    const found = validateDiscountCode(form!);
    setErrors(found);
    if (Object.keys(found).length === 0) save.mutate();
  }

  const code = query.data;
  const status = code ? statusOf(code) : null;

  return (
    <AppShell>
      <BackToList isAr={i18n.language === "ar"} label={t("discount.admin.backToList")} />
      {query.isError && <p className="text-sm text-rubric">{t("discount.admin.notFound")}</p>}
      {code && form && status && (
        <>
          <div className="mb-6 flex flex-wrap items-center gap-3">
            <SectionHeading level={2}>
              <Ltr className="font-mono">{code.code}</Ltr>
            </SectionHeading>
            <CopyCodeButton code={code.code} />
            <StatusTag status={status.tag} label={t(`discount.admin.statusFilter.${status.status}`)} />
            <Button variant="secondary" className="ms-auto" onClick={() => setSharing(true)}>
              <Share2 className="h-4 w-4" />
              {t("discount.share.share")}
            </Button>
          </div>

          <div className="grid max-w-5xl grid-cols-1 gap-4 lg:grid-cols-3">
            <Card className="lg:col-span-2">
              <SectionHeading level={3} className="mb-4">
                {t("discount.admin.edit")}
              </SectionHeading>
              <form onSubmit={submit}>
                <DiscountCodeFields
                  form={form}
                  onChange={(next) => {
                    save.reset();
                    setForm(next);
                    if (Object.keys(errors).length > 0) setErrors(validateDiscountCode(next));
                  }}
                  codeLocked
                  errors={errors}
                />
                {Object.keys(errors).length > 0 && <p className="mt-4 text-sm text-rubric">{t("discount.admin.errors.fixBelow")}</p>}
                {save.isError && <p className="mt-4 text-sm text-rubric">{t(`discount.admin.errors.${saveErrorKey(save.error)}`)}</p>}
                <div className="mt-6">
                  <Button type="submit" disabled={save.isPending}>
                    {t("discount.admin.save")}
                  </Button>
                </div>
              </form>
            </Card>

            <div className="flex flex-col gap-4">
              <Card>
                <dl className="flex flex-col gap-3 text-sm">
                  <Stat label={t("discount.admin.used")} value={`${code.used} / ${code.usageLimit ?? "∞"}`} />
                  <Stat label={t("discount.admin.totalDiscounted")} value={formatCurrency(code.totalDiscounted)} />
                  <Stat label={t("discount.admin.createdAt")} value={formatDateTime(code.createdAtUtc)} />
                </dl>
              </Card>
              <Card>
                <Redemptions codeId={code.id} />
              </Card>
            </div>
          </div>
        </>
      )}
      {sharing && code && <ShareDiscountDialog code={code} onClose={() => setSharing(false)} />}
    </AppShell>
  );
}

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between gap-3">
      <dt className="text-ink-faint">{label}</dt>
      <dd>
        <Ltr className="font-mono font-semibold text-ink">{value}</Ltr>
      </dd>
    </div>
  );
}
