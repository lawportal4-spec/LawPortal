import { useEffect, useRef, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useParams } from "react-router-dom";
import { ArrowLeft, ArrowRight, Ban, Flag, Send } from "lucide-react";
import type { HubConnection } from "@microsoft/signalr";
import { Button, Card, Ltr } from "@law-portal/ui";
import { useTranslation, formatDateTime } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import {
  blockThreadParticipant,
  connectChatHub,
  getMessages,
  getThreadDetail,
  getUserIdFromToken,
  listMyThreads,
  markThreadRead,
  reportThreadParticipant,
  sendMessage,
  unblockThreadParticipant,
  type MessageDto,
} from "../lib/chatApi";

export default function Chat() {
  const { requestId } = useParams<{ requestId: string }>();
  const { i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const queryClient = useQueryClient();

  const [messages, setMessages] = useState<MessageDto[]>([]);
  const [draft, setDraft] = useState("");
  const [isOnline, setIsOnline] = useState(false);
  const bottomRef = useRef<HTMLDivElement>(null);
  const hubRef = useRef<HubConnection | null>(null);

  const myUserId = getUserIdFromToken(localStorage.getItem("lp_access_token") ?? "");

  const threadsQuery = useQuery({ queryKey: ["myThreads"], queryFn: listMyThreads });
  const thread = threadsQuery.data?.find((t) => t.serviceRequestId === requestId);

  const detailQuery = useQuery({
    queryKey: ["threadDetail", thread?.id],
    queryFn: () => getThreadDetail(thread!.id),
    enabled: !!thread,
  });

  useEffect(() => {
    if (detailQuery.data) setIsOnline(detailQuery.data.isOtherPartyOnline);
  }, [detailQuery.data]);

  useEffect(() => {
    if (!thread) return;
    let cancelled = false;

    (async () => {
      const history = await getMessages(thread.id);
      if (!cancelled) setMessages(history);
      await markThreadRead(thread.id);
      void queryClient.invalidateQueries({ queryKey: ["myThreads"] });

      const token = localStorage.getItem("lp_access_token") ?? "";
      const hub = connectChatHub(token);
      hub.on("MessageReceived", (msg: MessageDto) => {
        setMessages((prev) => (prev.some((m) => m.id === msg.id) ? prev : [...prev, msg]));
        void markThreadRead(thread.id);
      });
      await hub.start();
      await hub.invoke("JoinThread", thread.id);
      hubRef.current = hub;
    })();

    return () => {
      cancelled = true;
      void hubRef.current?.stop();
      hubRef.current = null;
    };
  }, [thread?.id, queryClient]);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  async function handleSend() {
    if (!thread || !draft.trim()) return;
    const body = draft.trim();
    setDraft("");
    const message = await sendMessage(thread.id, body);
    setMessages((prev) => (prev.some((m) => m.id === message.id) ? prev : [...prev, message]));
  }

  async function handleBlockToggle() {
    if (!thread || !detailQuery.data) return;
    if (detailQuery.data.isBlockedByMe) await unblockThreadParticipant(thread.id);
    else await blockThreadParticipant(thread.id);
    void queryClient.invalidateQueries({ queryKey: ["threadDetail", thread.id] });
  }

  async function handleReport() {
    if (!thread) return;
    await reportThreadParticipant(thread.id, "Other");
  }

  return (
    <AppShell>
      <Link to="/orders" className="mb-4 inline-flex items-center gap-1.5 text-sm text-ink-faint hover:text-ink">
        {isAr ? <ArrowRight className="h-4 w-4" /> : <ArrowLeft className="h-4 w-4" />}
        {isAr ? "الرجوع إلى طلباتي" : "Back to my orders"}
      </Link>

      {threadsQuery.isLoading && <p className="text-sm text-ink-faint">{isAr ? "جارٍ التحميل…" : "Loading…"}</p>}
      {!threadsQuery.isLoading && !thread && (
        <p className="text-sm text-rubric">
          {isAr ? "لا توجد محادثة لهذا الطلب." : "No chat exists for this request."}
        </p>
      )}

      {thread && (
        <div className="mx-auto flex max-w-2xl flex-col gap-3">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <span className={`h-2.5 w-2.5 rounded-full ${isOnline ? "bg-seal" : "bg-ink-faint/40"}`} />
              <h1 className="font-display text-xl font-bold">{thread.otherPartyName}</h1>
              <span className="font-mono text-xs text-ink-faint">
                <Ltr>{thread.requestNumber}</Ltr>
              </span>
            </div>
            <div className="flex items-center gap-2">
              <button
                onClick={handleReport}
                className="flex items-center gap-1 rounded-md border border-border px-2.5 py-1.5 text-xs text-ink-soft hover:border-rubric hover:text-rubric"
                title={isAr ? "إبلاغ" : "Report"}
              >
                <Flag className="h-3.5 w-3.5" />
                {isAr ? "إبلاغ" : "Report"}
              </button>
              <button
                onClick={handleBlockToggle}
                className="flex items-center gap-1 rounded-md border border-border px-2.5 py-1.5 text-xs text-ink-soft hover:border-rubric hover:text-rubric"
              >
                <Ban className="h-3.5 w-3.5" />
                {detailQuery.data?.isBlockedByMe ? (isAr ? "إلغاء الحظر" : "Unblock") : isAr ? "حظر" : "Block"}
              </button>
            </div>
          </div>

          <Card className="flex h-[28rem] flex-col gap-3 overflow-y-auto p-4">
            {messages.map((m) => {
              const mine = m.senderUserId === myUserId;
              return (
                <div key={m.id} className={`flex flex-col ${mine ? "items-end" : "items-start"}`}>
                  <div
                    className={
                      "max-w-[75%] rounded-lg px-3 py-2 text-sm " +
                      (mine ? "bg-seal text-seal-on" : "bg-paper text-ink")
                    }
                  >
                    {m.body}
                  </div>
                  <span className="mt-1 font-mono text-[10px] text-ink-faint">
                    <Ltr>{formatDateTime(m.sentAtUtc)}</Ltr>
                  </span>
                </div>
              );
            })}
            <div ref={bottomRef} />
          </Card>

          {detailQuery.data?.hasBlockedMe ? (
            <p className="text-center text-sm text-ink-faint">
              {isAr ? "لا يمكنك مراسلة هذا المستخدم." : "You can't message this participant."}
            </p>
          ) : (
            <div className="flex items-center gap-2">
              <input
                value={draft}
                onChange={(e) => setDraft(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === "Enter") void handleSend();
                }}
                placeholder={isAr ? "اكتب رسالة…" : "Write a message…"}
                className="flex-1 rounded-md border border-border bg-surface-raised px-4 py-2.5 text-sm focus:border-seal focus:outline-none focus:ring-2 focus:ring-seal/30"
              />
              <Button onClick={() => void handleSend()} disabled={!draft.trim()}>
                <Send className="h-4 w-4" />
                {isAr ? "إرسال" : "Send"}
              </Button>
            </div>
          )}
        </div>
      )}
    </AppShell>
  );
}
