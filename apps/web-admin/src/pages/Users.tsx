import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { UserPlus } from "lucide-react";
import { Button, Card, Ltr } from "@law-portal/ui";
import { useTranslation, formatDateTime } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { getAdminUsers, getRoles, inviteAdmin } from "../lib/rbacApi";

export default function Users() {
  const { i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const queryClient = useQueryClient();
  const [showInvite, setShowInvite] = useState(false);
  const [displayName, setDisplayName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);

  const usersQuery = useQuery({ queryKey: ["adminUsers"], queryFn: getAdminUsers });
  const rolesQuery = useQuery({ queryKey: ["adminRoles"], queryFn: getRoles });

  const inviteMutation = useMutation({
    mutationFn: () => inviteAdmin(displayName, email, password),
    onSuccess: () => {
      setError(null);
      setShowInvite(false);
      setDisplayName("");
      setEmail("");
      setPassword("");
      void queryClient.invalidateQueries({ queryKey: ["adminUsers"] });
    },
    onError: () => setError(isAr ? "تعذّر إضافة المسؤول." : "Could not invite the admin."),
  });

  return (
    <AppShell>
      <h1 className="mb-6 font-display text-2xl font-bold">{isAr ? "المستخدمون والصلاحيات" : "Users & Roles"}</h1>

      <div className="mb-8">
        <div className="mb-3 flex items-center justify-between">
          <h2 className="text-sm font-semibold text-ink-soft">{isAr ? "المسؤولون" : "Admin Users"}</h2>
          {!showInvite && (
            <Button onClick={() => setShowInvite(true)}>
              <UserPlus className="h-4 w-4" />
              {isAr ? "إضافة مسؤول" : "Invite admin"}
            </Button>
          )}
        </div>

        {showInvite && (
          <Card className="mb-4">
            <div className="flex flex-col gap-3">
              <input
                value={displayName}
                onChange={(e) => setDisplayName(e.target.value)}
                placeholder={isAr ? "الاسم المعروض" : "Display name"}
                className="rounded-md border border-border bg-surface-raised px-3 py-2 text-sm"
              />
              <input
                type="email"
                dir="ltr"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder={isAr ? "البريد الإلكتروني" : "Email"}
                className="rounded-md border border-border bg-surface-raised px-3 py-2 text-sm font-mono"
              />
              <input
                type="password"
                dir="ltr"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder={isAr ? "كلمة المرور" : "Password"}
                className="rounded-md border border-border bg-surface-raised px-3 py-2 text-sm"
              />
              {error && <p className="text-sm text-rubric">{error}</p>}
              <div className="flex gap-2">
                <Button onClick={() => inviteMutation.mutate()} disabled={!displayName || !email || !password || inviteMutation.isPending}>
                  {isAr ? "إرسال الدعوة" : "Send invite"}
                </Button>
                <Button variant="ghost" onClick={() => setShowInvite(false)}>
                  {isAr ? "إلغاء" : "Cancel"}
                </Button>
              </div>
            </div>
          </Card>
        )}

        <div className="flex flex-col gap-2">
          {usersQuery.data?.map((u) => (
            <Card key={u.userId} className="flex items-center justify-between gap-4">
              <div>
                <p className="font-medium text-ink">{u.displayName}</p>
                <p className="text-xs text-ink-faint">
                  <Ltr className="font-mono">{u.email}</Ltr>
                </p>
              </div>
              <div className="text-end">
                <p className="text-xs text-ink-soft">{u.roles.join(", ")}</p>
                <p className="text-xs text-ink-faint">
                  {isAr ? "آخر دخول: " : "Last login: "}
                  {u.lastLoginAtUtc ? <Ltr className="font-mono">{formatDateTime(u.lastLoginAtUtc)}</Ltr> : "—"}
                </p>
              </div>
            </Card>
          ))}
        </div>
      </div>

      <div>
        <h2 className="mb-3 text-sm font-semibold text-ink-soft">{isAr ? "الأدوار والصلاحيات" : "Roles & Permissions"}</h2>
        <p className="mb-3 text-xs text-ink-faint">
          {isAr
            ? "عرض للاطلاع فقط — كل إجراءات الإدارة مصرَّح بها حاليًا على مستوى الدور (مسؤول)، وليس على مستوى الصلاحية الفردية."
            : "Read-only — every admin action is currently authorized at the role level (Admin), not per individual permission."}
        </p>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
          {rolesQuery.data?.map((r) => (
            <Card key={r.id}>
              <h3 className="mb-2 font-medium text-ink">{r.name}</h3>
              {r.permissions.length === 0 ? (
                <p className="text-xs text-ink-faint">{isAr ? "لا توجد صلاحيات محدّدة." : "No permissions assigned."}</p>
              ) : (
                <ul className="flex flex-col gap-1 text-xs text-ink-soft">
                  {r.permissions.map((p) => (
                    <li key={p} className="font-mono">
                      <Ltr>{p}</Ltr>
                    </li>
                  ))}
                </ul>
              )}
            </Card>
          ))}
        </div>
      </div>
    </AppShell>
  );
}
