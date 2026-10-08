import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { DeleteAccountCard, ProfileCard, SectionHeading } from "@law-portal/ui";
import { useTranslation } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { deleteMyAccount, getDeletionImpact, getMyProfile, getRegions, saveMyProfile } from "../lib/accountApi";
import { useAuth } from "../lib/authContext";

export default function Account() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const profile = useQuery({ queryKey: ["myProfile"], queryFn: getMyProfile });
  const { logout } = useAuth();
  const navigate = useNavigate();

  return (
    <AppShell>
      <SectionHeading level={2} className="mb-6">{t("account.title")}</SectionHeading>
      {profile.data && (
        <div className="flex max-w-2xl flex-col gap-4">
          <ProfileCard
            profile={profile.data}
            onSave={async (input) => {
              await saveMyProfile(input);
              await queryClient.invalidateQueries({ queryKey: ["myProfile"] });
            }}
            loadRegions={getRegions}
          />
          <DeleteAccountCard
            loadImpact={getDeletionImpact}
            onDelete={async () => {
              await deleteMyAccount();
              logout();
              navigate("/");
            }}
          />
        </div>
      )}
    </AppShell>
  );
}
