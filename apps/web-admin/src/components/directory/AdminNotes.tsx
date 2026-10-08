import { useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { StickyNote } from "lucide-react";
import { Button, Card, Ltr } from "@law-portal/ui";
import { formatDateTime, useTranslation } from "@law-portal/i18n";
import { addNote, getNotes } from "../../lib/directoryApi";

/** Internal notes on a client, lawyer or request; only admins ever see them. */
export function AdminNotes({ entityType, entityId }: { entityType: "Client" | "Lawyer" | "Request"; entityId: string }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const key = ["adminNotes", entityType, entityId];
  const notes = useQuery({ queryKey: key, queryFn: () => getNotes(entityType, entityId) });
  const [body, setBody] = useState("");
  const [tried, setTried] = useState(false);
  const save = useMutation({
    mutationFn: () => addNote(entityType, entityId, body),
    onSuccess: () => { setBody(""); setTried(false); void queryClient.invalidateQueries({ queryKey: key }); },
  });
  const error = tried && !body.trim() ? t("form.required") : null;

  function submit(e: FormEvent) {
    e.preventDefault();
    setTried(true);
    if (body.trim()) save.mutate();
  }

  return (
    <Card>
      <h2 className="mb-1 flex items-center gap-2 font-display text-base font-bold"><StickyNote className="h-4 w-4 text-seal" />{t("directory.notes")}</h2>
      <p className="mb-3 text-xs text-ink-faint">{t("directory.notesHint")}</p>
      <form className="flex flex-col gap-2" onSubmit={submit} noValidate>
        <textarea id={`note-${entityId}`} rows={2} maxLength={2000} value={body} onChange={(e) => setBody(e.target.value)} aria-invalid={!!error}
          className={(error ? "border-rubric " : "border-border ") + "rounded-md border bg-surface-raised px-3 py-2 text-sm text-ink focus:border-seal focus:outline-none"} />
        {error && <span className="text-xs text-rubric">{error}</span>}
        <Button type="submit" className="self-start" disabled={save.isPending}>{t("directory.addNote")}</Button>
      </form>
      <ul className="mt-3 flex flex-col">
        {notes.data?.map((n) => (
          <li key={n.id} className="border-t border-rule py-2 text-sm">
            <p className="whitespace-pre-wrap">{n.body}</p>
            <p className="text-xs text-ink-faint">{n.authorName ?? "—"} · <Ltr className="font-mono">{formatDateTime(n.createdAtUtc)}</Ltr></p>
          </li>
        ))}
      </ul>
    </Card>
  );
}
