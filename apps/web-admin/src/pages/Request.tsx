import { useState, type ReactNode } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link, useParams } from "react-router-dom";
import { ArrowLeft, ArrowRight, CreditCard, Eye, FileText, Flag, History, MessagesSquare, Paperclip, Star, Users } from "lucide-react";
import { Button, Card, Ltr, StatusTag } from "@law-portal/ui";
import { formatDateTime, useTranslation } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { ActivityList } from "../components/directory/ActivityList";
import { AdminNotes } from "../components/directory/AdminNotes";
import { Money, RequestStatus } from "../components/directory/bits";
import { getRequest, getRequestChat } from "../lib/directoryApi";

function Row({ label, children }: { label: ReactNode; children: ReactNode }) {
  return (
    <div className="flex items-start justify-between gap-3 border-b border-rule py-2 text-sm last:border-0">
      <span className="text-ink-faint">{label}</span>
      <span className="text-end">{children}</span>
    </div>
  );
}

function Section({ icon, title, children }: { icon: ReactNode; title: string; children: ReactNode }) {
  return (
    <Card className="mb-4">
      <h2 className="mb-2 flex items-center gap-2 font-display text-base font-bold"><span className="text-seal">{icon}</span>{title}</h2>
      {children}
    </Card>
  );
}

