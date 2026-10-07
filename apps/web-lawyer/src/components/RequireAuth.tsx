import type { ReactNode } from "react";
import { useQuery } from "@tanstack/react-query";
import { Navigate, useLocation } from "react-router-dom";
import { useAuth } from "../lib/authContext";
import { getLawyerMe } from "../lib/authApi";
import { RegistrationStatus } from "./RegistrationStatus";

export function RequireAuth({ children }: { children: ReactNode }) {
  const { isAuthenticated } = useAuth();
  const location = useLocation();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }

  return <RequireApproval>{children}</RequireApproval>;
}

/** Until an admin approves the licence, the whole portal is replaced by the registration's status:
 * under review, returned for changes (with a form to fix them), or rejected. */
function RequireApproval({ children }: { children: ReactNode }) {
  const location = useLocation();
  // gcTime 0: dropped once signed-out pages unmount this, so the next lawyer to sign in on this
  // browser never sees the previous one's approval state, even for a frame.
  const me = useQuery({ queryKey: ["lawyerMe"], queryFn: getLawyerMe, gcTime: 0 });

  if (me.isPending) return null;
  // A failed lookup shouldn't hide the portal from an approved lawyer; the pages' own calls will
  // surface a real problem (and a 401 already signs out via the API interceptor).
  if (me.isError || me.data.isApproved) return <>{children}</>;
  // The fee checkout is the one page an approved-but-unpaid lawyer may open.
  if (me.data.onboardingStep === "PayFee" && location.pathname === "/registration-fee") return <>{children}</>;

  return <RegistrationStatus me={me.data} />;
}
