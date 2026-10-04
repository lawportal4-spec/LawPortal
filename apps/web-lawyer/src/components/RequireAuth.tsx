import type { ReactNode } from "react";
import { useQuery } from "@tanstack/react-query";
import { Navigate, useLocation, useNavigate } from "react-router-dom";
import { Button } from "@law-portal/ui";
import { useTranslation } from "@law-portal/i18n";
import { useAuth } from "../lib/authContext";
import { getLawyerMe } from "../lib/authApi";
import { AuthLayout } from "./AuthForm";

export function RequireAuth({ children }: { children: ReactNode }) {
  const { isAuthenticated } = useAuth();
  const location = useLocation();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }

  return <RequireApproval>{children}</RequireApproval>;
}

/** Until an admin approves the licence, the whole portal is replaced by a "registration under
 * review" screen — the dashboard has nothing a not-yet-approved lawyer can act on. */
function RequireApproval({ children }: { children: ReactNode }) {
  const { t } = useTranslation();
  const { logout } = useAuth();
  const navigate = useNavigate();
  // gcTime 0: dropped once signed-out pages unmount this, so the next lawyer to sign in on this
  // browser never sees the previous one's approval state, even for a frame.
  const me = useQuery({ queryKey: ["lawyerMe"], queryFn: getLawyerMe, gcTime: 0 });

  if (me.isPending) return null;
  // A failed lookup shouldn't hide the portal from an approved lawyer; the pages' own calls will
  // surface a real problem (and a 401 already signs out via the API interceptor).
  if (me.isError || me.data.isApproved) return <>{children}</>;

  return (
    <AuthLayout hideNav>
      <div className="flex flex-col items-center gap-4 text-center">
        <p className="text-lg text-ink">{t("lawyerAuth.pending.greeting", { name: me.data.fullName })}</p>
        <p className="font-display text-2xl text-seal">{t("lawyerAuth.pending.thanks")}</p>
        <p className="text-sm text-ink-faint">{t("lawyerAuth.pending.review")}</p>
        <Button
          variant="secondary"
          className="mt-4"
          onClick={() => {
            logout();
            navigate("/login", { replace: true });
          }}
        >
          {t("lawyerAuth.pending.logout")}
        </Button>
      </div>
    </AuthLayout>
  );
}