/** One request: the service, parties, money, files, full history, rating, reports — and the chat, on demand. */
export default function Request() {
  const { id } = useParams<{ id: string }>();
  const { t, i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const query = useQuery({ queryKey: ["adminRequest", id], queryFn: () => getRequest(id!), enabled: !!id });
  const [showChat, setShowChat] = useState(false);
  const chat = useQuery({ queryKey: ["adminRequestChat", id], queryFn: () => getRequestChat(id!), enabled: showChat && !!id });
  const d = query.data;
  const s = d?.summary;

  return (
    <AppShell>
      <Link to="/requests" className="mb-4 inline-flex items-center gap-1.5 text-sm text-ink-faint hover:text-ink">
        {isAr ? <ArrowRight className="h-4 w-4" /> : <ArrowLeft className="h-4 w-4" />}{t("directory.backToRequests")}
      </Link>
      {d && s && (
        <>
          <div className="mb-4 flex flex-wrap items-start justify-between gap-3">
            <div>
              <h1 className="font-display text-2xl font-bold"><Ltr className="font-mono">{s.number}</Ltr></h1>
              <p className="mt-1 flex flex-wrap items-center gap-2 text-sm text-ink-faint">
                {t(`directory.types.${s.type}`, s.type)}
                {(isAr ? d.categoryNameAr : d.categoryNameEn) && <> · {isAr ? d.categoryNameAr : d.categoryNameEn}</>}
                <RequestStatus status={s.status} />
              </p>
            </div>
            {d.messagesCount > 0 && (
              <Button variant="secondary" onClick={() => setShowChat((v) => !v)}>
                <MessagesSquare className="h-4 w-4" />{showChat ? t("directory.hideChat") : t("directory.viewChat", { count: d.messagesCount })}
              </Button>
            )}
          </div>

          {showChat && (
            <Card className="mb-4 border-info/40">
              <p className="mb-3 flex items-center gap-2 text-xs text-info"><Eye className="h-4 w-4" />{t("directory.chatAudited")}</p>
              <div className="flex max-h-96 flex-col gap-2 overflow-y-auto">
                {chat.data?.map((m, i) => (
                  <div key={i} className={"max-w-[80%] rounded-lg px-3 py-2 text-sm " + (m.senderRole === "Lawyer" ? "self-end bg-seal-tint" : "self-start bg-surface-raised")}>
                    <span className="block text-xs text-ink-faint">{m.senderName ?? t(`directory.roles.${m.senderRole}`)} · <Ltr className="font-mono">{formatDateTime(m.sentAtUtc)}</Ltr></span>
                    <span className="whitespace-pre-wrap">{m.body}</span>
                  </div>
                ))}
              </div>
            </Card>
          )}

          <div className="grid gap-4 lg:grid-cols-3">
            <div className="lg:col-span-2">
              <Section icon={<FileText className="h-4 w-4" />} title={t("directory.serviceDetails")}>
                <Row label={t("directory.service")}>{(isAr ? d.serviceNameAr : d.serviceNameEn) ?? "—"}</Row>
                {d.title && <Row label={t("directory.subject")}>{d.title}</Row>}
                {d.description && <Row label={t("directory.description")}><span className="whitespace-pre-wrap">{d.description}</span></Row>}
                {d.scheduledStartUtc && <Row label={t("directory.scheduledAt")}><Ltr className="font-mono">{formatDateTime(d.scheduledStartUtc)}</Ltr></Row>}
                {s.type === "Bidding" && <Row label={t("directory.offers")}><Ltr className="font-mono">{d.offersCount}</Ltr></Row>}
                {d.cancelReason && <Row label={t("directory.cancelReason")}>{d.cancelReason}</Row>}
                <Row label={t("directory.createdAt")}><Ltr className="font-mono">{formatDateTime(s.createdAtUtc)}</Ltr></Row>
              </Section>

              <Section icon={<CreditCard className="h-4 w-4" />} title={t("directory.money")}>
                {d.payments.length === 0 && <p className="text-sm text-ink-faint">{t("directory.noPayments")}</p>}
                {d.payments.map((p) => (
                  <div key={p.id} className="border-b border-rule py-2 text-sm last:border-0">
                    <div className="flex flex-wrap items-center justify-between gap-2">
                      <Link to={`/payments/${p.id}`} className="text-seal-strong hover:underline"><Ltr className="font-mono">{p.number}</Ltr></Link>
                      <StatusTag status={p.status} label={t(`paymentStatus.${p.status}`, p.status)} />
                    </div>
                    <div className="mt-1 grid grid-cols-2 gap-x-6 gap-y-1 whitespace-nowrap text-xs text-ink-soft sm:grid-cols-3">
                      <span>{t("directory.price")} <Money value={p.gross} /></span>
                      {p.discount > 0 && <span>{t("directory.discount")} <Money value={-p.discount} className="text-rubric" /></span>}
                      <span>{t("directory.clientPaid")} <Money value={p.total} /></span>
                      {p.refunded > 0 && <span>{t("directory.refunded")} <Money value={-p.refunded} className="text-rubric" /></span>}
                      {p.lawyerShare != null && <span>{t("directory.lawyerShare")} <Money value={p.lawyerShare} /></span>}
                      {p.lawyerPaidOut != null && <span>{t("directory.paidOut")} <Money value={p.lawyerPaidOut} className="text-success" /></span>}
                    </div>
                  </div>
                ))}
              </Section>

              <Section icon={<History className="h-4 w-4" />} title={t("directory.fullHistory")}>
                <ol className="mb-2 flex flex-col">
                  {d.history.map((h, i) => (
                    <li key={i} className="flex items-center justify-between gap-3 border-b border-rule py-2 text-sm last:border-0">
                      <span>{h.from ? <>{t(`directory.requestStatuses.${h.from}`, h.from)} ← </> : null}<b>{t(`directory.requestStatuses.${h.to}`, h.to)}</b>
                        {h.trigger && <span className="block text-xs text-ink-faint">{t(`directory.triggers.${h.trigger}`, h.trigger)}</span>}</span>
                      <Ltr className="font-mono text-xs text-ink-faint">{formatDateTime(h.occurredAtUtc)}</Ltr>
                    </li>
                  ))}
                </ol>
                <p className="mb-1 mt-3 text-xs font-semibold text-ink-soft">{t("directory.adminActions")}</p>
                <ActivityList items={d.activity} />
              </Section>
            </div>

            <div>
              <Section icon={<Users className="h-4 w-4" />} title={t("directory.parties")}>
                <Row label={t("directory.client")}>
                  <Link to={`/clients/${s.clientProfileId}`} className="text-seal-strong hover:underline">{s.clientName ?? <Ltr className="font-mono">{s.clientPhoneE164}</Ltr>}</Link>
                </Row>
                <Row label={t("directory.lawyer")}>
                  {s.lawyerProfileId ? <Link to={`/lawyers/${s.lawyerProfileId}`} className="text-seal-strong hover:underline">{s.lawyerName}</Link> : "—"}
                </Row>
              </Section>
              <Section icon={<Paperclip className="h-4 w-4" />} title={t("directory.attachments")}>
                {d.attachments.length === 0 && <p className="text-sm text-ink-faint">{t("directory.none")}</p>}
                {d.attachments.map((a) => (
                  <Row key={a.id} label={a.url ? <a href={a.url} target="_blank" rel="noreferrer" className="text-seal-strong hover:underline">{a.fileName}</a> : a.fileName}>
                    <span className="text-xs">{t(`directory.scan.${a.scanStatus}`, a.scanStatus)}</span>
                  </Row>
                ))}
              </Section>
              <Section icon={<Star className="h-4 w-4" />} title={t("directory.rating")}>
                {d.review ? (
                  <p className="text-sm"><Ltr className="font-mono text-seal-strong">{"★".repeat(d.review.rating)}</Ltr>{d.review.comment && <span className="block text-ink-soft">{d.review.comment}</span>}</p>
                ) : <p className="text-sm text-ink-faint">{t("directory.noRating")}</p>}
              </Section>
              <Section icon={<Flag className="h-4 w-4" />} title={t("directory.reports")}>
                {d.reports.length === 0 && <p className="text-sm text-ink-faint">{t("directory.noReports")}</p>}
                {d.reports.map((r, i) => (
                  <Row key={i} label={t(`directory.reportReasons.${r.reason}`, r.reason)}><Ltr className="font-mono text-xs">{formatDateTime(r.createdAtUtc)}</Ltr></Row>
                ))}
              </Section>
              <AdminNotes entityType="Request" entityId={s.id} />
            </div>
          </div>
        </>
      )}
    </AppShell>
  );
}
