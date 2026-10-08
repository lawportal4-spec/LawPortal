import { useNavigate } from "react-router-dom";
import { Ltr } from "@law-portal/ui";
import { formatDate, useTranslation } from "@law-portal/i18n";
import type { AdminRequestRow } from "../../lib/directoryApi";
import { Money, RequestStatus } from "./bits";

/** Requests as rows; a row opens the request. Used on the requests page, a client's page and a lawyer's page. */
export function RequestsTable({ rows, hide = [] }: { rows: AdminRequestRow[]; hide?: ("client" | "lawyer")[] }) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  if (rows.length === 0) return <p className="py-8 text-center text-sm text-ink-faint">{t("directory.noRequests")}</p>;
  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[720px] text-sm">
        <thead><tr className="border-b border-border text-xs text-ink-faint">
          <th className="py-2 text-start font-medium">{t("directory.request")}</th>
          <th className="py-2 text-center font-medium">{t("directory.type")}</th>
          {!hide.includes("client") && <th className="py-2 text-start font-medium">{t("directory.client")}</th>}
          {!hide.includes("lawyer") && <th className="py-2 text-center font-medium">{t("directory.lawyer")}</th>}
          <th className="py-2 text-center font-medium">{t("directory.amount")}</th>
          <th className="py-2 text-center font-medium">{t("directory.status")}</th>
          <th className="py-2 text-center font-medium">{t("directory.date")}</th>
        </tr></thead>
        <tbody>
          {rows.map((r) => (
            <tr key={r.id} className="cursor-pointer border-b border-rule hover:bg-surface-raised/40" onClick={() => navigate(`/requests/${r.id}`)}>
              <td className="py-2.5"><Ltr className="font-mono text-seal-strong">{r.number}</Ltr></td>
              <td className="py-2.5 text-center">{t(`directory.types.${r.type}`, r.type)}</td>
              {!hide.includes("client") && <td className="py-2.5 text-start">{r.clientName ?? "—"}<span className="block text-xs text-ink-faint"><Ltr className="font-mono">{r.clientPhoneE164 ?? "—"}</Ltr></span></td>}
              {!hide.includes("lawyer") && <td className="py-2.5 text-center">{r.lawyerName ?? "—"}</td>}
              <td className="py-2.5 text-center">{r.amount != null ? <Money value={r.amount} /> : "—"}</td>
              <td className="py-2.5 text-center"><RequestStatus status={r.status} /></td>
              <td className="py-2.5 text-center"><Ltr className="font-mono">{formatDate(r.createdAtUtc)}</Ltr></td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
