import { useRef } from "react";
import { FileUp } from "lucide-react";
import { Ltr } from "@law-portal/ui";
import { useTranslation } from "@law-portal/i18n";
import { ACCEPTED_TYPES } from "../lib/licenseFile";


/** The licence document drop zone — registration step 3 and the "returned for changes" form. */
export function LicenseUpload({ file, onChange, error }: { file: File | null; onChange: (file: File | null) => void; error: string | null }) {
  const { t } = useTranslation();
  const inputRef = useRef<HTMLInputElement>(null);
  return (
    <div className="flex flex-col gap-1.5">
      <button
        type="button"
        onClick={() => inputRef.current?.click()}
        onDragOver={(e) => e.preventDefault()}
        onDrop={(e) => {
          e.preventDefault();
          onChange(e.dataTransfer.files[0] ?? null);
        }}
        className="flex flex-col items-center gap-3 rounded-lg border border-dashed border-border bg-surface px-4 py-6 text-center hover:border-seal"
      >
        <span className="flex h-10 w-10 items-center justify-center rounded-full bg-surface-raised">
          <FileUp className="h-5 w-5 text-ink-soft" />
        </span>
        <span className="text-sm font-semibold text-ink">{t("lawyerAuth.register.upload.required")}</span>
        {file ? (
          <>
            <span className="text-sm text-ink-soft">{t("lawyerAuth.register.upload.uploaded")}</span>
            {/* Two flex items, so the number sits at the start of the line in either direction. */}
            <span className="flex items-center gap-1.5 text-sm text-ink">
              <Ltr className="font-mono">1-</Ltr>
              <bdi>{file.name}</bdi>
            </span>
            <span className="text-sm font-semibold text-ink underline">{t("lawyerAuth.register.upload.change")}</span>
          </>
        ) : (
          <span className="text-sm font-semibold text-ink underline">{t("lawyerAuth.register.upload.prompt")}</span>
        )}
        <Ltr className="font-mono text-xs text-ink-faint">{t("lawyerAuth.register.upload.formats")}</Ltr>
      </button>
      <input
        ref={inputRef}
        type="file"
        accept={ACCEPTED_TYPES.join(",")}
        className="hidden"
        onChange={(e) => onChange(e.target.files?.[0] ?? null)}
      />
      {error && <p className="text-xs text-rubric">{error}</p>}
    </div>
  );
}

