import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ChangePasswordCard, ProfileCard, SectionHeading } from "@law-portal/ui";
import { useTranslation } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { changeMyPassword, getMyProfile, saveMyProfile } from "../lib/accountApi";

export default function Account() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const profile = useQuery({ queryKey: ["myProfile"], queryFn: getMyProfile });

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
          />
          {profile.data.hasPassword && <ChangePasswordCard onChange={changeMyPassword} />}
        </div>
      )}
    </AppShell>
  );
}
