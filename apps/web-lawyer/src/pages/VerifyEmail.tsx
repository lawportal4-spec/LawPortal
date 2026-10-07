import { useQuery } from "@tanstack/react-query";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { CircleCheck, XCircle } from "lucide-react";
import { useTranslation } from "@law-portal/i18n";
import { AuthLayout } from "../components/AuthForm";
import { Accepted } from "../components/Onboarding";
import { useAuth } from "../lib/authContext";
import { verifyLawyerEmail } from "../lib/authApi";

/** Landing page of the "تفعيل الآن" email link. Public: the link may be opened on a device where
 * the lawyer isn't signed in, so the next step goes through sign-in when needed. */
export default function VerifyEmail() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { isAuthenticated } = useAuth();
  const [params] = useSearchParams();
  const token = params.get("token") ?? "";

  // A query, not a mutation: it runs once per token even under StrictMode, and verifying the
  // same link twice is harmless on the server anyway.
  const result = useQuery({
    queryKey: ["verifyLawyerEmail", token],
    queryFn: () => verifyLawyerEmail(token),
    enabled: !!token,
    retry: false,
    staleTime: Infinity,
  });

  function start() {
    const target = result.data?.nextStep === "PayFee" ? "/registration-fee" : "/settings";
    if (isAuthenticated) navigate(target);
    else navigate("/login", { state: { from: target } });
  }

  return (
    <AuthLayout hideNav>
      {result.isPending && token && <p className="text-center text-sm text-ink-soft">{t("lawyerOnboarding.verifying")}</p>}

      {(!token || result.isError) && (
        <div className="flex flex-col items-center gap-4 text-center">
          <XCircle className="h-12 w-12 text-rubric" />
          <p className="text-sm text-ink">{t("lawyerOnboarding.linkInvalid")}</p>
          <Link to="/login" className="text-sm font-semibold text-seal hover:underline">
            {t("lawyerOnboarding.toLogin")}
          </Link>
        </div>
      )}

      {result.data?.nextStep === "EmailChanged" && (
        <div className="flex flex-col items-center gap-4 text-center">
          <CircleCheck className="h-12 w-12 text-seal" />
          <p className="text-lg font-semibold text-ink">{t("lawyerAccount.emailChanged")}</p>
          <Link to="/account" className="text-sm font-semibold text-seal hover:underline">
            {t("lawyerAccount.backToAccount")}
          </Link>
        </div>
      )}

      {result.data && result.data.nextStep !== "EmailChanged" && (
        <>
          <p role="status" className="mb-6 flex items-center justify-center gap-2 rounded-md bg-seal-tint px-4 py-2.5 text-sm font-medium text-seal-strong">
            <CircleCheck className="h-4 w-4" />
            {t("lawyerOnboarding.emailVerified")}
          </p>
          <Accepted name={result.data.fullName} onStart={start} />
        </>
      )}
    </AuthLayout>
  );
}
