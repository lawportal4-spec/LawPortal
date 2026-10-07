import { useId, type ReactNode } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { Button, Card } from "@law-portal/ui";
import { useTranslation } from "@law-portal/i18n";
import { useAuth } from "../lib/authContext";
import { acceptPledge, getMe } from "../lib/authApi";

/** Until the signed-in client accepts the platform pledge, every protected page sits behind it:
 * visible but inert, with the pledge on top — Confirm, or sign out. */
export function PledgeGate({ children }: { children: ReactNode }) {
  // gcTime 0: the next client to sign in on this browser must not inherit this one's answer.
  const me = useQuery({ queryKey: ["me"], queryFn: getMe, gcTime: 0 });
  // A failed lookup shouldn't lock the client out; a 401 already signs them out via the interceptor.
  const blocked = me.data?.userType === "Client" && !me.data.pledgeAccepted;

  return (
    <>
      <div inert={blocked}>{children}</div>
      {blocked && <PledgeDialog />}
    </>
  );
}

function PledgeDialog() {
  const { t, i18n } = useTranslation();
  const titleId = useId();
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const { logout } = useAuth();
  const items = t("clientPledge.items", { returnObjects: true }) as string[];

  const accept = useMutation({
    mutationFn: acceptPledge,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["me"] }),
  });

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 px-4">
      <Card role="dialog" aria-modal="true" aria-labelledby={titleId} elevated className="max-h-[90vh] w-full max-w-lg overflow-y-auto">
        <div className="mb-4 flex flex-col items-center gap-2 text-center">
          <img src="/favicon.svg" alt="" className="h-16 w-14" />
          <p className="font-display text-2xl font-bold text-seal-strong">{i18n.language === "ar" ? t("app.nameAr") : t("app.nameEn")}</p>
          <h2 id={titleId} className="text-base font-semibold text-ink-soft">
            {t("clientPledge.title")}
          </h2>
        </div>
        <p className="mb-3 text-sm font-medium text-ink">{t("clientPledge.intro")}</p>
        <ul className="mb-6 flex list-disc flex-col gap-2 ps-5 text-sm leading-relaxed text-ink-soft">
          {items.map((item) => (
            <li key={item}>{item}</li>
          ))}
        </ul>
        {accept.isError && <p className="mb-3 text-sm text-rubric">{t("clientPledge.failed")}</p>}
        <div className="flex flex-col gap-2">
          <Button autoFocus className="w-full justify-center" disabled={accept.isPending} onClick={() => accept.mutate()}>
            {t("clientPledge.confirm")}
          </Button>
          <Button
            variant="ghost"
            className="w-full justify-center"
            onClick={() => {
              logout();
              navigate("/", { replace: true });
            }}
          >
            {t("clientPledge.logout")}
          </Button>
        </div>
      </Card>
    </div>
  );
}
